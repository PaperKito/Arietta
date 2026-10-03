using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using System;

namespace Arietta.Features.Setting;







public sealed partial class DownloadSetting : PageBase
{

    
    private bool _restoringDownload;

    
    private bool _threadsReady;


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
        
        Slider_Threads.Minimum = GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsMin;
        Slider_Threads.Maximum = GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsMax;
        Slider_Threads.Value = GameLauncher.GameUpdate.GameUpdateService.DownloadThreads;
        _threadsReady = true;
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


    private void Slider_Threads_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs args)
    {
        
        if (!_threadsReady)
        {
            return;
        }
        AppConfig.SetConfig(GameLauncher.GameUpdate.GameUpdateService.DownloadThreadsConfigKey,
            ((int)Math.Round(((Slider)sender).Value)).ToString());
    }

}
