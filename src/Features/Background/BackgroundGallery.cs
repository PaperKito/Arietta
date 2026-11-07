using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Arietta.Features.Background;

public enum BackgroundKind
{
    Image,
    Video,
}

public sealed class BackgroundItem
{

    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string BadgeGlyph { get; set; } = "";

    public Visibility BadgeVisibility { get; set; } = Visibility.Collapsed;

    public Visibility PlaceholderVisibility => Thumbnail is null ? Visibility.Visible : Visibility.Collapsed;

    public string PlaceholderGlyph => IsVideo ? "\uE768" : "\uE91B";

    public string? FilePath { get; set; }

    public BackgroundKind Kind { get; set; }

    public bool IsOfficial { get; set; }

    public ImageSource? Thumbnail { get; set; }

    public bool IsVideo => Kind == BackgroundKind.Video;
}

public static class BackgroundGallery
{
    public const string OfficialId = "official";

    public const string OfficialStaticId = "official_static";

    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif" };
    private static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".webm" };

    public static event EventHandler<GameBiz>? SelectionChanged;

    public static bool IsVideoPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        return VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
    }

    public static bool IsSupportedFile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }
        string ext = Path.GetExtension(name).ToLowerInvariant();
        return ImageExtensions.Contains(ext) || VideoExtensions.Contains(ext);
    }

    public static string Folder => AppConfig.BackgroundFolder;

    public static string GetFolder(GameBiz biz) => Path.Combine(AppConfig.BackgroundFolder, biz.ToString());

    public static List<BackgroundItem> GetItems(GameBiz biz)
    {
        var items = new List<BackgroundItem>();

        var local = GameRegistry.IsDynamicBgEnabled(biz) ? DynamicBackground.DynamicBackgroundStore.GetLocal(biz) : null;
        if (local is not null)
        {
            items.Add(new BackgroundItem
            {
                Id = OfficialId,
                Name = Lang.Instance.Background_OfficialShort,
                Description = Lang.Instance.Background_Official,
                BadgeGlyph = "\uE768",
                BadgeVisibility = Visibility.Visible,
                FilePath = local.VideoPath,
                Kind = IsVideoPath(local.VideoPath) ? BackgroundKind.Video : BackgroundKind.Image,
                IsOfficial = true,
                Thumbnail = BuildThumbnail(local.PosterPath ?? local.VideoPath),
            });

            if (local.PosterPath is { Length: > 0 })
            {
                items.Add(new BackgroundItem
                {
                    Id = OfficialStaticId,
                    Name = Lang.Instance.Background_OfficialShort,
                    Description = Lang.Instance.Background_OfficialStatic,
                    BadgeGlyph = "\uE91B",
                    BadgeVisibility = Visibility.Visible,
                    FilePath = local.PosterPath,
                    Kind = BackgroundKind.Image,
                    IsOfficial = true,
                    Thumbnail = BuildThumbnail(local.PosterPath),
                });
            }
        }

        if (GameRegistry.IsCustomBgEnabled(biz))
        {
            foreach (string file in ListBackgroundFiles(biz))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                items.Add(new BackgroundItem
                {
                    Id = Path.GetFileName(file),
                    Name = name,
                    Description = name,
                    FilePath = file,
                    Kind = IsVideoPath(file) ? BackgroundKind.Video : BackgroundKind.Image,
                    Thumbnail = BuildThumbnail(file),
                });
            }
        }

        return items;
    }

    public static BackgroundItem? Resolve(GameBiz biz)
    {
        var items = GetItems(biz);
        string? id = GetSelectedId(biz);
        if (id is { Length: > 0 })
        {
            if (items.FirstOrDefault(x => x.Id == id) is { } picked)
            {
                return picked;
            }

            if (id.Equals("poster.webp", StringComparison.OrdinalIgnoreCase)
                && items.FirstOrDefault(x => x.Id == OfficialStaticId) is { } legacyStatic)
            {
                return legacyStatic;
            }
        }
        else
        {

            string? legacyCustom = GameRegistry.GetCustomBgPath(biz);
            if (legacyCustom is { Length: > 0 })
            {
                string name = Path.GetFileName(legacyCustom);
                if (items.FirstOrDefault(x => x.Id == name) is { } legacy)
                {
                    return legacy;
                }
            }
        }

        return items.FirstOrDefault(x => x.Id == OfficialId);
    }

    public static string? GetSelectedId(GameBiz biz)
    {
        string? id = AppConfig.GetConfig(GameRegistry.GetSelectedBgConfigKey(biz));
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    public static void SetSelectedId(GameBiz biz, string? id, bool notify = true)
    {
        AppConfig.SetConfig(GameRegistry.GetSelectedBgConfigKey(biz), string.IsNullOrWhiteSpace(id) ? null : id);
        if (notify)
        {
            SelectionChanged?.Invoke(null, biz);
        }
    }

    public static async Task<string> AddFileAsync(string sourcePath, GameBiz biz)
    {
        string name = Path.GetFileName(sourcePath);
        string folder = GetFolder(biz);
        string target = Path.Combine(folder, name);
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }
        await Task.Run(() =>
        {
            Directory.CreateDirectory(folder);
            File.Copy(sourcePath, target, true);
        });
        return name;
    }

    private static IEnumerable<string> ListBackgroundFiles(GameBiz biz)
    {
        var files = new List<string>();
        try
        {
            string folder = GetFolder(biz);
            if (Directory.Exists(folder))
            {
                files.AddRange(Directory.GetFiles(folder)
                                        .Where(IsSupportedFile)
                                        .Where(x => !DynamicBackground.DynamicBackgroundStore.IsOwnedFile(x)));
            }
        }
        catch { }
        try
        {
            if (Directory.Exists(Folder))
            {

                files.AddRange(Directory.GetFiles(Folder)
                                       .Where(IsSupportedFile)
                                       .Where(x => !DynamicBackground.DynamicBackgroundStore.IsOwnedFile(x))
                                       .Where(x => OwnerOf(Path.GetFileName(x)) == biz));
            }
        }
        catch { }
        return files.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static GameBiz OwnerOf(string fileName)
    {
        const string prefix = "custom_bg_";
        string name = Path.GetFileNameWithoutExtension(fileName);
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse(name[prefix.Length..], true, out GameBiz biz))
        {
            return biz;
        }

        return GameBiz.WutheringWaves;
    }

    private static ImageSource? BuildThumbnail(string? path)
    {
        try
        {
            const int decodeWidth = 232;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }
            if (IsVideoPath(path))
            {
                string cache = VideoThumbnailCachePath(path);
                if (!File.Exists(cache))
                {
                    return null;
                }
                path = cache;
            }

            return new BitmapImage(new Uri(path))
            {
                DecodePixelWidth = decodeWidth,
                CreateOptions = BitmapCreateOptions.IgnoreImageCache,
            };
        }
        catch
        {
            return null;
        }
    }

    private static string VideoThumbnailCachePath(string videoPath)
    {
        FileInfo info = new(videoPath);
        string key = $"{info.Name}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        return Path.Combine(Folder, ".thumbs", Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(key))) + ".png");
    }

    public static async Task<bool> EnsureVideoThumbnailsAsync(IReadOnlyList<BackgroundItem> items)
    {
        bool changed = false;
        foreach (BackgroundItem item in items)
        {
            if (item.Thumbnail is not null || item.FilePath is not { Length: > 0 } path
                || !IsVideoPath(path) || !File.Exists(path))
            {
                continue;
            }
            string cache = VideoThumbnailCachePath(path);
            if (!File.Exists(cache) && !await ExtractFirstFrameAsync(path, cache))
            {
                continue;
            }
            item.Thumbnail = BuildThumbnail(cache);
            changed |= item.Thumbnail is not null;
        }
        return changed;
    }

    private static async Task<bool> ExtractFirstFrameAsync(string videoPath, string cachePath)
    {
        try
        {
            return await Task.Run(async () =>
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(videoPath);
                var clip = await Windows.Media.Editing.MediaClip.CreateFromFileAsync(file);
                var composition = new Windows.Media.Editing.MediaComposition();
                composition.Clips.Add(clip);
                using var frame = await composition.GetThumbnailAsync(TimeSpan.Zero, 232, 130,
                    Windows.Media.Editing.VideoFramePrecision.NearestFrame);
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                using var output = File.Create(cachePath);
                await frame.AsStreamForRead().CopyToAsync(output);
                return new FileInfo(cachePath).Length > 0;
            });
        }
        catch
        {
            return false;
        }
    }

}
