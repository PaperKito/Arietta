using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

    public GameBannerAndPost()
    {
        this.InitializeComponent();
        this.Loaded += GameBannerAndPost_Loaded;
        this.Unloaded += GameBannerAndPost_Unloaded;
    }


    public List<GameBannerItem> Banners { get; set => SetProperty(ref field, value); }


    public List<GamePostGroup> PostGroups { get; set => SetProperty(ref field, value); }

    public GameBiz CurrentGameBiz { get; set; }


    private void GameBannerAndPost_Loaded(object sender, RoutedEventArgs e)
    {

        GameLauncherSettingDialog.AnnouncementSettingChanged += OnAnnouncementSettingChanged;
        _ = ReloadAsync();
    }


    private void GameBannerAndPost_Unloaded(object sender, RoutedEventArgs e)
    {
        GameLauncherSettingDialog.AnnouncementSettingChanged -= OnAnnouncementSettingChanged;
    }

    private void OnAnnouncementSettingChanged(object? sender, EventArgs e)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            _ = ReloadAsync();
        }
        else
        {
            DispatcherQueue.TryEnqueue(() => _ = ReloadAsync());
        }
    }

    public async Task ReloadAsync()
    {
        try
        {
            if (!GameNoticeSettings.ShowBannerAndPost)
            {
                Show(false);
                return;
            }
            var data = await GameNoticeService.GetAsync(CurrentGameBiz);
            if (data is null || (data.Value.Banners.Count == 0 && data.Value.Groups.Count == 0))
            {
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
        }
        catch (Exception ex)
        {

            AppConfig.GetLogger<GameBannerAndPost>().LogError(ex, "Unexpected exception while loading announcements");
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


}
