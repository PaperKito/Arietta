using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace Arietta.Features.GachaLog;











public static class GachaIcon
{

    
    private static readonly System.Collections.Generic.Dictionary<int, ImageSource?> _cache = [];

    private static readonly object _lock = new();


    public static ImageSource? Load(int resourceId)
    {
        if (resourceId <= 0)
        {
            return null;
        }
        lock (_lock)
        {
            if (_cache.TryGetValue(resourceId, out var cached))
            {
                return cached;
            }
        }
        ImageSource? image = LoadCore(resourceId);
        lock (_lock)
        {
            _cache[resourceId] = image;
        }
        return image;
    }


    
    public static ImageSource? FromFile(string file)
    {
        try
        {
            var bitmap = new BitmapImage
            {
                DecodePixelWidth = 160,
                DecodePixelType = DecodePixelType.Logical,
            };
            bitmap.UriSource = new Uri(file);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }


    private static ImageSource? LoadCore(int resourceId)
    {
        
        string? file = Path.Combine(AppContext.BaseDirectory, "Assets", "Gacha", $"{resourceId}.png");
        if (!File.Exists(file))
        {
            
            file = Path.Combine(AppConfig.LocalFolder, "GachaIcons", $"{resourceId}.png");
            if (!File.Exists(file))
            {
                return null;
            }
        }
        return FromFile(file);
    }

}
