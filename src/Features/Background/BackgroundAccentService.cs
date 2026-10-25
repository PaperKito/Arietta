using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Arietta.Features.ViewHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace Arietta.Features.Background;

internal static class BackgroundAccentService
{

    private static readonly ILogger _logger = AppConfig.GetLogger("BackgroundAccent");

    private static readonly Dictionary<string, Color> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly object _cacheLock = new();

    private const uint DecodeWidth = 128;

    private static Color? _appliedColor;

    private static bool _appliedColorKnown;

    public static void Apply(string? accentSourcePath, DispatcherQueue? dispatcher)
    {
        _ = Task.Run(async () =>
        {
            Color? color = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(accentSourcePath) && File.Exists(accentSourcePath))
                {
                    color = await GetAccentColorAsync(accentSourcePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get background accent color: {Path}", accentSourcePath);
            }

            Color effective = color ?? AccentColorHelper.DefaultAccent;
            if (_appliedColorKnown && _appliedColor == effective)
            {

                return;
            }
            _appliedColor = effective;
            _appliedColorKnown = true;

            void ApplyOnUiThread()
            {
                try
                {
                    AccentColorHelper.ApplyAccentColor(effective);

                    MainWindow? window = MainWindow.Current;
                    window?.ChangeAccentColor();

                    _logger.LogInformation("Background accent color {Color} (from {Source}); {Resolved}",
                                           ToHex(effective),
                                           accentSourcePath ?? "无（还原默认）",
                                           AccentColorHelper.DescribeResolvedAccent());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply background accent color");
                }
            }

            if (dispatcher is null || dispatcher.HasThreadAccess)
            {
                ApplyOnUiThread();
            }
            else
            {
                dispatcher.TryEnqueue(ApplyOnUiThread);
            }
        });
    }

    public static void Invalidate(string filePath)
    {
        lock (_cacheLock)
        {
            _cache.Remove(filePath);
        }
    }


    private static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";


    private static async Task<Color?> GetAccentColorAsync(string filePath)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(filePath, out Color cached))
            {
                return cached;
            }
        }

        using FileStream stream = File.OpenRead(filePath);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());

        uint width = Math.Min(DecodeWidth, decoder.PixelWidth);
        uint height = (uint)Math.Max(1, decoder.PixelHeight * (double)width / decoder.PixelWidth);
        PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height,
                InterpolationMode = BitmapInterpolationMode.Linear,
            },
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        Color? color = AccentColorHelper.GetAccentColor(pixelData.DetachPixelData(), (int)width, (int)height);
        if (color is { } value)
        {
            lock (_cacheLock)
            {
                _cache[filePath] = value;
            }
        }
        return color;
    }

}
