using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Arietta.Helpers;
using Microsoft.Extensions.Logging;

namespace Arietta.Features.Setting;

public sealed record SelfUpdateInfo(
    string Version,
    string ReleaseUrl,
    string? Notes,
    string AssetUrl,
    long AssetSize,
    string? Sha256,
    string? ReleaseTime = null);

public readonly record struct SelfUpdateProgress(double Percent, long Received);

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

    private const string MarkdownApiUrl = "https://api.github.com/markdown";

    private const string RecentReleasesApi = "https://api.github.com/repos/PaperKito/Arietta/releases?per_page=10";

    private const string LatestReleaseWebUrl = "https://github.com/PaperKito/Arietta/releases/latest";

    private const string ReleasesAtomUrl = "https://github.com/PaperKito/Arietta/releases.atom";

    private const string ReleaseDownloadBase = "https://github.com/PaperKito/Arietta/releases/download/";

    public sealed record ReleaseEntry(string Tag, string Date, string? Body);

    private static IReadOnlyList<ReleaseEntry>? _recentReleasesCache;

    private static IReadOnlyList<ReleaseEntry>? _atomReleasesCache;

    public static async Task<IReadOnlyList<ReleaseEntry>> FetchRecentReleasesAsync(CancellationToken ct = default)
    {
        if (_recentReleasesCache is { } cache)
        {
            return cache;
        }
        try
        {
            IReadOnlyList<ReleaseEntry> releases = await FetchRecentReleasesViaApiAsync(ct);
            _recentReleasesCache = releases;
            return releases;
        }
        catch (SelfUpdateException ex) when (ex.Kind is SelfUpdateErrorKind.RateLimited or SelfUpdateErrorKind.HttpError)
        {

            _logger.LogWarning(ex, "SelfUpdate: API history failed ({Kind}), falling back to releases.atom", ex.Kind);
            IReadOnlyList<ReleaseEntry> releases = await FetchAtomReleasesAsync(ct);
            _recentReleasesCache = releases;
            return releases;
        }
    }

    private static async Task<IReadOnlyList<ReleaseEntry>> FetchRecentReleasesViaApiAsync(CancellationToken ct)
    {

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(TimeSpan.FromSeconds(15));
        using var resp = await SharedHttp.SelfUpdate.GetAsync(RecentReleasesApi, limit.Token);
        string body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw MapHttpFailure((int)resp.StatusCode, body);
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
            throw new SelfUpdateException(SelfUpdateErrorKind.NotFound, "No releases found");
        }
        return list;
    }

    private static async Task<IReadOnlyList<ReleaseEntry>> FetchAtomReleasesAsync(CancellationToken ct)
    {
        if (_atomReleasesCache is { } cached)
        {
            return cached;
        }
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(CheckTimeout);
        string xml;
        try
        {
            xml = await SharedHttp.SelfUpdateWeb.GetStringAsync(ReleasesAtomUrl, limit.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                $"releases.atom timed out after {CheckTimeout.TotalSeconds:F0}s");
        }
        catch (HttpRequestException ex)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Network, ex.Message, inner: ex);
        }
        List<ReleaseEntry> list = ParseAtomReleases(xml);
        if (list.Count == 0)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.NotFound, "releases.atom has no entries");
        }
        _atomReleasesCache = list;
        return list;
    }

    public static async Task<IReadOnlyList<ReleaseEntry>> FetchNotesRangeAsync(
        string? fromVersion, string toVersion, CancellationToken ct)
    {
        _ = Version.TryParse(toVersion.TrimStart('v', 'V'), out Version? upper);
        _ = Version.TryParse((fromVersion ?? string.Empty).TrimStart('v', 'V'), out Version? lower);

        var collected = new List<ReleaseEntry>();
        for (int page = 1; page <= 3; page++)
        {
            IReadOnlyList<ReleaseEntry> entries;
            if (page == 1)
            {
                entries = await FetchAtomReleasesAsync(ct);
            }
            else
            {
                entries = await TryFetchAtomPageAsync(page, ct);
                if (entries.Count == 0)
                {
                    break;
                }
            }
            collected.AddRange(entries);

            Version? oldest = entries.Count == 0 ? null : entries.Min(e => ParseVersion(e.Tag));
            if (entries.Count < 10 || (lower is not null && oldest is not null && oldest <= lower))
            {
                break;
            }
        }

        return collected
            .Where(e =>
            {
                Version v = ParseVersion(e.Tag);
                return v != new Version(0, 0, 0)
                       && (upper is null || v <= upper)
                       && (lower is null || v > lower);
            })
            .OrderBy(e => ParseVersion(e.Tag))
            .ToList();
    }

    private static async Task<IReadOnlyList<ReleaseEntry>> TryFetchAtomPageAsync(int page, CancellationToken ct)
    {
        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limit.CancelAfter(CheckTimeout);
            string xml = await SharedHttp.SelfUpdateWeb.GetStringAsync($"{ReleasesAtomUrl}?page={page}", limit.Token);
            return ParseAtomReleases(xml);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SelfUpdate: releases.atom page {Page} unavailable", page);
            return [];
        }
    }

    private static List<ReleaseEntry> ParseAtomReleases(string xml)
    {
        var list = new List<ReleaseEntry>();

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        XDocument doc = XDocument.Load(reader);
        XNamespace atom = "http://www.w3.org/2005/Atom";
        foreach (XElement entry in doc.Root?.Elements(atom + "entry") ?? [])
        {
            try
            {
                string href = entry.Element(atom + "link")?.Attribute("href")?.Value ?? "";
                string tag = ParseTagFromReleaseUrl(href) ?? "";
                if (!TryParseVersion(tag, out _))
                {
                    continue;
                }
                string date = DateTimeOffset.TryParse(entry.Element(atom + "updated")?.Value, out var at)
                    ? at.ToString("yyyy-MM-dd")
                    : "";
                string? body = entry.Element(atom + "content")?.Value is { Length: > 0 } html
                    ? WebUtility.HtmlDecode(html)
                    : null;
                list.Add(new ReleaseEntry(tag, date, body));
            }
            catch
            {

            }
        }
        return list;
    }

    private static string? ParseTagFromReleaseUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }
        int at = url.IndexOf("/tag/", StringComparison.Ordinal);
        if (at < 0)
        {
            return null;
        }
        string tag = url[(at + "/tag/".Length)..].TrimEnd('/');
        return tag.Length == 0 ? null : Uri.UnescapeDataString(tag);
    }

    public static string GetAssetName(string tag)
    {
        string version = TryParseVersion(tag, out Version v)
            ? $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}"
            : tag.TrimStart('v', 'V');
        string arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        return $"Arietta-Portable-{version}-{arch}.zip";
    }

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan CheckHardCap = TimeSpan.FromSeconds(40);

    public const string IgnoreVersionConfigKey = "SelfUpdateIgnoreVersion";

    private static readonly TimeSpan HedgeDelay = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan AutoCheckDelay = TimeSpan.FromSeconds(1);

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
        App? app = App.Current as App;

        SelfUpdateCheckResult result;
        try
        {
            result = await CheckForUpdateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: startup check failed");
            return;
        }

        if (result is not { Status: SelfUpdateCheckStatus.UpdateAvailable, Info: { } info })
        {
            return;
        }
        _logger.LogInformation("SelfUpdate: new version {Version} found at startup", info.Version);
        app?.ApplyUpdateInfo(info);
    }

    public static void IgnoreVersion(string version)
    {
        AppConfig.SetConfig(IgnoreVersionConfigKey, version);
        _logger.LogInformation("SelfUpdate: version {Version} ignored", version);
    }

    public static SelfUpdateInfo CurrentVersionInfo() => new(
        AppConfig.AppVersion,
        $"https://github.com/PaperKito/Arietta/releases/tag/v{AppConfig.AppVersion}",
        null, string.Empty, 0, null);

    public static async Task<SelfUpdateInfo> TryEnrichAsync(SelfUpdateInfo info, CancellationToken ct)
    {
        try
        {
            SelfUpdateCheckResult result = await CheckViaApiAsync(ct, includeIgnored: true);
            if (result is { Status: SelfUpdateCheckStatus.UpdateAvailable, Info: { } api }
                && string.Equals(api.Version, info.Version, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("SelfUpdate: enriched from API (digest {HasDigest}, notes {Notes} chars)",
                    api.Sha256 is not null, api.Notes?.Length ?? 0);
                return info with
                {
                    Sha256 = api.Sha256 ?? info.Sha256,
                    Notes = api.Notes ?? info.Notes,
                    AssetSize = info.AssetSize > 0 ? info.AssetSize : api.AssetSize,
                    ReleaseTime = api.ReleaseTime ?? info.ReleaseTime,
                };
            }
        }
        catch (Exception ex)
        {

            _logger.LogInformation(ex, "SelfUpdate: API enrichment unavailable");
        }

        try
        {
            string tag = info.Version.StartsWith('v') ? info.Version : "v" + info.Version;
            string? fromAtom = await TryFetchReleaseTimeAsync(tag, ct);
            if (fromAtom is not null)
            {
                return info with { ReleaseTime = fromAtom };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SelfUpdate: release time unavailable via atom");
        }
        return info;
    }

    public static Task<SelfUpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default, bool includeIgnored = false)
    {
        if (includeIgnored)
        {

            return CheckWithHardCapAsync(ct, includeIgnored: true);
        }
        lock (_inflightLock)
        {
            if (_inflightCheck is { IsCompleted: false })
            {

                _logger.LogInformation("SelfUpdate: check already running, sharing in-flight task");
                return _inflightCheck;
            }
            _inflightCheck = CheckWithHardCapAsync(ct, includeIgnored: false);
            return _inflightCheck;
        }
    }


    private static Task<SelfUpdateCheckResult>? _inflightCheck;

    private static readonly object _inflightLock = new();


    private static async Task<SelfUpdateCheckResult> CheckWithHardCapAsync(CancellationToken ct, bool includeIgnored)
    {

        Task<SelfUpdateCheckResult> check = RunCheckAsync(ct, includeIgnored);
        if (await Task.WhenAny(check, Task.Delay(CheckHardCap, ct)) != check)
        {
            _ = check.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                $"self update check exceeded {CheckHardCap.TotalSeconds:F0}s hard cap");
        }
        return await check;
    }

    private static async Task<SelfUpdateCheckResult> RunCheckAsync(CancellationToken ct, bool includeIgnored)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task<SelfUpdateCheckResult> web = CheckViaWebAsync(cts.Token, includeIgnored);

        Task first = await Task.WhenAny(web, Task.Delay(HedgeDelay, cts.Token));
        ct.ThrowIfCancellationRequested();
        if (first == web)
        {
            try
            {
                return await web;
            }
            catch (SelfUpdateException ex) when (IsFallbackKind(ex.Kind))
            {
                _logger.LogWarning(ex, "SelfUpdate: web route failed ({Kind}), falling back to the API", ex.Kind);
                return await CheckViaApiAsync(cts.Token, includeIgnored);
            }
        }

        _logger.LogInformation("SelfUpdate: web route slow, racing the API");
        Task<SelfUpdateCheckResult> api = CheckViaApiAsync(cts.Token, includeIgnored);
        var pending = new List<Task<SelfUpdateCheckResult>> { web, api };
        Exception? last = null;
        while (pending.Count > 0)
        {
            Task<SelfUpdateCheckResult> done = await Task.WhenAny(pending);
            pending.Remove(done);
            try
            {
                SelfUpdateCheckResult result = await done;
                cts.Cancel();
                foreach (Task<SelfUpdateCheckResult> other in pending)
                {
                    _ = other.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                }
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }
        throw last as SelfUpdateException
            ?? new SelfUpdateException(SelfUpdateErrorKind.Network,
                "both update channels failed", last?.Message, last);
    }


    private static bool IsFallbackKind(SelfUpdateErrorKind kind) => kind is SelfUpdateErrorKind.RateLimited
        or SelfUpdateErrorKind.HttpError or SelfUpdateErrorKind.Network
        or SelfUpdateErrorKind.Timeout or SelfUpdateErrorKind.BadResponse;

    private static async Task<SelfUpdateCheckResult> CheckViaApiAsync(CancellationToken ct, bool includeIgnored)
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

            string? releaseTime = root.TryGetProperty("published_at", out var publishedAt)
                && DateTimeOffset.TryParse(publishedAt.GetString(), out var publishedOn)
                    ? publishedOn.ToLocalTime().ToString("yyyy/MM/dd HH:mm")
                    : null;
            if (EvaluateTag(tag, out Version version, includeIgnored) is { } decided)
            {
                return decided;
            }

            string assetName = GetAssetName(tag);
            JsonElement asset = default;
            bool hasAsset = false;
            foreach (JsonElement a in root.GetProperty("assets").EnumerateArray())
            {
                if (string.Equals(a.GetProperty("name").GetString() ?? "", assetName, StringComparison.OrdinalIgnoreCase))
                {
                    asset = a;
                    hasAsset = true;
                }
            }
            if (!hasAsset)
            {

                throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse,
                    $"Release {tag} does not contain asset {assetName}");
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
                sha256,
                releaseTime));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {

            throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse, ex.Message, inner: ex);
        }
    }

    private static async Task<SelfUpdateCheckResult> CheckViaWebAsync(CancellationToken ct, bool includeIgnored)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(CheckTimeout);

        string tag;
        try
        {
            using var resp = await SharedHttp.SelfUpdateWeb.GetAsync(
                LatestReleaseWebUrl, HttpCompletionOption.ResponseHeadersRead, limit.Token);
            int code = (int)resp.StatusCode;
            if (code == 404)
            {

                throw new SelfUpdateException(SelfUpdateErrorKind.NotFound, "GitHub web 404", "404");
            }
            if (code is < 300 or >= 400)
            {
                throw new SelfUpdateException(SelfUpdateErrorKind.HttpError, $"GitHub web {code}", code.ToString());
            }
            tag = ParseTagFromReleaseUrl(resp.Headers.Location?.ToString())
                ?? throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse,
                    "releases/latest did not redirect to a tag");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                $"GitHub web check timed out after {CheckTimeout.TotalSeconds:F0}s");
        }
        catch (HttpRequestException ex)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Network, ex.Message, inner: ex);
        }

        if (EvaluateTag(tag, out Version version, includeIgnored) is { } decided)
        {
            return decided;
        }

        string assetUrl = $"{ReleaseDownloadBase}{tag}/{GetAssetName(tag)}";

        long size = await TryGetAssetSizeAsync(assetUrl, ct);
        string? notes = null;
        _logger.LogInformation("SelfUpdate: web route resolved {Tag} (asset {Size} bytes, notes deferred)",
            tag, size);
        return new SelfUpdateCheckResult(SelfUpdateCheckStatus.UpdateAvailable, new SelfUpdateInfo(
            version.ToString(),
            $"https://github.com/PaperKito/Arietta/releases/tag/{tag}",
            notes,
            assetUrl,
            size,

            null));
    }

    private static SelfUpdateCheckResult? EvaluateTag(string tag, out Version version, bool includeIgnored)
    {
        if (!TryParseVersion(tag, out version))
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.BadResponse, $"unexpected tag '{tag}'", tag);
        }
        if (version <= ParseVersion(AppConfig.AppVersion))
        {
            return new SelfUpdateCheckResult(SelfUpdateCheckStatus.UpToDate);
        }
        if (!includeIgnored
            && Version.TryParse(AppConfig.GetConfig(IgnoreVersionConfigKey), out var ignored)
            && version <= ignored)
        {
            return new SelfUpdateCheckResult(SelfUpdateCheckStatus.Ignored, IgnoredVersion: version.ToString());
        }
        return null;
    }

    private static async Task<long> TryGetAssetSizeAsync(string assetUrl, CancellationToken ct)
    {
        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limit.CancelAfter(TimeSpan.FromSeconds(10));
            using var req = new HttpRequestMessage(HttpMethod.Head, assetUrl);
            using var resp = await SharedHttp.SelfUpdate.SendAsync(req, limit.Token);
            if (resp.IsSuccessStatusCode && resp.Content.Headers.ContentLength is long headLength && headLength > 0)
            {
                return headLength;
            }
        }
        catch
        {

        }
        try
        {

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limit.CancelAfter(TimeSpan.FromSeconds(10));
            using var req = new HttpRequestMessage(HttpMethod.Get, assetUrl);
            req.Headers.Range = new RangeHeaderValue(0, 0);
            using var resp = await SharedHttp.SelfUpdate.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, limit.Token);
            if (resp.Content.Headers.ContentRange?.Length is long total && total > 0)
            {
                return total;
            }
            if (resp.Content.Headers.ContentLength is long single && single > 0)
            {
                return single;
            }
        }
        catch
        {

        }
        return 0;
    }

    public static async Task<string?> TryFetchReleaseTimeAsync(string tag, CancellationToken ct)
    {
        try
        {
            IReadOnlyList<ReleaseEntry> releases = await FetchAtomReleasesAsync(ct);
            return releases.FirstOrDefault(r => string.Equals(r.Tag, tag, StringComparison.OrdinalIgnoreCase))?.Date
                is { Length: > 0 } date ? date : null;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SelfUpdate: release time unavailable via releases.atom");
            return null;
        }
    }

    public static async Task<string?> TryRenderMarkdownAsync(string markdown, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }
        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limit.CancelAfter(TimeSpan.FromSeconds(10));
            string payload = JsonSerializer.Serialize(new { text = markdown, mode = "gfm", context = "PaperKito/Arietta" });
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            using var resp = await SharedHttp.SelfUpdate.PostAsync(MarkdownApiUrl, content, limit.Token);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("SelfUpdate: markdown render API returned {Status}", (int)resp.StatusCode);
                return null;
            }
            string html = await resp.Content.ReadAsStringAsync(limit.Token);
            return html is { Length: > 0 } ? html : null;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SelfUpdate: markdown render unavailable");
            return null;
        }
    }

    public static async Task<string?> TryFetchNotesAsync(string tag, CancellationToken ct)
    {
        try
        {
            IReadOnlyList<ReleaseEntry> releases = await FetchAtomReleasesAsync(ct);
            return releases.FirstOrDefault(r => string.Equals(r.Tag, tag, StringComparison.OrdinalIgnoreCase))?.Body;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SelfUpdate: release notes unavailable via releases.atom");
            return null;
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

    public static async Task<string> DownloadAndStageAsync(SelfUpdateInfo info, IProgress<SelfUpdateProgress>? progress, CancellationToken ct = default)
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


    private static async Task<string> DownloadAndStageCoreAsync(SelfUpdateInfo info, IProgress<SelfUpdateProgress>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(_workDir);

        string zipPath = Path.Combine(_workDir, "Arietta-update.zip");

        if (info.AssetSize <= 0)
        {
            long probed = await TryGetAssetSizeAsync(info.AssetUrl, ct);
            if (probed > 0)
            {
                info = info with { AssetSize = probed };
                _logger.LogInformation("SelfUpdate: asset size probed at download time: {Size} bytes", probed);
            }
        }

        HttpClient[] channels = SharedHttp.IsSystemProxyActive()
            ? [SharedHttp.SelfUpdate, SharedHttp.GameUpdateDownload]
            : [SharedHttp.SelfUpdate];

        byte[]? hash = null;
        long received = 0;
        bool parallel = info.AssetSize > 0;
        var totalClock = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("SelfUpdate: download start ({Size} bytes, {Mode}, {Channels} channel(s) available)",
            info.AssetSize, parallel ? $"parallel x{ParallelConnections}" : "single connection", channels.Length);
        for (int i = 0; i < channels.Length; i++)
        {
            try
            {
                (hash, received) = info.AssetSize > 0
                    ? await DownloadParallelAsync(info, channels[i], zipPath, progress, ct, canSwitch: i < channels.Length - 1)
                    : await DownloadOnceAsync(info, channels[i], zipPath, progress, ct, canSwitch: i < channels.Length - 1);
                break;
            }
            catch (RangeNotSupportedException)
            {

                _logger.LogInformation("SelfUpdate: server ignored Range, using single connection");
                (hash, received) = await DownloadOnceAsync(info, channels[i], zipPath, progress, ct,
                    canSwitch: i < channels.Length - 1);
                break;
            }
            catch (SlowChannelException ex) when (i < channels.Length - 1)
            {
                _logger.LogWarning("SelfUpdate: channel {Index} too slow ({Speed:F0} KB/s), switching to the next one",
                    i, ex.KilobytesPerSecond);
                progress?.Report(new SelfUpdateProgress(0, 0));
            }
        }
        if (hash is null)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Network, "no usable download channel");
        }
        byte[] digest = hash;
        double seconds = Math.Max(0.001, totalClock.Elapsed.TotalSeconds);
        _logger.LogInformation("SelfUpdate: download done in {Seconds:F1}s ({Speed:F0} KB/s, {Size} bytes)",
            seconds, received / 1024.0 / seconds, received);

        if (info.Sha256 is not null)
        {
            string actual = Convert.ToHexStringLower(digest);
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

    private sealed class SlowChannelException(double kilobytesPerSecond)
        : Exception($"download too slow: {kilobytesPerSecond:F0} KB/s")
    {
        public double KilobytesPerSecond { get; } = kilobytesPerSecond;
    }

    private sealed class RangeNotSupportedException() : Exception("server does not support Range");

    private const int ParallelConnections = 8;

    private static async Task<(byte[] Hash, long Received)> DownloadParallelAsync(
        SelfUpdateInfo info, HttpClient client, string zipPath, IProgress<SelfUpdateProgress>? progress,
        CancellationToken ct, bool canSwitch)
    {
        long size = info.AssetSize;

        await using (var alloc = new FileStream(zipPath, FileMode.Create, FileAccess.Write,
                         FileShare.ReadWrite, 1, FileOptions.Asynchronous))
        {
            alloc.SetLength(size);
        }

        var handle = File.OpenHandle(zipPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        using var channelCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        long done = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();

        int blockCount = ParallelConnections * 4;
        long stripe = Math.Clamp((size + blockCount - 1) / blockCount, 1L << 20, 8L << 20);
        var ranges = new List<(long Start, long End)>();
        for (long start = 0; start < size; start += stripe)
        {
            ranges.Add((start, Math.Min(size - 1, start + stripe - 1)));
        }
        var tasks = new List<Task>(ParallelConnections);
        for (int worker = 0; worker < ParallelConnections; worker++)
        {
            int offset = worker;
            tasks.Add(Task.Run(async () =>
            {
                for (int i = offset; i < ranges.Count; i += ParallelConnections)
                {
                    await DownloadRangeAsync(ranges[i].Start, ranges[i].End);
                }
            }));
        }

        Task watchdog = Task.CompletedTask;
        if (canSwitch)
        {
            watchdog = Task.Run(async () =>
            {
                while (!channelCts.IsCancellationRequested)
                {
                    await Task.Delay(1000, CancellationToken.None);
                    if (clock.Elapsed < TimeSpan.FromSeconds(6))
                    {
                        continue;
                    }
                    double kbPerSecond = Interlocked.Read(ref done) / 1024.0 / clock.Elapsed.TotalSeconds;
                    if (kbPerSecond < 400)
                    {
                        channelCts.Cancel();
                        return;
                    }
                }
            });
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && channelCts.IsCancellationRequested)
        {
            double kbPerSecond = Interlocked.Read(ref done) / 1024.0 / Math.Max(0.001, clock.Elapsed.TotalSeconds);
            throw new SlowChannelException(kbPerSecond);
        }
        finally
        {
            channelCts.Cancel();
            try { await watchdog; } catch { }
            handle.Dispose();
        }

        await using var read = File.OpenRead(zipPath);
        byte[] hash = await SHA256.HashDataAsync(read, ct);
        return (hash, Interlocked.Read(ref done));


        async Task DownloadRangeAsync(long start, long end)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, info.AssetUrl);
            req.Headers.Range = new RangeHeaderValue(start, end);
            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, channelCts.Token);
            if (resp.StatusCode != HttpStatusCode.PartialContent)
            {
                throw new RangeNotSupportedException();
            }
            await using var src = await resp.Content.ReadAsStreamAsync(channelCts.Token);
            var buffer = new byte[1 << 16];
            long offset = start;
            int read;
            while ((read = await src.ReadAsync(buffer, channelCts.Token)) > 0)
            {
                await RandomAccess.WriteAsync(handle, buffer.AsMemory(0, read), offset, channelCts.Token);
                offset += read;
                long total = Interlocked.Add(ref done, read);
                progress?.Report(new SelfUpdateProgress(size > 0 ? Math.Min(100.0, total * 100.0 / size) : 0, total));
            }
        }
    }

    private static async Task<(byte[] Hash, long Received)> DownloadOnceAsync(
        SelfUpdateInfo info, HttpClient client, string zipPath, IProgress<SelfUpdateProgress>? progress,
        CancellationToken ct, bool canSwitch)
    {
        using var resp = await client.GetAsync(info.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct);
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
        long received = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        int read;
        try
        {
            while ((read = await src.ReadAsync(buffer, stallCts.Token)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                sha256.AppendData(buffer.AsSpan(0, read));
                received += read;
                progress?.Report(new SelfUpdateProgress(
                    info.AssetSize > 0 ? Math.Min(100.0, received * 100.0 / info.AssetSize) : 0, received));
                stallCts.CancelAfter(TimeSpan.FromSeconds(60));

                if (canSwitch && clock.Elapsed >= TimeSpan.FromSeconds(6))
                {
                    double kbPerSecond = received / 1024.0 / clock.Elapsed.TotalSeconds;
                    if (kbPerSecond < 400)
                    {
                        throw new SlowChannelException(kbPerSecond);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new SelfUpdateException(SelfUpdateErrorKind.Timeout,
                $"download stalled at {received} bytes");
        }
        return (sha256.GetHashAndReset(), received);
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

        return Version.TryParse(version.TrimStart('v', 'V'), out var v) ? v : new Version(0, 0, 0);
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
$log = Join-Path $Root '.arietta-update\updater.log'

function Write-Log([string]$Message) {
    try { Add-Content -Path $log -Value ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $Message) } catch { }
}

# Rename with retry: after the app exits its WebView2 child processes (msedgewebview2.exe)
# keep WebView2Loader.dll / native bits mapped for a few more seconds, which makes the
# first rename attempt fail with "file in use". Retry instead of giving up.
function Rename-WithRetry([string]$From, [string]$To, [int]$Seconds) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ($true) {
        try {
            Rename-Item $From $To -ErrorAction Stop
            return
        } catch {
            if ((Get-Date) -gt $deadline) { throw }
            Start-Sleep -Milliseconds 500
        }
    }
}

try {
    Write-Log "start root=$Root new=$NewApp pid=$ProcId launch=$Launch"

    # Wait for the installing process to exit (up to 90s)
    $deadline = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $deadline) {
        $p = Get-Process -Id $ProcId -ErrorAction SilentlyContinue
        if ($null -eq $p -or $p.HasExited) { break }
        Start-Sleep -Milliseconds 500
    }
    Start-Sleep -Seconds 2
    Write-Log "installing process gone"

    if (Test-Path $old) { Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue }
    Rename-WithRetry $app 'app.old' 30
    Write-Log "app -> app.old"
    Move-Item $NewApp $app
    Write-Log "new app in place"

    # Users running app\Arietta.exe without the stub keep their data inside app/:
    # move it into the new app so settings survive the upgrade
    $oldData = Join-Path $old 'data'
    if ((Test-Path $oldData) -and -not (Test-Path (Join-Path $app 'data'))) {
        Move-Item $oldData (Join-Path $app 'data')
        Write-Log "carried data over"
    }

    Start-Process -FilePath $Launch -ArgumentList '--after-update' -WorkingDirectory (Split-Path -Parent $Launch)
    Write-Log "relaunched $Launch"
    Start-Sleep -Seconds 3

    Remove-Item $old -Recurse -Force -ErrorAction SilentlyContinue
    Write-Log "cleanup done, exit 0"
    Remove-Item (Join-Path $Root '.arietta-update') -Recurse -Force -ErrorAction SilentlyContinue
    exit 0
} catch {
    Write-Log ("FAILED: " + $_.Exception.Message)
    # Rollback: if the new app never landed, restore app from old;
    # if it landed, drop the new app and restore old in its place
    try {
        if ((Test-Path $old) -and (-not (Test-Path $app))) {
            Rename-Item $old 'app'
            Write-Log "rolled back (app restored)"
        } elseif ((Test-Path $old) -and (Test-Path $app)) {
            Remove-Item $app -Recurse -Force
            Rename-Item $old 'app'
            Write-Log "rolled back (new app dropped)"
        }
    } catch {
        Write-Log ("ROLLBACK FAILED: " + $_.Exception.Message)
    }
    exit 1
}
""";
}
