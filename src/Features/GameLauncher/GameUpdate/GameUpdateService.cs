using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arietta.Helpers;
using Microsoft.Extensions.Logging;
using Arietta.Core;

namespace Arietta.Features.GameLauncher.GameUpdate;

public sealed record DownloadChannel(HttpClient Client, string Host, double ScoreBytesPerSec)
{

    public string Label
    {
        get { try { return new Uri(Host).Host; } catch { return Host; } }
    }
}

public sealed record DownloadChannelSet(IReadOnlyList<DownloadChannel> Channels, string Mode);

public static class GameUpdateService
{

    private sealed record BizApi(string GameId, string AppIdKey, string? TierIndexKey = null,
        string TierServerCode = "official");

    private static readonly Dictionary<GameBiz, BizApi> Configs = new()
    {

        [GameBiz.WutheringWaves] = new("G152", "10003_Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5",
            "10003_oLNgHF1CESo51DGHN2odtp40e3oI1HfZ"),

        [GameBiz.WutheringWavesBilibili] = new("G152", "10004_j5GWFuUFlb8N31Wi2uS3ZAVHcb7ZGN7y",
            "10004_IY6xmlAusbImBjWrpPj1OwNuirXEO44x", "bilibili"),
        [GameBiz.WutheringWavesGlobal] = new("G153", "50004_obOHXFrFanqsaIEOmuKroCcbZkQRBC7c",
            "50004_P7xcUZnEr1AXIGON25E6KjpOgTlVrg6e"),
        [GameBiz.PunishingGrayRaven] = new("G148", "10012_RnIUKs3r59Csliu3N0rl5uRWWBOFDaJL"),

        [GameBiz.PunishingGrayRavenGlobal] = new("G143", "50015_LWdk9D2Ep9mpJmqBZZkcPBU2YNraEWBQ"),
        [GameBiz.PunishingGrayRavenJapan] = new("G282", "50007_NxWGZ0d254oWqZKuuL6szOK7WRLPt668"),
        [GameBiz.PunishingGrayRavenKorea] = new("G286", "50011_XefwDdpgPxxLABoTOD0yuqTFBC3koJZ0"),
        [GameBiz.PunishingGrayRavenTaiwan] = new("G279", "50016_i2n5NLmdCAmOGP3J1tJOlWKNSMQuyWL7"),
    };

    private static readonly string[] DomesticBases =
    [
        "https://prod-cn-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.xyz",
        "https://prod-volcdn-gamestarter.kurogame.com",
        "https://prod-tencentcdn-gamestarter.kurogame.com",
    ];

    private static readonly string[] OverseasBases =
    [
        "https://prod-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.net",
        "https://prod-tencentcdn-gamestarter.kurogame.net",
    ];

    private static string[] IndexBasesFor(GameBiz biz) => biz.IsOverseas()
        ? [.. OverseasBases, .. DomesticBases]
        : [.. DomesticBases, .. OverseasBases];

    private static readonly HttpClient Client = SharedHttp.GameUpdateJson;

    private const long ProbeBytes = 1 << 20;

    private const long ProbeMinBytes = 256 << 10;

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private static (string Key, DownloadChannelSet Set, DateTimeOffset Time)? _channelCache;

    private static int _pathFlip;

    private static readonly TimeSpan ChannelCacheLifetime = TimeSpan.FromMinutes(5);

    private const int MaxDownloadChannels = 4;

    internal static async Task<DownloadChannelSet> EnsureDownloadChannelSetAsync(
        GameBiz biz, string? probeRel, bool forceRefresh = false, CancellationToken ct = default)
    {
        bool proxyActive = SharedHttp.IsSystemProxyActive();
        string cacheKey = $"{biz}|auto|{proxyActive}";
        if (!forceRefresh && _channelCache is { } hit && hit.Key == cacheKey && DateTimeOffset.Now - hit.Time < ChannelCacheLifetime)
        {
            return hit.Set;
        }

        var cdnItems = await GetCdnListAsync(biz);
        List<string> hosts = cdnItems.Where(c => c.Url != null).Select(c => c.Url!).Distinct().ToList();
        if (hosts.Count == 0)
        {
            hosts = [IndexBasesFor(biz)[0] + "/"];
        }
        HttpClient direct = SharedHttp.GameUpdateDownload;
        HttpClient viaProxy = SharedHttp.GameUpdateDownloadViaProxy;

        HttpClient client;
        List<DownloadChannel> ranked;
        if (!proxyActive)
        {
            client = direct;
            ranked = await RankHostsAsync(direct, hosts, cdnItems, probeRel, ct);
        }
        else
        {

            var directRanked = await RankHostsAsync(direct, hosts, cdnItems, probeRel, ct);
            var proxyRanked = await RankHostsAsync(viaProxy, hosts, cdnItems, probeRel, ct);
            double directBest = directRanked.Count > 0 ? directRanked[0].ScoreBytesPerSec : 0;
            double proxyBest = proxyRanked.Count > 0 ? proxyRanked[0].ScoreBytesPerSec : 0;
            if (directBest == 0 && proxyBest == 0)
            {

                bool directReachable = await ProbeReachableAsync(direct, hosts[0], ct);
                _logger.LogInformation(
                    "Download channel: probe inconclusive, CDN {Reachable} directly, {Decision}",
                    directReachable ? "reachable" : "unreachable", directReachable ? "going direct" : "falling back to system proxy");
                client = directReachable ? direct : viaProxy;
                ranked = await RankHostsAsync(client, hosts, cdnItems, null, ct);
            }
            else if (forceRefresh && proxyBest > 0 && (Interlocked.Increment(ref _pathFlip) & 1) == 1)
            {

                _logger.LogInformation("Download path flip: falling back to system proxy for this retry");
                client = viaProxy;
                ranked = proxyRanked;
            }
            else if (directBest > 0)
            {

                client = direct;
                ranked = directRanked;
            }
            else
            {
                client = viaProxy;
                ranked = proxyRanked;
            }
        }

        var set = new DownloadChannelSet(ranked, ReferenceEquals(client, direct) ? "direct" : "proxy");
        _channelCache = (cacheKey, set, DateTimeOffset.Now);
        _logger.LogInformation("Download channel: {Mode}, {Count} hosts ranked: {Ranking}",
            set.Mode, ranked.Count,
            string.Join(", ", ranked.Select(c => $"{c.Label}={c.ScoreBytesPerSec / 1048576.0:0.0}MB/s")));
        return set;
    }

    private static async Task<List<DownloadChannel>> RankHostsAsync(
        HttpClient client, List<string> hosts, List<GameCdnItem> cdnItems, string? probeRel, CancellationToken ct)
    {
        Dictionary<string, double> scores = await ScoreHostsAsync(client, hosts, probeRel, ct);
        if (probeRel is not null && scores.Values.All(s => s <= 0))
        {
            scores = await ScoreHostsAsync(client, hosts, null, ct);
        }
        double weightOf(string host) => cdnItems.FirstOrDefault(c => c.Url == host)?.P ?? 0;
        var ranked = hosts
            .Select(h => new DownloadChannel(client, h, scores.GetValueOrDefault(h)))
            .OrderByDescending(c => c.ScoreBytesPerSec)
            .ThenByDescending(c => weightOf(c.Host))
            .ToList();
        if (probeRel is null)
        {

            var alive = ranked.Where(c => c.ScoreBytesPerSec > 0).ToList();
            if (alive.Count > 0)
            {
                ranked = alive;
            }
        }
        return ranked.Take(MaxDownloadChannels).ToList();
    }

    private static async Task<Dictionary<string, double>> ScoreHostsAsync(
        HttpClient client, List<string> hosts, string? probeRel, CancellationToken ct)
    {
        var pairs = await Task.WhenAll(hosts.Select(async h => (Host: h, Score: probeRel is null
            ? (await ProbeReachableAsync(client, h, ct) ? 1.0 : 0.0)
            : await ProbeUrlAsync(client, h + probeRel, ct))));
        return pairs.ToDictionary(p => p.Host, p => p.Score);
    }

    private static async Task<double> ProbeUrlAsync(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ProbeTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, ProbeBytes - 1);
            using var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if ((int)resp.StatusCode is not ((int)HttpStatusCode.OK or (int)HttpStatusCode.PartialContent))
            {
                return 0;
            }
            await using var stream = resp.Content.ReadAsStream(cts.Token);
            var buffer = new byte[1 << 16];
            long total = 0;
            var sw = Stopwatch.StartNew();
            while (total < ProbeBytes)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(0, (int)Math.Min(buffer.Length, ProbeBytes - total)), cts.Token);
                if (read <= 0)
                {
                    break;
                }
                total += read;
            }
            sw.Stop();
            return total >= ProbeMinBytes ? total / sw.Elapsed.TotalSeconds : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<bool> ProbeReachableAsync(HttpClient client, string url, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ProbeTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly ILogger _logger = AppConfig.GetLogger<LogTag>();

    private sealed class LogTag;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<GameBiz, (GameUpdateIndex Index, DateTimeOffset Time)> _cache = new();

    public static async Task<GameUpdateIndex?> GetIndexAsync(GameBiz biz, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(biz, out var cached) && DateTimeOffset.Now - cached.Time < CacheLifetime)
        {
            return cached.Index;
        }

        if (!Configs.TryGetValue(biz, out var cfg) && !Configs.TryGetValue(biz.GetBaseBiz(), out cfg))
        {
            return null;
        }
        foreach (string baseUrl in IndexBasesFor(biz))
        {
            string url = $"{baseUrl}/launcher/game/{cfg.GameId}/{cfg.AppIdKey}/index.json";
            try
            {
                using var resp = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    continue;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var index = await System.Text.Json.JsonSerializer.DeserializeAsync<GameUpdateIndex>(stream, JsonOptions, ct);
                if (index?.Default?.Version is null)
                {
                    continue;
                }
                _cache[biz] = (index, DateTimeOffset.Now);
                return index;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch version index {Url}", url);
            }
        }
        return null;
    }

    private static readonly Dictionary<GameBiz, (GameUpdateIndex? Index, DateTimeOffset Time)> _tierCache = new();

    public static async Task<GameUpdateIndex?> GetTierIndexAsync(GameBiz biz, CancellationToken ct = default)
    {
        if (_tierCache.TryGetValue(biz, out var cached) && DateTimeOffset.Now - cached.Time < CacheLifetime)
        {
            return cached.Index;
        }
        if (!Configs.TryGetValue(biz, out var cfg) || cfg.TierIndexKey is null)
        {
            _tierCache[biz] = (null, DateTimeOffset.Now);
            return null;
        }
        string tierKey = cfg.TierIndexKey;
        foreach (string baseUrl in IndexBasesFor(biz))
        {
            string[] urls =
            [
                $"{baseUrl}/launcher/game/{tierKey}/{cfg.GameId}/{cfg.TierServerCode}/index.json",
                $"{baseUrl}/launcher/game/{cfg.GameId}/{cfg.AppIdKey}/index.json",
            ];
            foreach (string url in urls)
            {
                try
                {
                    using var resp = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                    var index = await System.Text.Json.JsonSerializer.DeserializeAsync<GameUpdateIndex>(stream, JsonOptions, ct);
                    if (index?.ResourcePacks is not { Count: > 0 } || index.Bundles is not { Count: > 0 })
                    {

                        _logger.LogInformation("Branch index {Url}: 200 but no resourcePacks/bundles (legacy format)", url);
                        continue;
                    }
                    _logger.LogInformation("Branch index {Biz}: {Count} branches ({Names}), from {Url}",
                        biz, index.Bundles!.Count, string.Join("/", index.Bundles.Keys), url);
                    _tierCache[biz] = (index, DateTimeOffset.Now);
                    return index;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch branch index {Url}", url);
                }
            }
        }

        _logger.LogInformation("Branch index {Biz}: no branch content at any probe point, treating as no branches", biz);
        _tierCache[biz] = (null, DateTimeOffset.Now);
        return null;
    }

    public static string? GetGameDir(GameBiz biz)
    {
        string? exe = GameRegistry.GetConfiguredExePath(biz);
        return Path.GetDirectoryName(string.IsNullOrWhiteSpace(exe) ? null : exe);
    }

    public static string? GetPersistedInstallDir(GameBiz biz)
    {
        string? dir = AppConfig.GetConfig(GameRegistry.GetInstallDirConfigKey(biz));
        return string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir) ? null : dir;
    }

    public static bool HasResumableInstall(GameBiz biz)
    {
        string? dir = GetPersistedInstallDir(biz);
        string root = Path.Combine(dir ?? "", "predownload");
        return Directory.Exists(root) && Directory.EnumerateDirectories(root).Any();
    }

    public static string? GetLocalVersion(GameBiz biz) =>
        GetGameDir(biz) is { } dir ? ReadLocalVersion(dir) : null;

    public static string? ReadLocalVersion(string dir)
    {
        try
        {
            string cfg = Path.Combine(dir, "launcherDownloadConfig.json");
            if (File.Exists(cfg))
            {
                var parsed = JsonSerializer.Deserialize<LauncherDownloadConfig>(File.ReadAllText(cfg), JsonOptions);
                if (!string.IsNullOrWhiteSpace(parsed?.Version))
                {
                    return parsed.Version;
                }
            }

            string versionFile = Path.Combine(dir, "version.json");
            if (File.Exists(versionFile))
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    File.ReadAllText(versionFile), @"package_version[""':\s]+([\d.]+)");
                if (m.Success)
                {
                    return m.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read local game version {Dir}", dir);
        }
        return null;
    }

    private static void WriteLocalVersion(string dir, string version)
    {
        try
        {
            string cfg = Path.Combine(dir, "launcherDownloadConfig.json");
            LauncherDownloadConfig data = File.Exists(cfg)
                ? JsonSerializer.Deserialize<LauncherDownloadConfig>(File.ReadAllText(cfg), JsonOptions) ?? new()
                : new();
            data.Version = version;
            data.IsPreDownload = false;
            data.State = "";
            File.WriteAllText(cfg, JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonOptions) { WriteIndented = false }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write back launcherDownloadConfig.json {Dir}", dir);
        }
    }

    private static void WritePerPackVersions(string gameDir, string version, IEnumerable<string> packNames)
    {
        try
        {
            foreach (string pack in packNames)
            {
                string path = Path.Combine(gameDir, "launcherDownloadConfig", $"{pack}.json");
                string? existing = null;
                try
                {
                    if (File.Exists(path))
                    {
                        existing = JsonSerializer.Deserialize<LauncherPackVersion>(File.ReadAllText(path), JsonOptions)?.Version;
                    }
                }
                catch { }
                if (existing is not null && CompareVersion(existing, version) > 0)
                {
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(new LauncherPackVersion { PackName = pack, Version = version }, JsonOptions));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write per-pack version records {Dir}", gameDir);
        }
    }

    private static void HealPerPackVersions(string gameDir, string localVersion)
    {
        foreach (string pack in new[] { "common", "hd" })
        {
            string path = Path.Combine(gameDir, "launcherDownloadConfig", $"{pack}.json");
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                var record = JsonSerializer.Deserialize<LauncherPackVersion>(File.ReadAllText(path), JsonOptions);
                if (record?.Version is { } v && CompareVersion(v, localVersion) < 0)
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(new LauncherPackVersion { PackName = pack, Version = localVersion }, JsonOptions));
                    _logger.LogInformation("Healed per-pack version record {Pack}: {Old} -> {New}", pack, v, localVersion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to heal per-pack version record {Path}", path);
            }
        }
    }

    public enum UpdateKind
    {
        None,

        Update,

        Predownload,
    }

    public sealed class GameUpdateState
    {
        public UpdateKind Kind { get; init; }

        public string? LocalVersion { get; init; }

        public string? TargetVersion { get; init; }

        public GameVersionNode? Target { get; init; }

        public string? Changelog { get; init; }

        public List<GameResourceEntry>? Plan { get; init; }

        public DownloadPlan? PlanInfo { get; init; }

        public long TotalBytes { get; init; }

        public int PredownloadedCount { get; init; }

        public long PredownloadedBytes { get; init; }

        public bool HasStagedProgress { get; init; }

        public long RequiredDiskSpace { get; init; }

        public string? TierName { get; init; }

        public List<string>? TierPackNames { get; init; }

        public List<string>? TierFullManifests { get; init; }

        public List<string>? TierDeleteFiles { get; init; }

        public bool HasUpdate => Kind != UpdateKind.None;
    }

    public static int CompareVersion(string? a, string? b)
    {
        if (a == b)
        {
            return 0;
        }
        if (a is null)
        {
            return -1;
        }
        if (b is null)
        {
            return 1;
        }
        int[] pa = Parse(a);
        int[] pb = Parse(b);
        for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            int x = i < pa.Length ? pa[i] : 0;
            int y = i < pb.Length ? pb[i] : 0;
            if (x != y)
            {
                return x.CompareTo(y);
            }
        }
        return string.CompareOrdinal(a, b);

        static int[] Parse(string v)
        {
            return v.Split('.', '-', ' ').Where(s => int.TryParse(s, out _)).Select(int.Parse).ToArray();
        }
    }

    private static readonly ConcurrentDictionary<GameBiz, GameUpdateState> _stateCache = new();

    public static GameUpdateState? GetCachedState(GameBiz biz) =>
        _stateCache.TryGetValue(biz, out var state) ? state : null;

    public static void InvalidateStateCache(GameBiz biz) => _stateCache.TryRemove(biz, out _);

    private static string UpdateHintKey(GameBiz biz) => $"GameUpdateHint_{biz}";

    public static GameUpdateState? GetPersistedUpdateHint(GameBiz biz)
    {
        string? hint = AppConfig.GetConfig(UpdateHintKey(biz));
        if (string.IsNullOrEmpty(hint))
        {
            return null;
        }
        string[] parts = hint.Split('|');
        if (parts.Length != 2)
        {
            return null;
        }
        string? local = GetLocalVersion(biz);
        if (local is null
            || !string.Equals(local, parts[0], StringComparison.OrdinalIgnoreCase)
            || CompareVersion(parts[1], local) <= 0)
        {
            return null;
        }
        return new GameUpdateState
        {
            Kind = UpdateKind.Update,
            LocalVersion = local,
            TargetVersion = parts[1],
        };
    }

    private static void RememberState(GameBiz biz, GameUpdateState state)
    {
        _stateCache[biz] = state;
        string? hint = state.Kind is UpdateKind.Update
            && state.LocalVersion is not null
            && state.TargetVersion is not null
                ? $"{state.LocalVersion}|{state.TargetVersion}"
                : null;
        string key = UpdateHintKey(biz);
        if (!string.Equals(AppConfig.GetConfig(key), hint, StringComparison.Ordinal))
        {
            AppConfig.SetConfig(key, hint);
        }
    }

    public static async Task<GameUpdateState> GetStateAsync(GameBiz biz)
    {
        var state = await GetStateCoreAsync(biz);
        RememberState(biz, state);
        return state;
    }

    private static async Task<GameUpdateState> GetStateCoreAsync(GameBiz biz)
    {

        string? gameDir = GetGameDir(biz);
        if (gameDir is null)
        {
            return new GameUpdateState { Kind = UpdateKind.None, LocalVersion = null };
        }
        var index = await GetIndexAsync(biz);
        string? local = GetLocalVersion(biz);
        if (local is not null)
        {

            HealPerPackVersions(gameDir, local);

            _ = Task.Run(() => CleanOrphanStaging(biz, local));
        }

        var latest = index?.Default;

        var tierIndex = await GetTierIndexAsync(biz);
        string? tierVersion = FirstPackVersion(tierIndex?.ResourcePacks);

        string? latestVersion = CompareVersion(latest?.Version, tierVersion) > 0 ? latest?.Version : tierVersion;

        bool localIsLatest = CompareVersion(latestVersion, local) <= 0;

        var pre = index?.Predownload;
        if (localIsLatest && index?.PredownloadEnabled == true && pre != null && CompareVersion(pre.Version, local) > 0)
        {
            var plan = await BuildPlanAsync(biz, pre, local);
            var staged = plan is null ? (0, 0L) : await CountStagedAsync(biz, pre.Version!, plan.StagedFiles);

            long partBytes = GetStagedPartBytes(biz, pre.Version!);
            return new GameUpdateState
            {
                Kind = UpdateKind.Predownload,
                LocalVersion = local,
                TargetVersion = pre.Version,
                Target = pre,
                Changelog = PickChangelog(pre),
                Plan = plan?.StagedFiles,
                PlanInfo = plan,
                TotalBytes = plan?.TotalBytes ?? 0,
                PredownloadedCount = staged.Item1,
                PredownloadedBytes = staged.Item2 + partBytes,
                HasStagedProgress = staged.Item1 > 0 || partBytes > 0,
                RequiredDiskSpace = plan?.RequiredDiskSpace ?? 0,
            };
        }

        var branch = tierIndex?.PredownloadBranch;
        string? branchVersion = FirstPackVersion(branch?.ResourcePacks);

        bool nodePublished = tierIndex?.PredownloadRaw is { ValueKind: JsonValueKind.Object }
            || index?.PredownloadRaw is { ValueKind: JsonValueKind.Object };
        if (nodePublished && branch is null && pre is null)
        {
            _logger.LogWarning("Predownload {Biz}: node published but unparsable (official changed the structure?)", biz);
        }
        else if (!nodePublished && pre is null && branch is null
            && (tierIndex?.PredownloadEnabled == true || index?.PredownloadEnabled == true)
            && ShouldLogNoPredownload(biz))
        {
            _logger.LogInformation(
                "Predownload {Biz}: switch is on but no node published (official has not opened pre-download)", biz);
        }
        if (localIsLatest && tierIndex?.PredownloadEnabled == true && branch is not null
            && CompareVersion(branchVersion, local) > 0)
        {
            var state = await BuildTierStateAsync(biz, branch, local, UpdateKind.Predownload);
            if (state is not null)
            {
                return state;
            }
            _logger.LogWarning("Tier predownload {Biz}: no usable plan, falling back to legacy state", biz);
        }

        string? updateTarget = latestVersion;
        if (CompareVersion(updateTarget, local) > 0)
        {
            var source = await ResolveTierSourceAsync(biz, updateTarget);
            if (source is not null)
            {
                var state = await BuildTierStateAsync(biz, source, local, UpdateKind.Update);
                if (state is not null)
                {
                    return state;
                }
            }
        }

        if (latest is not null && CompareVersion(latest.Version, local) > 0)
        {
            var plan = await BuildPlanAsync(biz, latest, local);
            var staged = plan is null ? (0, 0L) : await CountStagedAsync(biz, latest.Version!, plan.StagedFiles);
            return new GameUpdateState
            {
                Kind = UpdateKind.Update,
                LocalVersion = local,
                TargetVersion = latest.Version,
                Target = latest,
                Changelog = PickChangelog(latest),
                Plan = plan?.StagedFiles,
                PlanInfo = plan,
                TotalBytes = plan?.TotalBytes ?? 0,
                PredownloadedCount = staged.Item1,
                RequiredDiskSpace = plan?.RequiredDiskSpace ?? 0,
            };
        }

        return new GameUpdateState { Kind = UpdateKind.None, LocalVersion = local };
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<GameBiz, DateTimeOffset> _lastNoPredownloadLog = new();

    private static bool ShouldLogNoPredownload(GameBiz biz)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        if (_lastNoPredownloadLog.TryGetValue(biz, out var last) && now - last < TimeSpan.FromMinutes(5))
        {
            return false;
        }
        _lastNoPredownloadLog[biz] = now;
        return true;
    }

    private static string? FirstPackVersion(Dictionary<string, GameResourcePack>? packs)
    {
        if (packs is null)
        {
            return null;
        }
        if (packs.TryGetValue("common", out var common) && common.Version is not null)
        {
            return common.Version;
        }
        return packs.Values.FirstOrDefault(p => p.Version is not null)?.Version;
    }

    private static async Task<GamePredownloadBranch?> ResolveTierSourceAsync(GameBiz biz, string? version)
    {
        var index = await GetTierIndexAsync(biz);
        if (index is null)
        {
            return null;
        }
        if (index.ResourcePacks is { } packs && index.Bundles is { } bundles
            && FirstPackVersion(packs) == version)
        {
            return new GamePredownloadBranch { ResourcePacks = packs, Bundles = bundles };
        }
        var pre = index.PredownloadBranch;
        return pre is not null && FirstPackVersion(pre.ResourcePacks) == version ? pre : null;
    }

    private sealed record TierPlanBuild(
        DownloadPlan Plan,
        string Version,
        string TierName,
        List<string> PackNames,
        List<string> FullManifests,
        List<string> DeleteFiles);

    private static async Task<GameUpdateState?> BuildTierStateAsync(
        GameBiz biz, GamePredownloadBranch branch, string? local, UpdateKind kind)
    {
        if (GetGameDir(biz) is not { } gameDir || branch.ResourcePacks is null || branch.Bundles is null)
        {
            return null;
        }
        string tier = ResolveUpdateTier(biz, GetInstalledTiers(gameDir), branch.Bundles);
        var built = await BuildTierUpdatePlanAsync(biz, branch, tier, local);
        if (built is null)
        {
            return null;
        }
        var plan = built.Plan;
        var staged = await CountStagedAsync(biz, built.Version, plan.StagedFiles);
        long partBytes = GetStagedPartBytes(biz, built.Version);
        return new GameUpdateState
        {
            Kind = kind,
            LocalVersion = local,
            TargetVersion = built.Version,
            Target = null,
            Changelog = null,
            Plan = plan.StagedFiles,
            PlanInfo = plan,
            TotalBytes = plan.TotalBytes,
            PredownloadedCount = staged.Item1,
            PredownloadedBytes = staged.Item2 + partBytes,
            HasStagedProgress = staged.Item1 > 0 || partBytes > 0,
            RequiredDiskSpace = plan.RequiredDiskSpace,
            TierName = built.TierName,
            TierPackNames = built.PackNames,
            TierFullManifests = built.FullManifests,
            TierDeleteFiles = built.DeleteFiles,
        };
    }

    private static string ResolveUpdateTier(GameBiz biz, List<string> installed, Dictionary<string, GameBundle> bundles)
    {
        var candidates = new List<string?>();
        string? preferred = GetLaunchTier(biz);
        if (preferred is not null)
        {
            candidates.Add(preferred);
        }
        candidates.AddRange(installed);
        candidates.Add("HD");
        foreach (string? candidate in candidates)
        {
            if (candidate is null)
            {
                continue;
            }
            string? key = bundles.Keys.FirstOrDefault(k => string.Equals(k, candidate, StringComparison.OrdinalIgnoreCase));
            if (key is not null)
            {
                return key;
            }
        }
        return bundles.Keys.First();
    }

    private static string NormalizeBase(string? url) =>
        string.IsNullOrWhiteSpace(url) ? "" : url.EndsWith('/') ? url : url + "/";

    private static async Task<TierPlanBuild?> BuildTierUpdatePlanAsync(
        GameBiz biz, GamePredownloadBranch branch, string tierName, string? localVersion)
    {
        var packs = branch.ResourcePacks;
        var bundles = branch.Bundles;
        if (packs is not { Count: > 0 } || bundles is not { Count: > 0 })
        {
            return null;
        }
        string bundleKey = bundles.Keys.FirstOrDefault(k => string.Equals(k, tierName, StringComparison.OrdinalIgnoreCase))
            ?? bundles.Keys.First();
        if (!bundles.TryGetValue(bundleKey, out var bundle) || bundle?.ResourcePacks is not { Count: > 0 })
        {
            return null;
        }
        string? version = FirstPackVersion(packs);
        if (version is null)
        {
            return null;
        }

        var packMap = new Dictionary<string, GameResourcePack>(packs, StringComparer.OrdinalIgnoreCase);
        string commonBase = packMap.TryGetValue("common", out var common) ? NormalizeBase(common.BaseUrl) : "";

        var files = new List<GameResourceEntry>();
        var patchFiles = new List<GameResourceEntry>();
        var groups = new List<GameResourceGroupInfo>();
        var zipPackages = new List<GameResourceZipInfo>();
        var deletes = new List<string>();
        var fullManifests = new List<string>();
        var packNames = new List<string>();
        long total = 0;
        long required = 0;
        string? firstPatchBase = null;

        foreach (string packName in bundle.ResourcePacks)
        {
            if (!packMap.TryGetValue(packName, out var pack))
            {
                return null;
            }
            var patch = pack.PatchConfig?.FirstOrDefault(p => p.Version == localVersion);
            string? manifestPath = patch?.IndexFile ?? pack.IndexFile;
            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                return null;
            }
            var list = await FetchResourceListAsync(biz, manifestPath);
            if (list is null)
            {

                _logger.LogWarning("Tier plan {Biz}/{Tier}: manifest unavailable for pack {Pack}", biz, bundleKey, packName);
                return null;
            }
            string packBase = NormalizeBase(pack.BaseUrl);
            string patchBase = NormalizeBase(patch?.BaseUrl ?? pack.BaseUrl);
            firstPatchBase ??= patchBase;
            packNames.Add(packName);
            if (!string.IsNullOrWhiteSpace(pack.IndexFile))
            {
                fullManifests.Add(pack.IndexFile!);
            }
            deletes.AddRange(list.DeleteFiles ?? []);

            var zipDests = (list.ZipInfos ?? []).Select(z => z.Dest)
                .Where(d => !string.IsNullOrWhiteSpace(d)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var diffDests = (list.GroupInfos ?? []).Select(g => g.Dest)
                .Where(d => !string.IsNullOrWhiteSpace(d)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in list.Resource)
            {
                if (string.IsNullOrWhiteSpace(entry.Dest) || zipDests.Contains(entry.Dest))
                {
                    continue;
                }
                if (diffDests.Contains(entry.Dest))
                {
                    entry.FromFolder ??= patchBase;
                    patchFiles.Add(entry);
                }
                else
                {
                    entry.FromFolder ??= packBase;
                    files.Add(entry);
                }
            }
            foreach (var zip in list.ZipInfos ?? [])
            {
                zip.FromFolder = patchBase;
                zipPackages.Add(zip);
            }
            foreach (var g in list.GroupInfos ?? [])
            {

                foreach (var d in g.DstFiles ?? [])
                {
                    d.FromFolder ??= packBase;
                }
                groups.Add(g);
            }

            long packBytes = patch?.Size > 0 ? patch.Size : pack.Size;
            total += packBytes;
            required += patch?.Ext?.RequiredDiskSpace > 0 ? patch.Ext.RequiredDiskSpace : packBytes;
        }
        if (files.Count == 0 && patchFiles.Count == 0 && zipPackages.Count == 0)
        {
            return null;
        }
        var plan = new DownloadPlan(
            files,
            commonBase,
            total,
            firstPatchBase,
            zipPackages.Count > 0 ? zipPackages : null,
            version,
            groups.Count > 0 ? groups : null,
            patchFiles.Count > 0 ? patchFiles : null,
            required);
        _logger.LogInformation(
            "Tier plan {Biz}/{Tier}: version {Version}, {Files} files, {Patches} patches, {Groups} groups, {Bytes} bytes, {Required} disk",
            biz, bundleKey, version, files.Count, patchFiles.Count, groups.Count, total, required);
        return new TierPlanBuild(plan, version, bundleKey, packNames, fullManifests,
            deletes.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static async Task<GameResourceList?> BuildTierFullListAsync(GameBiz biz, List<string>? manifestPaths)
    {
        if (manifestPaths is not { Count: > 0 })
        {
            return null;
        }
        var merged = new GameResourceList();
        foreach (string path in manifestPaths)
        {
            var list = await FetchResourceListAsync(biz, path);
            if (list is null)
            {
                return null;
            }
            merged.Resource.AddRange(list.Resource);
        }
        return merged.Resource.Count > 0 ? merged : null;
    }

    private static string? PickChangelog(GameVersionNode node)
    {
        if (node.Changelog is null)
        {
            return null;
        }
        return node.Changelog.GetValueOrDefault("zh-Hans")
            ?? node.Changelog.GetValueOrDefault("zh-Hant")
            ?? node.Changelog.GetValueOrDefault("en-US")
            ?? node.Changelog.Values.FirstOrDefault();
    }

    private static async Task<DownloadPlan?> BuildPlanAsync(GameBiz biz, GameVersionNode target, string? localVersion)
    {
        string? manifestPath;
        string baseUrl;
        long total;

        var patch = target.Config?.PatchConfig?.FirstOrDefault(p => p.Version == localVersion);

        baseUrl = target.Config?.BaseUrl ?? "";
        if (patch?.IndexFile != null)
        {
            manifestPath = patch.IndexFile;
            total = patch.Size;
        }
        else
        {
            manifestPath = target.Config?.IndexFile;
            total = target.Config?.Size ?? 0;
        }

        var list = await FetchResourceListAsync(biz, manifestPath);
        if (list is null || baseUrl.Length == 0)
        {
            return null;
        }

        var packageDests = list.ZipInfos is { Count: > 0 }
            ? list.ZipInfos.Select(z => z.Dest).Where(d => !string.IsNullOrWhiteSpace(d)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        var krdiffDests = list.GroupInfos is { Count: > 0 }
            ? list.GroupInfos.Select(g => g.Dest).Where(d => !string.IsNullOrWhiteSpace(d)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];
        var patchFiles = krdiffDests.Count == 0
            ? null
            : list.Resource.Where(f => krdiffDests.Contains(f.Dest)).ToList();
        var files = list.Resource
            .Where(f => !string.IsNullOrWhiteSpace(f.Dest) && !packageDests.Contains(f.Dest) && !krdiffDests.Contains(f.Dest))
            .ToList();
        if (total == 0)
        {
            total = files.Sum(f => f.Size) + (patchFiles?.Sum(f => f.Size) ?? 0);
        }

        string? zipBaseUrl = patch?.BaseUrl;

        long requiredDiskSpace = patch?.Ext?.RequiredDiskSpace ?? 0;
        if (requiredDiskSpace <= 0 && patch is not null && patch.UnCompressSize > 0
            && list.ZipInfos is { Count: > 0 })
        {
            requiredDiskSpace = patch.Size + patch.UnCompressSize;
        }
        return new DownloadPlan(files, baseUrl, total, zipBaseUrl, list.ZipInfos, target.Version,
            list.GroupInfos is { Count: > 0 } ? list.GroupInfos : null, patchFiles, requiredDiskSpace);
    }

    internal static string BuildFileUrl(string cdn, string baseUrl, string dest)
    {
        return $"{cdn}{baseUrl}{dest}";
    }

    private static string? BuildProbeRel(IReadOnlyCollection<GameResourceEntry> files, string? zipBaseUrl, string? baseUrl)
    {
        if (files.Count == 0)
        {
            return null;
        }
        var file = files.Where(f => f.Size >= ProbeMinBytes).MaxBy(f => f.Size)
            ?? files.MaxBy(f => f.Size)!;
        return (file.FromFolder ?? zipBaseUrl ?? baseUrl) + file.Dest;
    }

    private static async Task<List<GameCdnItem>> GetCdnListAsync(GameBiz biz)
    {
        var index = await GetIndexAsync(biz);
        var list = new List<GameCdnItem>();
        if (index?.Default?.CdnList is { Count: > 0 } legacy)
        {
            list.AddRange(legacy);
        }

        var tier = await GetTierIndexAsync(biz);
        if (tier?.CdnList is { Count: > 0 } branch)
        {
            list.AddRange(branch);
        }
        if (list.Count > 0)
        {
            return list.DistinctBy(c => c.Url).ToList();
        }
        return [new GameCdnItem { Url = IndexBasesFor(biz)[0] + "/" }];
    }

    public sealed record DownloadPlan(
        List<GameResourceEntry> Files,
        string BaseUrl,
        long TotalBytes,
        string? ZipBaseUrl = null,
        List<GameResourceZipInfo>? ZipPackages = null,
        string? Version = null,
        List<GameResourceGroupInfo>? KrdiffGroups = null,
        List<GameResourceEntry>? KrdiffFiles = null,
        long RequiredDiskSpace = 0)
    {
        public bool HasZipPackages => ZipPackages is { Count: > 0 };

        public bool HasKrdiffGroups => KrdiffGroups is { Count: > 0 };

        public List<GameResourceEntry> ZipEntries =>
            ZipPackages is null ? [] : ZipPackages.SelectMany(z => z.Entries ?? []).ToList();

        public List<GameResourceEntry> PatchFiles => KrdiffFiles ?? [];

        public List<GameResourceEntry> DirectFiles => [.. Files, .. PatchFiles];

        public List<GameResourceEntry> StagedFiles => [.. DirectFiles, .. ZipEntries];

        public List<GameResourceEntry> AllFiles => [.. Files, .. ZipEntries];

        public List<GameResourceEntry> KrdiffDstFiles =>
            HasKrdiffGroups ? [.. KrdiffGroups!.SelectMany(g => g.DstFiles ?? [])] : [];
    }

    internal static async Task<GameResourceList?> FetchResourceListAsync(GameBiz biz, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        if (!Configs.ContainsKey(biz) && !Configs.ContainsKey(biz.GetBaseBiz()))
        {
            return null;
        }
        var index = await GetIndexAsync(biz);
        var bases = new List<string>();
        foreach (var cdn in index?.Default?.CdnList ?? [])
        {
            if (cdn.Url != null)
            {
                bases.Add(cdn.Url);
            }
        }

        var tierIndex = await GetTierIndexAsync(biz);
        foreach (var cdn in tierIndex?.CdnList ?? [])
        {
            if (cdn.Url != null)
            {
                bases.Add(cdn.Url);
            }
        }
        bases.AddRange(IndexBasesFor(biz));
        bases = bases.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (string baseUrl in bases)
        {
            string url = $"{baseUrl}/{path.TrimStart('/')}";
            try
            {
                using var resp = await Client.GetAsync(url, CancellationToken.None);
                if (!resp.IsSuccessStatusCode)
                {
                    continue;
                }

                byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes.Length > 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
                {
                    using var input = new MemoryStream(bytes);
                    using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
                    using var output = new MemoryStream();
                    gzip.CopyTo(output);
                    bytes = output.ToArray();
                }
                var list = JsonSerializer.Deserialize<GameResourceList>(bytes, JsonOptions);
                if (list is { Resource.Count: > 0 })
                {
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch file manifest {Url}", url);
            }
        }
        return null;
    }

    private delegate void ReportFileProgress(double done, int total, long doneBytes = -1, bool force = false, long speedOverride = -1);

    private static ReportFileProgress MakeThrottledReporter(
        IProgress<GameDownloadProgress>? progress, long totalBytes, DownloadPhase phase,
        Func<double, int, double>? overall = null)
    {
        var clock = Stopwatch.StartNew();
        long lastReportMs = -1;
        long lastBytes = -1;
        long lastSampleMs = 0;
        long speed = 0;
        double lastFileDone = -1;
        long lastFileMs = 0;
        long fileSpeed = 0;
        return (done, total, doneBytes, force, speedOverride) =>
        {
            if (progress == null)
            {
                return;
            }
            long nowMs = clock.ElapsedMilliseconds;

            if (!force && lastReportMs >= 0 && nowMs - lastReportMs < 200)
            {
                return;
            }
            if (doneBytes >= 0)
            {
                if (lastBytes >= 0 && nowMs > lastSampleMs)
                {
                    long inst = (doneBytes - lastBytes) * 1000 / (nowMs - lastSampleMs);
                    inst = Math.Max(0, inst);
                    speed = speed == 0 ? inst : (long)(speed * 0.6 + inst * 0.4);
                }
                lastBytes = doneBytes;
                lastSampleMs = nowMs;
            }
            else
            {

                if (lastFileDone >= 0 && nowMs > lastFileMs && done > lastFileDone)
                {
                    long inst = (long)((done - lastFileDone) * 1000 / (nowMs - lastFileMs));
                    fileSpeed = fileSpeed == 0 ? inst : (long)(fileSpeed * 0.6 + inst * 0.4);
                }
                lastFileDone = Math.Max(done, lastFileDone);
                lastFileMs = nowMs;
            }
            lastReportMs = nowMs;
            progress.Report(new GameDownloadProgress
            {
                Phase = phase,
                ReceivedBytes = doneBytes >= 0 ? doneBytes : totalBytes,
                TotalBytes = totalBytes,
                SpeedBytesPerSec = speedOverride >= 0 ? speedOverride : (doneBytes >= 0 ? speed : 0),
                SpeedFilesPerSec = doneBytes < 0 ? fileSpeed : 0,
                CompletedFiles = (int)done,
                TotalFiles = total,
                OverallProgress = overall?.Invoke(done, total),
            });
        };
    }

    private sealed class MappedProgress(IProgress<GameDownloadProgress>? inner, Func<GameDownloadProgress, GameDownloadProgress> map) : IProgress<GameDownloadProgress>
    {
        public void Report(GameDownloadProgress value)
        {
            if (inner != null)
            {
                inner.Report(map(value));
            }
        }
    }

    public static string GetStagingDir(GameBiz biz, string version)
    {
        string? dir = GetGameDir(biz)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_NoGameDir);
        return Path.Combine(dir, "predownload", version);
    }

    private static long GetStagedPartBytes(GameBiz biz, string version)
    {
        try
        {
            string dir = GetStagingDir(biz, version);

            var segmented = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.EnumerateFiles(dir, "*.part.seg*", SearchOption.AllDirectories))
            {
                try
                {
                    int cut = path.LastIndexOf(".part.seg", StringComparison.OrdinalIgnoreCase);
                    if (cut <= 0)
                    {
                        continue;
                    }
                    string owner = path[..cut];
                    segmented[owner] = segmented.GetValueOrDefault(owner) + new FileInfo(path).Length;
                }
                catch
                {

                }
            }

            long total = segmented.Values.Sum();
            foreach (string path in Directory.EnumerateFiles(dir, "*.part", SearchOption.AllDirectories))
            {
                try
                {

                    if (path.Contains(".part.seg", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (segmented.ContainsKey(path[..^".part".Length]))
                    {
                        continue;
                    }

                    string sidecar = path + ".s";
                    if (File.Exists(sidecar))
                    {
                        try { File.Delete(path); } catch { }
                        try { File.Delete(sidecar); } catch { }
                        continue;
                    }
                    total += new FileInfo(path).Length;
                }
                catch
                {

                }
            }
            return total;
        }
        catch
        {
            return 0;
        }
    }

    public const string UseHardLinkConfigKey = "GameUpdateUseHardLink";

    private static bool UseHardLink => AppConfig.GetConfig(UseHardLinkConfigKey) == "1";

    public const string UseSystemUnzipConfigKey = "GameUpdateUseSystemUnzip";

    private static bool UseSystemUnzip => AppConfig.GetConfig(UseSystemUnzipConfigKey) == "1";

    public const string SpeedLimitConfigKey = "GameUpdateDownloadSpeedLimitMBps";

    internal static long DownloadSpeedLimitBytesPerSec =>
        long.TryParse(AppConfig.GetConfig(SpeedLimitConfigKey), out long mb) && mb > 0
            ? mb * 1024 * 1024
            : 0;

    public const string DownloadThreadsConfigKey = "GameUpdateDownloadThreads";

    public const int DownloadThreadsMin = 1;
    public const int DownloadThreadsMax = 16;
    public const int DownloadThreadsDefault = 16;

    internal static int DownloadThreads
    {
        get
        {
            string raw = AppConfig.GetConfig(DownloadThreadsConfigKey);
            if (string.IsNullOrEmpty(raw) || raw == "0")
            {
                return DownloadThreadsDefault;
            }
            return int.TryParse(raw, out int value)
                ? Math.Clamp(value, DownloadThreadsMin, DownloadThreadsMax)
                : DownloadThreadsDefault;
        }
    }

    public static async Task<long> PredownloadAsync(
        GameBiz biz, GameUpdateState state, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        var plan = state.PlanInfo ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoPlan);
        if (state.TargetVersion is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoTarget);
        }
        if (GetGameDir(biz) is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoGamePath);
        }
        string stageDir = GetStagingDir(biz, state.TargetVersion);
        return await PrepareStagingAsync(biz, plan, stageDir, GetGameDir(biz), progress, ct);
    }

    private static async Task<long> PrepareStagingAsync(
        GameBiz biz, DownloadPlan plan, string stageDir, string? gameDir,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct,
        bool allowGameDirHits = false, IReadOnlySet<string>? appliedPatches = null)
    {

        DownloadChannelSet? channels = null;
        long downloaded = 0;

        var ledger = GameUpdateVerifiedLedger.Load(stageDir);

        var reportVerifying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Verifying);

        var directFiles = plan.DirectFiles;
        _logger.LogInformation("PrepareStaging: validating staging area, {Count} files at {Dir}", directFiles.Count, stageDir);
        var todo = await Task.Run(() => FilterOutdated(stageDir, directFiles,
            (done, total) => reportVerifying(done, total), ct,
            allowGameDirHits ? gameDir : null, appliedPatches, ledger), ct);
        reportVerifying(directFiles.Count, directFiles.Count, force: true);
        if (allowGameDirHits || appliedPatches is { Count: > 0 })
        {
            _logger.LogInformation(
                "PrepareStaging: resume mode (gameDirHits={GameHits}, appliedPatchGroups={Applied})",
                allowGameDirHits, appliedPatches?.Count ?? 0);
        }
        _logger.LogInformation("PrepareStaging: to download {Todo}/{Total}", todo.Count, directFiles.Count);

        if (todo.Count > 0)
        {
            var linked = await Task.Run(() => TryHardLinkFromSiblings(biz, todo, stageDir,
                (done, total) => reportVerifying(done, total), ct, ledger), ct);
            if (linked.Count > 0)
            {
                _logger.LogInformation("PrepareStaging: hard-linked {Count} files from another server's client, download skipped", linked.Count);
                var linkedDests = linked.Select(f => f.Dest).ToHashSet();
                todo.RemoveAll(f => linkedDests.Contains(f.Dest));
            }
        }

        if (todo.Count > 0)
        {

            long remainingTotal = todo.Sum(f => f.Size);

            long completedBytes = Math.Max(0, plan.DirectFiles.Sum(f => f.Size) - remainingTotal);
            string probeRel = BuildProbeRel(plan.DirectFiles, plan.ZipBaseUrl, plan.BaseUrl);
            channels = await EnsureDownloadChannelSetAsync(biz, probeRel, ct: ct);
            var downloader = new GameUpdateDownloader(channels, plan.BaseUrl, stageDir, todo, completedBytes,
                Math.Max(plan.TotalBytes, completedBytes + remainingTotal), plan.ZipBaseUrl,
                rebuildChannels: token => EnsureDownloadChannelSetAsync(biz, probeRel, forceRefresh: true, token),
                ledger: ledger);
            downloaded += await downloader.DownloadAsync(progress, ct);
        }

        if (plan.HasZipPackages)
        {
            var entries = plan.ZipEntries;

            await Task.Run(() => TryHardLinkFromSiblings(biz, entries, stageDir,
                (done, total) => reportVerifying(done, total), ct, ledger), ct);

            _logger.LogInformation("PrepareStaging: checking {Count} subpackage files are in place", entries.Count);
            bool allThere = await Task.Run(() => AllPresent(stageDir, gameDir, entries,
                (done, total) => reportVerifying(done, total), ct, ledger), ct);
            _logger.LogInformation("PrepareStaging: subpackage files in place = {AllThere} ({Count} entries)", allThere, entries.Count);
            if (!allThere)
            {
                channels ??= await EnsureDownloadChannelSetAsync(biz, BuildProbeRel(plan.DirectFiles, plan.ZipBaseUrl, plan.BaseUrl), ct: ct);
                downloaded += await DownloadAndExtractPackagesAsync(biz, plan, stageDir, channels, progress, ct, ledger);
            }
        }
        return downloaded;
    }

    private static List<GameResourceEntry> TryHardLinkFromSiblings(
        GameBiz biz, List<GameResourceEntry> files, string stageDir, Action<int, int>? onChecked = null,
        CancellationToken ct = default, GameUpdateVerifiedLedger? ledger = null)
    {
        if (!UseHardLink)
        {
            return [];
        }
        var sources = CollectSiblingSources(biz, GetGameDir(biz), Path.GetPathRoot(stageDir));
        if (sources.Count == 0)
        {
            return [];
        }
        var linked = new List<GameResourceEntry>();
        int done = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            string staged = Path.Combine(stageDir, file.Dest);
            if (!File.Exists(staged))
            {
                foreach (string src in sources)
                {
                    try
                    {
                        string candidate = Path.Combine(src, file.Dest);

                        if (!File.Exists(candidate) || new FileInfo(candidate).Length != file.Size
                            || !GameUpdateDownloader.VerifyMd5(candidate, file.Md5, ct))
                        {
                            continue;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                        if (CreateHardLinkSafe(candidate, staged))
                        {

                            ledger?.Record(file.Dest, staged, file.Size, file.Md5);
                            linked.Add(file);
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Hard link dedup check failed {Dest}", file.Dest);
                    }
                }
            }
            onChecked?.Invoke(++done, files.Count);
        }
        ledger?.Flush(force: true);
        return linked;
    }

    private static readonly ConcurrentDictionary<string, long> HardLinkEstimateCache = new();

    private static readonly object _verifiedLock = new();
    private static Dictionary<string, HashSet<string>>? _verifiedDests;
    private static string HardLinkCacheFile => Path.Combine(AppConfig.LocalFolder, "hardlink-estimate.json");

    public static long EstimateHardLinkBytes(
        GameBiz biz, DownloadPlan plan, string installDir, IProgress<long>? onRefined = null, CancellationToken ct = default)
    {
        if (!UseHardLink || plan.Files.Count == 0)
        {
            return 0;
        }
        var sources = CollectSiblingSources(biz, GetGameDir(biz), Path.GetPathRoot(Path.GetFullPath(installDir)));
        if (sources.Count == 0)
        {
            return 0;
        }
        string cacheKey = string.Join("|", plan.Version ?? "", installDir.ToUpperInvariant(), sources.Select(s => s.ToUpperInvariant()));
        if (HardLinkEstimateCache.TryGetValue(cacheKey, out long cached))
        {
            return cached;
        }
        long linked = 0;
        int done = 0;

        var verified = GetVerifiedDests(cacheKey);
        foreach (var file in plan.Files)
        {
            ct.ThrowIfCancellationRequested();
            if (IsLinkable(sources, file, verified, ct))
            {
                linked += file.Size;
                onRefined?.Report(linked);
            }

            if (++done % 64 == 0)
            {
                MergeVerifiedDests(cacheKey, verified);
            }
        }
        if (plan.HasZipPackages)
        {
            var entries = plan.ZipEntries;
            if (entries.Count > 0 && entries.All(e =>
            {
                ct.ThrowIfCancellationRequested();
                return IsLinkable(sources, e, verified, ct);
            }))
            {
                linked += entries.Sum(e => e.Size);
                onRefined?.Report(linked);
            }
        }
        MergeVerifiedDests(cacheKey, verified);
        _logger.LogInformation("Install estimate (md5 verified): {Linked} of {Total} hard-linkable from another server's client {Dir}",
            plan.TotalBytes, linked, installDir);
        HardLinkEstimateCache[cacheKey] = linked;
        return linked;
    }

    private static bool IsLinkable(List<string> sources, GameResourceEntry file, HashSet<string> verified, CancellationToken ct = default)
    {
        string? candidate = FindSizeMatchSource(sources, file);
        if (candidate is null)
        {
            return false;
        }

        if (!verified.Contains(file.Dest) && !GameUpdateDownloader.VerifyMd5(candidate, file.Md5, ct))
        {
            return false;
        }
        verified.Add(file.Dest);
        return true;
    }

    private static HashSet<string> GetVerifiedDests(string cacheKey)
    {
        lock (_verifiedLock)
        {
            if (_verifiedDests is null)
            {
                try
                {
                    _verifiedDests = File.Exists(HardLinkCacheFile)
                        ? JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(File.ReadAllText(HardLinkCacheFile)) ?? []
                        : [];
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read hard link estimate cache {File}", HardLinkCacheFile);
                    _verifiedDests = [];
                }
            }
            if (_verifiedDests.TryGetValue(cacheKey, out var set))
            {
                return [.. set];
            }
            return [];
        }
    }

    private static void MergeVerifiedDests(string cacheKey, HashSet<string> verified)
    {
        lock (_verifiedLock)
        {
            var dests = _verifiedDests ??= [];
            if (!dests.TryGetValue(cacheKey, out var set))
            {
                dests[cacheKey] = set = [];
            }
            set.UnionWith(verified);
            try
            {
                Directory.CreateDirectory(AppConfig.LocalFolder);
                File.WriteAllText(HardLinkCacheFile, JsonSerializer.Serialize(_verifiedDests));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save hard link estimate cache {File}", HardLinkCacheFile);
            }
        }
    }

    private static List<string> CollectSiblingSources(GameBiz biz, string? selfDir, string? selfRoot)
    {
        var sources = new List<string>();
        if (selfRoot is null)
        {
            return sources;
        }
        foreach (var g in GameRegistry.Games)
        {
            if (g.Biz == biz)
            {
                continue;
            }
            string? dir = GetGameDir(g.Biz);
            if (dir is null || (selfDir is not null && string.Equals(dir, selfDir, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            if (string.Equals(Path.GetPathRoot(dir), selfRoot, StringComparison.OrdinalIgnoreCase))
            {
                sources.Add(dir);
            }
        }
        return sources;
    }

    private static string? FindSizeMatchSource(List<string> sources, GameResourceEntry file)
    {
        foreach (string src in sources)
        {
            try
            {
                string candidate = Path.Combine(src, file.Dest);
                if (File.Exists(candidate) && new FileInfo(candidate).Length == file.Size)
                {
                    return candidate;
                }
            }
            catch
            {

            }
        }
        return null;
    }

    private static async Task<long> DownloadAndExtractPackagesAsync(
        GameBiz biz, DownloadPlan plan, string stageDir, DownloadChannelSet channels,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct, GameUpdateVerifiedLedger? ledger = null)
    {
        string zipBase = plan.ZipBaseUrl
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);

        var packages = new List<GameResourceEntry>();
        long alreadyOnDisk = 0;
        foreach (var info in plan.ZipPackages!)
        {
            long size = await GetRemoteFileSizeAsync(channels.Channels[0].Host, info.FromFolder ?? zipBase, info.Dest, ct);
            if (size <= 0)
            {
                throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, info.Dest));
            }
            string path = Path.Combine(stageDir, info.Dest);
            if (File.Exists(path))
            {

                if (GameUpdateDownloader.FileLengthMatches(path, size))
                {
                    alreadyOnDisk += size;
                }
                else
                {
                    File.Delete(path);
                }
            }

            packages.Add(new GameResourceEntry { Dest = info.Dest, Size = size, FromFolder = info.FromFolder });
        }

        long zipTotal = packages.Sum(p => p.Size);
        string probeRel = BuildProbeRel(plan.DirectFiles, plan.ZipBaseUrl, plan.BaseUrl);
        var downloader = new GameUpdateDownloader(channels, zipBase, stageDir, packages, alreadyOnDisk, zipTotal,
            rebuildChannels: token => EnsureDownloadChannelSetAsync(biz, probeRel, forceRefresh: true, token),
            ledger: ledger);
        long downloaded = await downloader.DownloadAsync(progress, ct);

        long unzipTotal = plan.ZipEntries.Sum(f => f.Size);
        var reportApplying = MakeThrottledReporter(progress, unzipTotal > 0 ? unzipTotal : plan.TotalBytes, DownloadPhase.Applying);

        int zipEntryTotal = plan.ZipPackages!.Sum(z => z.Entries?.Count ?? 0);
        reportApplying(0, zipEntryTotal, 0, force: true);
        await Task.Run(() => ExtractPackages(stageDir, plan.ZipPackages!,
            (done, total, bytes) => reportApplying(done, total, bytes), ct, ledger), ct);

        foreach (var info in plan.ZipPackages!)
        {
            try
            {
                File.Delete(Path.Combine(stageDir, info.Dest));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete krzip package {Dest}", info.Dest);
            }
        }
        return downloaded;
    }

    private static void ExtractPackages(string stageDir, List<GameResourceZipInfo> packages, Action<int, int, long>? onProgress = null, CancellationToken ct = default, GameUpdateVerifiedLedger? ledger = null)
    {
        int total = packages.Sum(p => p.Entries?.Count ?? 0);
        int done = 0;
        long bytes = 0;
        foreach (var info in packages)
        {
            ct.ThrowIfCancellationRequested();
            string path = Path.Combine(stageDir, info.Dest);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, info.Dest), path);
            }
            var entries = info.Entries ?? [];

            (string Exe, string[] Args)? tool = UseSystemUnzip ? ResolveDefaultAppUnzip(path, stageDir) : null;
            using var archive = tool is null ? System.IO.Compression.ZipFile.OpenRead(path) : null;
            Dictionary<string, System.IO.Compression.ZipArchiveEntry>? map = null;
            if (archive is not null)
            {
                map = new Dictionary<string, System.IO.Compression.ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var zipEntry in archive.Entries)
                {
                    map[zipEntry.FullName.Replace('\\', '/')] = zipEntry;
                }
            }
            if (tool is { } unzipTool)
            {
                RunExternalUnzip(unzipTool.Exe, unzipTool.Args, ct);
            }
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                string name = entry.Dest.Replace('\\', '/');
                string dest = Path.Combine(stageDir, name);
                if (map is not null)
                {
                    if (!map.TryGetValue(name, out var zipEntry))
                    {
                        throw new InvalidDataException(
                            string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, entry.Dest));
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    zipEntry.ExtractToFile(dest, true);
                }
                if (!VerifyFile(dest, entry, ct))
                {
                    File.Delete(dest);
                    throw new InvalidDataException(
                        string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, entry.Dest));
                }

                ledger?.Record(entry.Dest, dest, entry.Size, entry.Md5);
                done++;
                bytes += entry.Size;
                onProgress?.Invoke(done, total, bytes);
            }
        }

        ledger?.Flush(force: true);
    }

    [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int AssocQueryStringW(uint flags, uint str, string assoc, string? extra,
        System.Text.StringBuilder outBuf, ref uint outLen);

    private static string? GetZipDefaultApp()
    {
        try
        {
            var buf = new System.Text.StringBuilder(1024);
            uint len = (uint)buf.Capacity;
            const uint ASSOCF_NONE = 0;
            const uint ASSOCSTR_EXECUTABLE = 2;
            return AssocQueryStringW(ASSOCF_NONE, ASSOCSTR_EXECUTABLE, ".zip", "open", buf, ref len) == 0
                ? buf.ToString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve default app for .zip");
            return null;
        }
    }

    private static (string Exe, string[] Args)? ResolveDefaultAppUnzip(string zipPath, string destDir)
    {
        string? app = GetZipDefaultApp();
        if (string.IsNullOrWhiteSpace(app) || !File.Exists(app))
        {
            return null;
        }
        string dir = Path.GetDirectoryName(app)!;
        string name = Path.GetFileName(app);
        if (File.Exists(Path.Combine(dir, "7z.exe")))
        {
            return (Path.Combine(dir, "7z.exe"), ["x", "-y", $"-o{destDir}", "--", zipPath]);
        }
        if (name.Equals("WinRAR.exe", StringComparison.OrdinalIgnoreCase))
        {

            return (app, ["x", "-ibck", "-y", zipPath, destDir + Path.DirectorySeparatorChar]);
        }
        if (File.Exists(Path.Combine(dir, "bz.exe")))
        {
            return (Path.Combine(dir, "bz.exe"), ["x", "-y", $"-o:{destDir}", zipPath]);
        }
        _logger.LogInformation("Default app for .zip has no known extract CLI: {App}; falling back to built-in extraction", app);
        return null;
    }

    private static void RunExternalUnzip(string exe, string[] args, CancellationToken ct)
    {
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };
        foreach (string arg in args)
        {
            proc.StartInfo.ArgumentList.Add(arg);
        }
        proc.Start();
        using var killOnCancel = ct.Register(() => { try { proc.Kill(); } catch { } });
        proc.WaitForExit();
        ct.ThrowIfCancellationRequested();
        if (proc.ExitCode is not (0 or 1))
        {
            throw new InvalidDataException($"External unzip failed: {Path.GetFileName(exe)} exited {proc.ExitCode} for {Path.GetFileName(proc.StartInfo.ArgumentList.Last())}");
        }
    }

    private static bool AllPresent(string stageDir, string? gameDir, List<GameResourceEntry> files, Action<int, int>? onChecked = null, CancellationToken ct = default, GameUpdateVerifiedLedger? ledger = null)
    {
        int done = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            if (StagedFileOk(Path.Combine(stageDir, file.Dest), file))
            {
                onChecked?.Invoke(++done, files.Count);
                continue;
            }
            if (gameDir is null
                || (ledger?.IsVerified(file.Dest, Path.Combine(gameDir, file.Dest), file.Size, file.Md5) != true
                    && !VerifyFile(Path.Combine(gameDir, file.Dest), file, ct)))
            {
                return false;
            }
            onChecked?.Invoke(++done, files.Count);
        }
        return true;
    }

    private static async Task<long> GetRemoteFileSizeAsync(string cdn, string baseUrl, string dest, CancellationToken ct)
    {
        string url = BuildFileUrl(cdn, baseUrl, dest);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.IsSuccessStatusCode && resp.Content.Headers.ContentLength is > 0 and var length)
            {
                return length;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get subpackage size (HEAD) {Url}", url);
        }

        using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
        rangeRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
        using var rangeResp = await Client.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        rangeResp.EnsureSuccessStatusCode();
        return rangeResp.Content.Headers.ContentRange?.Length ?? 0;
    }

    private static List<GameResourceEntry> FilterOutdated(
        string dir, List<GameResourceEntry> files, Action<int, int>? onChecked = null,
        CancellationToken ct = default, string? gameDir = null, IReadOnlySet<string>? appliedPatches = null,
        GameUpdateVerifiedLedger? ledger = null)
    {
        var todo = new List<GameResourceEntry>();
        int done = 0;
        foreach (var f in files)
        {

            ct.ThrowIfCancellationRequested();
            bool alreadyApplied = appliedPatches?.Contains(f.Dest) == true;

            bool gameHit = gameDir is not null
                && !string.IsNullOrWhiteSpace(f.Md5)
                && (ledger?.IsVerified(f.Dest, Path.Combine(gameDir, f.Dest), f.Size, f.Md5) == true
                    || VerifyFile(Path.Combine(gameDir, f.Dest), f, ct));

            if (!alreadyApplied && !gameHit && !StagedFileOk(Path.Combine(dir, f.Dest), f))
            {
                todo.Add(f);
            }
            onChecked?.Invoke(++done, files.Count);
        }
        return todo;
    }

    private static async Task<(int Count, long Bytes)> CountStagedAsync(GameBiz biz, string version, List<GameResourceEntry> plan)
    {
        string dir = GetStagingDir(biz, version);
        if (!Directory.Exists(dir) || plan.Count == 0)
        {
            return (0, 0);
        }
        int count = 0;
        long bytes = 0;
        using var limiter = new SemaphoreSlim(8);
        var tasks = plan.Select(file => Task.Run(async () =>
        {
            await limiter.WaitAsync();
            try
            {
                string path = Path.Combine(dir, file.Dest);
                if (File.Exists(path) && new FileInfo(path).Length == file.Size)
                {
                    Interlocked.Increment(ref count);
                    Interlocked.Add(ref bytes, file.Size);
                }
            }
            finally
            {
                limiter.Release();
            }
        }));
        await Task.WhenAll(tasks);
        return (count, bytes);
    }

    public static async Task<long> ApplyUpdateAsync(
        GameBiz biz, GameVersionNode target, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        if (GetGameDir(biz) is null || target.Config?.BaseUrl is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoGamePath);
        }
        var plan = await BuildPlanAsync(biz, target, GetLocalVersion(biz))
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        return await ApplyUpdateCoreAsync(biz, target.Version!, plan,
            () => FetchResourceListAsync(biz, target.Config?.IndexFile),
            new[] { "common", "hd" }, null, false, progress, ct);
    }

    public static async Task<long> ApplyUpdateAsync(
        GameBiz biz, GameUpdateState state, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        if (state.TierName is null)
        {
            return await ApplyUpdateAsync(biz,
                state.Target ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoTarget),
                progress, ct);
        }

        DownloadPlan? plan = null;
        var source = await ResolveTierSourceAsync(biz, state.TargetVersion);
        if (source is not null)
        {
            var built = await BuildTierUpdatePlanAsync(biz, source, state.TierName, GetLocalVersion(biz));
            plan = built?.Plan;
        }
        plan ??= state.PlanInfo
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        return await ApplyUpdateCoreAsync(biz, state.TargetVersion!, plan,
            () => BuildTierFullListAsync(biz, state.TierFullManifests),
            state.TierPackNames is { Count: > 0 } names ? names : new List<string> { "common", "hd" },
            state.TierDeleteFiles, true, progress, ct);
    }

    private static async Task<long> ApplyUpdateCoreAsync(
        GameBiz biz, string version, DownloadPlan plan,
        Func<Task<GameResourceList?>> fullListFactory,
        IEnumerable<string> packNames,
        List<string>? explicitDeletes,
        bool keepOtherTiers,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        string gameDir = GetGameDir(biz)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoGamePath);
        EnsureGameNotRunning(biz);
        string stageDir = GetStagingDir(biz, version);

        HashSet<string>? appliedPatches = null;
        if (plan.HasKrdiffGroups)
        {
            string markerDir = Path.Combine(stageDir, "apply_tmp");
            var groups = plan.KrdiffGroups!;
            appliedPatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < groups.Count; i++)
            {
                if (File.Exists(Path.Combine(markerDir, $"g_{i}")))
                {
                    appliedPatches.Add(groups[i].Dest);
                }
            }
        }
        long downloaded = await PrepareStagingAsync(biz, plan, stageDir, gameDir, progress, ct,
            allowGameDirHits: true, appliedPatches: appliedPatches);

        var allFiles = plan.AllFiles;
        int groupCount = plan.HasKrdiffGroups ? plan.KrdiffGroups!.Count : 0;
        int applyUnits = allFiles.Count + groupCount;

        long moveBytes = allFiles.Sum(f => f.Size);
        long applyTotalBytes = moveBytes + plan.KrdiffDstFiles.Sum(f => f.Size);
        if (applyTotalBytes <= 0)
        {
            applyTotalBytes = plan.TotalBytes;
        }
        var reportApplying = MakeThrottledReporter(progress, applyTotalBytes, DownloadPhase.Applying,
            overall: (done, _) => applyUnits > 0 ? (double)done / applyUnits : 1.0);
        reportApplying(0, allFiles.Count, 0, force: true);

        var applyLedger = GameUpdateVerifiedLedger.Load(stageDir);
        bool keepStaging = await Task.Run(() => ApplyStagedToGame(gameDir, stageDir, allFiles, ct,
            (done, total, bytes) => reportApplying(done, total, bytes), applyLedger), ct);

        if (plan.HasKrdiffGroups)
        {
            await ApplyKrdiffGroupsAsync(biz, gameDir, stageDir, plan, progress, ct,
                allFiles.Count, applyUnits, applyTotalBytes, moveBytes, applyLedger);
        }

        var fullList = await fullListFactory();
        if (fullList != null)
        {
            var oldList = ReadLocalGameResources(gameDir);
            if (explicitDeletes is { Count: > 0 })
            {

                foreach (string dest in explicitDeletes)
                {
                    try { File.Delete(Path.Combine(gameDir, dest)); } catch { }
                }
            }
            else if (oldList != null)
            {
                var newSet = fullList.Resource.Select(f => f.Dest).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var old in oldList)
                {
                    if (!newSet.Contains(old.Dest))
                    {
                        try { File.Delete(Path.Combine(gameDir, old.Dest)); } catch { }
                    }
                }
            }
            if (keepOtherTiers && oldList != null)
            {

                var current = fullList.Resource.Select(f => f.Dest).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var old in oldList)
                {
                    if (!current.Contains(old.Dest) && IsTierPakPath(old.Dest))
                    {
                        fullList.Resource.Add(old);
                        current.Add(old.Dest);
                    }
                }
            }
            WriteLocalGameResources(gameDir, fullList);
        }

        WriteLocalVersion(gameDir, version);

        WritePerPackVersions(gameDir, version, packNames);
        if (keepStaging)
        {
            CleanStagingRoots(gameDir, keepVersion: version);
        }
        else
        {
            DeleteStaging(gameDir);
        }
        _cache.Remove(biz);
        return downloaded;
    }

    private static bool IsTierPakPath(string dest)
    {
        string path = dest.Replace('\\', '/');
        return path.Contains("/Content/UHD/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Content/HD/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/Content/SD/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ApplyStagedToGame(string gameDir, string stageDir, List<GameResourceEntry> files, CancellationToken ct, Action<int, int, long>? onProgress = null, GameUpdateVerifiedLedger? ledger = null)
    {

        bool linkMode = UseHardLink
            && string.Equals(Path.GetPathRoot(stageDir), Path.GetPathRoot(gameDir), StringComparison.OrdinalIgnoreCase)
            && !stageDir.StartsWith(gameDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        int done = 0;
        long bytes = 0;
        void Report() => onProgress?.Invoke(done, files.Count, bytes);
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            string staged = Path.Combine(stageDir, file.Dest);
            string destPath = Path.Combine(gameDir, file.Dest);
            if (!File.Exists(staged))
            {

                if (ledger?.IsVerified(file.Dest, destPath, file.Size, file.Md5) == true
                    || VerifyFile(destPath, file, ct))
                {
                    done++;
                    bytes += file.Size;
                    Report();
                    continue;
                }
                throw new FileNotFoundException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, file.Dest), staged);
            }

            if (ledger?.IsVerified(file.Dest, staged, file.Size, file.Md5) != true
                && !GameUpdateDownloader.VerifyMd5(staged, file.Md5, ct))
            {
                throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, file.Dest));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            if (linkMode)
            {
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }
                if (CreateHardLinkSafe(staged, destPath))
                {
                    done++;
                    bytes += file.Size;
                    Report();
                    continue;
                }

            }
            File.Move(staged, destPath, true);
            done++;
            bytes += file.Size;
            Report();
        }
        return linkMode;
    }

    private static bool CreateHardLinkSafe(string existingFile, string newLink)
    {
        try
        {
            if (CreateHardLinkW(newLink, existingFile, IntPtr.Zero))
            {
                return true;
            }
            _logger.LogWarning("Hard link failed (error={Error}), falling back to move {From} -> {To}",
                System.Runtime.InteropServices.Marshal.GetLastWin32Error(), existingFile, newLink);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hard link exception, falling back to move {From} -> {To}", existingFile, newLink);
            return false;
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "CreateHardLinkW", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CreateHardLinkW(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    private static void EnsureGameNotRunning(GameBiz biz)
    {
        using var running = GameProcessMonitor.FindRunningProcess(biz);
        if (running is null)
        {
            return;
        }
        _logger.LogInformation("Killing running game process {Pid} before update ({Biz})", running.Id, biz);
        running.Kill(entireProcessTree: true);
        if (!running.WaitForExit(15000))
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_GameRunning);
        }
    }

    private static async Task ApplyKrdiffGroupsAsync(
        GameBiz biz, string gameDir, string stageDir, DownloadPlan plan,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct,
        int doneOffset = 0, int totalUnits = 0, long totalBytes = 0, long byteOffset = 0,
        GameUpdateVerifiedLedger? ledger = null)
    {
        var groups = plan.KrdiffGroups!;
        int total = groups.Count;
        long maxGroupBytes = groups.Max(g => (g.DstFiles ?? []).Sum(f => f.Size));

        long free = new DriveInfo(Path.GetPathRoot(gameDir)!).AvailableFreeSpace;
        if (free < maxGroupBytes)
        {
            throw new InvalidOperationException(string.Format(
                Localization.Lang.Instance.GameUpdate_ErrNotEnoughSpaceForPatch, GameUpdateSession.FormatSize(maxGroupBytes)));
        }

        string tempDir = Path.Combine(stageDir, "apply_tmp");
        Directory.CreateDirectory(tempDir);

        int CountValidMarkers() => Enumerable.Range(0, total).Count(i =>
        {
            try { return File.ReadAllText(Path.Combine(tempDir, $"g_{i}")) == groups[i].Dest; }
            catch { return false; }
        });
        int resumed = CountValidMarkers();
        if (resumed > 0)
        {
            _logger.LogInformation("krpdiff: resuming, {Resumed}/{Total} groups already applied", resumed, total);
        }
        string hpatchz = LocateHpatchz();
        var patchMd5 = plan.PatchFiles.ToDictionary(f => f.Dest, f => f.Md5, StringComparer.OrdinalIgnoreCase);
        var dstSet = plan.KrdiffDstFiles.Select(f => f.Dest).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int effectiveUnits = totalUnits > 0 ? totalUnits : total;
        long applyTotalBytes = totalBytes > 0 ? totalBytes : plan.TotalBytes;
        var report = MakeThrottledReporter(progress, applyTotalBytes, DownloadPhase.Patching,
            overall: (done, _) => effectiveUnits > 0 ? (double)(doneOffset + done) / effectiveUnits : 1.0);
        _logger.LogInformation("krpdiff: {Count} patch groups, largest dst {Bytes}, temp {Dir}", total, maxGroupBytes, tempDir);
        long patchBytes = byteOffset;

        var reportGate = new object();
        long lastReportedBytes = byteOffset;
        double lastReportedDone = -1;
        double currentGroupDone = 0;
        long completedBytes = byteOffset;
        long liveRate = 0;

        long verifyBytes = 0;
        Action<long> HashCounter()
        {
            long last = 0;
            return cur => Interlocked.Add(ref verifyBytes, cur - Interlocked.Exchange(ref last, cur));
        }

        long groupWorkDenom = 0;
        void SafeReport(double done, int count, long bytes, bool force = false, long speedOverride = -1)
        {
            lock (reportGate)
            {

                done = Math.Max(done, lastReportedDone);
                lastReportedDone = done;
                if (bytes < lastReportedBytes)
                {
                    bytes = lastReportedBytes;
                }
                lastReportedBytes = bytes;
                report(done, count, bytes, force, speedOverride);
            }
        }
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task? heartbeat = progress is null ? null : Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            long lastIo = 0;
            long lastVerify = 0;
            long lastTick = Stopwatch.GetTimestamp();
            int seenGroup = -1;
            long groupIoBase = 0;
            long groupVerifyBase = 0;
            try
            {
                while (await timer.WaitForNextTickAsync(heartbeatCts.Token))
                {
                    long now = Stopwatch.GetTimestamp();
                    long elapsedMs = (now - lastTick) * 1000 / Stopwatch.Frequency;
                    lastTick = now;
                    long io = GetHpatchzIoBytes();

                    long delta = io >= lastIo ? io - lastIo : io;
                    lastIo = io;
                    long vNow = Interlocked.Read(ref verifyBytes);
                    long vDelta = vNow >= lastVerify ? vNow - lastVerify : vNow;
                    lastVerify = vNow;
                    long rate = elapsedMs > 0 ? (delta + vDelta) * 1000 / elapsedMs : delta + vDelta;
                    if (rate > 0)
                    {
                        Interlocked.Exchange(ref liveRate, rate);
                    }

                    double baseDone = Volatile.Read(ref currentGroupDone);
                    int gi = (int)baseDone;
                    if (gi != seenGroup)
                    {
                        seenGroup = gi;
                        groupIoBase = io;
                        groupVerifyBase = vNow;
                    }
                    long denom = Interlocked.Read(ref groupWorkDenom);
                    double frac = 0;
                    if (denom > 0)
                    {
                        long ioInGroup = io >= groupIoBase ? io - groupIoBase : 0;
                        long work = vNow - groupVerifyBase + ioInGroup;
                        frac = Math.Clamp(work / (double)denom, 0, 0.99);
                    }
                    SafeReport(Math.Max(baseDone, gi + frac), total, Interlocked.Read(ref completedBytes),
                        force: true, speedOverride: rate > 0 ? rate : -1);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogDebug(ex, "krpdiff heartbeat stopped"); }
        }, heartbeatCts.Token);

        try
        {
            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();
                var group = groups[i];
                var dstFiles = group.DstFiles ?? [];

                Interlocked.Exchange(ref groupWorkDenom,
                    2L * (group.SrcFiles ?? []).Sum(f => f.Size) + dstFiles.Sum(f => f.Size));
                Volatile.Write(ref currentGroupDone, (double)i);
                Interlocked.Exchange(ref completedBytes, patchBytes);

                SafeReport(i, total, patchBytes, force: true, speedOverride: Interlocked.Read(ref liveRate));
                string marker = Path.Combine(tempDir, $"g_{i}");
                bool markerValid;
                try { markerValid = File.ReadAllText(marker) == group.Dest; }
                catch { markerValid = false; }
                if (!markerValid)
                {

                    if (dstFiles.Count > 0 && !dstFiles.All(f => EntryVerifiedInDir(gameDir, f, ledger, ct, HashCounter())))
                    {
                        string patchPath = Path.Combine(stageDir, group.Dest);

                        var badSrcs = (group.SrcFiles ?? [])
                            .Where(f => !EntryVerifiedInDir(gameDir, f, ledger, ct, HashCounter())).ToList();
                        if (badSrcs.Count > 0)
                        {
                            _logger.LogWarning(
                                "krpdiff: group {Index}/{Total} src invalid ({Count} files, first {First}) — fallback to direct download",
                                i + 1, total, badSrcs.Count, badSrcs[0].Dest);

                            long groupBytes = dstFiles.Sum(f => f.Size);
                            var axisProgress = progress is null ? null : new MappedProgress(progress, p =>
                            {
                                long got = Math.Min(p.ReceivedBytes, groupBytes);
                                double frac = groupBytes > 0 ? (double)got / groupBytes : 1;
                                Volatile.Write(ref currentGroupDone, i + frac);
                                Interlocked.Exchange(ref completedBytes, patchBytes + got);
                                Interlocked.Exchange(ref liveRate, p.SpeedBytesPerSec);
                                return new GameDownloadProgress
                                {
                                    Phase = DownloadPhase.Patching,
                                    CompletedFiles = i,
                                    TotalFiles = total,
                                    ReceivedBytes = patchBytes + got,
                                    TotalBytes = applyTotalBytes,
                                    SpeedBytesPerSec = p.SpeedBytesPerSec,
                                    OverallProgress = effectiveUnits > 0 ? (doneOffset + i + frac) / effectiveUnits : 1.0,
                                };
                            });
                            long direct = await DownloadGroupDstAsync(biz, gameDir, dstFiles, axisProgress, ct, ledger, HashCounter());
                            _logger.LogInformation("krpdiff: group {Index} direct download {Bytes} bytes, patch body discarded",
                                i + 1, direct);
                        }
                        else
                        {
                            if (!File.Exists(patchPath) || !GameUpdateDownloader.VerifyMd5(patchPath, patchMd5.GetValueOrDefault(group.Dest), ct))
                            {
                                throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, group.Dest));
                            }

                            long srcBytes = (group.SrcFiles ?? []).Sum(f => f.Size);
                            string cacheArg = HpatchzCacheArg(srcBytes);
                            long patchStartMs = Environment.TickCount64;
                            await RunHpatchzAsync(hpatchz, gameDir, patchPath, tempDir, cacheArg, ct);
                            _logger.LogInformation("krpdiff: group {Index}/{Total} hpatchz {Cache} src={Src} took {Ms}ms",
                                i + 1, total, cacheArg, srcBytes, Environment.TickCount64 - patchStartMs);
                            foreach (var f in dstFiles)
                            {
                                if (!VerifyFile(Path.Combine(tempDir, f.Dest), f, ct, HashCounter()))
                                {
                                    throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, f.Dest));
                                }
                                string destPath = Path.Combine(gameDir, f.Dest);
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                                File.Move(Path.Combine(tempDir, f.Dest), destPath, true);

                                ledger?.Record(f.Dest, destPath, f.Size, f.Md5);
                            }
                        }
                    }
                    File.WriteAllText(marker, group.Dest);
                    _logger.LogInformation("krpdiff: group {Index}/{Total} applied {Dest} ({Files} files)",
                        i + 1, total, group.Dest, dstFiles.Count);
                }
                patchBytes += dstFiles.Sum(f => f.Size);
                Interlocked.Exchange(ref completedBytes, patchBytes);
                try { File.Delete(Path.Combine(stageDir, group.Dest)); } catch { }
                SafeReport(i + 1, total, patchBytes, force: true, speedOverride: Interlocked.Read(ref liveRate));
            }

            heartbeatCts.Cancel();

            foreach (var group in groups)
            {
                foreach (var src in group.SrcFiles ?? [])
                {
                    if (dstSet.Contains(src.Dest))
                    {
                        continue;
                    }
                    try { File.Delete(Path.Combine(gameDir, src.Dest)); } catch { }
                }
            }
            try { Directory.Delete(tempDir, true); } catch (Exception ex) { _logger.LogWarning(ex, "krpdiff temp cleanup failed {Dir}", tempDir); }
            foreach (var patch in plan.PatchFiles)
            {
                try { File.Delete(Path.Combine(stageDir, patch.Dest)); } catch { }
            }
        }
        finally
        {
            heartbeatCts.Cancel();
            if (heartbeat != null)
            {
                try { await heartbeat; } catch { }
            }
        }
        _logger.LogInformation("krpdiff: {Count} patch groups applied", total);
    }

    private static async Task<long> DownloadGroupDstAsync(
        GameBiz biz, string gameDir, List<GameResourceEntry> dstFiles,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct,
        GameUpdateVerifiedLedger? ledger = null, Action<long>? onHashed = null)
    {
        var todo = dstFiles.Where(f => !EntryVerifiedInDir(gameDir, f, ledger, ct, onHashed)).ToList();
        if (todo.Count == 0)
        {
            return 0;
        }
        long need = todo.Sum(f => f.Size);
        var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(gameDir))!);
        if (drive.IsReady && drive.AvailableFreeSpace < need)
        {
            throw new InvalidOperationException(string.Format(
                Localization.Lang.Instance.GameUpdate_ErrNotEnoughSpaceForPatch, GameUpdateSession.FormatSize(need)));
        }
        string probeRel = BuildProbeRel(todo, null, todo.FirstOrDefault()?.FromFolder ?? "");
        var channels = await EnsureDownloadChannelSetAsync(biz, probeRel, ct: ct);
        var downloader = new GameUpdateDownloader(channels, todo.FirstOrDefault()?.FromFolder ?? "", gameDir, todo,
            rebuildChannels: token => EnsureDownloadChannelSetAsync(biz, probeRel, forceRefresh: true, token));
        return await downloader.DownloadAsync(progress, ct);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong OtherTransferCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetProcessIoCounters(IntPtr handle, out IoCounters counters);

    private static long GetHpatchzIoBytes()
    {
        try
        {
            foreach (Process p in Process.GetProcessesByName("hpatchz"))
            {
                using (p)
                {
                    if (!p.HasExited && GetProcessIoCounters(p.Handle, out var io))
                    {

                        unchecked
                        {
                            return (long)(io.ReadOperationCount + io.WriteOperationCount
                                + io.OtherOperationCount + io.OtherTransferCount
                                + io.ReadTransferCount + io.WriteTransferCount);
                        }
                    }
                }
            }
        }
        catch
        {

        }
        return 0;
    }

    private static string LocateHpatchz()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Hpatchz", "hpatchz.exe");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(Localization.Lang.Instance.GameUpdate_ErrPatchTool, path);
        }
        return path;
    }

    private static string HpatchzCacheArg(long srcBytes)
    {
        long wantedMb = srcBytes >> 22;
        long cacheMb = 32;
        while (cacheMb < 512 && cacheMb < wantedMb)
        {
            cacheMb <<= 1;
        }
        return $"-s-{cacheMb}m";
    }

    private static async Task RunHpatchzAsync(string exe, string oldDir, string diffFile, string outDir,
        string cacheArg, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(cacheArg);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add(oldDir);
        psi.ArgumentList.Add(diffFile);
        psi.ArgumentList.Add(outDir);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrPatchTool);
        var stdoutTask = p.StandardOutput.ReadToEndAsync();
        var stderrTask = p.StandardError.ReadToEndAsync();
        bool cancelled = false;
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            try { p.Kill(true); } catch { }
        }
        string tail = "";
        try
        {
            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            var lines = (stdout + "\n" + stderr).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            tail = string.Join(" | ", lines.TakeLast(4));
        }
        catch
        {

        }
        if (cancelled)
        {
            throw new OperationCanceledException(ct);
        }
        if (p.ExitCode != 0)
        {
            _logger.LogError("hpatchz exit={Code} cache={Cache} diff={Diff} output={Output}",
                p.ExitCode, cacheArg, Path.GetFileName(diffFile), tail);
            throw new InvalidDataException(string.Format(
                Localization.Lang.Instance.GameUpdate_ErrPatchFailed, Path.GetFileName(diffFile), p.ExitCode));
        }
        _logger.LogDebug("hpatchz ok cache={Cache} diff={Diff}: {Output}", cacheArg, Path.GetFileName(diffFile), tail);
    }

    private static void DeleteStaging(string? gameDir)
    {
        CleanStagingRoots(gameDir, keepVersion: null);
        try
        {
            if (gameDir is not null)
            {
                string repair = Path.Combine(gameDir, "launcherDownload", "repair");
                if (Directory.Exists(repair))
                {
                    Directory.Delete(repair, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean staging dir launcherDownload\\repair");
        }
    }

    private static void CleanOrphanStaging(GameBiz biz, string localVersion)
    {
        try
        {
            if (GameUpdateSession.Current.IsActiveFor(biz))
            {
                return;
            }
            string? gameDir = GetGameDir(biz);
            string root = gameDir is null ? "" : Path.Combine(gameDir, "predownload");
            if (!Directory.Exists(root))
            {
                return;
            }
            foreach (string dir in Directory.GetDirectories(root))
            {
                if (CompareVersion(Path.GetFileName(dir), localVersion) > 0)
                {
                    continue;
                }
                try
                {
                    _logger.LogInformation("Cleaning orphaned staging {Dir} (local is already {Local})", dir, localVersion);
                    Directory.Delete(dir, true);
                }
                catch (Exception ex)
                {

                    _logger.LogWarning(ex, "Failed to clean orphaned staging {Dir}", dir);
                }
            }
            RemoveEmptyStagingRoot(root);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean orphaned staging {Biz}", biz);
        }
    }

    private static void CleanStagingRoots(string? gameDir, string? keepVersion)
    {
        if (gameDir is null)
        {
            return;
        }
        string root = Path.Combine(gameDir, "predownload");
        string[] dirs;
        try
        {
            if (!Directory.Exists(root))
            {
                return;
            }
            dirs = Directory.GetDirectories(root);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate staging dir {Path}", root);
            return;
        }

        foreach (string dir in dirs)
        {
            if (keepVersion is not null
                && string.Equals(Path.GetFileName(dir), keepVersion, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            try
            {
                Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean staging dir {Path}", dir);
            }
        }
        RemoveEmptyStagingRoot(root);
    }

    private static void RemoveEmptyStagingRoot(string root)
    {
        try
        {
            if (Directory.Exists(root) && !Directory.EnumerateFileSystemEntries(root).Any())
            {
                Directory.Delete(root);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove empty staging root {Path}", root);
        }
    }

    private static List<GameResourceEntry>? ReadLocalGameResources(string? gameDir)
    {
        try
        {
            string path = Path.Combine(gameDir!, "LocalGameResources.json");
            return File.Exists(path)
                ? JsonSerializer.Deserialize<GameResourceList>(File.ReadAllText(path), JsonOptions)?.Resource
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static void WriteLocalGameResources(string? gameDir, GameResourceList list)
    {
        try
        {
            File.WriteAllText(Path.Combine(gameDir!, "LocalGameResources.json"),
                JsonSerializer.Serialize(list, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write LocalGameResources.json");
        }
    }

    public static async Task<GameUpdateState?> GetInstallStateAsync(GameBiz biz, bool freshCopy = false)
    {
        if (!freshCopy && GetLocalVersion(biz) is not null)
        {
            return null;
        }
        var index = await GetIndexAsync(biz);
        if (index?.Default is not { } latest || latest.Config is null)
        {
            return null;
        }
        var plan = await BuildPlanAsync(biz, latest, null);
        if (plan is null)
        {
            return null;
        }
        return new GameUpdateState
        {
            Kind = UpdateKind.Update,
            LocalVersion = null,
            TargetVersion = latest.Version,
            Target = latest,
            Changelog = PickChangelog(latest),
            Plan = plan.AllFiles,
            PlanInfo = plan,
            TotalBytes = plan.TotalBytes,
        };
    }

    public sealed record GameTierOption(string Name, long SizeBytes, long TierPackSizeBytes = 0);

    public static async Task<List<GameTierOption>?> GetTierOptionsAsync(GameBiz biz)
    {
        var index = await GetTierIndexAsync(biz);
        if (index?.ResourcePacks is not { } packs || !packs.TryGetValue("common", out var common)
            || index.Bundles is not { Count: > 0 } bundles)
        {
            return null;
        }

        var packMap = new Dictionary<string, GameResourcePack>(packs, StringComparer.OrdinalIgnoreCase);
        return bundles.Keys
            .Where(packMap.ContainsKey)
            .OrderBy(k => packMap[k].Size)
            .Select(k => new GameTierOption(k, common.Size + packMap[k].Size, packMap[k].Size))
            .ToList();
    }

    public static async Task<DownloadPlan?> BuildTierPlanAsync(GameBiz biz, string tierName)
    {
        var index = await GetTierIndexAsync(biz);
        if (index?.ResourcePacks is not { } packs
            || !packs.TryGetValue("common", out var common)
            || index.Bundles?.TryGetValue(tierName, out var bundle) != true
            || bundle?.ResourcePacks is not { Count: > 0 })
        {
            return null;
        }
        var files = new List<GameResourceEntry>();
        long total = 0;
        string? version = null;
        foreach (string packName in bundle.ResourcePacks)
        {
            if (!packs.TryGetValue(packName, out var pack) || pack.IndexFile is null)
            {
                continue;
            }
            var list = await FetchResourceListAsync(biz, pack.IndexFile);
            if (list is null)
            {
                return null;
            }
            foreach (var entry in list.Resource)
            {
                if (string.IsNullOrWhiteSpace(entry.Dest))
                {
                    continue;
                }
                entry.FromFolder = pack.BaseUrl;
                files.Add(entry);
            }
            total += pack.Size;
            version ??= pack.Version;
        }
        if (files.Count == 0)
        {
            return null;
        }
        return new DownloadPlan(files, common.BaseUrl ?? "", total, null, null, version, null, null);
    }

    public static async Task<long> SwitchTierAsync(
        GameBiz biz, string tierName, IProgress<GameDownloadProgress>? progress, CancellationToken ct,
        string? installDir = null)
    {
        string? gameDir = string.IsNullOrWhiteSpace(installDir) ? GetGameDir(biz) : installDir;
        if (gameDir is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_NoGameDir);
        }
        var index = await GetTierIndexAsync(biz, ct);
        if (index?.ResourcePacks is not { } packs
            || index.Bundles?.TryGetValue(tierName, out var bundle) != true
            || bundle?.ResourcePacks is not { Count: > 0 })
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        }
        string packName = bundle.ResourcePacks.FirstOrDefault(p => !p.Equals("common", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        if (!packs.TryGetValue(packName, out var pack) || pack.IndexFile is not { } indexFile || pack.BaseUrl is not { } baseUrl)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        }
        string? local = ReadLocalVersion(gameDir);
        if (pack.Version is { } pv && (local is null || CompareVersion(local, pv) != 0))
        {
            throw new InvalidOperationException(string.Format(Localization.Lang.Instance.GameUpdate_ErrTierVersionMismatch, pv, local ?? "?"));
        }

        var list = await FetchResourceListAsync(biz, indexFile)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        List<GameResourceEntry> files = list.Resource
            .Where(f => !string.IsNullOrWhiteSpace(f.Dest))

            .Where(f => !VerifyFile(Path.Combine(gameDir, f.Dest), f, ct))
            .ToList();

        long downloaded = 0;
        if (files.Count > 0)
        {

            if (GameProcessMonitor.IsRunning(biz))
            {
                throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_GameRunning);
            }
            long need = files.Sum(f => f.Size);
            var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(gameDir))!);
            if (drive.IsReady && drive.AvailableFreeSpace < need)
            {
                throw new InvalidOperationException(string.Format(
                    Localization.Lang.Instance.GameUpdate_ErrNotEnoughSpaceForPatch, GameUpdateSession.FormatSize(need)));
            }
            foreach (var f in files)
            {
                f.FromFolder = baseUrl;
            }
            string probeRel = BuildProbeRel(files, null, baseUrl);
            var channels = await EnsureDownloadChannelSetAsync(biz, probeRel, ct: ct);
            var downloader = new GameUpdateDownloader(channels, baseUrl, gameDir, files,
                rebuildChannels: token => EnsureDownloadChannelSetAsync(biz, probeRel, forceRefresh: true, token));
            downloaded = await downloader.DownloadAsync(progress, ct);
        }

        var manifest = ReadLocalGameResources(gameDir);
        if (manifest is null)
        {
            var common = packs.TryGetValue("common", out var c) ? await FetchResourceListAsync(biz, c?.IndexFile) : null;
            if (common is not null)
            {
                manifest = common.Resource;
            }
        }
        if (manifest is not null)
        {
            var dests = manifest.Select(m => m.Dest).ToHashSet(StringComparer.OrdinalIgnoreCase);
            manifest.AddRange(list.Resource.Where(f => !dests.Contains(f.Dest)));
            WriteLocalGameResources(gameDir, new GameResourceList { Resource = manifest });
        }

        if (pack.Version is { } tierVersion)
        {
            WritePerPackVersions(gameDir, tierVersion, [packName]);
        }
        _logger.LogInformation("Tier switch {Biz} -> {Tier}: {Count} files, {Bytes} downloaded", biz, tierName, files.Count, downloaded);
        return downloaded;
    }

    public static string? GetInstallTier(GameBiz biz)
    {
        string v = AppConfig.GetConfig($"InstallTier_{biz}") ?? "";
        return v.Length == 0 ? null : v;
    }

    public static void SetInstallTier(GameBiz biz, string? tier)
    {
        AppConfig.SetConfig($"InstallTier_{biz}", tier ?? "");
    }

    public static string? GetLaunchTier(GameBiz biz)
    {
        string v = AppConfig.GetConfig($"LaunchTier_{biz}") ?? "";
        return v.Length == 0 ? null : v;
    }

    public static void SetLaunchTier(GameBiz biz, string? tier)
    {
        AppConfig.SetConfig($"LaunchTier_{biz}", tier ?? "");
    }

    public static List<string> GetInstalledTiers(string gameDir) =>
        new List<string> { "UHD", "HD", "SD" }
            .Where(t =>
            {
                string dir = Path.Combine(gameDir, "Client", "Content", t);
                return Directory.Exists(dir)
                    && Directory.EnumerateFiles(dir).Any(f => !IsDownloadLeftover(f));
            })
            .ToList();

    private static bool IsDownloadLeftover(string path) =>
        path.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".part.seg", StringComparison.OrdinalIgnoreCase);

    public static string TierDisplayName(string name) => name.ToUpperInvariant() switch
    {
        "UHD" => Localization.Lang.Instance.InstallGameDialog_TierUHD,
        "HD" => Localization.Lang.Instance.InstallGameDialog_TierHD,
        "SD" => Localization.Lang.Instance.InstallGameDialog_TierSD,
        _ => name,
    };

    public static bool HasGameBody(string dir) =>
        File.Exists(Path.Combine(dir, "launcherDownloadConfig.json"))
        || File.Exists(Path.Combine(dir, "LocalGameResources.json"))
        || Directory.Exists(Path.Combine(dir, "Client", "Content"));

    public static long GetMissingBytesForTier(string dir, GameTierOption option)
    {
        if (!HasGameBody(dir))
        {

            return option.SizeBytes;
        }
        return GetInstalledTiers(dir).Any(t => t.Equals(option.Name, StringComparison.OrdinalIgnoreCase))
            ? 0
            : option.TierPackSizeBytes;
    }

    public static async Task<long> InstallAsync(
        GameBiz biz, string installDir, GameVersionNode target, IProgress<GameDownloadProgress>? progress, CancellationToken ct,
        string? tierName = null)
    {
        if (target.Config?.BaseUrl is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoTarget);
        }
        var plan = await (tierName is null ? null : BuildTierPlanAsync(biz, tierName))
            ?? await BuildPlanAsync(biz, target, null)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);

        string stageDir = Path.Combine(installDir, "predownload", plan.Version ?? target.Version!);
        Directory.CreateDirectory(stageDir);

        long downloaded = await PrepareStagingAsync(biz, plan, stageDir, installDir, progress, ct);

        var allFiles = plan.AllFiles;
        var reportApplying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Applying);
        reportApplying(0, allFiles.Count, 0, force: true);

        bool keepStaging = await Task.Run(() => ApplyStagedToGame(installDir, stageDir, allFiles, ct,
            (done, total, bytes) => reportApplying(done, total, bytes)), ct);

        var localList = tierName is null
            ? await FetchResourceListAsync(biz, target.Config?.IndexFile)
            : new GameResourceList { Resource = plan.Files };
        if (localList != null)
        {
            WriteLocalGameResources(installDir, localList);
        }

        if (FindGameExe(installDir, biz) is { } exe)
        {
            AppConfig.SetConfig(GameRegistry.GetExePathConfigKey(biz), exe);
            _logger.LogInformation("Install: game exe backfilled {Exe}", exe);
        }
        else
        {
            _logger.LogWarning("Install: game exe not found in install dir {Biz}, locate it manually in settings", biz);
        }

        GameAutoSearch.WriteOfficialLedger(biz, installDir, elevateForHklm: true);

        WriteLocalVersion(installDir, target.Version!);

        IEnumerable<string> installedPacks = new[] { "common", "hd" };
        if (tierName is not null)
        {
            var tierIndex = await GetTierIndexAsync(biz);
            if (tierIndex?.Bundles?.TryGetValue(tierName, out var installedBundle) == true
                && installedBundle?.ResourcePacks is { Count: > 0 } bundlePacks)
            {
                installedPacks = bundlePacks;
            }
        }
        WritePerPackVersions(installDir, target.Version!, installedPacks);
        if (keepStaging)
        {
            CleanStagingRoots(installDir, keepVersion: target.Version!);
        }
        else
        {
            DeleteStaging(installDir);
        }
        _cache.Remove(biz);
        return downloaded;
    }

    private static string? FindGameExe(string installDir, GameBiz biz)
    {
        string[] names = GameRegistry.GetEntry(biz).ExeNames;
        foreach (string name in names)
        {
            string root = Path.Combine(installDir, name);
            if (File.Exists(root))
            {
                return root;
            }
        }
        try
        {
            return Directory.EnumerateFiles(installDir, "*.exe", SearchOption.AllDirectories)
                .FirstOrDefault(f => names.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Install: failed to scan install dir for exe {Dir}", installDir);
            return null;
        }
    }

    public sealed class VerifyResult
    {

        public List<GameResourceEntry> Damaged { get; } = new();

        public int TotalFiles { get; init; }

        public int CheckedFiles { get; init; }

    public long DamagedBytes { get; set; }

    public string? Version { get; init; }

    public bool CanRepair { get; init; }

    public string? BaseUrl { get; init; }

    public GameResourceList? Manifest { get; init; }
    }

    private static List<string> GetRepairTierCandidates(GameBiz biz, string gameDir, string? launchTier)
    {
        var tiers = new List<string>();
        void Add(string? t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return;
            }
            t = t.ToUpperInvariant();
            if (t is "SD" or "HD" or "UHD" && !tiers.Contains(t))
            {
                tiers.Add(t);
            }
        }
        Add(launchTier);
        try
        {

            string ledger = Path.Combine(gameDir, "launcherDownloadConfig");
            if (Directory.Exists(ledger))
            {
                foreach (string t in new[] { "SD", "HD", "UHD" })
                {
                    if (File.Exists(Path.Combine(ledger, $"{t.ToLowerInvariant()}.json")))
                    {
                        Add(t);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read per-pack version ledger for repair {Dir}", gameDir);
        }
        foreach (string t in GetInstalledTiers(gameDir))
        {
            Add(t);
        }
        if (tiers.Count == 0)
        {
            Add("HD");
        }

        var installed = GetInstalledTiers(gameDir);
        tiers.RemoveAll(t => !installed.Contains(t) && !t.Equals("HD", StringComparison.OrdinalIgnoreCase));
        if (tiers.Count == 0)
        {
            Add("HD");
        }
        return tiers;
    }

    private static bool ManifestPlausiblyMatches(string gameDir, List<GameResourceEntry> files)
    {
        const int SampleCount = 96;
        const double MaxFailRatio = 0.2;
        if (files.Count == 0)
        {
            return false;
        }
        int checkedCount = 0;
        int failed = 0;
        int sample = Math.Min(SampleCount, files.Count);
        for (int i = 0; i < sample; i++)
        {
            var f = files[i * files.Count / sample];
            checkedCount++;
            try
            {
                string path = Path.Combine(gameDir, f.Dest);
                if (!File.Exists(path) || new FileInfo(path).Length != f.Size)
                {
                    failed++;
                }
            }
            catch
            {
                failed++;
            }
        }
        double ratio = (double)failed / checkedCount;
        return ratio <= MaxFailRatio;
    }

    public static async Task<VerifyResult?> VerifyAsync(
        GameBiz biz, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        string? gameDir = GetGameDir(biz);
        string? local = GetLocalVersion(biz);
        if (gameDir is null || local is null)
        {
            return null;
        }

        List<GameResourceEntry>? files = null;
        GameResourceList? manifest = null;
        string? baseUrl = null;

        var candidates = new List<(List<GameResourceEntry> Files, GameResourceList Manifest, string? BaseUrl, string Source)>();
        List<string> tierCandidates = GetRepairTierCandidates(biz, gameDir, GetLaunchTier(biz));

        foreach (string tier in tierCandidates)
        {
            var plan = await BuildTierPlanAsync(biz, tier);
            if (plan is { Files.Count: > 0 } && (plan.Version is null || CompareVersion(local, plan.Version) == 0))
            {
                var list = plan.Files.Where(f => !string.IsNullOrWhiteSpace(f.Dest)).ToList();
                candidates.Add((list, new GameResourceList { Resource = list }, plan.BaseUrl, $"tier:{tier}"));
            }
        }

        var tierIdx = await GetTierIndexAsync(biz, ct);
        if (tierIdx?.ResourcePacks is { } packs && tierIdx.Bundles?.TryGetValue(tierCandidates[0], out var bundle) == true
            && bundle?.ResourcePacks is { Count: > 0 } packNames)
        {
            List<GameResourceEntry>? cached = new();
            foreach (string packName in packNames)
            {
                string path = Path.Combine(gameDir, "launcherDownload", packName, local, "OriginResource.json");
                if (!File.Exists(path))
                {
                    cached = null;
                    break;
                }
                var cachedList = JsonSerializer.Deserialize<GameResourceList>(File.ReadAllText(path), JsonOptions);
                if (cachedList is null)
                {
                    cached = null;
                    break;
                }
                packs.TryGetValue(packName, out var pack);
                foreach (var entry in cachedList.Resource)
                {
                    if (string.IsNullOrWhiteSpace(entry.Dest))
                    {
                        continue;
                    }
                    entry.FromFolder = pack?.BaseUrl;
                    cached.Add(entry);
                }
            }
            if (cached is { Count: > 0 })
            {
                candidates.Add((cached, new GameResourceList { Resource = cached },
                    packs.TryGetValue("common", out var common) ? common.BaseUrl : null, "cachedOriginResource"));
            }
        }

        {
            var localFiles = ReadLocalGameResources(gameDir);
            if (localFiles is null)
            {
                string origin = Path.Combine(gameDir, "launcherDownload", local, "OriginResource.json");
                if (File.Exists(origin))
                {
                    var parsed = JsonSerializer.Deserialize<GameResourceList>(File.ReadAllText(origin), JsonOptions);
                    localFiles = parsed?.Resource;
                }
            }
            localFiles = localFiles?.Where(f => !string.IsNullOrWhiteSpace(f.Dest)).ToList();
            if (localFiles is { Count: > 0 })
            {
                candidates.Add((localFiles, new GameResourceList { Resource = localFiles }, null, "localManifest"));
            }
        }

        var chosen = candidates.FirstOrDefault(c => ManifestPlausiblyMatches(gameDir, c.Files));
        if (chosen.Files is null && candidates.Count > 0)
        {

            _logger.LogWarning(
                "Verify {Biz}: no candidate manifest passed sanity sample ({Count} candidates), scanning the first anyway",
                biz, candidates.Count);
            chosen = candidates[0];
        }
        files = chosen.Files;
        if (files is null)
        {
            return null;
        }
        manifest = chosen.Manifest;
        baseUrl = chosen.BaseUrl;
        _logger.LogInformation("Verify {Biz}: manifest source {Source}, {Count} files", biz, chosen.Source, files.Count);

        if (files is null || files.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(baseUrl))
        {
            var legacy = await GetIndexAsync(biz, ct);
            baseUrl = legacy?.Default?.Config?.BaseUrl;
        }
        bool canRepair = !string.IsNullOrEmpty(baseUrl) || files.All(f => !string.IsNullOrEmpty(f.FromFolder));

        var result = new VerifyResult
        {
            Version = local,
            CanRepair = canRepair,
            BaseUrl = baseUrl,
            Manifest = manifest,
            TotalFiles = files.Count,
            CheckedFiles = files.Count,
        };

        var throttle = new Stopwatch();
        throttle.Start();
        int completed = 0;
        long[] checkedBytes = new long[1];
        long totalBytes = files.Sum(f => f.Size);
        using var limiter = new SemaphoreSlim(8);
        var tasks = files.Select(file => Task.Run(async () =>
        {
            await limiter.WaitAsync(ct);
            try
            {
                if (!VerifyFile(Path.Combine(gameDir, file.Dest), file, ct))
                {
                    lock (result.Damaged)
                    {
                        result.Damaged.Add(file);
                        result.DamagedBytes += file.Size;
                    }
                }
                int done = Interlocked.Increment(ref completed);
                long doneBytes = Interlocked.Add(ref checkedBytes[0], file.Size);
                ReportVerify(progress, throttle, done, files.Count, doneBytes, totalBytes, file.Dest);
            }
            finally
            {
                limiter.Release();
            }
        }, ct));
        await Task.WhenAll(tasks);
        return result;
    }

    private static void ReportVerify(IProgress<GameDownloadProgress>? progress, Stopwatch throttle,
        int done, int total, long doneBytes, long totalBytes, string currentFile)
    {
        if (progress is null)
        {
            return;
        }
        if (throttle.ElapsedMilliseconds < 200 && done < total)
        {
            return;
        }
        throttle.Restart();

        progress.Report(new GameDownloadProgress
        {
            Phase = DownloadPhase.Verifying,
            CurrentFile = Path.GetFileName(currentFile),
            CompletedFiles = done,
            TotalFiles = total,
            ReceivedBytes = doneBytes,
            TotalBytes = totalBytes,
            SpeedBytesPerSec = 0,
        });
    }

    public static async Task<long> RepairGameAsync(GameBiz biz, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        EnsureGameNotRunning(biz);
        string? gameDir = GetGameDir(biz);
        string? local = GetLocalVersion(biz);
        if (gameDir is null || local is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        }

        var saved = TryLoadRepairDetection(gameDir, local);
        if (saved is not null)
        {
            _logger.LogInformation(
                "Repair {Biz}: reuse persisted damage list, {Count} files ({Bytes}), version {Version}",
                biz, saved.Damaged.Count, GameUpdateSession.FormatSize(saved.Damaged.Sum(f => f.Size)), saved.Version);
            var reuse = new VerifyResult
            {
                Version = local,
                CanRepair = true,
                BaseUrl = saved.BaseUrl,
                TotalFiles = saved.TotalFiles,
                CheckedFiles = saved.TotalFiles,
            };
            reuse.Damaged.AddRange(saved.Damaged);
            long reused = await RepairAsync(biz, reuse, progress, ct);
            ClearRepairDetection(gameDir);
            return reused;
        }

        var result = await VerifyAsync(biz, progress, ct)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        if (result.Damaged.Count == 0)
        {
            ClearRepairDetection(gameDir);
            return 0;
        }
        SaveRepairDetection(gameDir, local, result);
        long downloaded = await RepairAsync(biz, result, progress, ct);
        ClearRepairDetection(gameDir);
        return downloaded;
    }

    public sealed class RepairDetection
    {
        public string Version { get; set; } = "";

        public string? Tier { get; set; }

        public string? BaseUrl { get; set; }

        public int TotalFiles { get; set; }

        public List<GameResourceEntry> Damaged { get; set; } = [];
    }

    private static string RepairDetectionPath(string gameDir) =>
        Path.Combine(gameDir, "launcherDownload", "arietta_repair_detection.json");

    private static RepairDetection? TryLoadRepairDetection(string gameDir, string localVersion)
    {
        try
        {
            string path = RepairDetectionPath(gameDir);
            if (!File.Exists(path))
            {
                return null;
            }
            var saved = JsonSerializer.Deserialize<RepairDetection>(File.ReadAllText(path), JsonOptions);
            if (saved?.Version == localVersion && saved.Damaged is { Count: > 0 })
            {
                return saved;
            }
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load repair detection {Dir}", gameDir);
        }
        return null;
    }

    private static void SaveRepairDetection(string gameDir, string localVersion, VerifyResult result)
    {
        try
        {
            var data = new RepairDetection
            {
                Version = localVersion,
                BaseUrl = result.BaseUrl,
                TotalFiles = result.TotalFiles,
                Damaged = result.Damaged,
            };
            string path = RepairDetectionPath(gameDir);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
            _logger.LogInformation(
                "Repair: damage list persisted, {Count} files ({Bytes}) at {Path}",
                result.Damaged.Count, GameUpdateSession.FormatSize(result.DamagedBytes), path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save repair detection {Dir}", gameDir);
        }
    }

    private static void ClearRepairDetection(string gameDir)
    {
        try
        {
            File.Delete(RepairDetectionPath(gameDir));
        }
        catch
        {

        }
    }

    public static async Task<long> RepairAsync(
        GameBiz biz, VerifyResult result, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        if (!result.CanRepair)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_RepairNoBaseUrl);
        }
        string? gameDir = GetGameDir(biz);
        if (gameDir is null || result.Damaged.Count == 0)
        {
            return 0;
        }
        EnsureGameNotRunning(biz);

        string baseUrl = result.BaseUrl ?? result.Damaged.FirstOrDefault()?.FromFolder ?? "";
        string probeRel = BuildProbeRel(result.Damaged, null, baseUrl);
        var channels = await EnsureDownloadChannelSetAsync(biz, probeRel, ct: ct);
        var downloader = new GameUpdateDownloader(channels, baseUrl, gameDir, result.Damaged,
            rebuildChannels: token => EnsureDownloadChannelSetAsync(biz, probeRel, forceRefresh: true, token));
        long downloaded = await downloader.DownloadAsync(progress, ct);

        if (result.Manifest is not null)
        {
            WriteLocalGameResources(gameDir, result.Manifest);
        }
        return downloaded;
    }

    internal static bool VerifyFile(string path, GameResourceEntry entry, CancellationToken ct = default, Action<long>? onHashed = null)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        if (new FileInfo(path).Length != entry.Size)
        {
            return false;
        }
        return GameUpdateDownloader.VerifyMd5(path, entry.Md5, ct, onHashed);
    }

    private static bool EntryVerifiedInDir(
        string dir, GameResourceEntry f, GameUpdateVerifiedLedger? ledger, CancellationToken ct, Action<long>? onHashed = null)
    {
        string path = Path.Combine(dir, f.Dest);
        if (ledger?.IsVerified(f.Dest, path, f.Size, f.Md5) == true)
        {
            return true;
        }
        if (!VerifyFile(path, f, ct, onHashed))
        {
            return false;
        }
        ledger?.Record(f.Dest, path, f.Size, f.Md5);
        return true;
    }

    private static bool StagedFileOk(string path, GameResourceEntry entry)
    {
        try
        {
            return File.Exists(path) && new FileInfo(path).Length == entry.Size;
        }
        catch
        {
            return false;
        }
    }

}
