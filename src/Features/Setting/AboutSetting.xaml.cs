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

    partial void OnIsCheckingChanged(bool value)
    {
        OnPropertyChanged(nameof(CheckingVisibility));
        OnPropertyChanged(nameof(CanCheckNow));
    }

    public Visibility CheckingVisibility => IsChecking ? Visibility.Visible : Visibility.Collapsed;

    public bool CanCheckNow => !IsChecking;


    public AboutSetting()
    {
        this.InitializeComponent();
    }


    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        if (IsChecking)
        {
            return;
        }
        IsChecking = true;
        try
        {

            SelfUpdateCheckResult result = await SelfUpdateService.CheckForUpdateAsync(includeIgnored: true);
            if (result is { Status: SelfUpdateCheckStatus.UpdateAvailable, Info: { } info })
            {

                (Application.Current as App)?.ShowUpdateDialog(info);
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
