using System;
using Arietta.Frameworks;
using Arietta.Helpers;
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
        try
        {

            SelfUpdateCheckResult result = await SelfUpdateService.CheckForUpdateAsync();
            if (result is { Status: SelfUpdateCheckStatus.UpdateAvailable, Info: { } info })
            {

                _info = info;
                NewVersionText = $"{Localization.Lang.Instance.SettingPage_UpdateAvailable}: v{info.Version}";
                ReleaseNotesText = info.Notes ?? "";
                HasUpdate = true;
            }
            else if (result.Status == SelfUpdateCheckStatus.Ignored)
            {
                InAppToast.MainWindow?.Information(
                    Localization.Lang.Instance.SettingPage_UpdateIgnored,
                    string.Format(Localization.Lang.Instance.SettingPage_UpdateIgnoredDesc, result.IgnoredVersion),
                    5000);
            }
            else
            {
                InAppToast.MainWindow?.Success(
                    Localization.Lang.Instance.SettingPage_UpToDate,
                    string.Format(Localization.Lang.Instance.SettingPage_UpToDateDesc, AppVersionText),
                    4000);
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: check for updates failed");
            ShowUpdateErrorToast(ex);
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
            InAppToast.MainWindow?.Warning(Localization.Lang.Instance.SettingPage_UpdateDevLayout);
            return;
        }
        IsDownloading = true;
        DownloadProgress = 0;
        try
        {

            string newAppDir = await SelfUpdateService.DownloadAndStageAsync(
                _info, new Progress<double>(p => DownloadProgress = p));
            SelfUpdateService.InstallNow(newAppDir);

            InAppToast.MainWindow?.Information(Localization.Lang.Instance.SettingPage_UpdateReady, null, 8000);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: download/install failed");
            ShowUpdateErrorToast(ex);
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private static void ShowUpdateErrorToast(Exception ex)
    {
        if (InAppToast.MainWindow is not { } toast)
        {
            return;
        }
        (string title, string message) = SelfUpdateService.DescribeFailure(ex);
        toast.Error(title, message);
    }

}
