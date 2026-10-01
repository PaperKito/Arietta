using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using Arietta.Localization;
using System;
using System.Threading.Tasks;

namespace Arietta.Features.Sync;







public sealed partial class CloudSyncServerPage : PageBase
{
    private readonly CloudSyncService _cloudSync = AppConfig.GetService<CloudSyncService>();

    private bool _busy;

    public CloudSyncServerPage()
    {
        this.InitializeComponent();
    }


    protected override void OnLoaded()
    {
        
        LoadConfig();
        base.OnLoaded();
    }


    private void LoadConfig()
    {
        CloudSyncConfig config = _cloudSync.Config;
        TextBox_ServerUrl.Text = config.ServerUrl;
        TextBox_UserName.Text = config.UserName;
        
        PasswordBox_Password.Password = config.Password;

        TextBlock_Status.Text = config.IsConfigured
            ? Lang.Instance.Sync_Configured
            : Lang.Instance.Sync_NotConfigured;
        Border_Status.Visibility = Visibility.Visible;
    }


    
    private void Button_TogglePasswordReveal_Click(object sender, RoutedEventArgs e)
    {
        bool reveal = PasswordBox_Password.Visibility == Visibility.Visible;
        if (reveal)
        {
            TextBox_PasswordRevealed.Text = PasswordBox_Password.Password;
        }
        else
        {
            PasswordBox_Password.Password = TextBox_PasswordRevealed.Text;
        }
        PasswordBox_Password.Visibility = reveal ? Visibility.Collapsed : Visibility.Visible;
        TextBox_PasswordRevealed.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
        
        FontIcon_Reveal.Glyph = reveal ? "\uED1A" : "\uE7B3";
    }


    
    private void TextBox_PasswordRevealed_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TextBox_PasswordRevealed.Visibility == Visibility.Visible)
        {
            PasswordBox_Password.Password = TextBox_PasswordRevealed.Text;
        }
    }


    
    private void Button_Back_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
            }
        }
        catch { }
    }


    private async void Button_SaveAndTest_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        string serverUrl = TextBox_ServerUrl.Text.Trim();
        
        if (serverUrl.Length > 0 &&
            !(Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri? uri)
              && uri.Scheme is "http" or "https"
              && !string.IsNullOrEmpty(uri.Host)))
        {
            SetStatus(Lang.Instance.Sync_ConnectInvalid, success: false);
            return;
        }

        SetBusy(true);
        try
        {
            
            
            _cloudSync.ApplySettings(
                serverUrl,
                TextBox_UserName.Text.Trim(),
                PasswordBox_Password.Password,
                _cloudSync.Config.AutoSync,
                _cloudSync.Config.Preference);

            CloudSyncResult result = await _cloudSync.TestConnectionAsync();
            SetStatus(result.Success
                ? Lang.Instance.Sync_ConnectOk
                : $"{Lang.Instance.Sync_ConnectFail}（{result.Message}）", result.Success);
        }
        catch (Exception ex)
        {
            SetStatus($"{Lang.Instance.Sync_ConnectFail}（{ex.Message}）", false);
        }
        finally
        {
            SetBusy(false);
        }
    }


    private void SetBusy(bool busy)
    {
        _busy = busy;
        ProgressRing_Busy.IsActive = busy;
        ProgressRing_Busy.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        Button_SaveAndTest.IsEnabled = !busy;
        TextBox_ServerUrl.IsEnabled = !busy;
        TextBox_UserName.IsEnabled = !busy;
        PasswordBox_Password.IsEnabled = !busy;
        TextBox_PasswordRevealed.IsEnabled = !busy;
        Button_TogglePasswordReveal.IsEnabled = !busy;
        if (busy)
        {
            SetStatus(Lang.Instance.Sync_Testing, success: null);
        }
    }


    
    
    
    
    private void SetStatus(string message, bool? success)
    {
        TextBlock_Status.Text = message;
        Border_Status.Visibility = message.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        Border_Status.Style = success switch
        {
            true => (Style)Resources["SyncStatusOkStyle"],
            false => (Style)Resources["SyncStatusErrorStyle"],
            _ => null,
        };
    }


}
