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
using System.Text.RegularExpressions;
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
    string? Sha256Url);

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

    private static readonly string AssetName = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
        ? "Arietta-portable-win-arm64.zip"
        : "Arietta-portable-win-x64.zip";

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
            SelfUpdateInfo? info = await CheckForUpdateAsync();
            if (info is null)
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

    public static async Task<SelfUpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var resp = await SharedHttp.SelfUpdate.GetAsync(LatestReleaseApi, ct);
        string body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"GitHub API {(int)resp.StatusCode}: {body}");
        }
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!TryParseVersion(tag, out Version version)
            || version <= ParseVersion(AppConfig.AppVersion)
            || (Version.TryParse(AppConfig.GetConfig(IgnoreVersionConfigKey), out var ignored) && version <= ignored))
        {
            return null;
        }

        JsonElement asset = default, sha = default;
        bool hasAsset = false, hasSha = false;
        foreach (var a in root.GetProperty("assets").EnumerateArray())
        {
            string name = a.GetProperty("name").GetString() ?? "";
            if (string.Equals(name, AssetName, StringComparison.OrdinalIgnoreCase))
            {
                asset = a;
                hasAsset = true;
            }
            else if (string.Equals(name, AssetName + ".sha256", StringComparison.OrdinalIgnoreCase))
            {
                sha = a;
                hasSha = true;
            }
        }
        if (!hasAsset)
        {
            throw new InvalidOperationException($"Release {tag} does not contain asset {AssetName}");
        }
        return new SelfUpdateInfo(
            version.ToString(),
            root.GetProperty("html_url").GetString() ?? LatestReleaseApi,
            root.TryGetProperty("body", out var notes) ? notes.GetString() : null,
            asset.GetProperty("browser_download_url").GetString() ?? "",
            asset.GetProperty("size").GetInt64(),
            hasSha ? sha.GetProperty("browser_download_url").GetString() : null);
    }

    public static async Task<string> DownloadAndStageAsync(SelfUpdateInfo info, IProgress<double>? progress, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_workDir);
        string zipPath = Path.Combine(_workDir, AssetName);
        long received = 0;
        byte[] hash;

        using (var resp = await SharedHttp.SelfUpdate.GetAsync(info.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            resp.EnsureSuccessStatusCode();
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
                throw new IOException($"下载停滞超时（60s 无数据），已收 {received} 字节");
            }
            hash = sha256.GetHashAndReset();
        }

        if (info.Sha256Url is not null)
        {
            string expected = await SharedHttp.SelfUpdate.GetStringAsync(info.Sha256Url, ct);
            string actual = Convert.ToHexStringLower(hash);
            var m = Regex.Match(expected, "[0-9a-fA-F]{64}");
            if (!m.Success || !string.Equals(m.Value, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"SHA256 mismatch (expected {m.Value}, got {actual})");
            }
        }
        else if (info.AssetSize > 0 && received != info.AssetSize)
        {
            throw new InvalidDataException($"Size mismatch (expected {info.AssetSize}, got {received})");
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
                : throw new InvalidDataException("Extracted package does not contain app\\Arietta.dll");
        }
        return newApp;
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
