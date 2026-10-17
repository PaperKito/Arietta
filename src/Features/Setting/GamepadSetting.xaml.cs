using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using Arietta.Features.Gamepad;
using Arietta.Helpers;
using Arietta.Localization;
using System;

namespace Arietta.Features.Setting;

public sealed partial class GamepadSetting : PageBase
{

    private bool _restoring;


    public GamepadSetting()
    {
        this.InitializeComponent();
        Restore();
    }


    private void Restore()
    {
        _restoring = true;
        try
        {
            Toggle_Enable.IsOn = GamepadController.Enabled;
            TextBox_GuideKeys.Text = AppConfig.GetConfig("GamepadGuideKeys") ?? "";
            TextBox_ShareKeys.Text = AppConfig.GetConfig("GamepadShareKeys") ?? "";
            UpdateRedistInfoBar();
        }
        finally
        {
            _restoring = false;
        }
    }

    private void UpdateRedistInfoBar()
    {
        try
        {
            if (GamepadController.GameInputRedistOutdate)
            {
                InfoBar_Redist.Message = Lang.Instance.GamepadPage_RedistOutdate;
                InfoBar_Redist.IsOpen = true;
            }
            else if (GamepadController.NeedInstallGameInputRedist)
            {
                InfoBar_Redist.Message = Lang.Instance.GamepadPage_NeedRedist;
                InfoBar_Redist.IsOpen = true;
            }
            else
            {
                InfoBar_Redist.IsOpen = false;
            }
        }
        catch { }
    }


    private void Toggle_Enable_Toggled(object sender, RoutedEventArgs e)
    {
        if (_restoring)
        {
            return;
        }
        try
        {
            GamepadController.SetEnabled(Toggle_Enable.IsOn);
            UpdateRedistInfoBar();
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GamepadPage_Enable);
        }
    }


    private void TextBox_GuideKeys_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateKeys(TextBox_GuideKeys, TextBlock_GuideError, GamepadController.SetGuideKeys);
    }


    private void TextBox_ShareKeys_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateKeys(TextBox_ShareKeys, TextBlock_ShareError, GamepadController.SetShareKeys);
    }

    private delegate bool KeysSetter(string? value, out string keysTextOrErrorKey);

    private void UpdateKeys(TextBox box, TextBlock error, KeysSetter setter)
    {
        if (_restoring)
        {
            return;
        }
        try
        {
            if (setter(box.Text, out string textOrErrorKey))
            {
                error.Visibility = Visibility.Collapsed;
            }
            else
            {
                error.Text = string.Format(Lang.Instance.GamepadPage_BadKey, textOrErrorKey);
                error.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

}
