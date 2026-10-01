using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using System;

namespace Arietta.Features.Setting;

public sealed partial class SettingPage : PageBase
{

    public SettingPage()
    {
        this.InitializeComponent();
        Frame_Setting.Navigate(typeof(AboutSetting));
    }


    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                var type = item.Tag switch
                {
                    nameof(AboutSetting) => typeof(AboutSetting),
                    nameof(GeneralSetting) => typeof(GeneralSetting),
                    nameof(DownloadSetting) => typeof(DownloadSetting),
                    nameof(FileManagementSetting) => typeof(FileManagementSetting),
                    nameof(GamepadSetting) => typeof(GamepadSetting),
                    
                    "ToolboxSetting" => typeof(Arietta.Features.ViewHost.ToolboxPage),
                    _ => null,
                };
                if (type is not null)
                {
                    Frame_Setting.Navigate(type);
                }
            }
        }
        catch { }
    }

}
