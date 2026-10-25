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
    }


    public string DataFolderText { get; }

    public string WebView2Text { get; private set => SetProperty(ref field, value); } = "--";

    public Brush WebView2Brush { get; private set => SetProperty(ref field, value); } = NeutralBrush;

    public Brush TransparencyBrush { get; private set => SetProperty(ref field, value); } = NeutralBrush;

    public Brush AnimationBrush { get; private set => SetProperty(ref field, value); } = NeutralBrush;


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
            TransparencyBrush = transparency != 0 ? NeutralBrush : ErrorBrush;
        }
        catch (Exception)
        {

        }

        try
        {
            AnimationBrush = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled ? NeutralBrush : ErrorBrush;
        }
        catch (Exception)
        {

        }
    }


    private static Brush NeutralBrush => (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

    private static Brush SuccessBrush => (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];

    private static Brush ErrorBrush => (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];

}
