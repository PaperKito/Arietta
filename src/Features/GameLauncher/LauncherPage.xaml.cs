using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Arietta.Core;
using Arietta.Features.Background;
using Arietta.Features.DynamicBackground;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Features.PlayTime;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

public sealed partial class LauncherPage : PageBase
{

    
    private bool _syncingBackgroundIndex;


    
    private const string PlayIcon = "\uF5B0";
    private const string PauseIcon = "\uE62E";


    
    public List<BackgroundItem> BackgroundImages { get; private set => SetProperty(ref field, value); } = new();

    
    public int CurrentBackgroundImageIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && !_syncingBackgroundIndex)
            {
                ChangeBackgroundImageIndex(value);
            }
        }
    }

    
    public bool CanStopVideo { get; private set => SetProperty(ref field, value); }

    public string StartStopButtonIcon { get; private set => SetProperty(ref field, value); } = PauseIcon;


    public GameState GameState
    {
        get => field;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(LocateGameVisibility));
            }
        }
    }

    
    public Visibility LocateGameVisibility => GameState is GameState.InstallGame ? Visibility.Visible : Visibility.Collapsed;


    public string? RunningGameInfo { get; private set => SetProperty(ref field, value); }

    
    
    
    
    public string? RunningGameTime { get; private set => SetProperty(ref field, value); }

    public GameBiz CurrentGameBiz { get; private set => SetProperty(ref field, value); } = ViewHost.MainView.CurrentGameBiz;


    #region 更新 / 预下载（借鉴 Starward 更新模块，数据来自库洛官方启动器接口）

    private GameUpdateService.GameUpdateState? _updateState;

    
    
    
    private bool _updateBusy => GameUpdateSession.Current.IsBusy;

    
    private bool _updateSessionWasActive;

    #endregion


    public LauncherPage()
    {
        this.InitializeComponent();
        UpdateGameState();
        InitializeBackgroundSwitcher();
        _ = RefreshUpdateStateAsync();
    }


    private void MainView_GameChanged(object? sender, GameBiz biz)
    {
        CurrentGameBiz = biz;
        RunningGameInfo = null;
        
        
        if (GameState is GameState.GameIsRunning && _runningBiz is not null && _runningBiz != biz)
        {
            StopRunningState();
        }
        UpdateGameState();
        EnterRunningStateIfGameRunning(biz);
        Button_PlayTime.Reload();
        GameAccountSwitcher.UpdateAccounts();
        RefreshBriefingIcon();
        
        TryPrefetchBriefing();
        InitializeBackgroundSwitcher();
        
        
        BannerAndPost.CurrentGameBiz = biz;
        _ = BannerAndPost.ReloadAsync();
        _ = RefreshUpdateStateAsync();
    }


    
    private void RefreshBriefingIcon()
    {
        try
        {
            Image_BriefingIcon.Source = DailyBriefingPanel.IconSource(CurrentGameBiz);
        }
        catch { }
    }


    private async void Button_DailyBriefing_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            await DailyBriefingPanel.ShowAsync(Button_DailyBriefing, CurrentGameBiz, GameAccountSwitcher.SelectAccount);
            
            RefreshBriefingIcon();
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.DailyBriefing);
        }
    }


    [RelayCommand]
    private void StartGame()
    {
        
        var biz = CurrentGameBiz;
        var entry = GameRegistry.GetEntry(biz);
        if (entry.CloudUrl is { } url)
        {
            
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                ApplyAfterLaunchWindowAction();
            }
            catch (Exception ex)
            {
                InAppToast.MainWindow?.Error(ex);
            }
            return;
        }
        string? exe = GameRegistry.GetConfiguredExePath(biz);
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
        {
            
            exe = GameRegistry.GetEnabledThirdPartyToolPath(biz);
            if (exe is null)
            {
                _ = OpenLauncherSettingDialogAsync();
                return;
            }
        }
        try
        {
            Button_StartGame.GameCommand = null;
            var process = StartGameProcess(exe, biz);
            if (process != null)
            {
                MonitorGameProcess(process, () =>
                {
                    
                    
                    
                    GameProcessMonitor.NotifyProcessExited(biz);
                });
                
                EnterRunningState(biz);
                
                GameProcessMonitor.SetRunning(biz, true);
                StartPlayTimeRecording();
                
                ApplyAfterLaunchWindowAction();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.LauncherPage_StartGame);
        }
        finally
        {
            if (GameState is not GameState.GameIsRunning)
            {
                Button_StartGame.GameCommand = StartGameCommand;
            }
        }
    }


    
    
    
    
    
    private static void ApplyAfterLaunchWindowAction()
    {
        try
        {
            if (AppConfig.GetConfig("StartGameWindowAction") == "Tray")
            {
                App.Current.MainWindow?.Hide();
            }
        }
        catch
        {
            
        }
    }


    
    
    
    
    
    private async Task ApplyAfterGameExitWindowActionAsync()
    {
        try
        {
            string action = AppConfig.GetConfig("AfterGameExitWindowAction") ?? "Restore";
            if (action == "CloseLauncher")
            {
                
                await RefreshPlayTimeAsync();
                App.Current.ExitApp();
            }
            else if (action != "KeepHidden")
            {
                App.Current.EnsureMainWindow();
            }
        }
        catch
        {
            
        }
    }


    [RelayCommand]
    private async Task OpenLauncherSettingDialogAsync()
    {
        var dialog = new GameLauncherSettingDialog(CurrentGameBiz) { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
        UpdateGameState();
    }


    protected override void OnLoaded()
    {
        SubscribeEvents();
        
        
        GameUpdateSession.Current.Changed += OnGameUpdateSessionChanged;
        ApplyUpdateSessionState();
        
        GameProcessMonitor.EnsureScanned();
        UpdateGameState();
        
        EnterRunningStateIfGameRunning(CurrentGameBiz);
        RefreshBriefingIcon();
        InitializeBackgroundSwitcher();
        
        TryPrefetchBriefing();
    }


    
    
    
    
    
    
    
    private void TryPrefetchBriefing(GameBiz? biz = null)
    {
        var target = biz ?? CurrentGameBiz;
        try
        {
            var account = GameAccountSwitcher.SelectAccount;
            if (account is null)
            {
                account = KrSdkAccountService.GetCurrentAccount(target);
                if (account is not null)
                {
                    KrSdkAccountStore.ApplyTo(target, account);
                }
            }
            _ = DailyBriefingPanel.PrefetchAsync(target, account);
        }
        catch { }
    }


    protected override void OnUnloaded()
    {
        UnsubscribeEvents();
        
        StopRunningState();
        
        GameUpdateSession.Current.Changed -= OnGameUpdateSessionChanged;
    }


    private bool _eventsSubscribed;

    
    
    
    
    private void SubscribeEvents()
    {
        if (_eventsSubscribed)
        {
            return;
        }
        _eventsSubscribed = true;
        ViewHost.MainView.GameChanged += MainView_GameChanged;
        BackgroundGallery.SelectionChanged += OnBackgroundSelectionChanged;
        DynamicBackgroundStore.Updated += OnDynamicBackgroundUpdated;
    }


    private void UnsubscribeEvents()
    {
        if (!_eventsSubscribed)
        {
            return;
        }
        _eventsSubscribed = false;
        ViewHost.MainView.GameChanged -= MainView_GameChanged;
        BackgroundGallery.SelectionChanged -= OnBackgroundSelectionChanged;
        DynamicBackgroundStore.Updated -= OnDynamicBackgroundUpdated;
    }


    #region 背景切换条（照搬 Starward GameLauncherPage 的底部切换条）


    
    private void InitializeBackgroundSwitcher()
    {
        try
        {
            BackgroundImages = BackgroundGallery.GetItems(CurrentGameBiz);
            var current = BackgroundGallery.Resolve(CurrentGameBiz);
            int index = BackgroundImages.FindIndex(x => x.Id == current?.Id);
            if (index < 0)
            {
                index = 0;
            }
            _syncingBackgroundIndex = true;
            CurrentBackgroundImageIndex = index;
            _syncingBackgroundIndex = false;
            
            
            PipsPager_SwitchBackgroundImage.NumberOfPages = Math.Max(1, BackgroundImages.Count);
            UpdateVideoControls();
            
            Border_SwitchBackgroundImage.Visibility = BackgroundImages.Count > 1
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
        }
    }


    
    private void ChangeBackgroundImageIndex(int index)
    {
        try
        {
            if (index < 0 || index >= BackgroundImages.Count)
            {
                return;
            }
            BackgroundGallery.SetSelectedId(CurrentGameBiz, BackgroundImages[index].Id);
        }
        catch { }
    }


    
    private void OnBackgroundSelectionChanged(object? sender, GameBiz biz)
    {
        if (biz.GetBaseBiz() != CurrentGameBiz.GetBaseBiz())
        {
            return;
        }
        if (DispatcherQueue.HasThreadAccess)
        {
            InitializeBackgroundSwitcher();
        }
        else
        {
            DispatcherQueue.TryEnqueue(InitializeBackgroundSwitcher);
        }
    }


    private void OnDynamicBackgroundUpdated(object? sender, GameBiz biz) => OnBackgroundSelectionChanged(sender, biz);


    
    private void UpdateVideoControls()
    {
        var host = DynamicBackgroundHost.Current;
        CanStopVideo = BackgroundGallery.Resolve(CurrentGameBiz)?.IsVideo == true && host?.IsPlaying == true;
        StartStopButtonIcon = host?.IsPaused == true ? PlayIcon : PauseIcon;
    }


    private void Border_SwitchBackgroundImage_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Border_SwitchBackgroundImage.Opacity = 1;
    }


    private void Border_SwitchBackgroundImage_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Border_SwitchBackgroundImage.Opacity = 0;
    }


    private int _switchBackgroundTotalDelta;

    
    private void Border_SwitchBackgroundImage_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        int delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        _switchBackgroundTotalDelta += delta;
        if (_switchBackgroundTotalDelta <= -120)
        {
            CurrentBackgroundImageIndex++;
            _switchBackgroundTotalDelta = 0;
        }
        else if (_switchBackgroundTotalDelta >= 120)
        {
            CurrentBackgroundImageIndex--;
            _switchBackgroundTotalDelta = 0;
        }
    }


    private void Button_PlayPauseBackground_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DynamicBackgroundHost.Current?.TogglePause();
            UpdateVideoControls();
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
        }
    }


    private void GridView_Backgrounds_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is BackgroundItem item)
        {
            BackgroundGallery.SetSelectedId(CurrentGameBiz, item.Id);
        }
    }


    private void Button_OpenBackgroundFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            
            string folder = BackgroundGallery.GetFolder(CurrentGameBiz);
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex);
        }
    }


    
    private async void Button_CopyBackground_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? path = BackgroundGallery.Resolve(CurrentGameBiz)?.FilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.Background_AddFailed);
                return;
            }
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetStorageItems(new[] { file });
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            InAppToast.MainWindow?.Success(Lang.Instance.Common_Copy);
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.Common_Copy);
        }
    }


    private async void Button_SaveBackgroundAs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? path = BackgroundGallery.Resolve(CurrentGameBiz)?.FilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.Background_AddFailed);
                return;
            }
            string ext = Path.GetExtension(path);
            var picker = new Windows.Storage.Pickers.FileSavePicker
            {
                SuggestedFileName = Path.GetFileNameWithoutExtension(path),
            };
            WinRT.Interop.InitializeWithWindow.Initialize(
                picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.FileTypeChoices.Add(ext.TrimStart('.').ToUpperInvariant(), new List<string> { ext });
            var target = await picker.PickSaveFileAsync();
            if (target is null)
            {
                return;
            }
            File.Copy(path, target.Path, true);
            InAppToast.MainWindow?.Success(Lang.Instance.ImageViewWindow2_SaveAs);
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.ImageViewWindow2_SaveAs);
        }
    }


    #endregion


    #region 拖放换背景（照搬 Starward：整页都是放置区，放进去即添加并选中）


    private void RootGrid_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            Border_BackgroundDragIn.Opacity = 1;
        }
    }


    private void RootGrid_DragLeave(object sender, DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
    }


    private async void RootGrid_Drop(object sender, DragEventArgs e)
    {
        Border_BackgroundDragIn.Opacity = 0;
        try
        {
            var deferral = e.GetDeferral();
            var files = await e.DataView.GetStorageItemsAsync();
            deferral.Complete();
            if (files.Count == 0 || !BackgroundGallery.IsSupportedFile(files[0].Name))
            {
                return;
            }
            await AddAndSelectBackgroundAsync(files[0].Path);
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.Background_AddFailed);
        }
    }


    
    private async Task AddAndSelectBackgroundAsync(string sourcePath)
    {
        string name = await BackgroundGallery.AddFileAsync(sourcePath, CurrentGameBiz);
        
        BackgroundAccentService.Invalidate(Path.Combine(BackgroundGallery.GetFolder(CurrentGameBiz), name));
        BackgroundGallery.SetSelectedId(CurrentGameBiz, name);
    }


    #endregion


    #region 更新 / 预下载（借鉴 Starward 更新模块，数据来自库洛官方启动器接口）

    
    private async Task RefreshUpdateStateAsync()
    {
        var biz = CurrentGameBiz;
        try
        {
            var state = await GameUpdateService.GetStateAsync(biz);
            
            if (biz != CurrentGameBiz || GameUpdateSession.Current.IsActiveFor(biz))
            {
                return;
            }
            _updateState = state;
            RenderUpdateState();
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<LauncherPage>().LogWarning(ex, "检查游戏更新失败 {Biz}", biz);
        }
    }


    
    private void OnGameUpdateSessionChanged(object? sender, EventArgs e)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            ApplyUpdateSessionState();
        }
        else
        {
            DispatcherQueue.TryEnqueue(ApplyUpdateSessionState);
        }
    }


    
    
    
    
    private void ApplyUpdateSessionState()
    {
        var session = GameUpdateSession.Current;
        if (!session.IsActiveFor(CurrentGameBiz))
        {
            if (_updateSessionWasActive)
            {
                _updateSessionWasActive = false;
                
                if (GameState is GameState.Installing)
                {
                    Button_StartGame.ResetInstallState();
                    Button_StartGame.GameCommand = StartGameCommand;
                }
                UpdateGameState();
                
                
                
                
                RenderUpdateState();
                _ = RefreshUpdateStateAsync();
                return;
            }
            RenderUpdateState();
            return;
        }
        _updateSessionWasActive = true;
        RenderUpdateSession(session);
    }


    
    private void RenderUpdateSession(GameUpdateSession session)
    {
        
        
        if (session.IsPausing)
        {
            Button_StartGame.ShowPausing();
            Popup_PreDownloadProgress.IsOpen = false;
            if (session.Kind == GameUpdateService.UpdateKind.Predownload)
            {
                Button_PreDownload.Visibility = Visibility.Visible;
                Button_PreDownload.IsEnabled = true;
                Text_PreDownload.Text = Lang.Instance.GameUpdate_Pausing;
                ToolTipService.SetToolTip(Button_PreDownload, null);
            }
            return;
        }

        if (session.Kind == GameUpdateService.UpdateKind.Update)
        {
            
            Button_PreDownload.Visibility = Visibility.Collapsed;
            Popup_PreDownloadProgress.IsOpen = false;
            if (GameState is not GameState.Installing)
            {
                GameState = GameState.Installing;
                Button_StartGame.ResetInstallState();
                Button_StartGame.GameCommand = PauseGameUpdateCommand;
            }
            if (session.LastProgress is { } p)
            {
                Button_StartGame.UpdateGameInstallProgress(p);
            }
            return;
        }

        
        
        Button_PreDownload.Visibility = Visibility.Visible;
        Button_PreDownload.IsEnabled = true;
        Text_PreDownload.Text = session.IsApplying
            ? Lang.Instance.GameUpdate_Applying
            : $"{session.ProgressValue / 10.0:0}%";
        ToolTipService.SetToolTip(Button_PreDownload, null);
        UpdatePreDownloadPopup(session);
    }


    private void RenderUpdateState()
    {
        var state = _updateState;
        if (state is not { HasUpdate: true } || state.TargetVersion is null)
        {
            Button_PreDownload.Visibility = Visibility.Collapsed;
            Popup_PreDownloadProgress.IsOpen = false;
            ApplyUpdateStateToMainButton();
            return;
        }
        
        if (GameUpdateSession.Current.IsActiveFor(CurrentGameBiz))
        {
            RenderUpdateSession(GameUpdateSession.Current);
            return;
        }
        Popup_PreDownloadProgress.IsOpen = false;
        Button_PreDownload.IsEnabled = true;
        
        Text_PreDownload.Text = state.HasStagedProgress
            ? Lang.Instance.GameUpdate_ContinuePredownload
            : Lang.Instance.GameUpdate_PredownloadButton;
        ToolTipService.SetToolTip(Button_PreDownload, Lang.Instance.GameUpdate_PredownloadButton);

        
        
        if (state.Kind == GameUpdateService.UpdateKind.Update)
        {
            Button_PreDownload.Visibility = Visibility.Collapsed;
            ApplyUpdateStateToMainButton();
            return;
        }

        
        
        bool done = state.PredownloadedCount > 0
            && state.Plan is { Count: > 0 }
            && state.PredownloadedCount >= state.Plan.Count;
        Button_PreDownload.Visibility = Visibility.Visible;
        if (done)
        {
            
            Button_PreDownload.IsEnabled = false;
            Text_PreDownload.Text = Lang.Instance.GameUpdate_PredownloadFinished;
            ToolTipService.SetToolTip(Button_PreDownload, string.Format(Lang.Instance.GameUpdate_PredownloadDone, state.TargetVersion));
        }
        ApplyUpdateStateToMainButton();
    }


    
    
    
    
    private void ApplyUpdateStateToMainButton()
    {
        if (_updateBusy || GameState is GameState.GameIsRunning or GameState.InstallGame or GameState.Installing)
        {
            return;
        }
        
        if (_updateState is { HasUpdate: true, Kind: GameUpdateService.UpdateKind.Update })
        {
            GameState = GameState.UpdateGame;
            Button_StartGame.GameCommand = UpdateGameCommand;
        }
        else
        {
            if (GameState is GameState.UpdateGame)
            {
                GameState = GameState.StartGame;
            }
            
            Button_StartGame.GameCommand = GameState is GameState.InstallGame ? InstallGameCommand : StartGameCommand;
        }
    }


    
    [RelayCommand]
    private void UpdateGame()
    {
        if (_updateState is not { HasUpdate: true, Kind: GameUpdateService.UpdateKind.Update } || _updateBusy)
        {
            return;
        }
        _ = RunUpdateActionAsync(GameUpdateService.UpdateKind.Update);
    }


    
    [RelayCommand]
    private void PauseGameUpdate()
    {
        GameUpdateSession.Current.Cancel();
    }


    
    
    
    private async void Button_PreDownload_Click(object sender, RoutedEventArgs e)
    {
        if (_updateState is not { HasUpdate: true, Kind: GameUpdateService.UpdateKind.Predownload })
        {
            return;
        }
        
        if (GameUpdateSession.Current.IsBusy)
        {
            GameUpdateSession.Current.Cancel();
            return;
        }
        if (_updateBusy)
        {
            return;
        }
        await RunUpdateActionAsync(GameUpdateService.UpdateKind.Predownload);
    }


    
    private void Button_PreDownload_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var session = GameUpdateSession.Current;
        if (!session.IsActiveFor(CurrentGameBiz))
        {
            return;
        }
        UpdatePreDownloadPopup(session);
        Popup_PreDownloadProgress.IsOpen = true;
    }


    private void Button_PreDownload_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Popup_PreDownloadProgress.IsOpen = false;
    }


    
    private void UpdatePreDownloadPopup(GameUpdateSession session)
    {
        var p = session.LastProgress;
        if (session.IsApplying || p is { Phase: DownloadPhase.Verifying or DownloadPhase.Applying })
        {
            
            Popup_PreDownloadStatus.Text = session.ProgressText;
            Popup_PreDownloadBytesRow.Visibility = Visibility.Collapsed;
            Popup_PreDownloadSpeedRow.Visibility = Visibility.Collapsed;
            Popup_PreDownloadRemainRow.Visibility = Visibility.Collapsed;
            return;
        }
        Popup_PreDownloadStatus.Text = string.Format(Lang.Instance.GameUpdate_Predownloading, session.TargetVersion);
        Popup_PreDownloadBytes.Text = $"{GameUpdateSession.FormatSize(session.ReceivedBytes)} / {GameUpdateSession.FormatSize(session.TotalBytes)}";
        Popup_PreDownloadBytesRow.Visibility = Visibility.Visible;
        long speed = p?.SpeedBytesPerSec ?? 0;
        Popup_PreDownloadSpeed.Text = $"{GameUpdateSession.FormatSize(speed)}/s";
        Popup_PreDownloadSpeedRow.Visibility = Visibility.Visible;
        
        if (speed > 0 && session.TotalBytes > session.ReceivedBytes)
        {
            var remain = TimeSpan.FromSeconds((session.TotalBytes - session.ReceivedBytes) / (double)speed);
            Popup_PreDownloadRemain.Text = $"{(int)remain.TotalHours}h {remain.Minutes}m";
            Popup_PreDownloadRemainRow.Visibility = Visibility.Visible;
        }
        else
        {
            Popup_PreDownloadRemainRow.Visibility = Visibility.Collapsed;
        }
    }


    private async Task RunUpdateActionAsync(GameUpdateService.UpdateKind kind)
    {
        var state = _updateState;
        if (state?.Target is null || state.TargetVersion is null)
        {
            return;
        }
        var biz = CurrentGameBiz;
        if (GameUpdateService.GetGameDir(biz) is null)
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.GameUpdate_NoGameDir);
            return;
        }

        
        if (kind == GameUpdateService.UpdateKind.Predownload)
        {
            var confirm = new PreDownloadDialog(biz, state)
            {
                XamlRoot = this.XamlRoot,
            };
            if (await confirm.ShowAsync() != ContentDialogResult.None || !confirm.Confirmed)
            {
                return;
            }
        }

        
        
        GameUpdateSession.Current.Start(biz, state);
    }


    
    private string? _lastInstallDir;


    
    
    
    
    
    [RelayCommand]
    private async Task InstallGameAsync()
    {
        if (_updateBusy || GameUpdateSession.Current.IsBusy)
        {
            return;
        }
        var biz = CurrentGameBiz;
        try
        {
            
            var state = await GameUpdateService.GetInstallStateAsync(biz);
            if (state is not { TargetVersion: not null })
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.GameUpdate_ErrNoManifest);
                return;
            }

            
            
            
            string? persisted = GameUpdateService.GetPersistedInstallDir(biz);
            if (persisted is not null
                && Directory.Exists(Path.Combine(persisted, "predownload", state.TargetVersion)))
            {
                
                StartInstallSession(biz, state, persisted, GameUpdateService.GetInstallTier(biz));
                return;
            }

            
            
            
            var tiers = await GameUpdateService.GetTierOptionsAsync(biz);
            var dialog = new InstallGameDialog(biz, state.PlanInfo!, state.TotalBytes, tiers,
                name => GameUpdateService.BuildTierPlanAsync(biz, name), persisted ?? _lastInstallDir)
            {
                XamlRoot = this.XamlRoot,
            };
            
            await dialog.ShowAsync();
            if (!dialog.Confirmed)
            {
                return;
            }

            
            
            
            var entry = GameRegistry.GetEntry(biz);
            string dir = dialog.InstallationPath;
            string? hint = entry.DirHint;
            if (hint is not null && !dir.Contains(hint, StringComparison.OrdinalIgnoreCase))
            {
                dir = Path.Combine(dir, entry.DefaultInstallDirName);
            }
            _lastInstallDir = dir;
            
            AppConfig.SetConfig(GameRegistry.GetInstallDirConfigKey(biz), dir);
            
            
            string? tier = dialog.SelectedTier;
            GameUpdateService.SetInstallTier(biz, tier);
            if (tier is not null && dialog.TotalBytes != state.TotalBytes)
            {
                state = new GameUpdateService.GameUpdateState
                {
                    Kind = state.Kind,
                    LocalVersion = state.LocalVersion,
                    Target = state.Target,
                    TargetVersion = state.TargetVersion,
                    Changelog = state.Changelog,
                    TotalBytes = dialog.TotalBytes,
                };
            }
            StartInstallSession(biz, state, dir, tier);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<LauncherPage>().LogError(ex, "安装游戏失败 {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, Lang.Instance.LauncherPage_InstallGame);
        }
    }

    
    private void StartInstallSession(GameBiz biz, GameUpdateService.GameUpdateState state, string dir, string? tier = null)
    {
        GameUpdateSession.Current.StartInstall(biz, state, dir, tier);
        Button_StartGame.ResetInstallState();
        GameState = GameState.Installing;
        Button_StartGame.GameCommand = PauseGameUpdateCommand;
    }


    #endregion


    #region 定位游戏（对齐 Starward 的「已安装？ 定位游戏」）

    private async void Hyperlink_LocateGame_Click(object sender, RoutedEventArgs e)
    {
        var biz = CurrentGameBiz;
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            string? exe = FindGameExeInDir(biz, folder.Path, 3);
            if (exe is null || !GameRegistry.IsExePathMatchBiz(biz, exe))
            {
                InAppToast.MainWindow?.Warning(string.Format(
                    Lang.Instance.GameLauncherSettingDialog_ExpectExe,
                    string.Join(" / ", GameRegistry.GetEntry(biz).ExeNames)));
                return;
            }

            AppConfig.SetConfig(GameRegistry.GetExePathConfigKey(biz), exe);
            AppConfig.SetConfig(GameRegistry.GetInstallDirConfigKey(biz), Path.GetDirectoryName(exe));
            UpdateGameState();
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<LauncherPage>().LogError(ex, "定位游戏失败 {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, Lang.Instance.InstallGameDialog_LocateGame);
        }
    }

    
    private static string? FindGameExeInDir(GameBiz biz, string dir, int depth)
    {
        foreach (string name in GameRegistry.GetEntry(biz).ExeNames)
        {
            string path = Path.Combine(dir, name);
            if (File.Exists(path))
            {
                return path;
            }
        }
        if (depth <= 0)
        {
            return null;
        }
        try
        {
            foreach (string sub in Directory.EnumerateDirectories(dir))
            {
                string? found = FindGameExeInDir(biz, sub, depth - 1);
                if (found is not null)
                {
                    return found;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            
        }
        return null;
    }

    #endregion


    private void UpdateGameState()
    {
        
        if (_updateBusy)
        {
            return;
        }
        var entry = GameRegistry.GetEntry(CurrentGameBiz);
        if (entry.CloudUrl is not null)
        {
            
            GameState = GameState.StartGame;
            RunningGameInfo = null;
            ApplyUpdateStateToMainButton();
            return;
        }
        string? exe = GameRegistry.GetConfiguredExePath(CurrentGameBiz);
        if ((string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            && GameRegistry.GetEnabledThirdPartyToolPath(CurrentGameBiz) is { } tool)
        {
            
            exe = tool;
        }
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
        {
            
            Button_StartGame.IsResumeInstall = GameUpdateService.HasResumableInstall(CurrentGameBiz);
            GameState = GameState.InstallGame;
            Button_StartGame.GameCommand = InstallGameCommand;
            RunningGameInfo = null;
        }
        else
        {
            GameState = GameState.StartGame;
            RunningGameInfo = null;
        }
        ApplyUpdateStateToMainButton();
    }


    #region 游戏运行中：本次实时时长（对齐 Starward 的 GameProcess + RunningGameTime）


    
    private Process? _runningProcess;

    
    private DateTime? _runningStart;

    
    
    
    
    
    
    
    
    private System.Timers.Timer? _runningTimer;

    
    private readonly object _runningGate = new();

    
    private GameBiz? _runningBiz;

    
    private DateTime _runningEnteredAt;

    
    private DateTime _nextRunningProbe;

    
    private bool _runningAdopted;

    
    private DateTime? _missingSince;

    
    private static readonly TimeSpan RunningProbeTimeout = TimeSpan.FromSeconds(30);

    
    private static readonly TimeSpan RunningMissGrace = TimeSpan.FromSeconds(5);

    private readonly PlayTimeRecordService _recordService = PlayTimeRecordService.Create();


    
    
    
    private void EnterRunningState(GameBiz biz)
    {
        GameState = GameState.GameIsRunning;
        Button_StartGame.GameCommand = null!;
        RunningGameInfo = null;
        RunningGameTime = null;
        lock (_runningGate)
        {
            _runningBiz = biz;
            _runningStart = null;
            _runningAdopted = false;
            _missingSince = null;
        }
        _runningEnteredAt = DateTime.Now;
        _nextRunningProbe = DateTime.MinValue;
        AppConfig.GetLogger<LauncherPage>().LogInformation("Enter running state: {biz}", biz);
        if (_runningTimer is null)
        {
            
            _runningTimer = new System.Timers.Timer(1000);
            _runningTimer.Elapsed += (_, _) => UpdateRunningTime();
        }
        _runningTimer.Start();
        UpdateRunningTime();
    }


    
    
    
    
    
    
    
    private void ExitRunningState()
    {
        GameBiz? biz = _runningBiz;
        StopRunningState();
        Button_StartGame.GameCommand = StartGameCommand;
        UpdateGameState();
        
        if (biz is not null)
        {
            GameProcessMonitor.SetRunning(biz.Value, false);
        }
        
        GameAccountSwitcher.UpdateAccounts();
        
        _ = ApplyAfterGameExitWindowActionAsync();
    }


    
    private void StopRunningState()
    {
        _runningTimer?.Stop();
        lock (_runningGate)
        {
            ReleaseRunningProcess();
            _runningBiz = null;
            _runningStart = null;
            _runningAdopted = false;
            _missingSince = null;
        }
        RunningGameTime = null;
        RunningGameInfo = null;
    }


    
    
    
    
    private void EnterRunningStateIfGameRunning(GameBiz biz)
    {
        if (_updateBusy || GameState is GameState.GameIsRunning)
        {
            return;
        }
        using Process? running = _recordService.FindRunningClient(biz);
        if (running is not null)
        {
            EnterRunningState(biz);
            
            
            
            
            _ = _recordService.StartProcessToLogAsync(new GameId(biz, biz.ToString()), running.Id);
        }
    }


    
    
    
    
    
    
    
    
    
    private void UpdateRunningTime()
    {
        GameBiz? biz;
        Process? tracked;
        DateTime? start;
        lock (_runningGate)
        {
            biz = _runningBiz;
            tracked = _runningProcess;
            start = _runningStart;
        }
        if (biz is not { } target)
        {
            return;
        }
        bool exited = tracked is not null && HasExited(tracked);
        if (exited)
        {
            
            lock (_runningGate)
            {
                ReleaseRunningProcess();
            }
            _nextRunningProbe = DateTime.MinValue;
        }
        else if (DateTime.Now < _nextRunningProbe)
        {
            
            if (start is { } s)
            {
                
                string text = TimeSpanToString(DateTime.Now - s);
                DispatcherQueue.TryEnqueue(() => RunningGameTime = text);
            }
            return;
        }
        bool missing;
        lock (_runningGate)
        {
            missing = _missingSince is not null;
        }
        
        _nextRunningProbe = DateTime.Now.AddSeconds(missing || exited ? 1 : 2);
        Process? client = _recordService.FindRunningClient(target);
        if (client is not null)
        {
            if (!exited && tracked is not null && client.Id == tracked.Id)
            {
                
                client.Dispose();
                lock (_runningGate)
                {
                    _runningAdopted = true;
                    _missingSince = null;
                }
            }
            else
            {
                AdoptRunningProcess(target, client);
            }
            return;
        }
        DateTime missFrom;
        bool adopted;
        bool firstMiss;
        lock (_runningGate)
        {
            firstMiss = _missingSince is null;
            _missingSince ??= DateTime.Now;
            missFrom = _missingSince.Value;
            adopted = _runningAdopted;
        }
        if (firstMiss)
        {
            
            AppConfig.GetLogger<LauncherPage>().LogWarning("Game process of {biz} not found yet (adopted={adopted})", target, adopted);
        }
        bool ended = adopted
            ? DateTime.Now - missFrom >= RunningMissGrace
            : DateTime.Now - _runningEnteredAt >= RunningProbeTimeout;
        if (ended)
        {
            DispatcherQueue.TryEnqueue(ExitRunningState);
        }
    }


    
    
    
    
    
    
    
    private void AdoptRunningProcess(GameBiz biz, Process client)
    {
        string? info = null;
        string? timeText = null;
        lock (_runningGate)
        {
            if (_runningBiz != biz)
            {
                
                client.Dispose();
                return;
            }
            _runningProcess?.Dispose();
            _runningProcess = client;
            _runningAdopted = true;
            _missingSince = null;
            try
            {
                DateTime start = client.StartTime;
                _runningStart = _runningStart is { } earlier && earlier < start ? earlier : start;
                if (_runningStart is { } s)
                {
                    timeText = TimeSpanToString(DateTime.Now - s);
                }
                info = $"{client.ProcessName}.exe ({client.Id})";
                AppConfig.GetLogger<LauncherPage>().LogInformation("Adopted game process: {biz}, {name} ({pid}), started at {start}", biz, client.ProcessName, client.Id, start);
            }
            catch
            {
                
            }
        }
        DispatcherQueue.TryEnqueue(() =>
        {
            RunningGameInfo = info ?? RunningGameInfo;
            if (timeText is not null)
            {
                RunningGameTime = timeText;
            }
        });
    }


    
    private void ReleaseRunningProcess()
    {
        _runningProcess?.Dispose();
        _runningProcess = null;
    }


    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            
            return true;
        }
    }


    
    private static string TimeSpanToString(TimeSpan value)
    {
        return $"{value.Days * 24 + value.Hours:D2}:{value.Minutes:D2}:{value.Seconds:D2}";
    }


    #endregion


    
    
    
    
    
    
    
    
    
    private void StartPlayTimeRecording()
    {
        var biz = CurrentGameBiz;
        _ = Task.Run(async () =>
        {
            try
            {
                await PlayTimeRecordService.Create()
                                           .StartProcessToLogAsync(new GameId(biz, biz.ToString()));
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<LauncherPage>().LogError(ex, "Start play time recording: {biz}", biz);
            }
        });
    }


    
    private async Task RefreshPlayTimeAsync()
    {
        await Task.Delay(1500);
        Button_PlayTime.Reload();
    }


    
    
    
    
    private Process? StartGameProcess(string exePath, GameBiz biz)
    {
        string args = AppConfig.GetConfig(GameRegistry.GetStartGameArgumentConfigKey(biz)) ?? "";
        
        
        
        
        if (biz is GameBiz.WutheringWaves or GameBiz.WutheringWavesGlobal or GameBiz.WutheringWavesBilibili
            && !args.Contains("krqlv", StringComparison.OrdinalIgnoreCase)
            && Path.GetDirectoryName(exePath) is { } gameDir)
        {
            string lv = Directory.Exists(Path.Combine(gameDir, "Client", "Content", "UHD")) ? "uhd"
                : Directory.Exists(Path.Combine(gameDir, "Client", "Content", "SD")) ? "sd"
                : "hd";
            args = string.IsNullOrEmpty(args) ? $"-krqlv={lv}" : $"{args} -krqlv={lv}";
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = true,
            Arguments = args,
        };
        return Process.Start(startInfo);
    }


    
    
    
    private void MonitorGameProcess(Process process, Action onExited)
    {
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => dispatcherQueue.TryEnqueue(() => onExited());
        
        _ = Task.Run(async () =>
        {
            try
            {
                await process.WaitForExitAsync();
                dispatcherQueue.TryEnqueue(() => onExited());
            }
            catch { }
        });
    }

}
