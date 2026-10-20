using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Features.ViewHost;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

public sealed record KuroBbsFeature(string Title, string Glyph);

public static class KuroBbsEvents
{
    public static event Action? AccountChanged;

    public static void RaiseAccountChanged() => AccountChanged?.Invoke();
}

public sealed partial class KuroBbsPage : PageBase
{
    private static ILogger Log => AppConfig.GetLogger("KuroBbsPage");

    private static readonly KuroBbsFeature SignInFeature = new(Lang.Instance.KuroBbs_SignIn, "\uE787");

    private static readonly KuroBbsFeature DataTerminalFeature = new(Lang.Instance.KuroBbs_DataTerminal, "\uE8B7");

    private static readonly KuroBbsFeature PgrRoleFeature = new(Lang.Instance.KuroBbs_PgrRole, "\uE716");
    private static readonly KuroBbsFeature PgrResourceFeature = new(Lang.Instance.KuroBbs_PgrResource, "\uE8A5");
    private static readonly KuroBbsFeature PgrCageFeature = new(Lang.Instance.KuroBbs_PgrCage, "\uE734");
    private static readonly KuroBbsFeature PgrWarAreaFeature = new(Lang.Instance.KuroBbs_PgrWarArea, "\uE945");
    private static readonly KuroBbsFeature PgrTransfiniteFeature = new(Lang.Instance.KuroBbs_PgrTransfinite, "\uE774");

    private static bool IsPgr => MainView.CurrentGameBiz.IsPunishingGrayRaven();

    private readonly NavigationViewItem[] _wuwaOnlyItems;

    private readonly NavigationViewItem[] _pgrOnlyItems;


    public KuroBbsPage()
    {
        this.InitializeComponent();
        _wuwaOnlyItems =
        [
            NavigationViewItem_Role,
            NavigationViewItem_ResourceBrief,
            NavigationViewItem_TowerOfAdversity,
            NavigationViewItem_SongOfVoidSea,
            NavigationViewItem_DoomsdayMatrix,
        ];
        _pgrOnlyItems =
        [
            NavigationViewItem_PgrResource,
            NavigationViewItem_PgrRole,
            NavigationViewItem_PgrCage,
            NavigationViewItem_PgrWarArea,
            NavigationViewItem_PgrTransfinite,
        ];
        UpdateGameHeader();
        UpdateAccountBar();

        NavigateTo(SignInFeature, new SuppressNavigationTransitionInfo());

        NavigationView_KuroBbs.SelectedItem = NavigationViewItem_SignIn;
    }


    protected override void OnLoaded()
    {
        MainView.GameChanged += MainView_GameChanged;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;

        _ = RefreshAccountInfoAsync();
    }


    protected override void OnUnloaded()
    {
        MainView.GameChanged -= MainView_GameChanged;
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
    }


    private void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(UpdateAccountBar);
    }


    private void MainView_GameChanged(object? sender, GameBiz biz)
    {
        UpdateGameHeader();

        bool stillValid = IsPgr
            ? Frame_KuroBbs.Content is SignInPage or PgrRolePage or PgrResourcePage or PgrChallengePage
            : Frame_KuroBbs.Content is not (PgrRolePage or PgrResourcePage or PgrChallengePage);
        if (!stillValid)
        {
            NavigateTo(SignInFeature, new SuppressNavigationTransitionInfo());
        }
        UpdateAccountBar();
        _ = RefreshAccountInfoAsync();
    }

    private void UpdateGameHeader()
    {
        var wuwaVisibility = IsPgr ? Visibility.Collapsed : Visibility.Visible;
        foreach (var item in _wuwaOnlyItems)
        {
            item.Visibility = wuwaVisibility;
        }
        foreach (var item in _pgrOnlyItems)
        {
            item.Visibility = IsPgr ? Visibility.Visible : Visibility.Collapsed;
        }

        TextBlock_DataTerminal.Text = IsPgr ? Lang.Instance.KuroBbs_CombatData : Lang.Instance.KuroBbs_DataTerminal;

        Image_DataTerminal.Source = new BitmapImage(new Uri(
            IsPgr ? "ms-appx:///Assets/Image/Common/CombatData.png"
                  : "ms-appx:///Assets/Image/Common/DataTerminal.png"));

        TextBlock_SignIn.Text = IsPgr ? Lang.Instance.PgrData_DailySupply : Lang.Instance.KuroBbs_SignIn;
        Image_SignIn.Source = new BitmapImage(new Uri(
            IsPgr ? "ms-appx:///Assets/Image/Common/DailySupply.png"
                  : "ms-appx:///Assets/Image/Common/CheckIn.png"));
    }


    #region 顶部账号栏（照抄 Starward GameRecordPage 交互）

    private void UpdateAccountBar()
    {
        var accounts = KuroBbsAccountStore.Accounts;

        var account = KuroBbsAccountStore.Load(IsPgr);

        bool pgr = IsPgr;
        Run_AccountLevelLabel.Text = pgr ? Lang.Instance.KuroBbs_CommanderLevel + " " : Lang.Instance.KuroBbs_UnionLevel + " ";
        string name = pgr ? (account?.PgrRoleName ?? "") : (account?.RoleName ?? "");
        int level = pgr ? (account?.PgrLevel ?? 0) : (account?.Level ?? 0);

        if (account is null || string.IsNullOrEmpty(name))
        {
            SetAvatar(TextBlock_AvatarChar, Image_Avatar, null, "?");
            StackPanel_RoleInfo.Visibility = Visibility.Collapsed;
        }
        else
        {
            SetAvatar(TextBlock_AvatarChar, Image_Avatar, GetDisplayAvatar(account, pgr), name[..1]);
            StackPanel_RoleInfo.Visibility = Visibility.Visible;
            TextBlock_AccountName.Text = name;
            Run_AccountLevel.Text = level > 0 ? level.ToString() : "-";
        }

        var all = accounts.Select((a, i) => new AccountItem(a, i, pgr)).ToList();
        var items = pgr ? all.Where(x => x.Account.HasPgrRole != false).ToList() : all;
        if (items.Count == 0)
        {

            items = all;
        }
        ListView_Accounts.ItemsSource = items;

        ListView_Accounts.SelectedItem = items.FirstOrDefault(x => x.Index == KuroBbsAccountStore.GetIndex(pgr));
    }

    private void SetAvatar(TextBlock fallbackText, Microsoft.UI.Xaml.Controls.Image image, string? url, string fallbackChar)
    {
        fallbackText.Text = fallbackChar;
        ImageSource? source = null;
        try
        {
            if (!string.IsNullOrEmpty(url))
            {
                source = new BitmapImage(new Uri(url));
            }
        }
        catch { }
        Brush fillBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        if (source is not null)
        {
            image.Source = source;
            image.Visibility = Visibility.Visible;
            fallbackText.Visibility = Visibility.Collapsed;
            if (ReferenceEquals(image, Image_Avatar))
            {
                Border_Avatar.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }
        else
        {
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
            fallbackText.Visibility = Visibility.Visible;
            if (ReferenceEquals(image, Image_Avatar))
            {
                Border_Avatar.Background = fillBrush;
            }
        }
    }

    internal static string GetDisplayAvatar(KuroBbsAccount account, bool pgr)
    {
        if (pgr)
        {
            return string.IsNullOrEmpty(account.PgrAvatarUrl) ? account.KuroAvatarUrl : account.PgrAvatarUrl;
        }
        if (!string.IsNullOrEmpty(account.KuroAvatarUrl))
        {
            return account.KuroAvatarUrl;
        }
        return account.AvatarUrl;
    }

    private static async Task BackfillPgrRoleInfoAsync(KuroBbsAccount account)
    {
        if (string.IsNullOrEmpty(account.PgrRoleId))
        {
            return;
        }
        try
        {
            var accData = await KuroBbsClient.Instance.GetPgrAccountDataAsync(account);
            if (accData is not { IsSuccess: true, Data: { } data })
            {
                Log.LogWarning("战双 accountData 回填失败：code={Code} msg={Msg}", accData?.Code, accData?.Msg);
                return;
            }
            bool dirty = false;
            if (!string.IsNullOrEmpty(data.HeadIconUrl) && data.HeadIconUrl != account.PgrAvatarUrl)
            {
                account.PgrAvatarUrl = data.HeadIconUrl;
                dirty = true;
            }
            if (!string.IsNullOrEmpty(data.RoleName) && data.RoleName != account.PgrRoleName)
            {
                account.PgrRoleName = data.RoleName;
                dirty = true;
            }
            if (data.Level > 0 && data.Level != account.PgrLevel)
            {
                account.PgrLevel = data.Level;
                dirty = true;
            }
            if (dirty)
            {
                KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "战双 accountData 回填异常");
        }
    }

    private void Grid_Avatar_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Flyout_Accounts.ShowAt(Button_AccountSwitcher);
    }

    private void NavigationViewItem_ModuleSettings_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Toggle_KuroCommunitySign.IsOn = AppConfig.GetConfig("KuroBbs_AutoSign_Community") != "0";
        FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
    }

    private void Toggle_KuroCommunitySign_Toggled(object sender, RoutedEventArgs e)
    {
        AppConfig.SetConfig("KuroBbs_AutoSign_Community", Toggle_KuroCommunitySign.IsOn ? "1" : "0");
    }


    private async void Button_Login_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Flyout_Accounts.Hide();
            var dialog = new KuroBbsLoginDialog(this.XamlRoot);
            int result = await dialog.ShowAsync();
            if (result >= 0)
            {
                UpdateAccountBar();
                KuroBbsEvents.RaiseAccountChanged();
                await RefreshAccountInfoAsync();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.KuroBbs_Login);
        }
    }


    private async void Button_RefreshInfo_Click(object sender, RoutedEventArgs e)
    {
        Flyout_Accounts.Hide();
        await RefreshAccountInfoAsync();
    }

    private async void ListView_Accounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListView_Accounts.SelectedItem is AccountItem item
            && item.Index != KuroBbsAccountStore.GetIndex(IsPgr))
        {
            KuroBbsAccountStore.SwitchTo(item.Index, IsPgr);
            UpdateAccountBar();
            KuroBbsEvents.RaiseAccountChanged();
            await RefreshAccountInfoAsync();
        }
    }

    private async void MenuFlyoutItem_SwitchRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: AccountItem item })
        {
            return;
        }
        var accounts = KuroBbsAccountStore.Accounts;
        if (item.Index < 0 || item.Index >= accounts.Count)
        {
            return;
        }
        var account = accounts[item.Index];
        Flyout_Accounts.Hide();

        bool pgr = IsPgr;
        int gameId = pgr ? 2 : 3;
        var roles = await KuroBbsClient.Instance.SeekGameRoleAsync(account.Token, gameId);
        if (roles is not { IsSuccess: true, Data: { Count: > 0 } roleList })
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.KuroBbs_RefreshInfo, roles.Msg ?? $"code={roles.Code}");
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = pgr ? Lang.Instance.KuroBbs_SelectRolePgr : Lang.Instance.KuroBbs_SelectRoleWuwa,
            PrimaryButtonText = Lang.Instance.Common_Ok,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Primary,
        };

        var listView = new ListView
        {
            Margin = new Thickness(0, 12, 0, 0),
            SelectionMode = ListViewSelectionMode.Single,
        };
        string currentId = pgr ? account.PgrRoleId : account.RoleId;
        for (int i = 0; i < roleList.Count; i++)
        {
            var role = roleList[i];
            string roleId = role.RoleId;
            var row = new StackPanel { Spacing = 2 };
            row.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(role.RoleName) ? roleId : role.RoleName });
            row.Children.Add(new TextBlock
            {
                Text = $"Lv.{role.Level} · {role.ServerId}",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            var rowItem = new ListViewItem { Content = row, Tag = role };
            if (string.Equals(roleId, currentId, StringComparison.OrdinalIgnoreCase))
            {
                listView.SelectedItem = rowItem;
            }
            listView.Items.Add(rowItem);
        }
        dialog.Content = new ScrollViewer
        {
            MaxHeight = 320,
            Content = listView,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary
            || listView.SelectedItem is not ListViewItem selectedItem
            || selectedItem.Tag is not KuroBbsAccount selected)
        {
            return;
        }
        if (pgr)
        {
            account.PgrRoleId = selected.RoleId;
            account.PgrServerId = selected.ServerId;
            account.PgrRoleName = selected.RoleName;
            account.PgrLevel = selected.Level;
            account.PgrAvatarUrl = "";
        }
        else
        {
            account.RoleId = selected.RoleId;
            account.ServerId = selected.ServerId;
            account.RoleName = selected.RoleName;
            account.Level = selected.Level;
            account.AvatarUrl = selected.AvatarUrl;
        }
        KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
        if (pgr)
        {
            await BackfillPgrRoleInfoAsync(account);
        }
        UpdateAccountBar();
        KuroBbsEvents.RaiseAccountChanged();
        InAppToast.MainWindow?.Success(pgr ? Lang.Instance.KuroBbs_SwitchedPgr : Lang.Instance.KuroBbs_SwitchedWuwa);
    }

    private void MenuFlyoutItem_DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: AccountItem item })
        {
            KuroBbsAccountStore.RemoveAt(item.Index);
            UpdateAccountBar();
            KuroBbsEvents.RaiseAccountChanged();
            InAppToast.MainWindow?.Success(Lang.Instance.KuroAccount_Cleared);
        }
    }

    private async Task RefreshAccountInfoAsync()
    {

        var account = KuroBbsAccountStore.Load(IsPgr);
        if (account is null)
        {
            return;
        }

        if (IsPgr)
        {

            int pgrIndexBefore = KuroBbsAccountStore.GetIndex(pgr: true);
            try
            {

                await KuroBbsAccountStore.EnsurePgrRolesAsync();
                await KuroBbsAccountStore.EnsurePgrRoleAsync(account);
                await BackfillPgrRoleInfoAsync(account);
            }
            finally
            {
                UpdateAccountBar();
            }
            if (KuroBbsAccountStore.GetIndex(pgr: true) != pgrIndexBefore)
            {
                KuroBbsEvents.RaiseAccountChanged();
            }
            return;
        }
        if (!account.IsValid)
        {
            return;
        }
        try
        {
            var roles = await KuroBbsClient.Instance.SeekGameRoleAsync(account.Token);
            if (roles is { IsSuccess: true, Data: { Count: > 0 } } )
            {

                var role = roles.Data.FirstOrDefault(r => string.Equals(r.RoleId, account.RoleId, StringComparison.OrdinalIgnoreCase))
                    ?? roles.Data[0];
                account.UserId = role.UserId;
                account.RoleId = role.RoleId;
                account.ServerId = role.ServerId;
                account.RoleName = role.RoleName;
                account.AvatarUrl = role.AvatarUrl;
                if (role.Level > 0)
                {
                    account.Level = role.Level;
                }
                KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
            }
            else if (roles.IsTokenExpired)
            {
                KuroBbsUi.ShowTokenExpiredToast();
            }

            if (account.Level <= 0)
            {
                var baseData = await KuroBbsClient.Instance.GetBaseDataAsync(account);
                if (baseData is { IsSuccess: true, Data.ValueKind: JsonValueKind.Object }
                    && baseData.Data.TryGetProperty("level", out var levelEl)
                    && levelEl.ValueKind == JsonValueKind.Number)
                {
                    account.Level = levelEl.GetInt32();
                    KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
                }
            }
        }
        catch (Exception ex)
        {
            KuroBbsUi.HandleException(ex, Lang.Instance.KuroBbs_RefreshInfo);
        }
        UpdateAccountBar();
    }

    #endregion


    private void NavigationView_KuroBbs_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                NavigateToTag(item.Tag as string);
            }
        }
        catch { }
    }


    private static KuroBbsFeature FeatureOf(string? tag)
    {
        return tag switch
        {
            "Role" => new KuroBbsFeature(Lang.Instance.KuroBbs_Role, "\uE716"),
            "PgrRole" => PgrRoleFeature,
            "PgrResource" => PgrResourceFeature,
            "PgrCage" => PgrCageFeature,
            "PgrWarArea" => PgrWarAreaFeature,
            "PgrTransfinite" => PgrTransfiniteFeature,
            "DataTerminal" => DataTerminalFeature,
            "ResourceBrief" => new KuroBbsFeature(Lang.Instance.KuroBbs_ResourceBrief, "\uE8A5"),
            "TowerOfAdversity" => new KuroBbsFeature(Lang.Instance.KuroBbs_TowerOfAdversity, "\uE945"),
            "SongOfVoidSea" => new KuroBbsFeature(Lang.Instance.KuroBbs_SongOfVoidSea, "\uE774"),
            "DoomsdayMatrix" => new KuroBbsFeature(Lang.Instance.KuroBbs_DoomsdayMatrix, "\uE9D9"),
            _ => SignInFeature,
        };
    }

    private void NavigateToTag(string? tag)
    {

        if (tag == "DataTerminal")
        {
            DataTerminalWindow.ShowTerminal();
            return;
        }
        var pageType = tag switch
        {
            "Role" => typeof(RolePage),
            "ResourceBrief" => typeof(ResourceBriefPage),
            "TowerOfAdversity" or "SongOfVoidSea" or "DoomsdayMatrix" => typeof(CombatPage),
            "PgrRole" => typeof(PgrRolePage),
            "PgrResource" => typeof(PgrResourcePage),
            "PgrCage" or "PgrWarArea" or "PgrTransfinite" => typeof(PgrChallengePage),
            _ => typeof(SignInPage),
        };
        Frame_KuroBbs.Navigate(pageType, FeatureOf(tag));
    }


    private void NavigateTo(KuroBbsFeature feature, NavigationTransitionInfo? infoOverride = null)
    {

        Frame_KuroBbs.Navigate(typeof(SignInPage), feature, infoOverride);
    }

}

public sealed class AccountItem
{
    public ImageSource? AvatarSource { get; init; }
    public string RoleName { get; init; } = "";
    public string LevelText { get; init; } = "";
    public string LevelLabel { get; init; } = Lang.Instance.KuroBbs_UnionLevel;
    public string SubText => LevelText == "-" ? Lang.Instance.KuroBbs_NoLevel : $"{LevelLabel} {LevelText}";
    public int Index { get; init; }

    public KuroBbsAccount Account { get; init; } = new();

    public AccountItem(KuroBbsAccount account, int index, bool pgr = false)
    {
        string name = pgr ? account.PgrRoleName : account.RoleName;
        int level = pgr ? account.PgrLevel : account.Level;
        Account = account;
        RoleName = string.IsNullOrEmpty(name) ? $"#{index + 1}" : name;
        LevelText = level > 0 ? level.ToString() : "-";
        LevelLabel = pgr ? Lang.Instance.KuroBbs_CommanderLevel : Lang.Instance.KuroBbs_UnionLevel;
        Index = index;
        try
        {
            string avatarUrl = KuroBbsPage.GetDisplayAvatar(account, pgr);
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                AvatarSource = new BitmapImage(new Uri(avatarUrl));
            }
        }
        catch { }
    }
}
