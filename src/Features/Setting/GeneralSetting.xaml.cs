using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Features.GameLauncher;
using Arietta.Features.GachaLog;
using Arietta.Frameworks;
using Arietta.Localization;
using System;
using System.Threading.Tasks;

namespace Arietta.Features.Setting;

public sealed partial class GeneralSetting : PageBase
{

    private readonly (string? Code, string Label)[] _languageOptions = AnnouncementLanguageSettings.Options;

    private bool _restoring;


    public GeneralSetting()
    {
        this.InitializeComponent();
        _restoring = true;

        ComboBox_Language.SelectedIndex = Math.Max(0, Array.FindIndex(_languageOptions, o => o.Code == AnnouncementLanguageSettings.Selected));

        Radio_CloseToTray.IsChecked = AppConfig.GetConfig("CloseWindowAction") != "Exit";
        Radio_ExitApp.IsChecked = AppConfig.GetConfig("CloseWindowAction") == "Exit";

        Radio_LaunchKeepWindow.IsChecked = AppConfig.GetConfig("StartGameWindowAction") != "Tray";
        Radio_LaunchToTray.IsChecked = AppConfig.GetConfig("StartGameWindowAction") == "Tray";

        var afterExit = AppConfig.GetConfig("AfterGameExitWindowAction");
        Radio_ExitRestoreWindow.IsChecked = afterExit is null or "" or "Restore";
        Radio_ExitKeepHidden.IsChecked = afterExit == "KeepHidden";
        Radio_ExitCloseLauncher.IsChecked = afterExit == "CloseLauncher";
        _restoring = false;
    }


    private void Radio_CloseOption_Checked(object sender, RoutedEventArgs e)
    {
        AppConfig.SetConfig("CloseWindowAction", Radio_CloseToTray.IsChecked == true ? "Tray" : "Exit");
    }

    private void Radio_LaunchOption_Checked(object sender, RoutedEventArgs e)
    {
        AppConfig.SetConfig("StartGameWindowAction", Radio_LaunchToTray.IsChecked == true ? "Tray" : "None");
    }

    private void Radio_ExitOption_Checked(object sender, RoutedEventArgs e)
    {
        string value = Radio_ExitKeepHidden.IsChecked == true ? "KeepHidden"
            : Radio_ExitCloseLauncher.IsChecked == true ? "CloseLauncher" : "Restore";
        AppConfig.SetConfig("AfterGameExitWindowAction", value);
    }

    private async void Hyperlink_VisualEffects_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess-visualeffects"));
        }
        catch
        {

        }
    }

    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoring)
        {
            return;
        }
        int index = ComboBox_Language.SelectedIndex;
        if (index < 0 || index >= _languageOptions.Length)
        {
            return;
        }
        string? code = _languageOptions[index].Code;
        AnnouncementLanguageSettings.SetSelected(code);

        GachaLanguageSettings.SetSelected(code);
        GameNoticeService.InvalidateCache();

        GameLauncherSettingDialog.RaiseAnnouncementSettingChanged();

        UiLanguage.SetLanguageFromCode(code);
    }

}
