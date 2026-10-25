using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Arietta.Features.Background;

internal static class AccentColorHelper
{

    private const double AccentSaturation = 0.6;

    private const double MinChroma = 10;

    private const double AccentValue = 0.7;

    private const double MinColoredSaturation = 0.25;

    private const double MinColoredValue = 0.15;
    private const double MaxColoredValue = 0.92;

    private static readonly Color FallbackDefaultAccent = Color.FromArgb(255, 0x00, 0xC2, 0xAC);

    private static Color _defaultAccent = FallbackDefaultAccent;

    private static bool _defaultAccentCaptured;

    public static Color DefaultAccent
    {
        get
        {
            CaptureDefaultAccent();
            return _defaultAccent;
        }
    }

    public static void CaptureDefaultAccent()
    {
        if (_defaultAccentCaptured)
        {
            return;
        }
        _defaultAccentCaptured = true;
        try
        {
            if (Application.Current is { } app && app.Resources["SystemAccentColor"] is Color color)
            {
                _defaultAccent = color;
            }
        }
        catch
        {

        }
    }

    private const int HueBins = 24;

    public static Color? GetAccentColor(byte[]? bgra, int width, int height)
    {
        if (bgra is null || width <= 0 || height <= 0 || bgra.Length < (long)width * height * 4)
        {
            return null;
        }
        try
        {
            var votes = new double[HueBins];

            var sinSum = new double[HueBins];
            var cosSum = new double[HueBins];
            int stride = width * 4;

            for (int y = 0; y < height; y += 2)
            {
                int row = y * stride;
                for (int x = 0; x < width; x += 2)
                {
                    int i = row + (x * 4);
                    (double hue, double saturation, double value) = ToHsv(bgra[i + 2], bgra[i + 1], bgra[i]);
                    if (saturation < MinColoredSaturation || value is < MinColoredValue or > MaxColoredValue)
                    {
                        continue;
                    }
                    double weight = saturation * value;
                    int bin = (int)(hue * HueBins / 360.0) % HueBins;
                    votes[bin] += weight;
                    double angle = hue * Math.PI / 180.0;
                    sinSum[bin] += Math.Sin(angle) * weight;
                    cosSum[bin] += Math.Cos(angle) * weight;
                }
            }
            int best = 0;
            for (int i = 1; i < HueBins; i++)
            {
                if (votes[i] > votes[best])
                {
                    best = i;
                }
            }
            if (votes[best] <= 0)
            {
                return null;
            }

            double sin = 0, cos = 0;
            for (int offset = -1; offset <= 1; offset++)
            {
                int bin = (best + offset + HueBins) % HueBins;
                sin += sinSum[bin];
                cos += cosSum[bin];
            }
            double hueAvg = (Math.Atan2(sin, cos) * 180.0 / Math.PI + 360.0) % 360.0;
            return FromHsv(hueAvg, AccentSaturation, AccentValue);
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyAccentColor(Color? color)
    {
        if (color is not { } value || Application.Current is not { } app)
        {
            return;
        }
        try
        {
            app.Resources["SystemAccentColor"] = value;
            app.Resources["SystemAccentColorLight1"] = ColorMix(value, Colors.White, 0.8);
            app.Resources["SystemAccentColorLight2"] = ColorMix(value, Colors.White, 0.6);
            app.Resources["SystemAccentColorLight3"] = ColorMix(value, Colors.White, 0.4);
            app.Resources["SystemAccentColorDark1"] = ColorMix(value, Colors.Black, 0.8);
            app.Resources["SystemAccentColorDark2"] = ColorMix(value, Colors.Black, 0.6);
            app.Resources["SystemAccentColorDark3"] = ColorMix(value, Colors.Black, 0.4);
        }
        catch
        {

        }
    }

    public static string DescribeResolvedAccent()
    {
        try
        {
            if (Application.Current is not { } app)
            {
                return "无 Application";
            }
            if (app.Resources["AccentFillColorDefaultBrush"] is SolidColorBrush brush)
            {
                return $"AccentFillColorDefaultBrush=#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
            }
            return "AccentFillColorDefaultBrush 不是 SolidColorBrush";
        }
        catch (Exception ex)
        {
            return $"读取 AccentFillColorDefaultBrush 失败: {ex.GetType().Name}";
        }
    }


    private static Color ColorMix(Color input, Color blend, double percent)
    {
        return Color.FromArgb(255,
                              (byte)((input.R * percent) + (blend.R * (1 - percent))),
                              (byte)((input.G * percent) + (blend.G * (1 - percent))),
                              (byte)((input.B * percent) + (blend.B * (1 - percent))));
    }

    private static (double Hue, double Saturation, double Value) ToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255d, gd = g / 255d, bd = b / 255d;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;
        double hue = 0;
        if (delta > 0)
        {
            if (max == rd)
            {
                hue = 60 * (((gd - bd) / delta) % 6);
            }
            else if (max == gd)
            {
                hue = 60 * (((bd - rd) / delta) + 2);
            }
            else
            {
                hue = 60 * (((rd - gd) / delta) + 4);
            }
            if (hue < 0)
            {
                hue += 360;
            }
        }
        double saturation = max <= 0 ? 0 : delta / max;
        return (hue, saturation, max);
    }

    private static Color FromHsv(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        double c = value * saturation;
        double x = c * (1 - Math.Abs(((hue / 60) % 2) - 1));
        double m = value - c;
        (double r, double g, double b) = hue switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x),
        };
        return Color.FromArgb(255,
                              (byte)Math.Round((r + m) * 255),
                              (byte)Math.Round((g + m) * 255),
                              (byte)Math.Round((b + m) * 255));
    }

}
