using GameEntry = Arietta.Core.GameRegistry.GameEntry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
using System.Numerics;

namespace Arietta.Features.ViewHost;

public sealed partial class MainView : UserControl
{

    public static GameBiz CurrentGameBiz { get; private set; } = GameBiz.WutheringWavesGlobal;

    public static GameEntry? CurrentGameEntry { get; private set; }

    public static event EventHandler<GameBiz>? GameChanged;

    private static readonly Microsoft.UI.Xaml.Media.Imaging.BitmapImage DefaultBackground =
        new(new Uri("ms-appx:///Assets/Image/bg_default.jpg"));


    public MainView()
    {
        this.InitializeComponent();

        CurrentGameBiz = GameSelectorControl.CurrentGameBiz ?? GameBiz.WutheringWavesGlobal;
        CurrentGameEntry = GameSelectorControl.CurrentGameEntry ?? GameRegistry.GetEntry(CurrentGameBiz);

        UpdateNavVisibility();
        LoadCustomBackground(null, null);
        GameLauncherSettingDialog.CustomBackgroundChanged += LoadCustomBackground;

        Features.GamePacks.GamePackService.Changed += OnPackEnableChanged;

        BackgroundGallery.SelectionChanged += OnBackgroundChanged;
        DynamicBackgroundStore.Updated += OnBackgroundChanged;

        Localization.UiLanguage.Changed += OnUiLanguageChanged;

        GameLauncherSettingDialog.ShowVersionPosterChanged += OnShowBgSloganChanged;

        if (GameSelectorControl.HasSelection)
        {
            NavigateTo(typeof(LauncherPage), null, new SuppressNavigationTransitionInfo());
        }
    }

    private async void LoadCustomBackground(object? sender, string? path)
    {
        try
        {

            BackgroundItem? item = GameSelectorControl.HasSelection ? BackgroundGallery.Resolve(CurrentGameBiz) : null;

            if (item is { Kind: BackgroundKind.Video } && item.FilePath is { Length: > 0 } video)
            {

                string? poster = item.IsOfficial ? DynamicBackgroundStore.GetLocal(CurrentGameBiz)?.PosterPath : null;
                Image_Background.Source = poster is { Length: > 0 }
                    ? new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(poster))
                    : null;
                DynamicBgHost.Play(video);

                UpdateSloganLayer(item.IsOfficial);

                BackgroundAccentService.Apply(poster, DispatcherQueue);
                return;
            }

            DynamicBgHost.Stop();
            UpdateSloganLayer(false);
            if (item?.FilePath is { Length: > 0 } image && File.Exists(image))
            {

                var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                using (FileStream stream = File.OpenRead(image))
                {
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                }
                Image_Background.Source = bitmap;
            }
            else
            {

                Image_Background.Source = DefaultBackground;
            }

            BackgroundAccentService.Apply(item?.FilePath, DispatcherQueue);
        }
        catch
        {
            DynamicBgHost.Stop();
            UpdateSloganLayer(false);
        }
    }

    private void UpdateSloganLayer(bool visible)
    {
        string? path = visible && GameRegistry.IsDynBgSloganShown(CurrentGameBiz)
            ? DynamicBackgroundStore.GetSloganPath(CurrentGameBiz)
            : null;
        if (path is null)
        {
            Image_Slogan.Source = null;
            Image_Slogan.Visibility = Visibility.Collapsed;
            return;
        }
        Image_Slogan.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(path));
        Image_Slogan.Visibility = Visibility.Visible;
    }

    private void OnUiLanguageChanged()
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            LoadCustomBackground(null, null);
        }
        else
        {
            DispatcherQueue.TryEnqueue(() => LoadCustomBackground(null, null));
        }
    }

    private void OnShowBgSloganChanged(object? sender, EventArgs e)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            LoadCustomBackground(null, null);
        }
        else
        {
            DispatcherQueue.TryEnqueue(() => LoadCustomBackground(null, null));
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

    private void OnPackEnableChanged()
    {
        try
        {
            GameSelectorControl.ReloadGames();
            bool has = GameSelectorControl.HasSelection;
            CurrentGameBiz = GameSelectorControl.CurrentGameBiz ?? GameBiz.WutheringWavesGlobal;
            CurrentGameEntry = GameSelectorControl.CurrentGameEntry;
            UpdateNavVisibility();
            if (has)
            {
                LoadCustomBackground(null, null);
                NavigateTo(typeof(LauncherPage));
                GameChanged?.Invoke(this, CurrentGameBiz);
            }
            else
            {

                MainView_Frame.Content = null;
            }
        }
        catch { }
    }

    private void GameSelector_CurrentGameChanged(object? sender, GameBiz biz)
    {
        CurrentGameBiz = biz;
        CurrentGameEntry = GameSelectorControl.CurrentGameEntry ?? GameRegistry.GetEntry(biz);

        LoadCustomBackground(null, null);

        if (CurrentGameEntry?.IsExternal != true)
        {
            _ = DynamicBackgroundStore.SyncAllAsync(biz);
        }

        UpdateNavVisibility();
        GameChanged?.Invoke(this, biz);
        NavigateTo(typeof(LauncherPage));
    }

    private void UpdateNavVisibility()
    {

        bool has = GameSelectorControl.HasSelection;

        bool external = CurrentGameEntry?.IsExternal == true;
        GameEntry entry = CurrentGameEntry ?? GameRegistry.GetEntry(CurrentGameBiz);
        NavigationViewItem_Launcher.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
        NavigationViewItem_GameSetting.Visibility = has && (!external || entry.Pack?.Settings is not null)
            ? Visibility.Visible
            : Visibility.Collapsed;
        NavigationViewItem_Screenshot.Visibility = has && !external ? Visibility.Visible : Visibility.Collapsed;
        NavigationViewItem_GachaLog.Visibility = has && entry.HasGachaPage
            ? Visibility.Visible
            : Visibility.Collapsed;

        NavigationViewItem_KuroBbs.Visibility = has && !external && !CurrentGameBiz.IsOverseas()
            ? Visibility.Visible
            : Visibility.Collapsed;
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

        if (page == typeof(GameSettingPage) && CurrentGameEntry?.IsExternal == true)
        {
            page = typeof(Features.GamePacks.PackSettingsPage);
        }
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

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        if (FindDescendant(MainView_NavigationView, "PaneContentGrid") is Grid paneGrid)
        {
            paneGrid.PointerEntered += NavPane_PointerEntered;
            paneGrid.PointerExited += NavPane_PointerExited;
        }
        if (FindDescendant(MainView_NavigationView, "PaneToggleButtonGrid") is Grid toggleGrid)
        {
            toggleGrid.PointerEntered += NavPane_PointerEntered;
            toggleGrid.PointerExited += NavPane_PointerExited;
        }
    }


    private void NavPane_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Border_NavPaneBackground.Opacity = 1;
        Border_NavPaneBackground.Translation = new Vector3(0, 0, 32);
    }


    private void NavPane_PointerExited(object sender, PointerRoutedEventArgs e)
    {

        float x = (float)e.GetCurrentPoint(Border_NavPaneBackground).Position.X;
        if (x <= 4 || x > Border_NavPaneBackground.Width - 8)
        {
            Border_NavPaneBackground.Opacity = 0;
            Border_NavPaneBackground.Translation = new Vector3(-56, 0, 0);
        }
    }


    private static DependencyObject? FindDescendant(DependencyObject parent, string name)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement { Name: string n } && n == name)
            {
                return child;
            }
            if (FindDescendant(child, name) is { } found)
            {
                return found;
            }
        }
        return null;
    }


}
