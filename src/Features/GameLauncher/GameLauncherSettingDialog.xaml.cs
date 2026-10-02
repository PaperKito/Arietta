using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Features.Background;
using Arietta.Features.DynamicBackground;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Features.Screenshot;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Arietta.Features.GameLauncher;

[ObservableObject]
public sealed partial class GameLauncherSettingDialog : ContentDialog
{
    private readonly ILogger<GameLauncherSettingDialog> _logger = AppConfig.GetLogger<GameLauncherSettingDialog>();

    public GameLauncherSettingDialog() : this(GameBiz.WutheringWaves) { }

    public GameLauncherSettingDialog(GameBiz biz)
    {
        CurrentGameBiz = biz;
        var entry = GameRegistry.GetEntry(biz);
        CurrentGameBizIcon = new GameSettingIcon
        {
            GameName = entry.Name,
            GameIcon = new Uri(entry.GameIconPath),
            ServerIcon = entry.ServerIconPath is null ? null : new Uri(entry.ServerIconPath),
            ServerCode = entry.ServerCode,
        };
        this.InitializeComponent();
        InitializeFromConfig();
    }

    public GameBiz CurrentGameBiz { get; private set; }

    public GameSettingIcon CurrentGameBizIcon { get; private set; } = new();

    public string? InstallPath { get; set => SetProperty(ref field, value); }
    public bool UninstallAndRepairEnabled { get; set => SetProperty(ref field, value); }
    public string? GameSize { get; set => SetProperty(ref field, value); }
    public string? UninstallError { get; set => SetProperty(ref field, value); }

    
    public string? LocateError { get => field; set { SetProperty(ref field, value); OnPropertyChanged(nameof(LocateErrorVisibility)); } }
    public Visibility LocateErrorVisibility => LocateError is null ? Visibility.Collapsed : Visibility.Visible;
    public bool EnableBannerAndPost
    {
        get => _enableBannerAndPost;
        set
        {
            if (SetProperty(ref _enableBannerAndPost, value))
            {
                
                GameNoticeSettings.SetShowBannerAndPost(value);
                AnnouncementSettingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private bool _enableBannerAndPost;

    
    public static event EventHandler? AnnouncementSettingChanged;

    
    
    
    
    public static void RaiseAnnouncementSettingChanged() => AnnouncementSettingChanged?.Invoke(null, EventArgs.Empty);

    
    
    
    private bool _enableCustomBg;
    public bool EnableCustomBg
    {
        get => _enableCustomBg;
        set
        {
            if (SetProperty(ref _enableCustomBg, value))
            {
                AppConfig.SetConfig(GameRegistry.GetEnableCustomBgConfigKey(CurrentGameBiz), value ? "1" : "0");
                
                BackgroundGallery.SetSelectedId(
                    CurrentGameBiz,
                    value && CustomBg is { Length: > 0 } ? Path.GetFileName(CustomBg) : null);
                CustomBackgroundChanged?.Invoke(this, CustomBg ?? "");
            }
        }
    }
    public string? CustomBg { get; set => SetProperty(ref field, value); }

    private double _videoBgVolume;
    
    public double VideoBgVolume
    {
        get => _videoBgVolume;
        set
        {
            if (SetProperty(ref _videoBgVolume, value))
            {
                AppConfig.SetConfig(DynamicBackgroundStore.VolumeConfigKey, ((int)Math.Round(value)).ToString());
                DynamicBackgroundHost.Current?.SetVolume(value);
            }
        }
    }
    public string VideoBgVolumeButtonIcon { get; set => SetProperty(ref field, "\uE767"); }
    public string? ChangeBgError { get; set => SetProperty(ref field, value); }

    
    private bool _fpsCapEnabled;
    public bool FpsCapEnabled
    {
        get => _fpsCapEnabled;
        set
        {
            if (SetProperty(ref _fpsCapEnabled, value))
            {
                
                WriteFpsCap(value ? (int)Math.Round(_fpsSliderValue) : 0);
                OnPropertyChanged(nameof(FpsHeaderText));
            }
        }
    }

    
    private double _fpsSliderValue = 30;
    public double FpsSliderValue
    {
        get => _fpsSliderValue;
        set
        {
            if (SetProperty(ref _fpsSliderValue, value))
            {
                if (_fpsCapEnabled)
                {
                    WriteFpsCap((int)Math.Round(value));
                }
                OnPropertyChanged(nameof(FpsHeaderText));
            }
        }
    }

    
    public string FpsHeaderText => $"{Lang.Instance.GameLauncherSettingDialog_BgFps} ({FpsCapText})";

    
    public string FpsCapText => _fpsCapEnabled
        ? $"{(int)Math.Round(_fpsSliderValue)} fps"
        : Lang.Instance.GameLauncherSettingDialog_FpsUnlimited;

    private void WriteFpsCap(int fps)
    {
        AppConfig.SetConfig(DynamicBackgroundStore.FpsCapConfigKey, fps.ToString());
        DynamicBackgroundHost.Current?.SetFpsCap(fps);
    }

    
    private string _bgQuality = "auto";
    public string BgQuality
    {
        get => _bgQuality;
        set
        {
            if (SetProperty(ref _bgQuality, value))
            {
                AppConfig.SetConfig(DynamicBackgroundStore.QualityConfigKey, value);
                DynamicBackgroundHost.Current?.SetQuality(value);
            }
        }
    }

    
    public int BgQualityIndex
    {
        get => _bgQuality switch { "high" => 1, "medium" => 2, "low" => 3, _ => 0 };
        set => BgQuality = value switch { 1 => "high", 2 => "medium", 3 => "low", _ => "auto" };
    }

    
    private bool _pauseOnGameRun;
    public bool PauseOnGameRun
    {
        get => _pauseOnGameRun;
        set
        {
            if (SetProperty(ref _pauseOnGameRun, value))
            {
                AppConfig.SetConfig(DynamicBackgroundStore.PauseOnGameConfigKey, value ? "1" : "0");
                DynamicBackgroundHost.Current?.RefreshGameRunningPause();
            }
        }
    }

    
    public string? StartGameArgument
    {
        get => field;
        set { if (SetProperty(ref field, value)) AppConfig.SetConfig(GameRegistry.GetStartGameArgumentConfigKey(CurrentGameBiz), string.IsNullOrWhiteSpace(value) ? null : value); }
    }

    
    private bool _enableThirdPartyTool;
    public bool EnableThirdPartyTool
    {
        get => _enableThirdPartyTool;
        set { if (SetProperty(ref _enableThirdPartyTool, value)) AppConfig.SetConfig(GameRegistry.GetEnableThirdPartyToolConfigKey(CurrentGameBiz), value ? "1" : "0"); }
    }
    public string? ThirdPartyToolPath { get; set => SetProperty(ref field, value); }


    private void InitializeFromConfig()
    {
        string configKey = GameRegistry.GetExePathConfigKey(CurrentGameBiz);
        string? exePath = GameRegistry.GetConfiguredExePath(CurrentGameBiz);
        
        InstallPath = exePath ?? GameUpdateService.GetPersistedInstallDir(CurrentGameBiz);
        
        if (exePath is null && AppConfig.GetConfig(configKey) is not null)
        {
            AppConfig.SetConfig(configKey, null);
        }
        
        UninstallAndRepairEnabled = exePath is not null;
        _enableBannerAndPost = GameNoticeSettings.ShowBannerAndPost;
        OnPropertyChanged(nameof(EnableBannerAndPost));
        
        CustomBg = AppConfig.GetConfig(GameRegistry.GetCustomBgConfigKey(CurrentGameBiz));
        _enableCustomBg = AppConfig.GetConfig(GameRegistry.GetEnableCustomBgConfigKey(CurrentGameBiz)) switch
        {
            "0" => false,
            "1" => true,
            
            _ => CustomBg is { Length: > 0 } && File.Exists(CustomBg),
        };
        OnPropertyChanged(nameof(EnableCustomBg));
        _videoBgVolume = DynamicBackgroundHost.GetConfiguredVolume();
        VideoBgVolumeButtonIcon = _videoBgVolume <= 0 ? "\uE74F" : "\uE767";
        OnPropertyChanged(nameof(VideoBgVolume));
        
        int fpsCap = int.TryParse(AppConfig.GetConfig(DynamicBackgroundStore.FpsCapConfigKey), out int v) ? v : 30;
        _fpsCapEnabled = fpsCap > 0;
        
        _fpsSliderValue = fpsCap is >= 10 and <= 60 ? fpsCap : 30;
        OnPropertyChanged(nameof(FpsCapEnabled));
        OnPropertyChanged(nameof(FpsSliderValue));
        OnPropertyChanged(nameof(FpsHeaderText));
        var configuredQuality = AppConfig.GetConfig(DynamicBackgroundStore.QualityConfigKey);
        _bgQuality = configuredQuality is "high" or "medium" or "low" ? configuredQuality : "auto";
        OnPropertyChanged(nameof(BgQuality));
        _pauseOnGameRun = AppConfig.GetConfig(DynamicBackgroundStore.PauseOnGameConfigKey) != "0";
        OnPropertyChanged(nameof(PauseOnGameRun));
        StartGameArgument = AppConfig.GetConfig(GameRegistry.GetStartGameArgumentConfigKey(CurrentGameBiz));
        _enableThirdPartyTool = AppConfig.GetConfig(GameRegistry.GetEnableThirdPartyToolConfigKey(CurrentGameBiz)) == "1";
        OnPropertyChanged(nameof(EnableThirdPartyTool));
        ThirdPartyToolPath = AppConfig.GetConfig(GameRegistry.GetThirdPartyToolPathConfigKey(CurrentGameBiz));
        if (exePath is not null)
        {
            _ = UpdateGameSizeAsync(exePath);
        }
    }


    private async Task UpdateGameSizeAsync(string exePath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(exePath);
            if (dir is null || !Directory.Exists(dir))
            {
                return;
            }
            long size = await Task.Run(() => GetDirectorySize(new DirectoryInfo(dir)));
            GameSize = FormatSize(size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update game size");
        }
    }

    private static long GetDirectorySize(DirectoryInfo dir)
    {
        long size = 0;
        try
        {
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }
        }
        catch { }
        return size;
    }


    [RelayCommand]
    private void Close()
    {
        Hide();
    }

    [RelayCommand]
    private async Task LocateGame()
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.FileTypeFilter.Add(".exe");
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                SetInstallPath(file.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Locate game");
            InAppToast.MainWindow?.Error(ex);
        }
    }

    private void SetInstallPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }
        
        if (!GameRegistry.IsExePathMatchBiz(CurrentGameBiz, path))
        {
            LocateError = Lang.Instance.GameLauncherSettingDialog_WrongServerExe;
            return;
        }
        
        string exeName = Path.GetFileName(path);
        if (!GameRegistry.GetEntry(CurrentGameBiz).ExeNames.Contains(exeName, StringComparer.OrdinalIgnoreCase))
        {
            LocateError = string.Format(Lang.Instance.GameLauncherSettingDialog_ExpectExe, string.Join(" / ", GameRegistry.GetEntry(CurrentGameBiz).ExeNames));
            return;
        }
        LocateError = null;
        AppConfig.SetConfig(GameRegistry.GetExePathConfigKey(CurrentGameBiz), path);
        InstallPath = path;
        UninstallAndRepairEnabled = true;
        _ = UpdateGameSizeAsync(path);
    }

    [RelayCommand]
    private void ShowUninstallGameWarning()
    {
        try
        {
            UninstallError = null;
            if (GetInstallDir() is null)
            {
                return;
            }
            
            string dir = GetInstallDir()!;
            string[] shotDirs;
            try
            {
                shotDirs = Directory.GetDirectories(dir, "Screenshot*", SearchOption.AllDirectories);
            }
            catch
            {
                shotDirs = [];
            }
            SetUninstallScreenshotCount(shotDirs.Length == 0 ? 0 : ScreenshotService.EnumerateImages(shotDirs).Count);
            Grid_UninstallWarning.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show uninstall game warning");
            UninstallError = ex.Message;
        }
    }

    
    [RelayCommand]
    private async Task UninstallGame()
    {
        try
        {
            UninstallError = null;
            string? dir = GetInstallDir();
            if (dir is null)
            {
                return;
            }
            if (GameProcessMonitor.IsRunning(CurrentGameBiz))
            {
                UninstallError = Lang.Instance.GameLauncherPage_GameIsRunning;
                return;
            }
            bool deleted = await Task.Run(() =>
            {
                if (!Directory.Exists(dir))
                {
                    return false;
                }
                
                foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                }
                Directory.Delete(dir, true);
                return true;
            });
            ClearInstallPathUi();
            InAppToast.MainWindow?.Success(deleted ? Lang.Instance.GameLauncherSettingDialog_Uninstalled : Lang.Instance.GameLauncherSettingDialog_PathRemoved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game {Biz}", CurrentGameBiz);
            UninstallError = ex.Message;
        }
    }

    
    [RelayCommand]
    private async Task BackupAndUninstall()
    {
        try
        {
            string? dir = GetInstallDir();
            if (dir is null)
            {
                return;
            }
            var picker = new FolderPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }
            string[] shotDirs = Directory.GetDirectories(dir, "Screenshot*", SearchOption.AllDirectories);
            await Task.Run(() => MoveScreenshots(shotDirs, Path.Combine(folder.Path, CurrentGameBiz.ToString())));
            await UninstallGame();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup and uninstall game {Biz}", CurrentGameBiz);
            UninstallError = ex.Message;
        }
    }

    
    private static void MoveScreenshots(string[] folders, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (string source in folders)
        {
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(dest, Path.GetFileName(file));
                if (!File.Exists(target))
                {
                    File.Move(file, target);
                }
            }
        }
    }

    
    private string? GetInstallDir()
    {
        string? dir = Path.GetDirectoryName(InstallPath);
        if (dir is null || !Directory.Exists(dir))
        {
            return null;
        }
        string full = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (full == Path.GetPathRoot(full)!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            UninstallError = Lang.Instance.GameLauncherSettingDialog_CannotDeleteDriveRoot;
            return null;
        }
        return full;
    }

    private void ClearInstallPathUi()
    {
        AppConfig.SetConfig(GameRegistry.GetExePathConfigKey(CurrentGameBiz), null);
        InstallPath = null;
        UninstallAndRepairEnabled = false;
        GameSize = null;
        SetUninstallScreenshotCount(0);
        Grid_UninstallWarning.Visibility = Visibility.Collapsed;
    }

    private int _uninstallScreenshotCount;
    public Visibility UninstallScreenshotVisibility => _uninstallScreenshotCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public string UninstallScreenshotHint => string.Format(Lang.Instance.GameLauncherSettingDialog_ScreenshotsFound, _uninstallScreenshotCount);
    private void SetUninstallScreenshotCount(int count)
    {
        if (_uninstallScreenshotCount != count)
        {
            _uninstallScreenshotCount = count;
            OnPropertyChanged(nameof(UninstallScreenshotVisibility));
            OnPropertyChanged(nameof(UninstallScreenshotHint));
        }
    }

    [RelayCommand]
    private void OpenInstalGameFolder()
    {
        try
        {
            if (InstallPath is { } path && File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open install game folder");
        }
    }

    
    [RelayCommand]
    private void DeleteGameInstllPath()
    {
        ClearInstallPathUi();
    }

    #region 校验 / 修复游戏

    private GameUpdateService.VerifyResult? _verifyResult;
    private System.Threading.CancellationTokenSource? _repairCts;
    private bool _repairBusy;

    
    
    
    
    
    [RelayCommand]
    private async Task RepairGame()
    {
        if (_repairBusy)
        {
            return;
        }
        try
        {
            if (GameUpdateService.GetGameDir(CurrentGameBiz) is null)
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.GameUpdate_NoGameDir);
                return;
            }
            StartRepairUi();
            _repairBusy = true;
            _repairCts = new System.Threading.CancellationTokenSource();
            var progress = new Progress<GameDownloadProgress>(OnRepairProgress);
            var result = await GameUpdateService.VerifyAsync(CurrentGameBiz, quick: false, progress, _repairCts.Token);
            if (result is null)
            {
                ShowRepairError(Lang.Instance.GameUpdate_VerifyNoManifest);
                return;
            }
            _verifyResult = result;
            if (result.Damaged.Count == 0)
            {
                
                HideRepairProgress();
                InAppToast.MainWindow?.Success($"{Lang.Instance.GameUpdate_VerifyReportTitle}：{Lang.Instance.GameUpdate_VerifyDoneNone}");
                return;
            }
            
            ProgressBar_Repair.Visibility = Visibility.Collapsed;
            Text_RepairStatus.Text = string.Format(Lang.Instance.GameUpdate_VerifyDoneDamage, result.Damaged.Count, FormatSize(result.DamagedBytes))
                + (result.CanRepair ? "" : "\n" + Lang.Instance.GameUpdate_RepairNoBaseUrl);
            if (result.CanRepair)
            {
                Button_StartRepairing.Visibility = Visibility.Visible;
            }
            else
            {
                Button_RepairGame.Visibility = Visibility.Visible;
            }
        }
        catch (OperationCanceledException)
        {
            ResetRepairUi();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify game {Biz}", CurrentGameBiz);
            ShowRepairError(ex.Message);
        }
        finally
        {
            _repairBusy = false;
            _repairCts?.Dispose();
            _repairCts = null;
        }
    }

    
    [RelayCommand]
    private async Task StartRepair()
    {
        if (_repairBusy || _verifyResult is not { CanRepair: true, Damaged.Count: > 0 } result)
        {
            return;
        }
        try
        {
            Button_StartRepairing.Visibility = Visibility.Collapsed;
            ProgressBar_Repair.Visibility = Visibility.Visible;
            ProgressBar_Repair.IsIndeterminate = true;
            Text_RepairStatus.Text = Lang.Instance.GameUpdate_Repairing;
            _repairBusy = true;
            _repairCts = new System.Threading.CancellationTokenSource();
            var progress = new Progress<GameDownloadProgress>(OnRepairProgress);
            await GameUpdateService.RepairAsync(CurrentGameBiz, result, progress, _repairCts.Token);
            InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_RepairDone, result.Damaged.Count));
            ResetRepairUi();
        }
        catch (OperationCanceledException)
        {
            ResetRepairUi();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Repair game {Biz}", CurrentGameBiz);
            ShowRepairError(ex.Message);
        }
        finally
        {
            _repairBusy = false;
            _repairCts?.Dispose();
            _repairCts = null;
        }
    }

    private void OnRepairProgress(GameDownloadProgress p)
    {
        if (p.Phase == DownloadPhase.Verifying)
        {
            ProgressBar_Repair.IsIndeterminate = false;
            ProgressBar_Repair.Value = p.TotalFiles > 0 ? p.CompletedFiles * 100.0 / p.TotalFiles : 0;
            Text_RepairStatus.Text = string.Format(Lang.Instance.GameUpdate_Verifying, p.CompletedFiles, p.TotalFiles);
        }
        else if (p.Phase == DownloadPhase.Applying)
        {
            ProgressBar_Repair.IsIndeterminate = true;
            Text_RepairStatus.Text = Lang.Instance.GameUpdate_Applying;
        }
        else
        {
            ProgressBar_Repair.IsIndeterminate = false;
            ProgressBar_Repair.Value = p.TotalBytes > 0 ? p.ReceivedBytes * 100.0 / p.TotalBytes : 0;
            Text_RepairStatus.Text = $"{FormatSize(p.ReceivedBytes)} / {FormatSize(p.TotalBytes)} · {FormatSize(p.SpeedBytesPerSec)}/s";
        }
    }

    private void Button_RepairCancel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _repairCts?.Cancel();
        }
        catch { }
    }

    
    private void StartRepairUi()
    {
        Button_RepairGame.Visibility = Visibility.Collapsed;
        Button_StartRepairing.Visibility = Visibility.Collapsed;
        TextBlock_RepairError.Visibility = Visibility.Collapsed;
        ProgressBar_Repair.Visibility = Visibility.Visible;
        ProgressBar_Repair.IsIndeterminate = true;
        Text_RepairStatus.Text = "";
        Panel_RepairProgress.Visibility = Visibility.Visible;
    }

    private void HideRepairProgress()
    {
        Panel_RepairProgress.Visibility = Visibility.Collapsed;
        Button_RepairGame.Visibility = UninstallAndRepairEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ResetRepairUi()
    {
        _verifyResult = null;
        HideRepairProgress();
    }

    private void ShowRepairError(string message)
    {
        Panel_RepairProgress.Visibility = Visibility.Collapsed;
        Button_StartRepairing.Visibility = Visibility.Collapsed;
        Button_RepairGame.Visibility = UninstallAndRepairEnabled ? Visibility.Visible : Visibility.Collapsed;
        TextBlock_RepairError.Text = message;
        TextBlock_RepairError.Visibility = Visibility.Visible;
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1L << 30 => $"{bytes / 1024.0 / 1024 / 1024:F2}GB",
            >= 1L << 20 => $"{bytes / 1024.0 / 1024:F2}MB",
            _ => $"{bytes / 1024.0:F2}KB",
        };
    }

    #endregion

    [RelayCommand]
    private async Task ChangeCustomBg()
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".mkv");
            picker.FileTypeFilter.Add(".webm");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }
            
            
            string target = Path.Combine(AppConfig.BackgroundFolder, $"custom_bg_{CurrentGameBiz}{Path.GetExtension(file.Name).ToLowerInvariant()}");
            BackgroundAccentService.Invalidate(target);
            await ApplyCustomBackgroundAsync(file.Path, Path.GetExtension(file.Name).ToLowerInvariant());
            ChangeBgError = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change custom background");
            ChangeBgError = ex.Message;
        }
    }

    
    private async Task<string> ApplyCustomBackgroundAsync(string source, string ext)
    {
        string target = await CopyBackgroundAsync(source, ext);
        AppConfig.SetConfig(GameRegistry.GetCustomBgConfigKey(CurrentGameBiz), target);
        CustomBg = target;
        BackgroundGallery.SetSelectedId(CurrentGameBiz, Path.GetFileName(target));
        if (EnableCustomBg)
        {
            CustomBackgroundChanged?.Invoke(this, target);
        }
        else
        {
            
            EnableCustomBg = true;
        }
        return target;
    }

    
    
    
    
    private async Task<string> CopyBackgroundAsync(string source, string ext)
    {
        string prefix = $"custom_bg_{CurrentGameBiz}";
        return await Task.Run(() =>
        {
            string target = Path.Combine(AppConfig.BackgroundFolder, prefix + ext);
            Directory.CreateDirectory(AppConfig.BackgroundFolder);
            foreach (var old in Directory.GetFiles(AppConfig.BackgroundFolder, "custom_bg*"))
            {
                
                string name = Path.GetFileNameWithoutExtension(old);
                if (name == prefix || (CurrentGameBiz == GameBiz.WutheringWaves && name == "custom_bg"))
                {
                    try { File.Delete(old); } catch { }
                }
            }
            File.Copy(source, target, true);
            return target;
        });
    }

    
    public static event EventHandler<string>? CustomBackgroundChanged;

    [RelayCommand]
    private void OpenCustomBg()
    {
        try
        {
            if (CustomBg is { } path && File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open custom background");
        }
    }

    [RelayCommand]
    private void DeleteCustomBg()
    {
        try
        {
            if (CustomBg is { } path && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch { }
        AppConfig.SetConfig(GameRegistry.GetCustomBgConfigKey(CurrentGameBiz), null);
        CustomBg = null;
        if (EnableCustomBg)
        {
            
            EnableCustomBg = false;
        }
        else
        {
            BackgroundGallery.SetSelectedId(CurrentGameBiz, null);
            CustomBackgroundChanged?.Invoke(this, "");
        }
    }

    [RelayCommand]
    private async Task ChangeThirdPartyPath()
    {
        try
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add(".bat");
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }
            AppConfig.SetConfig(GameRegistry.GetThirdPartyToolPathConfigKey(CurrentGameBiz), file.Path);
            ThirdPartyToolPath = file.Path;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change third party tool path");
            InAppToast.MainWindow?.Error(ex);
        }
    }

    [RelayCommand]
    private void OpenThirdPartyToolFolder()
    {
        try
        {
            if (ThirdPartyToolPath is { } path && File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open third party tool folder");
        }
    }

    [RelayCommand]
    private void DeleteThirdPartyToolPath()
    {
        AppConfig.SetConfig(GameRegistry.GetThirdPartyToolPathConfigKey(CurrentGameBiz), null);
        ThirdPartyToolPath = null;
    }

    [RelayCommand]
    private void Mute()
    {
        VideoBgVolume = 0;
        VideoBgVolumeButtonIcon = "\uE74F";
    }


    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            FlipView_Settings.SelectedIndex = int.Parse(tag);
        }
    }

    private void FlipView_Settings_Loaded(object sender, RoutedEventArgs e)
    {
        FlipView_Settings.SelectedIndex = 0;
    }

    private void Grid_BackgroundDragIn_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    }

    private async void Grid_BackgroundDragIn_Drop(object sender, DragEventArgs e)
    {
        try
        {
            var deferral = e.GetDeferral();
            var files = await e.DataView.GetStorageItemsAsync();
            deferral.Complete();
            if (files.Count > 0)
            {
                string ext = Path.GetExtension(files[0].Name).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".mp4" or ".mkv" or ".webm")
                {
                    await ApplyCustomBackgroundAsync(files[0].Path, ext);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drop custom background");
        }
    }
}






public class GameSettingIcon
{
    public Uri? GameIcon { get; set; }
    public Uri? ServerIcon { get; set; }
    public string ServerCode { get; set; } = "";
    public string GameName { get; set; } = "";
}
