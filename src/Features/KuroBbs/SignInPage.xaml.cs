using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using Arietta.Features.ViewHost;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

public sealed partial class SignInPage : PageBase
{

    public SignInPage()
    {
        this.InitializeComponent();
        this.Loaded += SignInPage_Loaded;
    }


    private async void SignInPage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= SignInPage_Loaded;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;
        await LoadAllAsync();
    }


    protected override void OnUnloaded()
    {
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        base.OnUnloaded();
    }


    private async void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () => await LoadAllAsync());
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e) => await LoadAllAsync();

    private static bool IsPgr => MainView.CurrentGameBiz.IsPunishingGrayRaven();

    private static string AutoSignConfigKey => IsPgr ? "KuroBbs_AutoSign_Pgr" : "KuroBbs_AutoSign_Wuwa";

    private async Task<(string gameId, string serverId, string roleId)?> GetSignTargetAsync(KuroBbsAccount account)
    {
        if (!IsPgr)
        {
            return (KuroBbsClient.ParamGameId, KuroBbsClient.ParamServerId, account.RoleId);
        }

        return await KuroBbsAccountStore.EnsurePgrRoleAsync(account)
            ? ("2", account.PgrServerId, account.PgrRoleId)
            : null;
    }


    private async Task LoadAllAsync()
    {

        var account = KuroBbsAccountStore.Load(IsPgr);
        if (account is null || !account.IsValid)
        {
            InfoBar_NoAccount.IsOpen = true;
            return;
        }
        InfoBar_NoAccount.IsOpen = false;
        ProgressRing_Loading.IsActive = true;
        try
        {
            var target = await GetSignTargetAsync(account);
            var result = target is { } t
                ? await KuroBbsClient.Instance.GetSignInInitAsync(account, t.gameId, t.serverId, t.roleId)
                : await KuroBbsClient.Instance.GetSignInInitAsync(account);
            FillSignGoods(result);
            Toggle_AutoSign.IsOn = AppConfig.GetConfig(AutoSignConfigKey) != "0";
        }
        catch (Exception ex)
        {
            KuroBbsUi.HandleException(ex, Lang.Instance.KuroBbs_SignIn);
        }
        finally
        {
            ProgressRing_Loading.IsActive = false;
        }
    }

    private void FillSignGoods(KuroBbsResponse<KuroSignInitData> result)
    {
        if (result is not { IsSuccess: true, Data: { } data })
        {
            if (result.IsTokenExpired)
            {
                KuroBbsUi.ShowTokenExpiredToast();
            }
            else
            {
                TextBlock_SignStatus.Text = result.Msg ?? "";
                TextBlock_SignProgress.Text = "";
            }
            return;
        }

        TextBlock_SignStatus.Text = data.IsSignIn ? Lang.Instance.SignPage_SignedToday : Lang.Instance.SignPage_NotSignedToday;
        TextBlock_SignProgress.Text = string.Format(Lang.Instance.SignPage_SignedDaysFormat, data.SignInNum);
        Button_SignIn.IsEnabled = !data.IsSignIn;

        List<KuroSignGood> goods = data.SignInGoodsConfigs ?? [];
        GridView_Goods.ItemsSource = goods
            .Select((g, i) => new SignGoodItem(g, i + 1, !data.IsSignIn && i == data.SignInNum))
            .ToList();
    }


    private async void Button_SignIn_Click(object sender, RoutedEventArgs e)
    {
        var account = KuroBbsAccountStore.Load(IsPgr);
        if (account is null) return;
        Button_SignIn.IsEnabled = false;
        try
        {
            var result = await GetSignTargetAsync(account) is { } t
                ? await KuroBbsClient.Instance.SignInAsync(account, t.gameId, t.serverId, t.roleId)
                : await KuroBbsClient.Instance.SignInAsync(account);
            InAppToast.MainWindow?.Success(result.Msg ?? Lang.Instance.SignPage_Done);
        }
        catch (Exception ex)
        {
            KuroBbsUi.HandleException(ex, Lang.Instance.KuroBbs_SignIn);
        }
        await LoadAllAsync();
    }


    private void Toggle_AutoSign_Toggled(object sender, RoutedEventArgs e)
    {
        AppConfig.SetConfig(AutoSignConfigKey, Toggle_AutoSign.IsOn ? "1" : "0");
    }

}

public sealed class SignGoodItem
{
    public ImageSource? IconSource { get; init; }
    public string GoodsText { get; init; } = "";
    public string DayText { get; init; } = "";
    public bool IsSign { get; init; }
    public bool IsNext { get; init; }

    public Visibility SignedVisibility => IsSign ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NextVisibility => IsNext ? Visibility.Visible : Visibility.Collapsed;

    public double DimOpacity => IsSign ? 0.45 : 1.0;

    public Brush DayBrush { get; init; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);

    public SignGoodItem(KuroSignGood good, int dayNumber, bool isNext)
    {
        try
        {
            if (!string.IsNullOrEmpty(good.GoodsUrl))
            {
                IconSource = new BitmapImage(new Uri(good.GoodsUrl));
            }
        }
        catch { }
        GoodsText = $"{good.GoodsName} ×{good.GoodsNum}";
        IsSign = good.IsSign;
        IsNext = isNext;

        int day = good.SerialNum is > 0 ? good.SerialNum!.Value : dayNumber;
        DayText = string.Format(Lang.Instance.SignPage_DayFormat, day);
        DayBrush = FindBrush(isNext ? "AccentTextFillColorPrimaryBrush" : "TextFillColorTertiaryBrush");
    }

    private static Brush FindBrush(string key)
    {
        try
        {
            if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
            {
                return brush;
            }
        }
        catch { }
        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
