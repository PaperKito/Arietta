using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arietta.Helpers;
using Microsoft.Extensions.Logging;
using Arietta.Core;

namespace Arietta.Features.GameLauncher.GameUpdate;














public static class GameUpdateService
{
    
    
    
    
    private sealed record BizApi(string GameId, string AppIdKey, string? TierIndexKey = null);

    private static readonly Dictionary<GameBiz, BizApi> Configs = new()
    {
        [GameBiz.WutheringWaves] = new("G152", "10003_Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5"),
        [GameBiz.WutheringWavesBilibili] = new("G152", "10004_j5GWFuUFlb8N31Wi2uS3ZAVHcb7ZGN7y"),
        [GameBiz.WutheringWavesGlobal] = new("G153", "50004_obOHXFrFanqsaIEOmuKroCcbZkQRBC7c",
            "50004_P7xcUZnEr1AXIGON25E6KjpOgTlVrg6e"),
        [GameBiz.PunishingGrayRaven] = new("G148", "10012_RnIUKs3r59Csliu3N0rl5uRWWBOFDaJL"),
        
        
        [GameBiz.PunishingGrayRavenGlobal] = new("G143", "50015_LWdk9D2Ep9mpJmqBZZkcPBU2YNraEWBQ"),
        [GameBiz.PunishingGrayRavenJapan] = new("G282", "50007_NxWGZ0d254oWqZKuuL6szOK7WRLPt668"),
        [GameBiz.PunishingGrayRavenKorea] = new("G286", "50011_XefwDdpgPxxLABoTOD0yuqTFBC3koJZ0"),
        [GameBiz.PunishingGrayRavenTaiwan] = new("G279", "50016_i2n5NLmdCAmOGP3J1tJOlWKNSMQuyWL7"),
    };

    
    
    
    
    private static readonly string[] IndexBases =
    [
        "https://prod-cn-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.xyz",
        "https://prod-alicdn-gamestarter.kurogame.com",
    ];

    private static readonly HttpClient Client = SharedHttp.GameUpdateJson;

    
    
    
    
    private static readonly HttpClient DownloadClient = SharedHttp.GameUpdateDownload;

    private static readonly ILogger _logger = AppConfig.GetLogger<LogTag>();

    
    private sealed class LogTag;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<GameBiz, (GameUpdateIndex Index, DateTimeOffset Time)> _cache = new();


    #region 索引获取

    
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
        foreach (string baseUrl in IndexBases)
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
                _logger.LogWarning(ex, "版本索引拉取失败 {Url}", url);
            }
        }
        return null;
    }

    private static readonly Dictionary<GameBiz, (GameUpdateIndex Index, DateTimeOffset Time)> _tierCache = new();

    
    
    
    
    
    
    public static async Task<GameUpdateIndex?> GetTierIndexAsync(GameBiz biz, CancellationToken ct = default)
    {
        if (_tierCache.TryGetValue(biz, out var cached) && DateTimeOffset.Now - cached.Time < CacheLifetime)
        {
            return cached.Index;
        }
        if (!Configs.TryGetValue(biz, out var cfg) || cfg.TierIndexKey is null)
        {
            return null;
        }
        foreach (string baseUrl in IndexBases)
        {
            string url = $"{baseUrl}/launcher/game/{cfg.TierIndexKey}/{cfg.GameId}/official/index.json";
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
                    continue;
                }
                _tierCache[biz] = (index, DateTimeOffset.Now);
                return index;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "分档索引拉取失败 {Url}", url);
            }
        }
        return null;
    }

    #endregion


    #region 本地版本

    
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

    
    public static string? GetLocalVersion(GameBiz biz)
    {
        string? dir = GetGameDir(biz);
        if (dir is null)
        {
            return null;
        }
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
            _logger.LogWarning(ex, "读取本地游戏版本失败 {Biz}", biz);
        }
        return null;
    }

    
    private static void WriteLocalVersion(GameBiz biz, string version)
    {
        string? dir = GetGameDir(biz);
        if (dir is null)
        {
            return;
        }
        WriteLocalVersion(dir, version);
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
            _logger.LogWarning(ex, "回写 launcherDownloadConfig.json 失败 {Dir}", dir);
        }
    }

    #endregion


    #region 更新状态

    
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

    
    
    
    
    public static async Task<GameUpdateState> GetStateAsync(GameBiz biz)
    {
        
        
        
        
        if (GetGameDir(biz) is null)
        {
            return new GameUpdateState { Kind = UpdateKind.None, LocalVersion = null };
        }
        var index = await GetIndexAsync(biz);
        string? local = GetLocalVersion(biz);
        if (local is not null)
        {
            
            _ = Task.Run(() => CleanOrphanStaging(biz, local));
        }
        if (index?.Default is not { } latest)
        {
            return new GameUpdateState { Kind = UpdateKind.None, LocalVersion = local };
        }

        
        
        bool localIsLatest = CompareVersion(latest.Version, local) <= 0;

        
        var pre = index.Predownload;
        if (localIsLatest && index.PredownloadSwitch == 1 && pre != null && CompareVersion(pre.Version, local) > 0)
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
            };
        }

        if (CompareVersion(latest.Version, local) > 0)
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
            };
        }

        return new GameUpdateState { Kind = UpdateKind.None, LocalVersion = local };
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
        return new DownloadPlan(files, baseUrl, total, zipBaseUrl, list.ZipInfos, target.Version,
            list.GroupInfos is { Count: > 0 } ? list.GroupInfos : null, patchFiles);
    }

    
    internal static string BuildFileUrl(string cdn, string baseUrl, string dest)
    {
        return $"{cdn}{baseUrl}{dest}";
    }

    
    internal static HttpClient SharedClient => DownloadClient;

    
    private static async Task<string> GetDownloadCdnAsync(GameBiz biz)
    {
        var index = await GetIndexAsync(biz);
        var list = index?.Default?.CdnList;
        if (list is { Count: > 0 })
        {
            int totalWeight = list.Sum(c => c.P);
            if (totalWeight > 0)
            {
                int roll = Random.Shared.Next(totalWeight);
                foreach (var cdn in list)
                {
                    roll -= cdn.P;
                    if (roll < 0 && cdn.Url != null)
                    {
                        return cdn.Url;
                    }
                }
            }
            return list[0].Url ?? "";
        }
        return IndexBases[0] + "/";
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public sealed record DownloadPlan(
        List<GameResourceEntry> Files,
        string BaseUrl,
        long TotalBytes,
        string? ZipBaseUrl = null,
        List<GameResourceZipInfo>? ZipPackages = null,
        string? Version = null,
        List<GameResourceGroupInfo>? KrdiffGroups = null,
        List<GameResourceEntry>? KrdiffFiles = null)
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
        bases.AddRange(IndexBases);
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
                _logger.LogWarning(ex, "文件清单拉取失败 {Url}", url);
            }
        }
        return null;
    }

    #endregion


    #region 预下载暂存与应用更新

    
    private delegate void ReportFileProgress(int done, int total, bool force = false);

    
    
    
    
    private static ReportFileProgress MakeThrottledReporter(
        IProgress<GameDownloadProgress>? progress, long totalBytes, DownloadPhase phase)
    {
        var clock = Stopwatch.StartNew();
        return (done, total, force) =>
        {
            if (progress == null || (!force && clock.ElapsedMilliseconds < 200))
            {
                return;
            }
            clock.Restart();
            progress.Report(new GameDownloadProgress
            {
                Phase = phase,
                ReceivedBytes = totalBytes,
                TotalBytes = totalBytes,
                CompletedFiles = done,
                TotalFiles = total,
            });
        };
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
            long total = 0;
            foreach (string path in Directory.EnumerateFiles(GetStagingDir(biz, version), "*.part"))
            {
                try
                {
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

    
    public const string SpeedLimitConfigKey = "GameUpdateDownloadSpeedLimitMBps";

    
    internal static long DownloadSpeedLimitBytesPerSec =>
        long.TryParse(AppConfig.GetConfig(SpeedLimitConfigKey), out long mb) && mb > 0
            ? mb * 1024 * 1024
            : 0;

    
    
    
    
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
        IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        string cdn = await GetDownloadCdnAsync(biz);
        long downloaded = 0;

        
        
        
        var reportVerifying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Verifying);

        
        
        var directFiles = plan.DirectFiles;
        _logger.LogInformation("PrepareStaging: 开始校验暂存区 {Count} 个文件 {Dir}", directFiles.Count, stageDir);
        var todo = await Task.Run(() => FilterOutdated(stageDir, directFiles,
            (done, total) => reportVerifying(done, total)), ct);
        reportVerifying(directFiles.Count, directFiles.Count, force: true);
        _logger.LogInformation("PrepareStaging: 待下载 {Todo}/{Total}", todo.Count, directFiles.Count);

        
        if (todo.Count > 0)
        {
            var linked = await Task.Run(() => TryHardLinkFromSiblings(biz, todo, stageDir,
                (done, total) => reportVerifying(done, total)), ct);
            if (linked.Count > 0)
            {
                _logger.LogInformation("PrepareStaging: 从其它区服客户端硬链接 {Count} 个文件，免下载", linked.Count);
                var linkedDests = linked.Select(f => f.Dest).ToHashSet();
                todo.RemoveAll(f => linkedDests.Contains(f.Dest));
            }
        }

        if (todo.Count > 0)
        {
            
            
            long remainingTotal = todo.Sum(f => f.Size);
            var downloader = new GameUpdateDownloader(cdn, plan.BaseUrl, stageDir, todo, 0, remainingTotal, plan.ZipBaseUrl);
            downloaded += await downloader.DownloadAsync(progress, ct);
        }

        
        if (plan.HasZipPackages)
        {
            var entries = plan.ZipEntries;
            
            await Task.Run(() => TryHardLinkFromSiblings(biz, entries, stageDir,
                (done, total) => reportVerifying(done, total)), ct);
            
            
            _logger.LogInformation("PrepareStaging: 校验 {Count} 个分包内文件是否就位", entries.Count);
            bool allThere = await Task.Run(() => AllPresent(stageDir, gameDir, entries,
                (done, total) => reportVerifying(done, total)), ct);
            _logger.LogInformation("PrepareStaging: 分包内文件就位判定 {AllThere}（{Count} 条）", allThere, entries.Count);
            if (!allThere)
            {
                downloaded += await DownloadAndExtractPackagesAsync(biz, plan, stageDir, cdn, progress, ct);
            }
        }
        return downloaded;
    }


    
    
    
    
    
    
    
    
    
    
    private static List<GameResourceEntry> TryHardLinkFromSiblings(
        GameBiz biz, List<GameResourceEntry> files, string stageDir, Action<int, int>? onChecked = null)
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
            string staged = Path.Combine(stageDir, file.Dest);
            if (!File.Exists(staged))
            {
                foreach (string src in sources)
                {
                    try
                    {
                        string candidate = Path.Combine(src, file.Dest);
                        
                        if (!File.Exists(candidate) || new FileInfo(candidate).Length != file.Size
                            || !GameUpdateDownloader.VerifyMd5(candidate, file.Md5))
                        {
                            continue;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                        if (CreateHardLinkSafe(candidate, staged))
                        {
                            linked.Add(file);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "硬链接去重检查失败 {Dest}", file.Dest);
                    }
                }
            }
            onChecked?.Invoke(++done, files.Count);
        }
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
            if (IsLinkable(sources, file, verified))
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
                return IsLinkable(sources, e, verified);
            }))
            {
                linked += entries.Sum(e => e.Size);
                onRefined?.Report(linked);
            }
        }
        MergeVerifiedDests(cacheKey, verified);
        _logger.LogInformation("安装预估（md5 校验）：{Total} 中 {Linked} 可从其它区服客户端硬链接复用 {Dir}",
            plan.TotalBytes, linked, installDir);
        HardLinkEstimateCache[cacheKey] = linked;
        return linked;
    }

    
    private static bool IsLinkable(List<string> sources, GameResourceEntry file, HashSet<string> verified)
    {
        string? candidate = FindSizeMatchSource(sources, file);
        if (candidate is null)
        {
            return false;
        }
        
        if (!verified.Contains(file.Dest) && !GameUpdateDownloader.VerifyMd5(candidate, file.Md5))
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
                    _logger.LogWarning(ex, "读取硬链接预估缓存失败 {File}", HardLinkCacheFile);
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
                _logger.LogWarning(ex, "保存硬链接预估缓存失败 {File}", HardLinkCacheFile);
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
        GameBiz biz, DownloadPlan plan, string stageDir, string cdn,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        string zipBase = plan.ZipBaseUrl
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);

        var packages = new List<GameResourceEntry>();
        long alreadyOnDisk = 0;
        foreach (var info in plan.ZipPackages!)
        {
            long size = await GetRemoteFileSizeAsync(cdn, zipBase, info.Dest, ct);
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
            packages.Add(new GameResourceEntry { Dest = info.Dest, Size = size });
        }

        
        long zipTotal = packages.Sum(p => p.Size);
        var downloader = new GameUpdateDownloader(cdn, zipBase, stageDir, packages, alreadyOnDisk, zipTotal);
        long downloaded = await downloader.DownloadAsync(progress, ct);

        
        
        var reportApplying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Applying);

        int zipEntryTotal = plan.ZipPackages!.Sum(z => z.Entries?.Count ?? 0);
        reportApplying(0, zipEntryTotal, force: true);
        await Task.Run(() => ExtractPackages(stageDir, plan.ZipPackages!,
            (done, total) => reportApplying(done, total)), ct);

        
        foreach (var info in plan.ZipPackages!)
        {
            try
            {
                File.Delete(Path.Combine(stageDir, info.Dest));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "删除 krzip 包失败 {Dest}", info.Dest);
            }
        }
        return downloaded;
    }


    
    
    
    
    
    
    
    private static void ExtractPackages(string stageDir, List<GameResourceZipInfo> packages, Action<int, int>? onProgress = null)
    {
        int total = packages.Sum(p => p.Entries?.Count ?? 0);
        int done = 0;
        foreach (var info in packages)
        {
            string path = Path.Combine(stageDir, info.Dest);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, info.Dest), path);
            }
            var entries = info.Entries ?? [];
            using var archive = System.IO.Compression.ZipFile.OpenRead(path);
            var map = new Dictionary<string, System.IO.Compression.ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var zipEntry in archive.Entries)
            {
                map[zipEntry.FullName.Replace('\\', '/')] = zipEntry;
            }
            foreach (var entry in entries)
            {
                string name = entry.Dest.Replace('\\', '/');
                if (!map.TryGetValue(name, out var zipEntry))
                {
                    throw new InvalidDataException(
                        string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, entry.Dest));
                }
                string dest = Path.Combine(stageDir, name);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                zipEntry.ExtractToFile(dest, true);
                if (!VerifyFile(dest, entry))
                {
                    File.Delete(dest);
                    throw new InvalidDataException(
                        string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, entry.Dest));
                }
                onProgress?.Invoke(++done, total);
            }
        }
    }


    
    
    private static bool AllPresent(string stageDir, string? gameDir, List<GameResourceEntry> files, Action<int, int>? onChecked = null)
    {
        int done = 0;
        foreach (var file in files)
        {
            if (VerifyFile(Path.Combine(stageDir, file.Dest), file))
            {
                onChecked?.Invoke(++done, files.Count);
                continue;
            }
            if (gameDir is null || !VerifyFile(Path.Combine(gameDir, file.Dest), file))
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
            _logger.LogWarning(ex, "分包大小获取失败（HEAD）{Url}", url);
        }

        using var rangeRequest = new HttpRequestMessage(HttpMethod.Get, url);
        rangeRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
        using var rangeResp = await Client.SendAsync(rangeRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        rangeResp.EnsureSuccessStatusCode();
        return rangeResp.Content.Headers.ContentRange?.Length ?? 0;
    }


    
    
    
    private static List<GameResourceEntry> FilterOutdated(string dir, List<GameResourceEntry> files, Action<int, int>? onChecked = null)
    {
        var todo = new List<GameResourceEntry>();
        int done = 0;
        foreach (var f in files)
        {
            if (!VerifyFile(Path.Combine(dir, f.Dest), f))
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
        string version = target.Version!;
        string? gameDir = GetGameDir(biz);
        if (gameDir is null || target.Config?.BaseUrl is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoGamePath);
        }
        EnsureGameNotRunning(biz);
        string stageDir = GetStagingDir(biz, version);

        
        
        var plan = await BuildPlanAsync(biz, target, GetLocalVersion(biz))
            ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrNoManifest);
        long downloaded = await PrepareStagingAsync(biz, plan, stageDir, gameDir, progress, ct);

        
        
        var allFiles = plan.AllFiles;
        var reportApplying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Applying);
        reportApplying(0, allFiles.Count, force: true);
        bool keepStaging = await Task.Run(() => ApplyStagedToGame(gameDir, stageDir, allFiles, ct,
            (done, total) => reportApplying(done, total)), ct);

        
        
        if (plan.HasKrdiffGroups)
        {
            await ApplyKrdiffGroupsAsync(biz, gameDir, stageDir, plan, progress, ct);
        }

        
        var fullList = await FetchResourceListAsync(biz, target.Config?.IndexFile);
        if (fullList != null)
        {
            var oldList = ReadLocalGameResources(gameDir);
            if (oldList != null)
            {
                var newSet = fullList.Resource.Select(f => f.Dest).ToHashSet();
                foreach (var old in oldList)
                {
                    if (!newSet.Contains(old.Dest))
                    {
                        try { File.Delete(Path.Combine(gameDir, old.Dest)); } catch { }
                    }
                }
            }
            WriteLocalGameResources(gameDir, fullList);
        }

        
        WriteLocalVersion(biz, version);
        if (keepStaging)
        {
            CleanStagingRoots(biz, keepVersion: version);
        }
        else
        {
            DeleteStaging(biz);
        }
        _cache.Remove(biz);
        return downloaded;
    }

    
    
    
    
    
    
    
    
    
    
    
    private static bool ApplyStagedToGame(string gameDir, string stageDir, List<GameResourceEntry> files, CancellationToken ct, Action<int, int>? onProgress = null)
    {
        
        bool linkMode = UseHardLink
            && string.Equals(Path.GetPathRoot(stageDir), Path.GetPathRoot(gameDir), StringComparison.OrdinalIgnoreCase)
            && !stageDir.StartsWith(gameDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        int done = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            string staged = Path.Combine(stageDir, file.Dest);
            string destPath = Path.Combine(gameDir, file.Dest);
            if (!File.Exists(staged))
            {
                
                if (VerifyFile(destPath, file))
                {
                    onProgress?.Invoke(++done, files.Count);
                    continue;
                }
                throw new FileNotFoundException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileMissing, file.Dest), staged);
            }
            if (!GameUpdateDownloader.VerifyMd5(staged, file.Md5))
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
                    onProgress?.Invoke(++done, files.Count);
                    continue;
                }
                
            }
            File.Move(staged, destPath, true);
            onProgress?.Invoke(++done, files.Count);
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
            _logger.LogWarning("硬链接失败（error={Error}），回退为移动 {From} -> {To}",
                System.Runtime.InteropServices.Marshal.GetLastWin32Error(), existingFile, newLink);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "硬链接异常，回退为移动 {From} -> {To}", existingFile, newLink);
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
        _logger.LogInformation("更新前关闭运行中的游戏进程 {Pid}（{Biz}）", running.Id, biz);
        running.Kill(entireProcessTree: true);
        if (!running.WaitForExit(15000))
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_GameRunning);
        }
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    private static async Task ApplyKrdiffGroupsAsync(
        GameBiz biz, string gameDir, string stageDir, DownloadPlan plan,
        IProgress<GameDownloadProgress>? progress, CancellationToken ct)
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
        string hpatchz = LocateHpatchz();
        var patchMd5 = plan.PatchFiles.ToDictionary(f => f.Dest, f => f.Md5, StringComparer.OrdinalIgnoreCase);
        var dstSet = plan.KrdiffDstFiles.Select(f => f.Dest).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var report = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Patching);
        _logger.LogInformation("krpdiff: {Count} 组补丁，最大组 dst {Bytes}，temp {Dir}", total, maxGroupBytes, tempDir);

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            var group = groups[i];
            var dstFiles = group.DstFiles ?? [];
            report(i, total);
            string marker = Path.Combine(tempDir, $"g_{i}");
            if (!File.Exists(marker))
            {
                
                if (dstFiles.Count > 0 && !dstFiles.All(f => VerifyFile(Path.Combine(gameDir, f.Dest), f)))
                {
                    string patchPath = Path.Combine(stageDir, group.Dest);
                    if (!File.Exists(patchPath) || !GameUpdateDownloader.VerifyMd5(patchPath, patchMd5.GetValueOrDefault(group.Dest)))
                    {
                        throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, group.Dest));
                    }
                    await RunHpatchzAsync(hpatchz, gameDir, patchPath, tempDir, ct);
                    foreach (var f in dstFiles)
                    {
                        if (!VerifyFile(Path.Combine(tempDir, f.Dest), f))
                        {
                            throw new InvalidDataException(string.Format(Localization.Lang.Instance.GameUpdate_ErrFileChecksum, f.Dest));
                        }
                        string destPath = Path.Combine(gameDir, f.Dest);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        File.Move(Path.Combine(tempDir, f.Dest), destPath, true);
                    }
                }
                File.WriteAllText(marker, "ok");
                _logger.LogInformation("krpdiff: 组 {Index}/{Total} 落位完成 {Dest}（{Files} 个文件）",
                    i + 1, total, group.Dest, dstFiles.Count);
            }
            try { File.Delete(Path.Combine(stageDir, group.Dest)); } catch { }   
            report(i + 1, total, force: true);
        }

        
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
        try { Directory.Delete(tempDir, true); } catch (Exception ex) { _logger.LogWarning(ex, "krpdiff temp 清理失败 {Dir}", tempDir); }
        foreach (var patch in plan.PatchFiles)
        {
            try { File.Delete(Path.Combine(stageDir, patch.Dest)); } catch { }
        }
        _logger.LogInformation("krpdiff: {Count} 组补丁应用完成", total);
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

    private static async Task RunHpatchzAsync(string exe, string oldDir, string diffFile, string outDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add(oldDir);
        psi.ArgumentList.Add(diffFile);
        psi.ArgumentList.Add(outDir);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_ErrPatchTool);
        try
        {
            await p.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { p.Kill(true); } catch { }
            throw;
        }
        if (p.ExitCode != 0)
        {
            throw new InvalidDataException(string.Format(
                Localization.Lang.Instance.GameUpdate_ErrPatchFailed, Path.GetFileName(diffFile), p.ExitCode));
        }
    }

    
    
    
    
    private static void DeleteStaging(GameBiz biz)
    {
        DeleteStaging(GetGameDir(biz));
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
            _logger.LogWarning(ex, "清理暂存目录失败 launcherDownload\\repair");
        }
    }

    
    
    
    
    
    private static void CleanStagingRoots(GameBiz biz, string? keepVersion)
    {
        CleanStagingRoots(GetGameDir(biz), keepVersion);
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
            foreach (string dir in Directory.EnumerateDirectories(root))
            {
                if (CompareVersion(Path.GetFileName(dir), localVersion) <= 0)
                {
                    _logger.LogInformation("清理孤儿暂存 {Dir}（本地已是 {Local}）", dir, localVersion);
                    Directory.Delete(dir, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "孤儿暂存清理失败 {Biz}", biz);
        }
    }

    private static void CleanStagingRoots(string? gameDir, string? keepVersion)
    {
        if (gameDir is null)
        {
            return;
        }
        string root = Path.Combine(gameDir, "predownload");
        try
        {
            if (!Directory.Exists(root))
            {
                return;
            }
            foreach (string dir in Directory.EnumerateDirectories(root))
            {
                if (keepVersion is not null
                    && string.Equals(Path.GetFileName(dir), keepVersion, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                Directory.Delete(dir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理暂存目录失败 {Path}", root);
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
            _logger.LogWarning(ex, "写 LocalGameResources.json 失败");
        }
    }


    #region 全新安装（本地没有任何版本记录时，把最新版全量下载并落位）

    
    
    
    
    
    public static async Task<GameUpdateState?> GetInstallStateAsync(GameBiz biz)
    {
        if (GetLocalVersion(biz) is not null)
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

    
    public sealed record GameTierOption(string Name, long SizeBytes);

    
    
    
    
    public static async Task<List<GameTierOption>?> GetTierOptionsAsync(GameBiz biz)
    {
        var index = await GetTierIndexAsync(biz);
        if (index?.ResourcePacks is not { } packs || !packs.TryGetValue("common", out var common)
            || index.Bundles is not { Count: > 0 } bundles)
        {
            return null;
        }
        return bundles.Keys
            .Where(k => packs.ContainsKey(k))
            .OrderByDescending(k => packs[k].Size)
            .Select(k => new GameTierOption(k, common.Size + packs[k].Size))
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

    
    public static string? GetInstallTier(GameBiz biz)
    {
        string v = AppConfig.GetConfig($"InstallTier_{biz}") ?? "";
        return v.Length == 0 ? null : v;
    }

    public static void SetInstallTier(GameBiz biz, string? tier)
    {
        AppConfig.SetConfig($"InstallTier_{biz}", tier ?? "");
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
        string stageDir = Path.Combine(installDir, "predownload", target.Version!);
        Directory.CreateDirectory(stageDir);

        long downloaded = await PrepareStagingAsync(biz, plan, stageDir, installDir, progress, ct);

        var allFiles = plan.AllFiles;
        var reportApplying = MakeThrottledReporter(progress, plan.TotalBytes, DownloadPhase.Applying);
        reportApplying(0, allFiles.Count, force: true);
        
        bool keepStaging = await Task.Run(() => ApplyStagedToGame(installDir, stageDir, allFiles, ct,
            (done, total) => reportApplying(done, total)), ct);

        
        
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
            _logger.LogInformation("Install: 已回填游戏 exe {Exe}", exe);
        }
        else
        {
            _logger.LogWarning("Install: 安装目录里没找到游戏 exe {Biz}，请到设置里手动定位", biz);
        }

        WriteLocalVersion(installDir, target.Version!);
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
            _logger.LogWarning(ex, "Install: 遍历安装目录找 exe 失败 {Dir}", installDir);
            return null;
        }
    }

    #endregion

    #endregion


    #region 校验 / 修复游戏

    
    public sealed class VerifyResult
    {
        
        public List<GameResourceEntry> Damaged { get; } = new();

        
        public int TotalFiles { get; init; }

        
        public int CheckedFiles { get; init; }

        
        public long DamagedBytes { get; set; }

        
        public string? Version { get; init; }

        
        public bool CanRepair { get; init; }

        
        public string? BaseUrl { get; init; }
    }

    
    
    
    
    
    
    
    public static async Task<VerifyResult?> VerifyAsync(
        GameBiz biz, bool quick, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        string? gameDir = GetGameDir(biz);
        string? local = GetLocalVersion(biz);
        if (gameDir is null || local is null)
        {
            return null;
        }

        var index = await GetIndexAsync(biz);
        string? baseUrl = null;
        List<GameResourceEntry>? files = null;
        bool canRepair = false;

        
        
        if (index?.Default != null && CompareVersion(local, index.Default.Version) == 0)
        {
            var cfg = index.Default.Config;
            if (cfg?.IndexFile != null)
            {
                var list = await FetchResourceListAsync(biz, cfg.IndexFile);
                if (list != null)
                {
                    files = list.Resource.Where(f => !string.IsNullOrWhiteSpace(f.Dest)).ToList();
                    baseUrl = cfg.BaseUrl;
                    canRepair = !string.IsNullOrEmpty(baseUrl);
                }
            }
        }

        
        if (files is null)
        {
            files = ReadLocalGameResources(gameDir);
            if (files is null)
            {
                string origin = Path.Combine(gameDir, "launcherDownload", local, "OriginResource.json");
                if (File.Exists(origin))
                {
                    var parsed = JsonSerializer.Deserialize<GameResourceList>(File.ReadAllText(origin), JsonOptions);
                    files = parsed?.Resource;
                }
            }
        }

        
        if (files is null)
        {
            return null;
        }

        
        List<GameResourceEntry> toCheck = files;
        if (quick && index?.KeyFileCheckList is { } keyFiles)
        {
            var keySet = keyFiles.Select(NormalizeDest).ToHashSet(StringComparer.OrdinalIgnoreCase);
            toCheck = files.Where(f => keySet.Contains(NormalizeDest(f.Dest))).ToList();
        }

        var result = new VerifyResult
        {
            Version = local,
            CanRepair = canRepair,
            BaseUrl = baseUrl,
            TotalFiles = files.Count,
            CheckedFiles = toCheck.Count,
        };

        var throttle = new Stopwatch();
        throttle.Start();
        int completed = 0;
        using var limiter = new SemaphoreSlim(8);
        var tasks = toCheck.Select(file => Task.Run(async () =>
        {
            await limiter.WaitAsync(ct);
            try
            {
                if (!VerifyFile(Path.Combine(gameDir, file.Dest), file))
                {
                    lock (result.Damaged)
                    {
                        result.Damaged.Add(file);
                        result.DamagedBytes += file.Size;
                    }
                }
                int done = Interlocked.Increment(ref completed);
                ReportVerify(progress, throttle, done, toCheck.Count, file.Dest);
            }
            finally
            {
                limiter.Release();
            }
        }, ct));
        await Task.WhenAll(tasks);
        return result;
    }

    private static void ReportVerify(IProgress<GameDownloadProgress>? progress, Stopwatch throttle, int done, int total, string currentFile)
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
        });
    }

    
    
    
    
    
    public static async Task<long> RepairAsync(
        GameBiz biz, VerifyResult result, IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        if (!result.CanRepair || result.BaseUrl is null)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.GameUpdate_RepairNoBaseUrl);
        }
        string? gameDir = GetGameDir(biz);
        if (gameDir is null || result.Damaged.Count == 0)
        {
            return 0;
        }
        EnsureGameNotRunning(biz);
        string cdn = await GetDownloadCdnAsync(biz);
        var downloader = new GameUpdateDownloader(cdn, result.BaseUrl, gameDir, result.Damaged);
        long downloaded = await downloader.DownloadAsync(progress, ct);

        
        var index = await GetIndexAsync(biz);
        if (index?.Default?.Config?.IndexFile is { } manifestPath)
        {
            var fullList = await FetchResourceListAsync(biz, manifestPath);
            if (fullList != null)
            {
                WriteLocalGameResources(gameDir, fullList);
            }
        }
        return downloaded;
    }

    
    internal static bool VerifyFile(string path, GameResourceEntry entry)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        if (new FileInfo(path).Length != entry.Size)
        {
            return false;
        }
        return GameUpdateDownloader.VerifyMd5(path, entry.Md5);
    }

    
    private static string NormalizeDest(string dest) =>
        dest.Replace('\\', '/').Trim('/');

    #endregion
}
