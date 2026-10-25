using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arietta.Helpers;
using Arietta.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class GameAccountSwitcher : UserControl
{

    public GameAccountSwitcher()
    {
        this.InitializeComponent();
        this.Loaded += GameAccountSwitcher_Loaded;
        this.Unloaded += GameAccountSwitcher_Unloaded;
    }

    private bool _refreshing;

    private string? _currentCuid;


    private void GameAccountSwitcher_Loaded(object sender, RoutedEventArgs e)
    {
        GameProcessMonitor.Changed += GameProcessMonitor_Changed;
        RefreshRunningState();
        UpdateAccounts();
    }


    private void GameAccountSwitcher_Unloaded(object sender, RoutedEventArgs e)
    {
        GameProcessMonitor.Changed -= GameProcessMonitor_Changed;
        SelectAccount = null;
        AccountList = null!;
    }


    private void GameProcessMonitor_Changed()
    {
        DispatcherQueue.TryEnqueue(RefreshRunningState);
    }

    public bool IsGameRunning { get; private set => SetProperty(ref field, value); }

    public string? GameRunningHint { get; private set => SetProperty(ref field, value); }

    public Core.GameBiz CurrentGameBiz
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshRunningState();
            }
        }
    }


    public List<KrSdkAccount> AccountList { get; set => SetProperty(ref field, value); }


    public KrSdkAccount? SelectAccount
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                ShowUnsavedHint = value is not null && string.IsNullOrWhiteSpace(value.Nickname);
            }
        }
    }

    public bool ShowUnsavedHint { get; set => SetProperty(ref field, value); }

    public bool CanSwitchAccount { get; set => SetProperty(ref field, value); }


    public void UpdateAccounts()
    {

        _ = UpdateAccountsAsync();
    }


    private async Task UpdateAccountsAsync()
    {
        if (_refreshing)
        {
            return;
        }
        _refreshing = true;
        try
        {
            var biz = CurrentGameBiz;

            var accounts = KrSdkAccountService.GetAccounts(biz);
            foreach (var acc in accounts)
            {
                KrSdkAccountStore.ApplyTo(biz, acc);
            }

            _currentCuid = KrSdkAccountService.GetCurrentCuid(biz);
            AccountList = FilterByRole(biz, accounts);
            SelectAccount = AccountList.FirstOrDefault(a => a.Cuid == _currentCuid) ?? AccountList.FirstOrDefault();
            UpdateCanSwitch();

            string? recentUid = await Task.Run(() => KrSdkAccountService.GetRecentlyLoginUid(biz));

            bool changed = await KrSdkAccountService.RefreshAccountInfosAsync(biz, accounts, recentUid);
            if (changed)
            {
                foreach (var acc in accounts)
                {
                    KrSdkAccountStore.ApplyTo(biz, acc);
                }

                AccountList = FilterByRole(biz, accounts);
                var selected = AccountList.FirstOrDefault(a => a.Cuid == _currentCuid) ?? AccountList.FirstOrDefault();
                SelectAccount = selected;
                ShowUnsavedHint = selected is not null && string.IsNullOrWhiteSpace(selected.Nickname);
                UpdateCanSwitch();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.AccountSwitcher_AccountInfo);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private List<KrSdkAccount> FilterByRole(Core.GameBiz biz, List<KrSdkAccount> accounts)
    {
        if (accounts.Count == 0)
        {
            return accounts;
        }
        var kept = accounts.Where(a => KrSdkAccountStore.Get(biz, a.Cuid)?.HasRole != false).ToList();
        return kept.Count == 0 ? accounts : kept;
    }

    private void ComboBox_GameAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = SelectAccount;
        ShowUnsavedHint = selected is not null && string.IsNullOrWhiteSpace(selected.Nickname);
        UpdateCanSwitch();
    }

    private void UpdateCanSwitch()
    {
        var selected = SelectAccount;
        CanSwitchAccount = !IsGameRunning && selected is not null && selected.Cuid != _currentCuid;
    }

    private void RefreshRunningState()
    {
        bool running = GameProcessMonitor.IsRunning(CurrentGameBiz);
        if (IsGameRunning == running)
        {
            return;
        }
        IsGameRunning = running;
        GameRunningHint = running ? Lang.Instance.AccountSwitcher_GameRunning : null;
        UpdateCanSwitch();
    }


    [RelayCommand]
    private void SwitchAccount()
    {
        try
        {

            if (SelectAccount is not null && CanSwitchAccount && !IsGameRunning)
            {
                KrSdkAccountService.SwitchTo(CurrentGameBiz, SelectAccount.Cuid);
                _currentCuid = SelectAccount.Cuid;
                CanSwitchAccount = false;
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.AccountSwitcher_SwitchAccount);
        }
    }


    [RelayCommand]
    private void SaveAccount()
    {
        try
        {
            if (SelectAccount is not null)
            {

                KrSdkAccountStore.SetNickname(CurrentGameBiz, SelectAccount.Cuid, string.IsNullOrWhiteSpace(SelectAccount.Nickname) ? null : SelectAccount.Nickname);
                ShowUnsavedHint = false;
                UpdateAccounts();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.AccountSwitcher_Save);
        }
    }


    [RelayCommand]
    private void DeleteAccount()
    {
        try
        {
            if (SelectAccount is not null)
            {

                KrSdkAccountStore.Remove(CurrentGameBiz, SelectAccount.Cuid);
                SelectAccount.Nickname = null;
                SelectAccount.RoleId = null;
                SelectAccount.RoleName = null;
                SelectAccount.Level = 0;
                SelectAccount.ServerName = null;
                ShowUnsavedHint = true;
                UpdateAccounts();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.AccountSwitcher_Delete);
        }
    }


}
