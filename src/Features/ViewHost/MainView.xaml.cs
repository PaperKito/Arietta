using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Arietta.Core;
using Arietta.Features.Background;
using Arietta.Features.DynamicBackground;
using Arietta.Features.GameLauncher;
using Arietta.Features.GameSelector;
using Arietta.Features.GachaLog;
using Arietta.Features.GameSetting;
using Arietta.Features.KuroBbs;
using Arietta.Features.Screenshot;
using Arietta.Features.Setting;
using System;
using System.IO;

namespace Arietta.Features.ViewHost;

public sealed partial class MainView : UserControl
{

    public static GameBiz CurrentGameBiz { get; private set; } = GameBiz.WutheringWavesGlobal;

    public static event EventHandler<GameBiz>? GameChanged;


    public MainView()
    {
        this.InitializeComponent();
        CurrentGameBiz = GameSelectorControl.CurrentGameBiz;

        UpdateNavVisibility();
        LoadCustomBackground(null, null);
        GameLauncherSettingDialog.CustomBackgroundChanged += LoadCustomBackground;

        BackgroundGallery.SelectionChanged += OnBackgroundChanged;
        DynamicBackgroundStore.Updated += OnBackgroundChanged;
        NavigateTo(typeof(LauncherPage), null, new SuppressNavigationTransitionInfo());
    }

    private async void LoadCustomBackground(object? sender, string? path)
    {
        try
        {
            BackgroundItem? item = BackgroundGallery.Resolve(CurrentGameBiz);

            if (item is { Kind: BackgroundKind.Video } && item.FilePath is { Length: > 0 } video)
            {

                string? poster = item.IsOfficial ? DynamicBackgroundStore.GetLocal(CurrentGameBiz)?.PosterPath : null;
                Image_Background.Source = poster is { Length: > 0 }
                    ? new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(poster))
                    : null;
                DynamicBgHost.Play(video);

                BackgroundAccentService.Apply(poster, DispatcherQueue);
                return;
            }

            DynamicBgHost.Stop();
            Image_Background.Source = null;
            if (item?.FilePath is { Length: > 0 } image && File.Exists(image))
            {
                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                using (FileStream stream = File.OpenRead(image))
                {
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                }
                Image_Background.Source = bitmap;
            }

            BackgroundAccentService.Apply(item?.FilePath, DispatcherQueue);
        }
        catch
        {
            DynamicBgHost.Stop();
        }
    }

    private void OnBackgroundChanged(object? sender, GameBiz biz)
    {
        if (biz.GetBaseBiz() != CurrentGameBiz.GetBaseBiz())
        {
            return;
        }
        if (DispatcherQueue.HasThreadAccess)
        {
            LoadCustomBackground(null, null);
        }
        else
        {
            DispatcherQueue.TryEnqueue(() => LoadCustomBackground(null, null));
        }
    }

    private void GameSelector_CurrentGameChanged(object? sender, GameBiz biz)
    {
        CurrentGameBiz = biz;

        LoadCustomBackground(null, null);

        _ = DynamicBackgroundStore.SyncAllAsync(biz);

        UpdateNavVisibility();
        GameChanged?.Invoke(this, biz);
        NavigateTo(typeof(LauncherPage));
    }

    private void UpdateNavVisibility()
    {
        NavigationViewItem_GachaLog.Visibility = GameRegistry.GetEntry(CurrentGameBiz).HasGachaPage
            ? Visibility.Visible
            : Visibility.Collapsed;

        NavigationViewItem_KuroBbs.Visibility = CurrentGameBiz.IsOverseas()
            ? Visibility.Collapsed
            : Visibility.Visible;
    }


    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
                NavigateTo(typeof(SettingPage));
            }
            else if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var type = item.Tag switch
                {
                    nameof(LauncherPage) => typeof(LauncherPage),
                    nameof(GameSettingPage) => typeof(GameSettingPage),
                    nameof(ScreenshotPage) => typeof(ScreenshotPage),
                    nameof(GachaLogPage) => typeof(GachaLogPage),
                    nameof(KuroBbsPage) => typeof(KuroBbsPage),
                    nameof(ToolboxPage) => typeof(ToolboxPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
        catch { }
    }


    private void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null)
    {
        page ??= typeof(LauncherPage);
        MainView_Frame.Navigate(page, param, infoOverride);
        if (page.Name is nameof(LauncherPage))
        {
            MainView_NavigationView.SelectedItem = NavigationViewItem_Launcher;
            Border_OverlayMask.Opacity = 0;
        }
        else
        {
            Border_OverlayMask.Opacity = 1;
        }
    }


}
