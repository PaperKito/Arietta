using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arietta.Helpers;
using Microsoft.Extensions.Logging;

namespace Arietta.Features.Setting;

public sealed record SelfUpdateInfo(
    string Version,
    string ReleaseUrl,
    string? Notes,
    string AssetUrl,
    long AssetSize,
    string? Sha256);

public enum SelfUpdateCheckStatus
{

    UpToDate,

    UpdateAvailable,

    Ignored,
}

public sealed record SelfUpdateCheckResult(
    SelfUpdateCheckStatus Status,
    SelfUpdateInfo? Info = null,
    string? IgnoredVersion = null);

public enum SelfUpdateErrorKind
{

    Network,

    Timeout,

    RateLimited,

    NotFound,

    BadResponse,

    HttpError,

    Checksum,

    PackageInvalid,

    Disk,
}

public sealed class SelfUpdateException(SelfUpdateErrorKind kind, string message, string? detail = null, Exception? inner = null)
    : Exception(message, inner)
{
    public SelfUpdateErrorKind Kind { get; } = kind;

    public string? Detail { get; } = detail;
}

public static class SelfUpdateService
{
    private static readonly ILogger _logger = AppConfig.GetLogger<LogTag>();

    private sealed class LogTag;

    private const string LatestReleaseApi = "https://api.github.com/repos/PaperKito/Arietta/releases/latest";

    private const string RecentReleasesApi = "https://api.github.com/repos/PaperKito/Arietta/releases?per_page=10";

    public sealed record ReleaseEntry(string Tag, string Date, string? Body);

    private static IReadOnlyList<ReleaseEntry>? _recentReleasesCache;

    public static async Task<IReadOnlyList<ReleaseEntry>> FetchRecentReleasesAsync(CancellationToken ct = default)
    {
        if (_recentReleasesCache is { } cache)
        {
            return cache;
        }

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(TimeSpan.FromSeconds(15));
        using var resp = await SharedHttp.SelfUpdate.GetAsync(RecentReleasesApi, limit.Token);
        string body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub API {(int)resp.StatusCode}: {body}");
        }
        using var doc = JsonDocument.Parse(body);
        var list = new List<ReleaseEntry>();
        foreach (var rel in doc.RootElement.EnumerateArray())
        {
            try
            {
                if (rel.GetProperty("draft").GetBoolean())
                {
                    continue;
                }
                string tag = rel.GetProperty("tag_name").GetString() ?? "";
                if (!TryParseVersion(tag, out _))
                {
                    continue;
                }
                string date = rel.TryGetProperty("published_at", out var published)
                    && DateTimeOffset.TryParse(published.GetString(), out var at)
                        ? at.ToString("yyyy-MM-dd")
                        : "";
                list.Add(new ReleaseEntry(tag, date, rel.TryGetProperty("body", out var notes) ? notes.GetString() : null));
            }
            catch
            {

            }
        }
        if (list.Count == 0)
        {
            throw new InvalidOperationException("No releases found");
        }
        _recentReleasesCache = list;
        return list;
    }

    public static string AssetName { get; } = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
        ? "Arietta-portable-win-arm64.zip"
        : "Arietta-portable-win-x64.zip";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    public const string IgnoreVersionConfigKey = "SelfUpdateIgnoreVersion";

    private static readonly TimeSpan AutoCheckDelay = TimeSpan.FromSeconds(6);

    public static bool IsPortableLayout { get; }

    private static readonly string _appDir;

    private static readonly string _rootDir;

    private static readonly string _workDir;

    private static readonly string _launchExe;

    static SelfUpdateService()
    {
        _appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _rootDir = Path.GetDirectoryName(_appDir) ?? _appDir;
        IsPortableLayout = string.Equals(Path.GetFileName(_appDir), "app", StringComparison.OrdinalIgnoreCase);
        _workDir = Path.Combine(_rootDir, ".arietta-update");
        bool stubLaunched = Environment.GetEnvironmentVariable("ARIETTA_ROOT") is not null;
        _launchExe = stubLaunched
            ? Path.Combine(_rootDir, "Arietta.exe")
            : Path.Combine(_appDir, "Arietta.exe");
    }

    public static Task CleanupLeftoversAsync() => Task.Run(() =>
    {
        try
        {
            string app = Path.Combine(_rootDir, "app");
            string old = Path.Combine(_rootDir, "app.old");
            if (Directory.Exists(old))
            {
                if (!Directory.Exists(app))
                {
                    Directory.Move(old, app);
                    _logger.LogInformation("SelfUpdate: app directory missing, restored from app.old");
                }
                else
                {
                    Directory.Delete(old, recursive: true);
                }
            }
            if (Directory.Exists(_workDir))
            {
                Directory.Delete(_workDir, recursive: true);
            }
        }
        catch (Exception ex)
        {

            _logger.LogWarning(ex, "SelfUpdate: failed to clean up leftovers");
        }
    });

    public static async Task AutoCheckAsync()
    {
        try
        {
            await Task.Delay(AutoCheckDelay);
            SelfUpdateCheckResult result = await CheckForUpdateAsync();
            if (result is not { Status: SelfUpdateCheckStatus.UpdateAvailable, Info: { } info })
            {
                return;
            }
            _logger.LogInformation("SelfUpdate: new version {Version} found at startup", info.Version);
            (App.Current as App)?.ShowUpdateDialog(info);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SelfUpdate: auto check failed (ignored)");
        }
    }

    public static void IgnoreVersion(string version)
    {
        AppConfig.SetConfig(IgnoreVersionConfigKey, version);
        _logger.LogInformation("SelfUpdate: version {Version} ignored", version);
    }

    public static async Task<SelfUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(CheckTimeout);

        string body;
        int statusCode;
        try
        {
            using var resp = await SharedHttp.SelfUpdate.GetAsync(LatestReleaseApi, limit.Token);
            statusCode = (int)resp.StatusCode;
            body = await resp.Content.ReadAsStringAsync(limit.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                $"GitHub API check timed out after {CheckTimeout.TotalSeconds:F0}s");
        }
        catch (HttpRequestException ex)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.Network, ex.Message, inner: ex);
        }

        if (statusCode is < 200 or >= 300)
        {
            throw MapHttpFailure(statusCode, body);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            JsonElement root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!TryParseVersion(tag, out Version version))
            {
                throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse, $"unexpected tag '{tag}'", tag);
            }
            if (version <= ParseVersion(AppConfig.AppVersion))
            {
                return new SelfUpdateCheckResult(SelfUpdateCheckStatus.UpToDate);
            }
            if (Version.TryParse(AppConfig.GetConfig(IgnoreVersionConfigKey), out var ignored) && version <= ignored)
            {
                return new SelfUpdateCheckResult(SelfUpdateCheckStatus.Ignored, IgnoredVersion: version.ToString());
            }

            JsonElement asset = default;
            bool hasAsset = false;
            foreach (JsonElement a in root.GetProperty("assets").EnumerateArray())
            {
                if (string.Equals(a.GetProperty("name").GetString() ?? "", AssetName, StringComparison.OrdinalIgnoreCase))
                {
                    asset = a;
                    hasAsset = true;
                }
            }
            if (!hasAsset)
            {

                throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse,
                    $"Release {tag} does not contain asset {AssetName}");
            }

            string? sha256 = null;
            if (asset.TryGetProperty("digest", out var digest) && digest.GetString() is { } dg)
            {
                sha256 = dg.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? dg["sha256:".Length..] : dg;
            }
            return new SelfUpdateCheckResult(SelfUpdateCheckStatus.UpdateAvailable, new SelfUpdateInfo(
                version.ToString(),
                root.GetProperty("html_url").GetString() ?? LatestReleaseApi,
                root.TryGetProperty("body", out var notes) ? notes.GetString() : null,
                asset.GetProperty("browser_download_url").GetString() ?? "",
                asset.GetProperty("size").GetInt64(),
                sha256));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse, ex.Message, inner: ex);
        }
    }

    private static SelfUpdateException MapHttpFailure(int statusCode, string body)
    {
        string code = statusCode.ToString();
        if (statusCode == 404)
        {
            return new SelfUpdateException(SelfUpdateErrorKind.NotFound, "GitHub API 404", code);
        }
        if ((statusCode is 403 or 429) && body.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return new SelfUpdateException(SelfUpdateErrorKind.RateLimited, "GitHub API rate limited", code);
        }
        return new SelfUpdateException(SelfUpdateErrorKind.HttpError, $"GitHub API {code}", code);
    }

    public static async Task<string> DownloadAndStageAsync(SelfUpdateInfo info, IProgress<double>? progress, CancellationToken ct = default)
    {
        try
        {
            return await DownloadAndStageCoreAsync(info, progress, ct);
        }
        catch (SelfUpdateException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout, "download stalled");
        }
        catch (HttpRequestException ex)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Network, ex.Message, inner: ex);
        }
        catch (InvalidDataException ex)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.PackageInvalid, ex.Message, inner: ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.Disk, ex.Message, inner: ex);
        }
    }


    private static async Task<string> DownloadAndStageCoreAsync(SelfUpdateInfo info, IProgress<double>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(_workDir);
        string zipPath = Path.Combine(_workDir, AssetName);
        long received = 0;
        byte[] hash;

        using (var resp = await SharedHttp.SelfUpdate.GetAsync(info.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            if (!resp.IsSuccessStatusCode)
            {

                throw new SelfUpdateException(SelfUpdateErrorKind.HttpError,
                    $"asset HTTP {(int)resp.StatusCode}", ((int)resp.StatusCode).ToString());
            }
            await using var src = await resp.Content.ReadAsStreamAsync(ct);
            await using var dst = new FileStream(zipPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 1 << 16, FileOptions.Asynchronous);
            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[1 << 16];
            using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            stallCts.CancelAfter(TimeSpan.FromSeconds(60));
            int read;
            try
            {
                while ((read = await src.ReadAsync(buffer, stallCts.Token)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                    sha256.AppendData(buffer.AsSpan(0, read));
                    received += read;
                    if (info.AssetSize > 0)
                    {
                        progress?.Report(Math.Min(100.0, received * 100.0 / info.AssetSize));
                    }
                    stallCts.CancelAfter(TimeSpan.FromSeconds(60));
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                    $"download stalled at {received} bytes");
            }
            hash = sha256.GetHashAndReset();
        }

        if (info.Sha256 is not null)
        {
            string actual = Convert.ToHexStringLower(hash);
            if (!string.Equals(info.Sha256, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new SelfUpdateException(SelfUpdateErrorKind.Checksum,
                    $"SHA256 mismatch (expected {info.Sha256}, got {actual})");
            }
        }
        else if (info.AssetSize > 0 && received != info.AssetSize)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Checksum,
                $"Size mismatch (expected {info.AssetSize}, got {received})");
        }

        string extract = Path.Combine(_workDir, "extract");
        if (Directory.Exists(extract))
        {
            Directory.Delete(extract, recursive: true);
        }
        ZipFile.ExtractToDirectory(zipPath, extract);

        string newApp = Path.Combine(extract, "app");
        if (!File.Exists(Path.Combine(newApp, "Arietta.dll")))
        {
            newApp = Directory.EnumerateDirectories(extract)
                .FirstOrDefault(d => File.Exists(Path.Combine(d, "app", "Arietta.dll"))) is { } outer
                ? Path.Combine(outer, "app")
                : throw new SelfUpdateException(SelfUpdateErrorKind.PackageInvalid,
                    "Extracted package does not contain app\\Arietta.dll");
        }
        return newApp;
    }

    public static (string Title, string Message) DescribeFailure(Exception ex)
    {
        var lang = Localization.Lang.Instance;
        return ex switch
        {
            SelfUpdateException { Kind: SelfUpdateErrorKind.Network } => (lang.SettingPage_UpdateErrNetwork, lang.SettingPage_UpdateErrNetworkDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.Timeout } => (lang.SettingPage_UpdateErrTimeout, lang.SettingPage_UpdateErrTimeoutDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.RateLimited } => (lang.SettingPage_UpdateErrRateLimited, lang.SettingPage_UpdateErrRateLimitedDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.NotFound } => (lang.SettingPage_UpdateErrNotFound, lang.SettingPage_UpdateErrNotFoundDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.BadResponse } => (lang.SettingPage_UpdateErrBadResponse, lang.SettingPage_UpdateErrBadResponseDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.HttpError, Detail: var code } =>
                (lang.SettingPage_UpdateErrHttp, string.Format(lang.SettingPage_UpdateErrHttpDesc, code ?? "?")),
            SelfUpdateException { Kind: SelfUpdateErrorKind.Checksum } => (lang.SettingPage_UpdateErrChecksum, lang.SettingPage_UpdateErrChecksumDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.PackageInvalid } => (lang.SettingPage_UpdateErrPackage, lang.SettingPage_UpdateErrPackageDesc),
            SelfUpdateException { Kind: SelfUpdateErrorKind.Disk } => (lang.SettingPage_UpdateErrDisk, lang.SettingPage_UpdateErrDiskDesc),

            HttpRequestException => (lang.SettingPage_UpdateErrNetwork, lang.SettingPage_UpdateErrNetworkDesc),
            TaskCanceledException => (lang.SettingPage_UpdateErrTimeout, lang.SettingPage_UpdateErrTimeoutDesc),
            _ => (lang.SettingPage_UpdateFailed, ex.Message),
        };
    }

    public static void InstallNow(string newAppDir)
    {
        if (!IsPortableLayout)
        {
            throw new InvalidOperationException("Self-update requires the portable layout (app directory)");
        }
        Directory.CreateDirectory(_workDir);
        string script = Path.Combine(_workDir, "updater.ps1");
        File.WriteAllText(script, UpdaterScript);
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"{script}\" " +
                $"-Root \"{_rootDir}\" -NewApp \"{newAppDir}\" -ProcId {Environment.ProcessId} -Launch \"{_launchExe}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        Process.Start(psi);
        _logger.LogInformation("SelfUpdate: updater spawned, exiting for swap ({New} -> app)", newAppDir);

        (App.Current as App)?.ExitApp();
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        if (Version.TryParse(tag.TrimStart('v', 'V'), out var parsed))
        {
            version = parsed;
            return true;
        }
        version = new Version(0, 0, 0);
        return false;
    }

    private static Version ParseVersion(string version)
    {
        return Version.TryParse(version, out var v) ? v : new Version(0, 0, 0);
    }

    private const string UpdaterScript = """
param(
    [string]$Root,
    [string]$NewApp,
    [int]$ProcId,
    [string]$Launch
)
$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'app'
$old = Join-Path $Root 'app.old'
try {
    # 等安装发起进程退出（最长 90 秒），再多等 2 秒让文件句柄释放
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        $p = Get-Process -Id $ProcId -ErrorAction SilentlyContinue
        if ($null -eq $p -or $p.HasExited) { break }
        Start-Sleep -Milliseconds 500
    }
    Start-Sleep -Seconds 2

    if (Test-Path $old) { Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue }
    Rename-Item $app 'app.old'
    Move-Item $NewApp $app

    # 无 stub 直接运行 app\Arietta.exe 的用户，data 落在 app 里：搬进新 app，避免升级后配置"丢失"
    $oldData = Join-Path $old 'data'
    if ((Test-Path $oldData) -and -not (Test-Path (Join-Path $app 'data'))) {
        Move-Item $oldData (Join-Path $app 'data')
    }

    Start-Process -FilePath $Launch -WorkingDirectory (Split-Path -Parent $Launch)
    Start-Sleep -Seconds 3

    Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $Root '.arietta-update') -Recurse -Force -ErrorAction SilentlyContinue
    exit 0
} catch {
    # 回滚：新 app 就位前失败 → old 换回 app；就位后失败 → 删新 app、old 换回
    try {
        if ((Test-Path $old) -and (-not (Test-Path $app))) {
            Rename-Item $old 'app'
        } elseif ((Test-Path $old) -and (Test-Path $app)) {
            Remove-Item $app -Recurse -Force
            Rename-Item $old 'app'
        }
    } catch { }
    exit 1
}
""";
}
