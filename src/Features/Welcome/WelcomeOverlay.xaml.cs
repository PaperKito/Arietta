using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Arietta.Core;
using Arietta.Localization;
using System;

namespace Arietta.Features.Welcome;

[ObservableObject]
public sealed partial class WelcomeOverlay : UserControl
{

    public WelcomeOverlay()
    {
        InitializeComponent();
        DataFolderText = AppConfig.LocalFolder;
        RunChecks();

        this.RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
    }

    public static bool IsShowing { get; private set; }

    public static event EventHandler? Dismissed;


    private static void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is not WelcomeOverlay overlay)
        {
            return;
        }
        if (overlay.Visibility == Visibility.Visible)
        {
            IsShowing = true;
        }
        else if (IsShowing)
        {
            IsShowing = false;
            Dismissed?.Invoke(overlay, EventArgs.Empty);
        }
    }


    public string DataFolderText { get; }

    public string WebView2Text { get; private set => SetProperty(ref field, value); } = "--";

    public Brush WebView2Brush { get; private set => SetProperty(ref field, value); } = NeutralBrush;

    public Brush TransparencyBrush { get; private set => SetProperty(ref field, value); } = NeutralBrush;

    public Brush AnimationBrush { get; private set => SetProperty(ref field, value); } = NeutralBrush;

    public void PlayEntrance()
    {
        try
        {
            if (!new Windows.UI.ViewManagement.UISettings().AnimationsEnabled)
            {
                return;
            }
        }
        catch
        {

        }

        var sb = new Storyboard();
        int i = 0;
        foreach (object? child in ContentPanel.Children)
        {
            var fe = (FrameworkElement)child;
            var translateY = new TranslateTransform { Y = RiseHeight };
            fe.RenderTransform = translateY;
            fe.Opacity = 0;
            double delay = 0.1 + i * 0.07;
            sb.Children.Add(new DoubleAnimation { From = 0, To = 1, Duration = FadeDuration, BeginTime = TimeSpan.FromSeconds(delay) });
            sb.Children.Add(new DoubleAnimation { From = RiseHeight, To = 0, Duration = FadeDuration, BeginTime = TimeSpan.FromSeconds(delay) });
            Storyboard.SetTarget(sb.Children[^2], fe);
            Storyboard.SetTargetProperty(sb.Children[^2], "Opacity");
            Storyboard.SetTarget(sb.Children[^1], translateY);
            Storyboard.SetTargetProperty(sb.Children[^1], "Y");
            i++;
        }

        StartButton.Opacity = 0;
        var btnFade = new DoubleAnimation { From = 0, To = 1, Duration = FadeDuration, BeginTime = TimeSpan.FromSeconds(0.8) };
        sb.Children.Add(btnFade);
        Storyboard.SetTarget(btnFade, StartButton);
        Storyboard.SetTargetProperty(btnFade, "Opacity");
        sb.Begin();
    }


    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(500));
    private const double RiseHeight = 14;

    private async void OpenVisualEffectsSettings(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args) =>
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess-visualeffects"));


    [RelayCommand]
    private void Start()
    {

        AppConfig.SetConfig("WelcomeShown", "1");
        var fade = new DoubleAnimation { From = 1, To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(300)) };
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, "Opacity");
        var sb = new Storyboard { Children = { fade } };
        sb.Completed += (_, _) => Visibility = Visibility.Collapsed;
        sb.Begin();
    }


    private void RunChecks()
    {

        try
        {
            WebView2Text = CoreWebView2Environment.GetAvailableBrowserVersionString();
            WebView2Brush = SuccessBrush;
        }
        catch (Exception)
        {
            WebView2Text = Lang.Instance.Welcome_NotInstalled;
            WebView2Brush = ErrorBrush;
        }

        try
        {
            int transparency = (int?)Registry.GetValue(
                @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "EnableTransparency", 1) ?? 1;
            TransparencyBrush = transparency != 0 ? LinkBlueBrush : ErrorBrush;
        }
        catch (Exception)
        {

        }

        try
        {
            AnimationBrush = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled ? LinkBlueBrush : ErrorBrush;
        }
        catch (Exception)
        {

        }
    }


    private static Brush NeutralBrush => (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

    private static Brush LinkBlueBrush { get; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x7A, 0xA2, 0xF7));

    private static Brush SuccessBrush => (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];

    private static Brush ErrorBrush => (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];

}
