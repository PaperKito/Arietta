using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Localization;
using System;
using System.Windows.Input;

namespace Arietta.Features.GameLauncher;

[ObservableObject]
public sealed partial class StartGameButton : UserControl
{


    private static Brush AccentFillColorDefaultBrush => (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
    private static Brush TextOnAccentFillColorDisabled => (Brush)Application.Current.Resources["TextOnAccentFillColorDisabledBrush"];
    private static Brush TextOnAccentFillColorPrimaryBrush => (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];


    public StartGameButton()
    {
        this.InitializeComponent();
        this.ActualThemeChanged += StartGameButton_ActualThemeChanged;
    }


    public ICommand GameCommand { get; set => SetProperty(ref field, value); }


    public ICommand SettingCommand { get; set => SetProperty(ref field, value); }

    public string? RunningGameTime { get; set { field = value; OnPropertyChanged(nameof(StartGameButtonText)); } }


    public string? RunningGameInfo { get; set => SetProperty(ref field, value); }


    public GameState GameState { get; set { if (SetProperty(ref field, value)) UpdateActionButtonState(); } }

    public bool IsResumeInstall { get; set { if (SetProperty(ref field, value)) OnPropertyChanged(nameof(StartGameButtonText)); } }


    public bool ActionButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateButtonForeground(); } }


    public bool SettingButtonPointerOver { get; set { if (SetProperty(ref field, value)) UpdateButtonForeground(); } }


    public bool IsAccentColorBackgroundVisible => Button_GameAction.IsEnabled
        && GameState is not GameState.GameIsRunning and not GameState.Installing;


    public bool IsGameActionCommandRunning => !Button_GameAction.IsEnabled && GameState is not GameState.GameIsRunning;


    public string StartGameButtonText => GameState switch
    {
        GameState.StartGame => Lang.Instance.LauncherPage_StartGame,

        GameState.GameIsRunning => RunningGameTime ?? "",
        GameState.InstallGame => IsResumeInstall ? Lang.Instance.LauncherPage_ResumeInstall : Lang.Instance.LauncherPage_InstallGame,
        GameState.UpdateGame => Lang.Instance.LauncherPage_UpdateGame,

        GameState.CheckingUpdate => Lang.Instance.UpdateWindow_Checking,
        GameState.Installing => "",
        _ => "",
    };


    public Brush ActionButtonForeground => (Button_GameAction.IsEnabled, IsAccentColorBackgroundVisible, ActionButtonPointerOver) switch
    {
        (false, _, _) => TextOnAccentFillColorDisabled,
        (true, false, true) => AccentFillColorDefaultBrush,
        (true, false, false) => TextOnAccentFillColorDisabled,
        _ => TextOnAccentFillColorPrimaryBrush
    };


    public Brush SettingButtonForeground => (IsAccentColorBackgroundVisible, SettingButtonPointerOver) switch
    {
        (false, true) => AccentFillColorDefaultBrush,
        (false, false) => TextOnAccentFillColorDisabled,
        _ => TextOnAccentFillColorPrimaryBrush
    };


    private void UpdateActionButtonState()
    {

        Button_GameAction.IsEnabled = GameState is not (GameState.GameIsRunning or GameState.CheckingUpdate);
        OnPropertyChanged(nameof(IsAccentColorBackgroundVisible));
        OnPropertyChanged(nameof(IsGameActionCommandRunning));
        OnPropertyChanged(nameof(StartGameButtonText));
        RefreshInstallStateBindings();
        UpdateButtonForeground();
    }


    private void UpdateButtonForeground()
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));

        RefreshInstallStateBindings();
    }


    private void Button_GameAction_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsAccentColorBackgroundVisible));
        OnPropertyChanged(nameof(IsGameActionCommandRunning));
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }


    private void Control_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender as Grid == Grid_Root)
        {
            if (GameState is GameState.GameIsRunning or GameState.Installing)
            {
                Popup_GameInfo.IsOpen = true;
            }
        }
        else if (sender as Button == Button_GameAction)
        {
            ActionButtonPointerOver = true;
        }
        else if (sender as Button == Button_Setting)
        {
            SettingButtonPointerOver = true;
        }
    }


    private void Control_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender as Grid == Grid_Root)
        {
            Popup_GameInfo.IsOpen = false;
        }
        else if (sender as Button == Button_GameAction)
        {
            ActionButtonPointerOver = false;
        }
        else if (sender as Button == Button_Setting)
        {
            SettingButtonPointerOver = false;
        }
    }

    public DownloadPhase InstallPhase { get; private set => SetProperty(ref field, value); }

    public int ProgressRingValue { get; private set => SetProperty(ref field, value); }

    public string ProgressPercentText { get; private set => SetProperty(ref field, value); } = "";

    public string RemainTimeText { get; private set => SetProperty(ref field, value); } = "--:--:--";

    public string DownloadBytesText { get; private set => SetProperty(ref field, value); } = "";

    public string DownloadSpeedText { get; private set => SetProperty(ref field, value); } = "";


    public bool GameStateIsInstalling => GameState is GameState.Installing;

    public Visibility NonInstallingVisibility => GameStateIsInstalling ? Visibility.Collapsed : Visibility.Visible;

    public Visibility InstallingVisibility => GameStateIsInstalling ? Visibility.Visible : Visibility.Collapsed;

    public bool InstallStateIsApplying => InstallPhase is DownloadPhase.Applying;

    public bool InstallRingIndeterminate => InstallPhase is DownloadPhase.Applying && !ApplyingHasCounts;

    public bool ApplyingHasCounts { get; private set => SetProperty(ref field, value); }

    public string InstallStateText => InstallPhase switch
    {
        DownloadPhase.Downloading => Lang.Instance.DownloadGamePage_Downloading,
        DownloadPhase.Verifying => Lang.Instance.DownloadGamePage_Verifying,

        DownloadPhase.Applying or DownloadPhase.Patching => Lang.Instance.GameUpdate_Applying,
        _ => "",
    };

    public string PauseHintText => InstallPhase is DownloadPhase.Downloading
        ? Lang.Instance.DownloadGamePage_Pause
        : Lang.Instance.Common_Cancel;

    public double RemainTimeRowOpacity => !ActionButtonPointerOver && RemainTimeText != "--:--:--" ? 1 : 0;

    public double InstallStateCenterOpacity => !ActionButtonPointerOver && RemainTimeText == "--:--:--" ? 1 : 0;

    public double PauseHintOpacity => ActionButtonPointerOver ? 1 : 0;

    public Visibility RemainTimeVisibility => RemainTimeText != "--:--:--"
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility DownloadBytesVisibility => InstallPhase is DownloadPhase.Downloading
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility RingValueTextVisibility => InstallStateIsApplying && !ApplyingHasCounts ? Visibility.Collapsed : Visibility.Visible;

    public void UpdateGameInstallProgress(GameDownloadProgress p)
    {
        InstallPhase = p.Phase;

        static string Remain(long remaining, long rate)
            => rate > 0 && remaining > 0
                ? TimeSpan.FromSeconds(remaining / (double)rate).ToString(@"hh\:mm\:ss")
                : "--:--:--";
        if (p.Phase is DownloadPhase.Downloading)
        {
            double progress = p.TotalBytes > 0 ? (double)p.ReceivedBytes / p.TotalBytes : 0;
            ProgressRingValue = (int)(progress * 100);
            ProgressPercentText = $"{progress:P1}";
            DownloadBytesText = ToBytesText(p.ReceivedBytes, p.TotalBytes) ?? "";
            DownloadSpeedText = ToSpeedText(p.SpeedBytesPerSec);
            RemainTimeText = Remain(p.TotalBytes - p.ReceivedBytes, p.SpeedBytesPerSec);
        }
        else if (p.Phase is DownloadPhase.Verifying && p.TotalFiles > 0)
        {

            double verifyProgress = (double)p.CompletedFiles / p.TotalFiles;
            ProgressRingValue = (int)(verifyProgress * 100);
            ProgressPercentText = $"{verifyProgress:P1}";
            DownloadBytesText = "";
            DownloadSpeedText = "";
            RemainTimeText = Remain(p.TotalFiles - p.CompletedFiles, p.SpeedFilesPerSec);
        }
        else if ((p.Phase is DownloadPhase.Applying or DownloadPhase.Patching) && p.TotalFiles > 0)
        {

            ApplyingHasCounts = true;
            double applyProgress = Math.Clamp(p.OverallProgress ?? (double)p.CompletedFiles / p.TotalFiles, 0, 1);
            ProgressRingValue = (int)Math.Round(applyProgress * 100);
            ProgressPercentText = $"{applyProgress:P1}";
            DownloadBytesText = "";
            DownloadSpeedText = "";
            RemainTimeText = "--:--:--";
        }
        else
        {

            ApplyingHasCounts = false;
            ProgressRingValue = 0;
            ProgressPercentText = "";
            DownloadBytesText = "";
            DownloadSpeedText = "";
            RemainTimeText = "--:--:--";
        }
        RefreshInstallStateBindings();
    }

    public void ResetInstallState()
    {
        InstallPhase = DownloadPhase.Downloading;
        ApplyingHasCounts = false;
        ProgressRingValue = 0;
        ProgressPercentText = "";
        DownloadBytesText = "";
        DownloadSpeedText = "";
        RemainTimeText = "--:--:--";
        RefreshInstallStateBindings();
    }

    public void ShowPausing()
    {
        RemainTimeText = Lang.Instance.GameUpdate_Pausing;
        RefreshInstallStateBindings();
    }

    private void RefreshInstallStateBindings()
    {
        OnPropertyChanged(nameof(GameStateIsInstalling));
        OnPropertyChanged(nameof(NonInstallingVisibility));
        OnPropertyChanged(nameof(InstallingVisibility));
        OnPropertyChanged(nameof(InstallStateIsApplying));
        OnPropertyChanged(nameof(InstallRingIndeterminate));
        OnPropertyChanged(nameof(ApplyingHasCounts));
        OnPropertyChanged(nameof(InstallStateText));
        OnPropertyChanged(nameof(PauseHintText));
        OnPropertyChanged(nameof(RemainTimeRowOpacity));
        OnPropertyChanged(nameof(InstallStateCenterOpacity));
        OnPropertyChanged(nameof(PauseHintOpacity));
        OnPropertyChanged(nameof(RemainTimeVisibility));
        OnPropertyChanged(nameof(DownloadBytesVisibility));
        OnPropertyChanged(nameof(RingValueTextVisibility));
    }


    private static string? ToBytesText(long finish, long total)
    {
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        if (total == 0)
        {
            return null;
        }
        return total >= GB
            ? $"{finish / GB:F2}/{total / GB:F2} GB"
            : $"{finish / MB:F2}/{total / MB:F2} MB";
    }


    private static string ToSpeedText(long bytes)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        return bytes >= MB ? $"{bytes / MB:F2} MB/s" : $"{bytes / KB:F2} KB/s";
    }

    private void StartGameButton_ActualThemeChanged(FrameworkElement sender, object args)
    {
        OnPropertyChanged(nameof(ActionButtonForeground));
        OnPropertyChanged(nameof(SettingButtonForeground));
    }


}
