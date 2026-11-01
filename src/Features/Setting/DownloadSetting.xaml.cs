using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using System;
using System.Linq;

namespace Arietta.Features.Setting;

public sealed partial class DownloadSetting : PageBase
{

    private bool _restoringDownload;


    public DownloadSetting()
    {
        this.InitializeComponent();
    }


    protected override void OnLoaded()
    {
        base.OnLoaded();
        RefreshHardLink();
        RefreshSpeedLimit();
        RefreshThreads();
    }


    private void RefreshHardLink()
    {
        _restoringDownload = true;
        Toggle_HardLink.IsOn = AppConfig.GetConfig(GameLauncher.GameUpdate.GameUpdateService.UseHardLinkConfigKey) == "1";
        _restoringDownload = false;
    }


    private void RefreshSpeedLimit()
    {
        _restoringDownload = true;
        Box_SpeedLimit.Value = double.TryParse(
            AppConfig.GetConfig(GameLauncher.GameUpdate.GameUpdateService.SpeedLimitConfigKey), out double mb)
            ? mb
            : 0;
        _restoringDownload = false;
    }


    private void RefreshThreads()
    {
        _restoringDownload = true;

        bool auto = !int.TryParse(AppConfig.GetConfig(GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsConfigKey), out int manual)
                    || manual == 0;
        Combo_Threads.ItemsSource = Enumerable.Range(
            GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsMin,
            GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsMax - GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsMin + 1);
        Combo_Threads.SelectedItem = GameLauncher.GameUpdate.GameUpdateService.DownloadThreads;
        Combo_Threads.Visibility = auto ? Visibility.Collapsed : Visibility.Visible;
        (auto ? Radio_ThreadsAuto : Radio_ThreadsManual).IsChecked = true;
        _restoringDownload = false;
    }


    private void Toggle_HardLink_Toggled(object sender, RoutedEventArgs e)
    {
        if (_restoringDownload)
        {
            return;
        }
        AppConfig.SetConfig(GameLauncher.GameUpdate.GameUpdateService.UseHardLinkConfigKey,
            Toggle_HardLink.IsOn ? "1" : "0");
    }


    private void Box_SpeedLimit_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_restoringDownload)
        {
            return;
        }

        double mb = double.IsNaN(sender.Value) ? 0 : Math.Floor(sender.Value);
        if (sender.Value != mb)
        {
            sender.Value = mb;
            return;
        }
        AppConfig.SetConfig(GameLauncher.GameUpdate.GameUpdateService.SpeedLimitConfigKey,
            ((long)mb).ToString());
    }


    private void Radio_Threads_ModeChanged(object sender, RoutedEventArgs e)
    {

        if (_restoringDownload || sender is not RadioButton { IsChecked: true })
        {
            return;
        }

        bool auto = Radio_ThreadsAuto.IsChecked == true;
        AppConfig.SetConfig(GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsConfigKey,
            auto ? "0" : ManualThreadsValue().ToString());
        Combo_Threads.Visibility = auto ? Visibility.Collapsed : Visibility.Visible;
    }


    private void Combo_Threads_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringDownload)
        {
            return;
        }
        AppConfig.SetConfig(GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsConfigKey,
            ManualThreadsValue().ToString());
    }


    private int ManualThreadsValue()
        => Combo_Threads.SelectedItem is int n
            ? n
            : GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsDefault;

}
