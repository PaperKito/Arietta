using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Arietta.Core;
using Arietta.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Arietta.Features.GameSelector;








[INotifyPropertyChanged]
public sealed partial class GameSelector : UserControl
{

    
    public event EventHandler<GameBiz>? CurrentGameChanged;


    public GameSelector()
    {
        this.InitializeComponent();
        InitializeGameSelector();
        
        GameBizIcons.CollectionChanged += GameBizIcons_CollectionChanged;
        
        Localization.UiLanguage.Changed += OnUiLanguageChanged;
        TeachTip_SelectGame.Closed += (_, _) => AppConfig.SetConfig("GameSelectorTipDismissed", "1");
        this.Loaded += GameSelector_Loaded;
    }



    public GameBiz CurrentGameBiz { get; set => SetProperty(ref field, value); }


    public GameBizIcon? CurrentGameBizIcon { get; set => SetProperty(ref field, value); }


    public ObservableCollection<GameBizIcon> GameBizIcons { get; set; } = [];


    public bool IsPinned { get; set => SetProperty(ref field, value); }


    public List<GameBizDisplay> GameBizDisplays { get; set => SetProperty(ref field, value); }


    public ObservableCollection<GameBizIcon> InstalledGames { get; } = [];


    
    public string? InstalledGamesActualSize { get; set => SetProperty(ref field, value); }





    #region 初始化


    private void InitializeGameSelector()
    {
        try
        {
            
            if (Enum.TryParse(AppConfig.GetConfig("CurrentGameBiz"), out GameBiz saved) && Enum.IsDefined(saved))
            {
                CurrentGameBiz = saved;
            }
            else
            {
                
                CurrentGameBiz = GameBiz.WutheringWavesGlobal;
            }

            
            GameBizIcons.Clear();
            var pinnedBizs = GetPinnedBizs();
            foreach (string str in pinnedBizs)
            {
                if (CreateIcon(str, true) is GameBizIcon icon)
                {
                    GameBizIcons.Add(icon);
                }
            }
            SyncSelection();

            IsPinned = AppConfig.GetConfig("GameBizSelectorPinned") is "1";
            if (IsPinned)
            {
                GameIconsAreaVisible = true;
            }

            
            var displays = new List<GameBizDisplay>();
            foreach (var group in GameRegistry.Groups)
            {
                var servers = group.Servers.Select(s => CreateIcon(s.Biz.ToString(), pinnedBizs.Contains(s.Biz.ToString(), StringComparer.OrdinalIgnoreCase))!).ToList();
                displays.Add(new GameBizDisplay
                {
                    GameName = group.Game,
                    Logo = GameBizIcon.LoadIcon(group.IconPath),
                    
                    Icon = GameBizIcon.LoadIcon(group.Servers[0].GameIconPath),
                    Servers = servers,
                });
            }
            GameBizDisplays = displays;
        }
        catch { }
    }


    
    private void OnUiLanguageChanged()
    {
        
        GameBizIcons.CollectionChanged -= GameBizIcons_CollectionChanged;
        InitializeGameSelector();
        GameBizIcons.CollectionChanged += GameBizIcons_CollectionChanged;
        
        _installedSignature = null;
    }


    private static string[] GetPinnedBizs()
    {
        return (AppConfig.GetConfig("PinnedGames") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }


    private static GameBizIcon? CreateIcon(string bizName, bool isPinned)
    {
        try
        {
            var biz = Enum.Parse<GameBiz>(bizName, true);
            var entry = GameRegistry.GetEntry(biz);
            var group = GameRegistry.Groups.FirstOrDefault(g => g.Servers.Contains(entry));
            
            
            
            return new GameBizIcon
            {
                Biz = biz,
                GameName = group?.Game ?? entry.ShortName,
                ServerName = entry.ShortName,
                GameIconPath = entry.GameIconPath,
                ServerIconPath = entry.ServerIconPath,
                ServerCode = entry.ServerCode,
                GameIcon = GameBizIcon.LoadIcon(entry.GameIconPath),
                ServerIcon = entry.ServerIconPath is null ? null : GameBizIcon.LoadIcon(entry.ServerIconPath),
                IsPinned = isPinned,
            };
        }
        catch
        {
            return null;
        }
    }


    
    private void SyncSelection()
    {
        foreach (var icon in GameBizIcons)
        {
            bool selected = icon.Biz == CurrentGameBiz;
            icon.IsSelected = selected;
            icon.MaskOpacity = selected ? 0 : 1;
        }
        
        CurrentGameBizIcon = GameBizIcons.FirstOrDefault(x => x.Biz == CurrentGameBiz)
                             ?? CreateIcon(CurrentGameBiz.ToString(), false);
    }


    private async void GameSelector_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            this.XamlRoot.Changed -= XamlRoot_Changed;
            this.XamlRoot.Changed += XamlRoot_Changed;
            
            
            UpdateDragRectangles();
            await Task.Delay(600);
            if (GameBizIcons.Count == 0 && AppConfig.GetConfig("GameSelectorTipDismissed") is not "1")
            {
                TeachTip_SelectGame.IsOpen = true;
            }
        }
        catch { }
    }


    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        UpdateDragRectangles();
    }


    #endregion




    #region 拖动窗口区域


    
    public void UpdateDragRectangles()
    {
        try
        {
            double x = Border_CurrentGameIcon.ActualWidth;
            if (GameIconsAreaVisible)
            {
                x += Grid_GameIconsArea.ActualWidth;
            }
            XamlRoot? root = this.XamlRoot;
            root?.SetWindowDragRectangles(new Rect(x, 0, 10000, 48));
        }
        catch { }
    }


    #endregion




    #region 待选图标栏


    
    public bool GameIconsAreaVisible
    {
        get => Grid_GameIconsArea.Translation == Vector3.Zero;
        set
        {
            Grid_GameIconsArea.Translation = value ? Vector3.Zero : new Vector3(0, -100, 0);
            UpdateDragRectangles();
        }
    }


    
    private void Border_CurrentGameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        GameIconsAreaVisible = true;
    }


    
    private void Border_CurrentGameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            return;
        }
        if (sender is UIElement ele)
        {
            var position = e.GetCurrentPoint(ele).Position;
            if (position.X > ele.ActualSize.X - 1 && position.Y > 0 && position.Y < ele.ActualSize.Y)
            {
                
                return;
            }
        }
        GameIconsAreaVisible = false;
    }


    
    private void Grid_GameIconsArea_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            return;
        }
        GameIconsAreaVisible = false;
    }


    private void Grid_GameIconsArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }


    
    private void Grid_GameIcon_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            e.Handled = true;
            SelectIcon(icon);
        }
    }


    
    private void Grid_GameIcon_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            e.Handled = true;
            SelectIcon(icon);
            HideFullBackground();
        }
    }


    private void Grid_GameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon && !icon.IsSelected)
        {
            icon.MaskOpacity = 0;
        }
    }


    private void Grid_GameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon && !icon.IsSelected)
        {
            icon.MaskOpacity = 1;
        }
    }


    
    [RelayCommand]
    private void Pin()
    {
        IsPinned = !IsPinned;
        if (IsPinned)
        {
            GameIconsAreaVisible = true;
        }
        else
        {
            
            var temp = CurrentGameBizIcon;
            CurrentGameBizIcon = null;
            CurrentGameBizIcon = temp;
            if (!FullBackgroundVisible)
            {
                GameIconsAreaVisible = false;
            }
        }
        AppConfig.SetConfig("GameBizSelectorPinned", IsPinned ? "1" : null);
    }


    
    private void GameBizIcons_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        try
        {
            AppConfig.SetConfig("PinnedGames", string.Join(",", GameBizIcons.Select(i => i.Biz)));
            SyncSelection();
        }
        catch { }
    }


    private void SelectIcon(GameBizIcon icon)
    {
        SelectCore(icon.Biz);
    }


    
    private bool SelectCore(GameBiz biz)
    {
        if (biz == CurrentGameBiz)
        {
            return true;
        }
        CurrentGameBiz = biz;
        AppConfig.SetConfig("CurrentGameBiz", biz.ToString());
        SyncSelection();
        CurrentGameChanged?.Invoke(this, biz);
        return true;
    }


    #endregion




    #region 全屏选择层


    public bool FullBackgroundVisible => Border_FullBackground.Opacity > 0;


    private DispatcherQueueTimer? _hideFullBackgroundTimer;


    [RelayCommand]
    private void ShowFullBackground()
    {
        _hideFullBackgroundTimer?.Stop();
        Border_FullBackground.Opacity = 1;
        Border_FullBackground.IsHitTestVisible = true;
        Border_FullBackground.Visibility = Visibility.Visible;
        Border_Pin.Opacity = 1;
        Border_Pin.IsHitTestVisible = true;
        Border_Pin.Visibility = Visibility.Visible;
        GameIconsAreaVisible = true;
        _ = InitializeInstalledGamesAsync();
    }


    private void HideFullBackground()
    {
        Border_FullBackground.Opacity = 0;
        Border_FullBackground.IsHitTestVisible = false;
        Border_Pin.Opacity = 0;
        Border_Pin.IsHitTestVisible = false;
        
        _hideFullBackgroundTimer?.Stop();
        DispatcherQueueTimer timer = DispatcherQueue.CreateTimer();
        _hideFullBackgroundTimer = timer;
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (ReferenceEquals(_hideFullBackgroundTimer, timer))
            {
                Border_FullBackground.Visibility = Visibility.Collapsed;
                Border_Pin.Visibility = Visibility.Collapsed;
            }
        };
        timer.Start();
        if (!IsPinned)
        {
            GameIconsAreaVisible = false;
        }
    }


    private void Border_FullBackground_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        _isGameBizDisplayPressed = false;
        HideFullBackground();
    }


    
    private void Expander_InstalledGamesActualSize_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }


    #endregion




    #region 服务器列表


    private bool _isGameBizDisplayPressed;


    
    private void Grid_GameBizDisplay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement ele)
        {
            e.Handled = true;
            _isGameBizDisplayPressed = !_isGameBizDisplayPressed;
            if (_isGameBizDisplayPressed)
            {
                FlyoutBase.GetAttachedFlyout(ele)?.ShowAt(ele, new FlyoutShowOptions
                {
                    Placement = FlyoutPlacementMode.Bottom,
                    ShowMode = FlyoutShowMode.Transient,
                });
            }
            else
            {
                FlyoutBase.GetAttachedFlyout(ele)?.Hide();
            }
        }
    }


    
    private void Grid_GameBizDisplay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement ele && _isGameBizDisplayPressed)
        {
            FlyoutBase.GetAttachedFlyout(ele)?.ShowAt(ele, new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Bottom,
                ShowMode = FlyoutShowMode.Transient,
            });
        }
    }


    
    private void Button_GameServer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon server)
            {
                bool selected;
                try
                {
                    selected = SelectCore(Enum.Parse<GameBiz>(server.GameId, true));
                }
                catch
                {
                    selected = false;
                }
                if (selected)
                {
                    
                    if (VisualTreeHelper.GetOpenPopupsForXamlRoot(this.XamlRoot).FirstOrDefault() is Popup popup)
                    {
                        popup.IsOpen = false;
                    }
                    _isGameBizDisplayPressed = false;
                    HideFullBackground();
                }
            }
        }
        catch { }
    }


    
    private void Button_PinGameBiz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon server)
        {
            TogglePin(server);
        }
    }


    private void TogglePin(GameBizIcon server)
    {
        var biz = server.Biz;
        if (GameBizIcons.FirstOrDefault(x => x.Biz == biz) is GameBizIcon icon)
        {
            GameBizIcons.Remove(icon);
            server.IsPinned = false;
        }
        else
        {
            GameBizIcons.Add(server);
            server.IsPinned = true;
        }
        
    }


    #endregion




    #region 已安装游戏


    private CancellationTokenSource? _initializeInstalledGamesCancellationTokenSource;

    
    
    
    
    private string? _installedSignature;


    
    [RelayCommand]
    private async Task InitializeInstalledGamesAsync()
    {
        const double GB = 1 << 30;
        try
        {
            _initializeInstalledGamesCancellationTokenSource?.Cancel();
            _initializeInstalledGamesCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _initializeInstalledGamesCancellationTokenSource.Token;

            var allServers = GameBizDisplays.SelectMany(x => x.Servers).ToList();
            var exes = allServers
                .Select(s => GameRegistry.GetConfiguredExePath(s.Biz))
                .ToList();
            string signature = string.Join("|", exes);
            if (signature == _installedSignature && InstalledGamesActualSize is not null)
            {
                return; 
            }
            _installedSignature = signature;

            InstalledGames.Clear();
            InstalledGamesActualSize = null;
            var dirSizes = new Dictionary<string, long>();
            var shownDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (GameBizIcon server in allServers)
            {
                string? exe = GameRegistry.GetConfiguredExePath(server.Biz);
                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                {
                    continue; 
                }
                string dir = Path.GetFullPath(Path.GetDirectoryName(exe)!).TrimEnd('\\', '/').ToLowerInvariant();
                server.InstallPath = exe;
                if (!dirSizes.TryGetValue(dir, out long size))
                {
                    size = await Task.Run(() =>
                    {
                        try
                        {
                            
                            var options = new EnumerationOptions
                            {
                                IgnoreInaccessible = true,
                                RecurseSubdirectories = true,
                            };
                            long t = 0;
                            foreach (var f in new DirectoryInfo(Path.GetDirectoryName(exe)!).EnumerateFiles("*", options))
                            {
                                t += f.Length;
                            }
                            return t;
                        }
                        catch
                        {
                            return 0;
                        }
                    }, token);
                    dirSizes[dir] = size;
                }
                server.TotalSize = size;
                if (token.IsCancellationRequested)
                {
                    return;
                }
                
                if (shownDirs.Add(dir))
                {
                    InstalledGames.Add(server);
                }
            }
            InstalledGamesActualSize = $"{dirSizes.Values.Sum() / GB:F2}GB";
        }
        catch (OperationCanceledException) { }
        catch { }
    }


    
    [RelayCommand]
    private async Task AutoSearchInstalledGamesAsync()
    {
        await Task.Run(() =>
        {
            foreach (var group in GameRegistry.Groups)
            {
                foreach (var server in group.Servers)
                {
                    if (server.CloudUrl is not null)
                    {
                        continue;
                    }
                    string key = GameRegistry.GetExePathConfigKey(server.Biz);
                    string? exe = AppConfig.GetConfig(key);
                    if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                    {
                        continue;
                    }
                    try
                    {
                        string? result = GameAutoSearch.FindGame(server.Biz);
                        if (result is not null)
                        {
                            AppConfig.SetConfig(key, result);
                        }
                    }
                    catch { }
                }
            }
        });
        await InitializeInstalledGamesAsync();
    }


    #endregion


}
