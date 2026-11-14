using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Arietta.Features.DynamicBackground;

public enum DynamicBackgroundUpdateKind
{

    NotSupported,

    Disabled,

    Skipped,

    UpToDate,

    Updated,

    Failed,
}

public sealed class DynamicBackgroundUpdateResult
{
    public DynamicBackgroundUpdateKind Kind { get; init; }
    public string Message { get; init; } = "";
    public long Size { get; init; }
}

public sealed class DynamicBackgroundLocal
{
    public string VideoPath { get; init; } = "";
    public string? PosterPath { get; init; }

    public string? Source { get; init; }
    public string? BackgroundId { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public long Size { get; init; }
}

public static class DynamicBackgroundStore
{

    public const string VideoFileName = "bg.mp4";
    public const string PosterFileName = "poster.webp";
    public const string MetaFileName = "meta.json";

    public const string SloganFileName = "slogan.png";

    public const int AutoUpdateCooldownHours = 6;

    private const string LastCheckKeyVersion = "v3";

    public const string AutoUpdateConfigKey = "DynBgAutoUpdate";

    public const string VolumeConfigKey = "DynBgVolume";

    public const string FpsCapConfigKey = "DynBgFpsCap";

    public const string QualityConfigKey = "DynBgQuality";

    public const string PauseOnGameConfigKey = "DynBgPauseOnGame";

    public static event EventHandler<GameBiz>? Updated;

    private static Microsoft.Extensions.Logging.ILogger Log => AppConfig.GetLogger("DynamicBackgroundStore");


    public static bool IsAutoUpdateEnabled => AppConfig.GetConfig(AutoUpdateConfigKey) != "0";

    public static string GetLastCheckConfigKey(GameBiz biz) =>
        $"DynBgLastCheck_{LastCheckKeyVersion}_{biz.GetBaseBiz()}";

    public static string GetFolder(GameBiz biz) =>
        Path.Combine(AppConfig.DynamicBackgroundFolder, biz.GetBaseBiz().ToString());

    public static string? GetSloganPath(GameBiz biz)
    {
        try
        {
            string folder = GetFolder(biz);
            string lang = SloganLangCode(biz);
            if (lang.Length > 0 && File.Exists(Path.Combine(folder, $"slogan-{lang}.png")))
            {
                return Path.Combine(folder, $"slogan-{lang}.png");
            }
            string fallback = Path.Combine(folder, SloganFileName);
            return File.Exists(fallback) ? fallback : null;
        }
        catch
        {
            return null;
        }
    }

    private static string SloganLangCode(GameBiz biz) => biz switch
    {
        GameBiz.PunishingGrayRavenTaiwan => "zh-Hant",
        GameBiz.PunishingGrayRavenJapan => "ja",
        GameBiz.PunishingGrayRavenKorea => "ko",
        GameBiz.PunishingGrayRavenGlobal => "en",
        GameBiz.WutheringWavesGlobal => UiLangCode(UiLanguage.Current),
        _ => "",
    };

    private static string UiLangCode(UiLanguageKind kind) => kind switch
    {
        UiLanguageKind.TraditionalChinese => "zh-Hant",
        UiLanguageKind.English => "en",
        UiLanguageKind.Japanese => "ja",
        UiLanguageKind.Korean => "ko",
        UiLanguageKind.French => "fr",
        UiLanguageKind.German => "de",
        UiLanguageKind.Spanish => "es",
        UiLanguageKind.Thai => "th",
        UiLanguageKind.PortugueseBrazil => "pt-BR",
        _ => "",
    };

    public static bool IsOwnedFile(string? fileName)
    {
        string name = Path.GetFileName(fileName ?? "");
        return name.Equals(VideoFileName, StringComparison.OrdinalIgnoreCase)
            || name.Equals(PosterFileName, StringComparison.OrdinalIgnoreCase)
            || name.Equals(MetaFileName, StringComparison.OrdinalIgnoreCase)

            || Regex.IsMatch(name, @"^slogan(-[a-z0-9-]+)?\.png$", RegexOptions.IgnoreCase)

            || Regex.IsMatch(name, @"^bg-.+-\d{10}\.mp4$", RegexOptions.IgnoreCase);
    }

    public static DynamicBackgroundLocal? GetLocal(GameBiz biz)
    {
        try
        {
            string folder = GetFolder(biz);
            Meta? meta = ReadMeta(folder);
            string videoPath = Path.Combine(folder, ResolveVideoFile(folder, meta?.File));
            if (!File.Exists(videoPath))
            {
                return null;
            }
            var video = new FileInfo(videoPath);
            if (video.Length == 0)
            {
                return null;
            }

            return new DynamicBackgroundLocal
            {
                VideoPath = videoPath,
                PosterPath = Existing(Path.Combine(folder, PosterFileName)),
                Source = meta?.Source,
                BackgroundId = meta?.BackgroundId,
                UpdatedAt = ParseTime(meta?.UpdatedAt, video.LastWriteTime),
                Size = meta?.Size is > 0 ? meta!.Size : video.Length,
            };
        }
        catch
        {
            return null;
        }
    }

    private static Meta? ReadMeta(string folder)
    {
        try
        {
            string metaPath = Path.Combine(folder, MetaFileName);
            return File.Exists(metaPath)
                ? JsonSerializer.Deserialize<Meta>(File.ReadAllText(metaPath))
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveVideoFile(string folder, string? file)
    {
        if (!string.IsNullOrEmpty(file) && File.Exists(Path.Combine(folder, file)))
        {
            return file!;
        }
        return File.Exists(Path.Combine(folder, VideoFileName)) ? VideoFileName : (file ?? VideoFileName);
    }

    private static DateTimeOffset ParseTime(string? raw, DateTimeOffset fallback)
    {
        return DateTimeOffset.TryParse(
            raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var value)
            ? value
            : fallback;
    }

    public static async Task<DynamicBackgroundUpdateResult> CheckAndUpdateAsync(
        GameBiz biz,
        bool force,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        if (!KuroLauncherBackgroundClient.CanFetch(biz))
        {
            return new DynamicBackgroundUpdateResult { Kind = DynamicBackgroundUpdateKind.NotSupported };
        }
        if (!force && !IsAutoUpdateEnabled)
        {
            return new DynamicBackgroundUpdateResult { Kind = DynamicBackgroundUpdateKind.Disabled };
        }
        if (!force && IsCoolingDown(biz))
        {

            Log.LogInformation("Kuro background {Biz} skipped (cooldown {Hours}h)", biz, AutoUpdateCooldownHours);
            return new DynamicBackgroundUpdateResult { Kind = DynamicBackgroundUpdateKind.Skipped };
        }

        try
        {
            var info = await KuroLauncherBackgroundClient.GetBackgroundAsync(biz, ct);
            if (info?.BackgroundFile is not { Length: > 0 })
            {
                return new DynamicBackgroundUpdateResult
                {
                    Kind = DynamicBackgroundUpdateKind.Failed,
                    Message = "API returned no background URL",
                };
            }
            if (!info.IsVideo)
            {

                return new DynamicBackgroundUpdateResult
                {
                    Kind = DynamicBackgroundUpdateKind.Failed,
                    Message = $"Unsupported background file type {info.BackgroundFileType}",
                };
            }

            var local = GetLocal(biz);
            if (!force && local is not null &&
                string.Equals(local.Source, info.BackgroundFile, StringComparison.OrdinalIgnoreCase))
            {

                MarkChecked(biz);
                await TrySyncSlogansAsync(biz, info, versionChanged: false, ct);
                Log.LogInformation("Kuro background {Biz} up to date ({Size} bytes)", biz, local.Size);
                return new DynamicBackgroundUpdateResult
                {
                    Kind = DynamicBackgroundUpdateKind.UpToDate,
                    Size = local.Size,
                    Message = info.BackgroundId ?? "",
                };
            }

            string folder = GetFolder(biz);
            Directory.CreateDirectory(folder);

            string destFile = $"bg-{info.BackgroundId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.mp4";
            long size = await KuroLauncherBackgroundClient.DownloadAsync(
                info.BackgroundFile!, Path.Combine(folder, destFile), progress, ct);

            await KuroLauncherBackgroundClient.TryDownloadAsync(
                info.FirstFrameImage, Path.Combine(folder, PosterFileName), ct);

            WriteMeta(folder, info, size, destFile);
            MarkChecked(biz);
            Log.LogInformation("Kuro background {Biz} updated: {Size} bytes, id={Id}", biz, size, info.BackgroundId);

            await TrySyncSlogansAsync(biz, info, versionChanged: true, ct);
            Updated?.Invoke(null, biz);

            CleanupOldVideos(folder, destFile);

            return new DynamicBackgroundUpdateResult
            {
                Kind = DynamicBackgroundUpdateKind.Updated,
                Size = size,
                Message = info.BackgroundId ?? "",
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Kuro background {Biz} update failed", biz);
            return new DynamicBackgroundUpdateResult
            {
                Kind = DynamicBackgroundUpdateKind.Failed,
                Message = ex.Message,
            };
        }
    }

    private static async Task TrySyncSlogansAsync(GameBiz biz, KuroBackgroundInfo info, bool versionChanged, CancellationToken ct)
    {
        try
        {
            string folder = GetFolder(biz);
            Directory.CreateDirectory(folder);

            string zhPath = Path.Combine(folder, SloganFileName);
            if (versionChanged || !File.Exists(zhPath))
            {
                await KuroLauncherBackgroundClient.TryDownloadAsync(info.Slogan, zhPath, ct);
            }

            if (versionChanged)
            {

                foreach (string stale in Directory.GetFiles(folder, "slogan-*.png"))
                {
                    try { File.Delete(stale); }
                    catch { }
                }
            }

            HashSet<string> missing = KuroLauncherBackgroundClient.SloganLanguagesFor(biz)
                .Where(lang => !File.Exists(Path.Combine(folder, $"slogan-{lang}.png")))
                .ToHashSet();
            if (missing.Count == 0)
            {
                return;
            }

            foreach (var set in await KuroLauncherBackgroundClient.GetSloganSetsAsync(biz, ct))
            {
                HashSet<string> langs = missing.Where(set.Slogans.ContainsKey).ToHashSet();
                if (langs.Count == 0)
                {
                    continue;
                }
                if (!await PosterMatchesAsync(folder, set.PosterUrl, ct))
                {
                    Log.LogInformation("Kuro slogan set skipped for {Biz}: {Langs} (background differs from local)",
                        biz, string.Join(',', langs));
                    continue;
                }
                foreach (string lang in langs)
                {
                    await KuroLauncherBackgroundClient.TryDownloadAsync(
                        set.Slogans[lang], Path.Combine(folder, $"slogan-{lang}.png"), ct);
                    missing.Remove(lang);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Kuro background slogans sync failed for {Biz}", biz);
        }
    }

    private static async Task<bool> PosterMatchesAsync(string folder, string remotePosterUrl, CancellationToken ct)
    {
        try
        {
            string poster = Path.Combine(folder, PosterFileName);
            if (!File.Exists(poster))
            {
                return false;
            }
            byte[] local = await File.ReadAllBytesAsync(poster, ct);
            byte[]? remote = await KuroLauncherBackgroundClient.TryGetBytesAsync(remotePosterUrl, ct);
            if (remote is null)
            {
                return false;
            }
            if (local.SequenceEqual(remote))
            {
                return true;
            }
            double diff = await BitmapDiffAsync(local, remote);
            return diff < 10;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<double> BitmapDiffAsync(byte[] a, byte[] b)
    {
        byte[]? ga = await DecodeGrayThumbAsync(a);
        byte[]? gb = await DecodeGrayThumbAsync(b);
        if (ga is null || gb is null || ga.Length != gb.Length)
        {
            return double.MaxValue;
        }
        double sum = 0;
        for (int i = 0; i < ga.Length; i++)
        {
            sum += Math.Abs(ga[i] - gb[i]);
        }
        return sum / ga.Length;
    }

    private static async Task<byte[]?> DecodeGrayThumbAsync(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes).AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var data = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                new BitmapTransform { ScaledWidth = 32, ScaledHeight = 32, InterpolationMode = BitmapInterpolationMode.Fant },
                ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            byte[] px = data.DetachPixelData();
            var gray = new byte[32 * 32];
            for (int i = 0; i < gray.Length; i++)
            {
                gray[i] = (byte)((px[i * 4 + 2] * 299 + px[i * 4 + 1] * 587 + px[i * 4] * 114) / 1000);
            }
            return gray;
        }
        catch
        {
            return null;
        }
    }

    public static async Task SyncAllAsync(GameBiz activeBiz, bool force = false, CancellationToken ct = default)
    {
        if (!force && !IsAutoUpdateEnabled)
        {
            return;
        }
        MigrateLegacyFolders();

        GameBiz first = activeBiz.GetBaseBiz();
        GameBiz second = first == GameBiz.WutheringWaves ? GameBiz.PunishingGrayRaven : GameBiz.WutheringWaves;
        if (first == GameBiz.External)
        {
            first = GameBiz.WutheringWaves;
            second = GameBiz.PunishingGrayRaven;
        }
        foreach (GameBiz biz in new[] { first, second })
        {
            if (!force && !GameRegistry.IsDynamicBgEnabled(biz))
            {
                continue;
            }
            try
            {
                await CheckAndUpdateAsync(biz, force, null, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {

                Log.LogWarning(ex, "Kuro background sync failed for {Biz}", biz);
            }
        }
    }

    private static void MigrateLegacyFolders()
    {
        try
        {
            string old = Path.Combine(AppConfig.LocalFolder, "DynamicBg");
            if (!Directory.Exists(old))
            {
                return;
            }
            foreach (GameBiz biz in new[] { GameBiz.WutheringWaves, GameBiz.PunishingGrayRaven })
            {
                string from = Path.Combine(old, biz.ToString());
                string to = GetFolder(biz);
                if (Directory.Exists(from) && !File.Exists(Path.Combine(to, VideoFileName)))
                {
                    Directory.CreateDirectory(AppConfig.DynamicBackgroundFolder);
                    Directory.Move(from, to);
                }
            }
            Directory.Delete(old, true);
            Log.LogInformation("Migrated legacy DynamicBg folders to {Folder}", AppConfig.DynamicBackgroundFolder);
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Migrate legacy DynamicBg folders failed");
        }
    }

    public static void Clear(GameBiz biz)
    {
        try
        {
            string folder = GetFolder(biz);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
        catch { }
        AppConfig.SetConfig(GetLastCheckConfigKey(biz), null);
    }


    private static bool IsCoolingDown(GameBiz biz)
    {
        string? raw = AppConfig.GetConfig(GetLastCheckConfigKey(biz));
        if (!long.TryParse(raw, out long seconds))
        {
            return false;
        }
        var last = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return DateTimeOffset.UtcNow - last < TimeSpan.FromHours(AutoUpdateCooldownHours);
    }


    private static void MarkChecked(GameBiz biz)
    {
        AppConfig.SetConfig(GetLastCheckConfigKey(biz), DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
    }


    private static string? Existing(string path) => File.Exists(path) ? path : null;

    private static void CleanupOldVideos(string folder, string keepFile)
    {
        try
        {
            foreach (string file in Directory.GetFiles(folder, "bg*.mp4*"))
            {
                if (!string.Equals(Path.GetFileName(file), keepFile, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
        }
        catch { }
    }


    private static void WriteMeta(string folder, KuroBackgroundInfo info, long size, string videoFile)
    {
        try
        {
            var meta = new Meta
            {
                Source = info.BackgroundFile,
                BackgroundId = info.BackgroundId,
                File = videoFile,
                UpdatedAt = DateTimeOffset.Now.ToString("o"),
                Size = size,
            };
            File.WriteAllText(
                Path.Combine(folder, MetaFileName),
                JsonSerializer.Serialize(meta, JsonOpts.Indented));
        }
        catch { }
    }


    private sealed class Meta
    {
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("backgroundId")] public string? BackgroundId { get; set; }
        [JsonPropertyName("file")] public string? File { get; set; }
        [JsonPropertyName("updatedAt")] public string? UpdatedAt { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
    }

}
