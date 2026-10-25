using System;
using Arietta.Frameworks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Arietta.Features.Setting;

public sealed partial class AboutSetting : PageBase
{
    private static readonly ILogger _logger = AppConfig.GetLogger<AboutSetting>();

    public string AppVersionText => AppConfig.AppVersion;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _newVersionText = "";

    [ObservableProperty]
    private string _releaseNotesText = "";

    [ObservableProperty]
    private double _downloadProgress;

    private SelfUpdateInfo? _info;

    partial void OnIsCheckingChanged(bool value) => NotifyStateProjections();

    partial void OnIsDownloadingChanged(bool value) => NotifyStateProjections();

    partial void OnHasUpdateChanged(bool value) => NotifyStateProjections();

    private void NotifyStateProjections()
    {
        OnPropertyChanged(nameof(CheckingVisibility));
        OnPropertyChanged(nameof(DownloadingVisibility));
        OnPropertyChanged(nameof(AvailableVisibility));
        OnPropertyChanged(nameof(CanCheckNow));
        OnPropertyChanged(nameof(CanDownloadNow));
    }

    public Visibility CheckingVisibility => ToVisibility(IsChecking);

    public Visibility DownloadingVisibility => ToVisibility(IsDownloading);

    public Visibility AvailableVisibility => ToVisibility(HasUpdate);

    public bool CanCheckNow => !IsChecking && !IsDownloading;

    public bool CanDownloadNow => HasUpdate && !IsDownloading && !IsChecking;

    private static Visibility ToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }


    public AboutSetting()
    {
        this.InitializeComponent();
    }


    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        if (IsChecking || IsDownloading)
        {
            return;
        }
        IsChecking = true;
        HasUpdate = false;
        StatusText = Localization.Lang.Instance.SettingPage_CheckingUpdate;
        try
        {
            SelfUpdateInfo? info = await SelfUpdateService.CheckForUpdateAsync();
            if (info is null)
            {
                StatusText = Localization.Lang.Instance.SettingPage_UpToDate;
            }
            else
            {
                _info = info;
                NewVersionText = $"{Localization.Lang.Instance.SettingPage_UpdateAvailable}: v{info.Version}";
                ReleaseNotesText = info.Notes ?? "";
                StatusText = "";
                HasUpdate = true;
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: check for updates failed");
            StatusText = $"{Localization.Lang.Instance.SettingPage_UpdateFailed}: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
        }
    }


    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (_info is null || IsDownloading)
        {
            return;
        }
        if (!SelfUpdateService.IsPortableLayout)
        {
            StatusText = Localization.Lang.Instance.SettingPage_UpdateDevLayout;
            return;
        }
        IsDownloading = true;
        DownloadProgress = 0;
        StatusText = "";
        try
        {

            string newAppDir = await SelfUpdateService.DownloadAndStageAsync(
                _info, new Progress<double>(p => DownloadProgress = p));
            SelfUpdateService.InstallNow(newAppDir);

            StatusText = Localization.Lang.Instance.SettingPage_UpdateReady;
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: download/install failed");
            StatusText = $"{Localization.Lang.Instance.SettingPage_UpdateFailed}: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

}
