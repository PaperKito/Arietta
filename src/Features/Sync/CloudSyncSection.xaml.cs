using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Arietta.Localization;
using System;
using System.Threading.Tasks;

namespace Arietta.Features.Sync;

public sealed partial class CloudSyncSection : UserControl
{
    private readonly CloudSyncService _cloudSync = AppConfig.GetService<CloudSyncService>();

    private bool _busy;

    public CloudSyncSection()
    {
        this.InitializeComponent();
        Loaded += CloudSyncSection_Loaded;
        Unloaded += CloudSyncSection_Unloaded;
        ToggleSwitch_EnableSync.Toggled += ToggleSwitch_EnableSync_Toggled;
        _cloudSync.StatusChanged += CloudSync_StatusChanged;
    }


    private void ToggleSwitch_EnableSync_Toggled(object sender, RoutedEventArgs e)
    {

        CloudSyncHost.SetAutoSync(ToggleSwitch_EnableSync.IsOn);
        RefreshFeedback();
    }


    private void CloudSyncSection_Loaded(object sender, RoutedEventArgs e) => LoadConfig();


    private void CloudSyncSection_Unloaded(object sender, RoutedEventArgs e)
    {
        _cloudSync.StatusChanged -= CloudSync_StatusChanged;
        SaveConfig();
    }


    private void LoadConfig()
    {
        CloudSyncConfig config = _cloudSync.Config;
        ToggleSwitch_EnableSync.IsOn = config.AutoSync;
        TextBlock_ServerSummary.Text = GetServerSummary(config);
        RefreshFeedback(config);
    }


    private void SaveConfig()
    {

        _cloudSync.ApplyAutoSync(ToggleSwitch_EnableSync.IsOn);
    }

    private static string GetServerSummary(CloudSyncConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return Lang.Instance.Sync_NotConfigured;
        }
        return Uri.TryCreate(config.ServerUrl, UriKind.Absolute, out Uri? uri) && !string.IsNullOrEmpty(uri.Host)
            ? uri.Host
            : config.ServerUrl;
    }

    private void RefreshFeedback(CloudSyncConfig? config = null)
    {
        config ??= _cloudSync.Config;
        TextBlock_Feedback.Text = !config.IsConfigured ? Lang.Instance.Sync_EnableNeedServer
            : !ToggleSwitch_EnableSync.IsOn ? Lang.Instance.Sync_EnableOff
            : config.LastSyncTime is { } time
                ? $"{Lang.Instance.Sync_LastSync} {time.LocalDateTime:yyyy-MM-dd HH:mm:ss}"
                : Lang.Instance.Sync_NeverSynced;
    }

    private void Button_ServerEntry_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DependencyObject node = this;
            while ((node = VisualTreeHelper.GetParent(node)) is not null)
            {
                if (node is Page { Frame: not null } page)
                {
                    page.Frame.Navigate(typeof(CloudSyncServerPage));
                    return;
                }
            }
        }
        catch { }
    }


    private void CloudSync_StatusChanged(object? sender, CloudSyncResult e)
    {
        DispatcherQueue.TryEnqueue(() => SetStatus(e.Message, e.Success));
    }


    private void SetStatus(string message, bool success)
    {
        TextBlock_Feedback.Text = success ? message : $"{Lang.Instance.Common_Error}：{message}";
    }


    private void SetBusy(bool busy)
    {
        _busy = busy;
        ProgressRing_Busy.IsActive = busy;
        ProgressRing_Busy.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Button_ServerEntry.IsEnabled = !busy;
        Button_SyncNow.IsEnabled = !busy;
        if (busy)
        {
            TextBlock_Feedback.Text = Lang.Instance.Sync_Working;
        }
        else
        {
            RefreshFeedback();
        }
    }


    private async Task RunAsync(Func<CloudSyncService, Task<CloudSyncResult>> action)
    {
        if (_busy)
        {
            return;
        }
        SaveConfig();
        SetBusy(true);
        CloudSyncResult result;
        try
        {
            result = await action(_cloudSync);
        }
        catch (Exception ex)
        {
            result = CloudSyncResult.Fail(CloudSyncAction.None, ex.Message);
        }

        SetBusy(false);
        SetStatus(result.Message, result.Success);
    }


    private async void Button_SyncNow_Click(object sender, RoutedEventArgs e)
    {
        await RunAsync(service => service.SyncAsync());
    }


}
