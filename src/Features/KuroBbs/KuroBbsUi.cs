using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

internal static class KuroBbsUi
{

    public static void ShowInfoBar(InfoBar bar, bool open)
    {
        bar.IsOpen = open;
        bar.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
    }

    public static void AnimateWidth(Border element, double targetWidth, int delayMs)
    {
        try
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = Math.Max(0, targetWidth),
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                EnableDependentAnimation = true,
            };
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, "Width");
            storyboard.Begin();
        }
        catch { }
    }

    public static Border KuroCard(
        FrameworkElement? child = null,
        double cornerRadius = 8,
        Thickness? padding = null,
        bool secondaryBackground = false,
        Brush? borderBrush = null,
        double? height = null,
        Thickness? margin = null)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(cornerRadius),
            Background = (Brush)Application.Current.Resources[secondaryBackground
                ? "CardBackgroundFillColorSecondaryBrush"
                : "CardBackgroundFillColorDefaultBrush"],
            BorderBrush = borderBrush ?? (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
        };
        if (height is { } h)
        {
            border.Height = h;
        }
        if (padding is { } p)
        {
            border.Padding = p;
        }
        if (margin is { } m)
        {
            border.Margin = m;
        }
        if (child is not null)
        {
            border.Child = child;
        }
        return border;
    }

    public static Border MakeCornerBadge(
        string text,
        double fontSize = 11,
        Thickness? padding = null,
        CornerRadius? cornerRadius = null,
        bool italic = false,
        Brush? foreground = null)
    {
        var label = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = foreground ?? new SolidColorBrush(Microsoft.UI.Colors.White),
        };
        if (italic)
        {
            label.FontStyle = Windows.UI.Text.FontStyle.Italic;
        }
        return new Border
        {
            Padding = padding ?? new Thickness(5, 1, 5, 1),
            CornerRadius = cornerRadius ?? new CornerRadius(0, 8, 0, 6),
            Background = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = label,
        };
    }

    public static Border MakeDivider(string brushKey, double height, Thickness? margin = null, double width = 0, bool verticalCenter = false) =>
        MakeDivider((Brush)Application.Current.Resources[brushKey], height, margin, width, verticalCenter);

    public static Border MakeDivider(Brush background, double height, Thickness? margin = null, double width = 0, bool verticalCenter = false)
    {
        var border = new Border { Height = height, Background = background };
        if (margin is { } m)
        {
            border.Margin = m;
        }
        if (width > 0)
        {
            border.Width = width;
        }
        if (verticalCenter)
        {
            border.VerticalAlignment = VerticalAlignment.Center;
        }
        return border;
    }

    public static Image MakeThemeIcon(FrameworkElement themeHost, string iconKey, double size, bool centered = false)
    {
        string suffix = themeHost.ActualTheme == ElementTheme.Light ? "Light" : "Dark";
        var img = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
        if (centered)
        {
            img.VerticalAlignment = VerticalAlignment.Center;
        }
        try
        {
            img.Source = new BitmapImage(new Uri($"ms-appx:///Assets/Image/Combat/Pgr{iconKey}_{suffix}.png"));
        }
        catch { }
        return img;
    }

    public static void ShowTokenExpiredToast()
    {
        InAppToast.MainWindow?.ShowSingle(InfoBarSeverity.Warning,
            Lang.Instance.KuroBbs_TokenExpired, Lang.Instance.KuroBbs_TokenExpiredDesc);
    }

    public static bool IsTokenExpiredException(Exception ex) =>
        ex.Message.Contains("登录已过期", StringComparison.Ordinal)
        || ex.Message.Contains("accessToken", StringComparison.OrdinalIgnoreCase);

    public static void HandleException(Exception ex, string? toastTitle)
    {
        if (IsTokenExpiredException(ex))
        {
            ShowTokenExpiredToast();
        }
        else
        {
            InAppToast.MainWindow?.Error(ex, toastTitle);
        }
    }

    public static void HandleKuroError(int code, string? msg, string? toastTitle, string fallbackText, InfoBar? errorBar = null, bool foldVisibility = true)
    {
        if (code == 220)
        {
            ShowTokenExpiredToast();
            return;
        }
        if (errorBar is null)
        {
            InAppToast.MainWindow?.Warning(toastTitle, msg ?? fallbackText);
        }
        else
        {
            errorBar.Message = string.IsNullOrEmpty(msg) ? fallbackText : msg!;
            if (foldVisibility)
            {
                ShowInfoBar(errorBar, true);
            }
            else
            {
                errorBar.IsOpen = true;
            }
        }
    }

    public static async Task RunPgrLoadAsync(
        InfoBar noAccountBar,
        InfoBar? errorBar,
        ProgressRing ring,
        bool foldInfoBar,
        bool showRing,
        bool collapseRing,
        Action? beforeLoad,
        Func<KuroBbsAccount, Task> body,
        string toastTitle)
    {

        var account = KuroBbsAccountStore.Load(pgr: true);
        if (account is null || string.IsNullOrEmpty(account.Token))
        {
            SetInfoBar(noAccountBar, true, foldInfoBar);
            return;
        }
        SetInfoBar(noAccountBar, false, foldInfoBar);
        if (errorBar is not null)
        {
            SetInfoBar(errorBar, false, foldInfoBar);
        }
        beforeLoad?.Invoke();
        ring.IsActive = true;
        if (showRing)
        {
            ring.Visibility = Visibility.Visible;
        }
        try
        {
            if (!await KuroBbsAccountStore.EnsurePgrRoleAsync(account))
            {
                SetInfoBar(noAccountBar, true, foldInfoBar);
                return;
            }
            await body(account);
        }
        catch (Exception ex)
        {
            HandleException(ex, toastTitle);
        }
        finally
        {
            ring.IsActive = false;
            if (collapseRing)
            {
                ring.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void SetInfoBar(InfoBar bar, bool open, bool foldVisibility)
    {
        if (foldVisibility)
        {
            ShowInfoBar(bar, open);
        }
        else
        {
            bar.IsOpen = open;
        }
    }
}
