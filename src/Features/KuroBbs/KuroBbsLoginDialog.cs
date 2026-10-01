using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;






public sealed class KuroBbsLoginDialog
{
    private readonly ContentDialog _dialog;
    private readonly TextBox _textBoxToken = new() { PlaceholderText = Lang.Instance.Login_TokenPlaceholder };
    private readonly TextBox _textBoxDid = new() { PlaceholderText = Lang.Instance.Login_DidPlaceholder };
    private readonly TextBox _textBoxPhone = new() { PlaceholderText = Lang.Instance.Login_PhonePlaceholder };
    private readonly TextBox _textBoxCode = new() { PlaceholderText = Lang.Instance.Login_CodePlaceholder };
    private readonly Button _buttonSendCode = new()
    {
        Content = Lang.Instance.Login_SendCode,
        VerticalAlignment = VerticalAlignment.Stretch,
    };
    private readonly TextBlock _textHint = new()
    {
        Opacity = 0.75,
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12,
        MinHeight = 18,
    };
    private readonly RadioButton _radioToken = new() { Content = Lang.Instance.Login_ByToken, IsChecked = true };
    private readonly RadioButton _radioSms = new() { Content = Lang.Instance.Login_BySms };
    private readonly StackPanel _panelToken;
    private readonly StackPanel _panelSms;
    private int _busy;

    
    public int ResultIndex { get; private set; } = -1;


    public KuroBbsLoginDialog(XamlRoot xamlRoot)
    {
        
        _panelToken = new StackPanel { Spacing = 12 };
        _panelToken.Children.Add(FormRow(Lang.Instance.KuroAccount_Token, _textBoxToken));
        _panelToken.Children.Add(FormRow(Lang.Instance.KuroAccount_Did, _textBoxDid));
        _panelToken.Children.Add(new TextBlock
        {
            Text = Lang.Instance.Login_TokenTip,
            FontSize = 11,
            Opacity = 0.6,
            TextWrapping = TextWrapping.Wrap,
        });

        
        var phoneRow = new Grid { ColumnSpacing = 8 };
        phoneRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        phoneRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_textBoxPhone, 0);
        Grid.SetColumn(_buttonSendCode, 1);
        _buttonSendCode.MinWidth = 110;
        phoneRow.Children.Add(_textBoxPhone);
        phoneRow.Children.Add(_buttonSendCode);

        _panelSms = new StackPanel { Spacing = 12 };
        _panelSms.Children.Add(phoneRow);
        _panelSms.Children.Add(FormRow(Lang.Instance.Login_CodeLabel, _textBoxCode));
        _panelSms.Children.Add(new TextBlock
        {
            Text = Lang.Instance.Login_SmsTip,
            FontSize = 11,
            Opacity = 0.6,
            TextWrapping = TextWrapping.Wrap,
        });

        
        var radioRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 20,
            Margin = new Thickness(0, 0, 0, 4),
        };
        radioRow.Children.Add(_radioToken);
        radioRow.Children.Add(_radioSms);

        var card = new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
        };

        var stack = new StackPanel { Spacing = 12, MinWidth = 400 };
        stack.Children.Add(radioRow);
        card.Child = _panelToken;
        stack.Children.Add(card);
        stack.Children.Add(_textHint);

        _dialog = new ContentDialog
        {
            Title = Lang.Instance.Login_Title,
            Content = stack,
            CloseButtonText = Lang.Instance.Login_Cancel,
            PrimaryButtonText = Lang.Instance.Login_Confirm,
            XamlRoot = xamlRoot,
        };

        _radioToken.Checked += (_, _) => UpdatePanels();
        _radioSms.Checked += (_, _) => UpdatePanels();
        _buttonSendCode.Click += async (_, _) => await SendCodeAsync();
        _dialog.PrimaryButtonClick += async (_, args) =>
        {
            args.Cancel = true;
            await ConfirmAsync();
        };
        UpdatePanels();
    }


    
    private static Grid FormRow(string label, FrameworkElement control)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        };
        Grid.SetColumn(text, 0);
        Grid.SetColumn(control, 1);
        row.Children.Add(text);
        row.Children.Add(control);
        return row;
    }


    private bool ByToken => _radioToken.IsChecked == true;


    private void UpdatePanels()
    {
        ((Border)((StackPanel)_dialog.Content).Children[1]).Child = ByToken ? _panelToken : _panelSms;
        _textHint.Text = "";
    }


    public async Task<int> ShowAsync()
    {
        try
        {
            _ = await _dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            
            throw new InvalidOperationException($"登录窗口打开失败：{ex.Message} (0x{ex.HResult:X8})", ex);
        }
        return ResultIndex;
    }


    private async Task SendCodeAsync()
    {
        if (System.Threading.Interlocked.CompareExchange(ref _busy, 1, 0) == 1)
        {
            return;
        }
        try
        {
            string phone = _textBoxPhone.Text.Trim();
            if (!Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
            {
                _textHint.Text = Lang.Instance.Login_PhoneInvalid;
                return;
            }
            _buttonSendCode.IsEnabled = false;
            _textHint.Text = Lang.Instance.Login_Sending;
            string? error = await KuroBbsClient.Instance.SendSmsCodeAsync(phone);
            if (error is null)
            {
                _textHint.Text = Lang.Instance.Login_CodeSent;
                _ = StartCountdownAsync();
            }
            else
            {
                _textHint.Text = string.Format(Lang.Instance.Login_CodeSendFailed, error);
                _buttonSendCode.IsEnabled = true;
            }
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _busy, 0);
        }
    }


    private async Task StartCountdownAsync()
    {
        for (int i = 60; i > 0; i--)
        {
            _buttonSendCode.Content = $"{i}s";
            await Task.Delay(1000);
        }
        _buttonSendCode.Content = Lang.Instance.Login_SendCode;
        _buttonSendCode.IsEnabled = true;
    }


    private async Task ConfirmAsync()
    {
        if (System.Threading.Interlocked.CompareExchange(ref _busy, 1, 0) == 1)
        {
            return;
        }
        try
        {
            _textHint.Text = Lang.Instance.Login_Working;
            string token;
            string did;
            
            string phoneUsed = "";
            KuroLoginResult? loginData = null;

            if (!ByToken)
            {
                
                string phone = _textBoxPhone.Text.Trim();
                string code = _textBoxCode.Text.Trim();
                if (!Regex.IsMatch(phone, @"^1[3-9]\d{9}$"))
                {
                    _textHint.Text = Lang.Instance.Login_PhoneInvalid;
                    return;
                }
                if (code.Length == 0)
                {
                    _textHint.Text = Lang.Instance.Login_NeedCode;
                    return;
                }
                var login = await KuroBbsClient.Instance.LoginWithSmsAsync(phone, code);
                if (login is not { IsSuccess: true, Data: { } data })
                {
                    _textHint.Text = string.Format(Lang.Instance.Login_Failed, login.Msg ?? $"code={login.Code}");
                    return;
                }
                loginData = data;
                token = data.Token;
                did = data.Did;
                phoneUsed = phone;
            }
            else
            {
                
                token = _textBoxToken.Text.Trim();
                if (token.Length < 20)
                {
                    _textHint.Text = Lang.Instance.Login_TokenInvalid;
                    return;
                }
                did = _textBoxDid.Text.Trim();
                if (did.Length == 0)
                {
                    did = Guid.NewGuid().ToString("N");
                }
            }

            
            var roles = await KuroBbsClient.Instance.SeekGameRoleAsync(token);
            if (roles is not { IsSuccess: true, Data: { Count: > 0 } roleList })
            {
                _textHint.Text = string.Format(Lang.Instance.Login_SeekRoleFailed, roles.Msg ?? $"code={roles.Code}");
                return;
            }

            var role = roleList[0];
            var account = new KuroBbsAccount
            {
                Token = token,
                Did = did,
                Phone = phoneUsed,
                UserId = role.UserId,
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                AvatarUrl = role.AvatarUrl,
                KuroAvatarUrl = loginData?.HeadPhotoUrl ?? "",
            };
            KuroBbsAccountStore.AddOrUpdate(account);
            ResultIndex = KuroBbsAccountStore.CurrentIndex;
            InAppToast.MainWindow?.Success(string.Format(Lang.Instance.Login_Success, account.RoleName));
            _dialog.Hide();
        }
        catch (Exception ex)
        {
            _textHint.Text = $"{string.Format(Lang.Instance.Login_Failed, ex.Message)} (0x{ex.HResult:X8})";
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _busy, 0);
        }
    }

}
