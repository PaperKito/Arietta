using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Arietta.Core;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class GameBannerAndPost : UserControl
{

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _bannerTimer;

    public GameBannerAndPost()
    {
        this.InitializeComponent();
        _bannerTimer = DispatcherQueue.CreateTimer();
        _bannerTimer.Interval = TimeSpan.FromSeconds(5);
        _bannerTimer.IsRepeating = true;
        _bannerTimer.Tick += BannerTimer_Tick;
        this.Loaded += GameBannerAndPost_Loaded;
        this.Unloaded += GameBannerAndPost_Unloaded;
    }


    public List<GameBannerItem> Banners { get; set => SetProperty(ref field, value); }


    public List<GamePostGroup> PostGroups { get; set => SetProperty(ref field, value); }

    public GameBiz CurrentGameBiz { get; set; }


    private void GameBannerAndPost_Loaded(object sender, RoutedEventArgs e)
    {

        GameLauncherSettingDialog.AnnouncementSettingChanged += OnAnnouncementSettingChanged;
        ApplyScale();
        _ = ReloadAsync();
    }


    private void GameBannerAndPost_Unloaded(object sender, RoutedEventArgs e)
    {
        GameLauncherSettingDialog.AnnouncementSettingChanged -= OnAnnouncementSettingChanged;
        _bannerTimer.Stop();
    }

    private void OnAnnouncementSettingChanged(object? sender, EventArgs e)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            ApplyScale();
            _ = ReloadAsync();
        }
        else
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ApplyScale();
                _ = ReloadAsync();
            });
        }
    }

    public async Task ReloadAsync()
    {
        try
        {
            if (!GameNoticeSettings.ShowBannerAndPost)
            {
                _bannerTimer.Stop();
                Show(false);
                return;
            }
            var data = await GameNoticeService.GetAsync(CurrentGameBiz);
            if (data is null || (data.Value.Banners.Count == 0 && data.Value.Groups.Count == 0))
            {
                _bannerTimer.Stop();
                Show(false);

                var kind = GameNoticeService.LastFailure;
                if (kind is not GameNoticeService.NoticeFailureKind.Offline
                         and not GameNoticeService.NoticeFailureKind.NotSupported)
                {
                    InAppToast.MainWindow?.Warning(Lang.Instance.Notice_LoadFailed);
                }
                return;
            }
            Banners = data.Value.Banners;
            PostGroups = data.Value.Groups;
            Show(true);

            if (Banners.Count > 1)
            {
                _bannerTimer.Start();
            }
            else
            {
                _bannerTimer.Stop();
            }
        }
        catch (Exception ex)
        {

            AppConfig.GetLogger<GameBannerAndPost>().LogError(ex, "Unexpected exception while loading announcements");
            _bannerTimer.Stop();
            InAppToast.MainWindow?.Warning(Lang.Instance.Notice_LoadFailed);
            Show(false);
        }
    }

    private void Show(bool visible)
    {
        IsHitTestVisible = visible;
        Opacity = visible ? 1 : 0;
    }


    private void Grid_BannerContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (Banners is { Count: > 1 })
        {
            Border_PipsPager.Visibility = Visibility.Visible;
        }
    }


    private void Grid_BannerContainer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Border_PipsPager.Visibility = Visibility.Collapsed;
    }

    private void BannerTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (Banners is { Count: > 0 })
            {
                FlipView_Banner.SelectedIndex = (FlipView_Banner.SelectedIndex + 1) % Banners.Count;
            }
        }
        catch { }
    }


    private void FlipView_Banner_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView flip)
        {
            TextBlock_BannerIndex.Text = $"{flip.SelectedIndex + 1}/{Banners?.Count ?? 0}";
        }
    }


    private void Image_Banner_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (FlipView_Banner.SelectedItem is GameBannerItem { Link: { } link })
        {
            OpenUrl(link);
        }
    }


    private static void OpenUrl(string url)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Process.Start(new ProcessStartInfo { FileName = uri.ToString(), UseShellExecute = true });
            }
        }
        catch { }
    }

    private const double RestingScale = 0.8;

    private bool _isPointerInside;
    private bool _isScaledOut;
    private Storyboard? _scaleStoryboard;

    private void ApplyScale()
    {
        _scaleStoryboard?.Stop();
        _isPointerInside = false;
        _isScaledOut = false;
        double scale = GameNoticeSettings.ScaleUpOnHover ? RestingScale : 1.0;
        Transform_Card.ScaleX = scale;
        Transform_Card.ScaleY = scale;
    }


    private async void Grid_Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = true;
        if (!GameNoticeSettings.ScaleUpOnHover || _isScaledOut)
        {
            return;
        }

        await Task.Delay(150);
        if (!_isPointerInside || _isScaledOut || !GameNoticeSettings.ScaleUpOnHover)
        {
            return;
        }
        AnimateScale(1.0, 200);
        _isScaledOut = true;
    }


    private void Grid_Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerInside = false;
        if (!GameNoticeSettings.ScaleUpOnHover || !_isScaledOut)
        {
            return;
        }
        AnimateScale(RestingScale, 250);
        _isScaledOut = false;
    }

    private void AnimateScale(double to, int durationMs)
    {
        double from = Transform_Card.ScaleX;
        Storyboard storyboard = new();
        foreach (string property in new[] { "ScaleX", "ScaleY" })
        {
            DoubleAnimation anim = new()
            {
                From           = from,
                To             = to,
                Duration       = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(anim, Transform_Card);
            Storyboard.SetTargetProperty(anim, property);
            storyboard.Children.Add(anim);
        }
        _scaleStoryboard = storyboard;
        storyboard.Begin();
    }

}
