using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Helpers;

namespace Arietta.Features.GameLauncher;

































public static class GameNoticeService
{
    
    private enum NoticePathKind
    {
        
        Legacy,
        
        AppWithServer,
    }

    private sealed record BizConfig(
        string[] Bases,
        string AppIdKey,
        string GameId,
        string? ServerCode,
        NoticePathKind PathKind,
        bool HasAnnouncement,
        string[]? Languages = null,
        BizConfig? Fallback = null)
    {
        
        
        
        
        public string[] SupportedLanguages => Languages is { Length: > 0 } ? Languages : OnlyZhHans;
    }

    
    private static readonly string[] CnBases =
    [
        "https://prod-cn-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.com",
        "https://prod-tencentcdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.xyz",
        "https://prod-alicdn-gamestarter.kurogame.com",
    ];

    
    private static readonly string[] GlobalBases =
    [
        "https://prod-alicdn-gamestarter.kurogame.com",
    ];

    
    
    
    
    private static readonly string[] OverseasPgrBases =
    [
        "https://prod-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.net",
    ];

    
    
    
    
    
    
    
    private static readonly string[] OnlyZhHans = ["zh-Hans"];

    
    private static readonly string[] GlobalLanguages =
    ["zh-Hans", "zh-Hant", "en", "ja", "ko", "fr", "de", "es", "th", "pt-BR"];

    private static readonly Dictionary<GameBiz, BizConfig> Configs = new()
    {
        [GameBiz.WutheringWaves] = new(CnBases,
            "10003_Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5", "G152", null, NoticePathKind.Legacy, true,
            OnlyZhHans),
        
        
        [GameBiz.WutheringWavesBilibili] = new(CnBases,
            "10004_j5GWFuUFlb8N31Wi2uS3ZAVHcb7ZGN7y", "G152", null, NoticePathKind.Legacy, false,
            OnlyZhHans),
        
        
        
        
        [GameBiz.WutheringWavesGlobal] = new(GlobalBases,
            "50004_P7xcUZnEr1AXIGON25E6KjpOgTlVrg6e", "G153", "official", NoticePathKind.AppWithServer, true,
            GlobalLanguages,
            new(GlobalBases, "50004_obOHXFrFanqsaIEOmuKroCcbZkQRBC7c", "G153", null, NoticePathKind.Legacy, true,
                GlobalLanguages)),
        [GameBiz.PunishingGrayRaven] = new(CnBases,
            "10012_RnIUKs3r59Csliu3N0rl5uRWWBOFDaJL", "G148", null, NoticePathKind.Legacy, true,
            OnlyZhHans),
        
        
        
        
        [GameBiz.PunishingGrayRavenGlobal] = new(OverseasPgrBases,
            "50015_LWdk9D2Ep9mpJmqBZZkcPBU2YNraEWBQ", "G143", null, NoticePathKind.Legacy, true,
            ["en"]),
        [GameBiz.PunishingGrayRavenJapan] = new(OverseasPgrBases,
            "50007_NxWGZ0d254oWqZKuuL6szOK7WRLPt668", "G282", null, NoticePathKind.Legacy, true,
            ["ja"]),
        [GameBiz.PunishingGrayRavenKorea] = new(OverseasPgrBases,
            "50011_XefwDdpgPxxLABoTOD0yuqTFBC3koJZ0", "G286", null, NoticePathKind.Legacy, true,
            ["ko"]),
        [GameBiz.PunishingGrayRavenTaiwan] = new(OverseasPgrBases,
            "50016_i2n5NLmdCAmOGP3J1tJOlWKNSMQuyWL7", "G279", null, NoticePathKind.Legacy, true,
            ["zh-Hant"]),
    };

    private static readonly HttpClient Client = SharedHttp.Notice;

    private static readonly Microsoft.Extensions.Logging.ILogger _logger = AppConfig.GetLogger<LogTag>();

    
    private sealed class LogTag;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    
    private static readonly Dictionary<GameBiz, (List<GameBannerItem> Banners, List<GamePostGroup> Groups, string Lang, DateTimeOffset Time)> _cache = new();


    
    public static void InvalidateCache()
    {
        _cache.Clear();
    }


    
    public static string? LastError { get; private set; }

    
    public static NoticeFailureKind LastFailure { get; private set; }


    
    public static async Task<(List<GameBannerItem> Banners, List<GamePostGroup> Groups)?> GetAsync(GameBiz biz)
    {
        LastFailure = NoticeFailureKind.None;
        
        if (!Configs.TryGetValue(biz, out var cfg) && !Configs.TryGetValue(biz.GetBaseBiz(), out cfg))
        {
            LastError = $"{biz} 未配置通知接口";
            LastFailure = NoticeFailureKind.Error;
            _logger.LogWarning("通知接口未配置：{Biz}", biz);
            return null;
        }

        
        if (!cfg.HasAnnouncement)
        {
            LastError = null;
            LastFailure = NoticeFailureKind.NotSupported;
            _logger.LogInformation("该渠道无公告内容，跳过加载：{Biz}", biz);
            return null;
        }

        
        
        
        
        string lang = ResolveLanguage(cfg);

        if (_cache.TryGetValue(biz, out var cached)
            && DateTimeOffset.Now - cached.Time < CacheLifetime
            && string.Equals(cached.Lang, lang, StringComparison.OrdinalIgnoreCase))
        {
            return (cached.Banners, cached.Groups);
        }

        
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            LastError = null;
            LastFailure = NoticeFailureKind.Offline;
            _logger.LogInformation("当前无网络连接，跳过公告加载：{Biz}", biz);
            return _cache.TryGetValue(biz, out var offline) ? (offline.Banners, offline.Groups) : null;
        }

        var failures = new List<string>();
        var result = await TryFetchAsync(cfg, lang, failures);
        
        if (result is null && cfg.Fallback is { } fb)
        {
            result = await TryFetchAsync(fb, lang, failures);
        }

        if (result is not null)
        {
            LastError = null;
            _cache[biz] = (result.Value.Banners, result.Value.Groups, lang, DateTimeOffset.Now);
            return result;
        }

        
        
        LastError = string.Join(" | ", failures);
        LastFailure = NoticeFailureKind.Error;
        _logger.LogWarning("公告全部 CDN 加载失败：{Biz} lang={Lang} {Detail}", biz, lang, LastError);
        return _cache.TryGetValue(biz, out var old) ? (old.Banners, old.Groups) : null;
    }


    
    private static string ResolveLanguage(BizConfig cfg)
    {
        string want = AnnouncementLanguageSettings.EffectiveLanguage;
        foreach (string supported in cfg.SupportedLanguages)
        {
            if (string.Equals(supported, want, StringComparison.OrdinalIgnoreCase))
            {
                return supported;
            }
        }
        return cfg.SupportedLanguages[0];
    }


    
    private static async Task<(List<GameBannerItem> Banners, List<GamePostGroup> Groups)?> TryFetchAsync(
        BizConfig cfg, string lang, List<string> failures)
    {
        foreach (string baseAddress in cfg.Bases)
        {
            string path = cfg.PathKind == NoticePathKind.AppWithServer
                ? $"launcher/app/{cfg.AppIdKey}/{cfg.GameId}/{cfg.ServerCode}/information/{lang}.json"
                : $"launcher/{cfg.AppIdKey}/{cfg.GameId}/information/{lang}.json";
            string url = $"{baseAddress}/{path}?_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            string tag = $"{Host(baseAddress)}/{cfg.AppIdKey}";
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(6));
                using var response = await Client.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"{tag}:HTTP{(int)response.StatusCode}");
                    continue;
                }
                await using var stream = await response.Content.ReadAsStreamAsync();
                var data = await JsonSerializer.DeserializeAsync<LauncherInformation>(stream, JsonOptions);
                if (data is null)
                {
                    failures.Add($"{tag}:解析为空");
                    continue;
                }

                var banners = (data.Slideshow ?? []).Select(s => new GameBannerItem
                {
                    Image = ParseUri(s.Url),
                    Link = s.JumpUrl,
                }).Where(b => b.Image is not null).ToList();

                var groups = new List<GamePostGroup>();
                AddGroup(groups, Lang_GameActivity, data.Guidance?.Activity);
                AddGroup(groups, Lang_GameNotice, data.Guidance?.Notice);
                AddGroup(groups, Lang_GameNews, data.Guidance?.News);

                if (banners.Count == 0 && groups.Count == 0)
                {
                    failures.Add($"{tag}:内容为空");
                    continue;
                }

                return (banners, groups);
            }
            catch (Exception ex)
            {
                failures.Add($"{tag}:{ex.GetType().Name} {ex.Message}");
            }
        }
        return null;
    }


    
    public enum NoticeFailureKind
    {
        None,
        
        Offline,
        
        Error,
        
        NotSupported,
    }


    private static string Host(string baseAddress)
    {
        try { return new Uri(baseAddress).Host; }
        catch { return baseAddress; }
    }


    private static void AddGroup(List<GamePostGroup> groups, string fallbackTitle, GuidanceSection? section)
    {
        if (section?.Contents is not { Count: > 0 })
        {
            return;
        }
        groups.Add(new GamePostGroup
        {
            Header = string.IsNullOrWhiteSpace(section.Title) ? fallbackTitle : section.Title!,
            List = section.Contents.Select(c => new GamePost
            {
                Title = c.Content ?? "",
                Date = c.Time ?? "",
                Link = c.JumpUrl,
            }).ToList(),
        });
    }


    private static Uri? ParseUri(string? url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;


    private static string Lang_GameActivity => Arietta.Localization.Lang.Instance.Notice_Activity;
    private static string Lang_GameNotice => Arietta.Localization.Lang.Instance.Notice_NoticeTab;
    private static string Lang_GameNews => Arietta.Localization.Lang.Instance.Notice_News;


    #region 接口 JSON 结构（camelCase 原样）

    public sealed class LauncherInformation
    {
        [JsonPropertyName("guidance")]
        public GuidanceRoot? Guidance { get; set; }

        [JsonPropertyName("slideshow")]
        public List<SlideshowItem>? Slideshow { get; set; }
    }

    public sealed class GuidanceRoot
    {
        [JsonPropertyName("activity")]
        public GuidanceSection? Activity { get; set; }

        [JsonPropertyName("notice")]
        public GuidanceSection? Notice { get; set; }

        [JsonPropertyName("news")]
        public GuidanceSection? News { get; set; }
    }

    public sealed class GuidanceSection
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("contents")]
        public List<ContentItem>? Contents { get; set; }
    }

    public sealed class ContentItem
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("jumpUrl")]
        public string? JumpUrl { get; set; }

        [JsonPropertyName("time")]
        public string? Time { get; set; }
    }

    public sealed class SlideshowItem
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("jumpUrl")]
        public string? JumpUrl { get; set; }
    }

    #endregion
}
