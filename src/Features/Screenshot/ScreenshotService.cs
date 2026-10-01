using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arietta.Features.Screenshot;


public sealed record ScreenshotFile(string Path, DateTime Time);


public sealed class ScreenshotItem
{

    public ScreenshotItem(ScreenshotFile file)
    {
        FilePath = file.Path;
        Time = file.Time;
        var bitmap = new BitmapImage
        {
            DecodePixelWidth = 412,
            DecodePixelType = DecodePixelType.Logical,
        };
        try
        {
            bitmap.UriSource = new Uri(file.Path);
        }
        catch { }
        Thumbnail = bitmap;
    }

    public string FilePath { get; }

    public DateTime Time { get; }

    public ImageSource Thumbnail { get; }

}



public sealed class ScreenshotGroup : List<ScreenshotItem>
{

    public ScreenshotGroup(string header, IEnumerable<ScreenshotItem> items) : base(items)
    {
        Header = header;
    }

    public string Header { get; }

}






public static class ScreenshotService
{

    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp"];

    
    private static readonly string[] FolderNames = ["Screenshots", "Screenshot", "ScreenShot"];

    
    public static string GetConfigKey(GameBiz biz) => $"ScreenshotFolder_{biz}";


    
    public static List<string> GetFolders(GameBiz biz)
    {
        var folders = new List<string>();
        AddIfExists(folders, GetConfiguredFolder(biz));

        string? exe = null;
        try
        {
            exe = GameRegistry.GetConfiguredExePath(biz);
        }
        catch { }
        if (!string.IsNullOrWhiteSpace(exe))
        {
            string? dir = Path.GetDirectoryName(exe);
            for (int i = 0; i < 5 && dir is not null; i++)
            {
                string saved = Path.Combine(dir, "Client", "Saved");
                foreach (string name in FolderNames)
                {
                    AddIfExists(folders, Path.Combine(saved, name));
                }
                dir = Path.GetDirectoryName(dir);
            }
        }

        
        try
        {
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            AddIfExists(folders, Path.Combine(pictures, "Wuthering Waves"));
            AddIfExists(folders, Path.Combine(pictures, "鸣潮"));
        }
        catch { }
        return folders;
    }


    public static string? GetConfiguredFolder(GameBiz biz)
    {
        try
        {
            return AppConfig.GetConfig(GetConfigKey(biz));
        }
        catch
        {
            return null;
        }
    }


    
    public static List<ScreenshotFile> EnumerateImages(IEnumerable<string> folders)
    {
        var result = new List<ScreenshotFile>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string folder in folders)
        {
            foreach (string file in EnumerateFolder(folder))
            {
                if (!seen.Add(file))
                {
                    continue;
                }
                try
                {
                    result.Add(new ScreenshotFile(file, File.GetLastWriteTime(file)));
                }
                catch { }
            }
        }
        return result.OrderByDescending(x => x.Time).ToList();
    }


    private static IEnumerable<string> EnumerateFolder(string folder)
    {
        var files = new List<string>();
        try
        {
            if (!Directory.Exists(folder))
            {
                return files;
            }
            files.AddRange(EnumerateFiles(folder));
            foreach (string sub in Directory.GetDirectories(folder))
            {
                files.AddRange(EnumerateFiles(sub));
            }
        }
        catch { }
        return files;
    }


    private static IEnumerable<string> EnumerateFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder)
                            .Where(x => Extensions.Contains(System.IO.Path.GetExtension(x), StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return [];
        }
    }


    private static void AddIfExists(List<string> folders, string? folder)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && !folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(folder);
            }
        }
        catch { }
    }

}
