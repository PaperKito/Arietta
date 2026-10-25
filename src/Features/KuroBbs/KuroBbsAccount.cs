using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Arietta.Helpers;

namespace Arietta.Features.KuroBbs;

public sealed class KuroBbsAccount
{

    public string Token { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Did { get; set; } = "";

    public string UserId { get; set; } = "";

    public string RoleId { get; set; } = "";

    public string ServerId { get; set; } = "";

    public string PgrRoleId { get; set; } = "";

    public string PgrServerId { get; set; } = "";

    public bool? HasPgrRole { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTimeOffset PgrRoleCheckedAt { get; set; }

    public string PgrRoleName { get; set; } = "";

    public int PgrLevel { get; set; }

    public string PgrAvatarUrl { get; set; } = "";

    public string RoleName { get; set; } = "";

    public string AvatarUrl { get; set; } = "";

    public string KuroAvatarUrl { get; set; } = "";

    public int Level { get; set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(Token)
        && !string.IsNullOrWhiteSpace(Did)
        && !string.IsNullOrWhiteSpace(UserId)
        && !string.IsNullOrWhiteSpace(RoleId);
}

public static class KuroBbsAccountStore
{
    private const string KeyAccounts = "KuroBbs_Accounts";

    private const string KeyCurrent = "KuroBbs_Current";

    private const string KeyCurrentPgr = "KuroBbs_Current_Pgr";

    private static readonly Microsoft.Extensions.Logging.ILogger Log = AppConfig.GetLogger("KuroBbsAccountStore");

    private static readonly TimeSpan PgrRoleRecheckInterval = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static List<KuroBbsAccount> _accounts = [];

    private static int _currentIndex;

    private static int _pgrIndex;

    public static IReadOnlyList<KuroBbsAccount> Accounts
    {
        get
        {
            EnsureLoaded();
            return _accounts;
        }
    }

    public static int CurrentIndex
    {
        get
        {
            EnsureLoaded();
            return _currentIndex;
        }
    }

    public static int GetIndex(bool pgr)
    {
        EnsureLoaded();
        return Math.Clamp(pgr ? _pgrIndex : _currentIndex, 0, Math.Max(_accounts.Count - 1, 0));
    }

    public static KuroBbsAccount? Load()
    {
        EnsureLoaded();
        return _accounts.Count == 0 ? null : _accounts[Math.Clamp(_currentIndex, 0, _accounts.Count - 1)];
    }

    public static KuroBbsAccount? Load(bool pgr)
    {
        EnsureLoaded();
        if (_accounts.Count == 0)
        {
            return null;
        }
        int idx = Math.Clamp(pgr ? _pgrIndex : _currentIndex, 0, _accounts.Count - 1);
        if (pgr && _accounts[idx].HasPgrRole == false)
        {
            int better = _accounts.FindIndex(a => a.HasPgrRole == true);
            if (better >= 0 && better != idx)
            {
                idx = better;
                _pgrIndex = better;
                AppConfig.SetConfig(KeyCurrentPgr, better.ToString());
            }
        }
        return _accounts[idx];
    }

    public static void SwitchTo(int index, bool pgr)
    {
        EnsureLoaded();
        if (index >= 0 && index < _accounts.Count)
        {
            if (pgr)
            {
                _pgrIndex = index;
                AppConfig.SetConfig(KeyCurrentPgr, index.ToString());
            }
            else
            {
                _currentIndex = index;
                AppConfig.SetConfig(KeyCurrent, index.ToString());
            }
        }
    }

    public static void AddOrUpdate(KuroBbsAccount account, bool setCurrent = true)
    {
        EnsureLoaded();
        var existing = _accounts.FindIndex(a => SameAccount(a, account));
        if (existing < 0)
        {

            existing = _accounts.FindIndex(a => a.Token == account.Token
                && (string.IsNullOrEmpty(a.RoleId) || string.IsNullOrEmpty(account.RoleId)));
        }
        if (existing >= 0)
        {

            if (string.IsNullOrEmpty(account.Phone))
            {
                account.Phone = _accounts[existing].Phone;
            }
            _accounts[existing] = account;
            if (setCurrent)
            {
                _currentIndex = existing;
                _pgrIndex = existing;
            }
        }
        else
        {
            _accounts.Add(account);
            if (setCurrent)
            {
                _currentIndex = _accounts.Count - 1;
                _pgrIndex = _accounts.Count - 1;
            }
        }
        Persist();
    }

    private static bool SameAccount(KuroBbsAccount a, KuroBbsAccount b)
    {
        if (a.Token != b.Token)
        {
            return false;
        }
        return string.Equals(a.RoleId, b.RoleId, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(a.RoleId) || string.IsNullOrEmpty(b.RoleId);
    }

    public static async Task<bool> EnsurePgrRoleAsync(KuroBbsAccount account)
    {
        if (!string.IsNullOrEmpty(account.PgrRoleId))
        {

            if (account.HasPgrRole != true)
            {
                account.HasPgrRole = true;
                AddOrUpdate(account, setCurrent: false);
            }
            return true;
        }
        if (string.IsNullOrEmpty(account.Token))
        {
            return false;
        }

        if (account.HasPgrRole == false
            && DateTimeOffset.UtcNow - account.PgrRoleCheckedAt < PgrRoleRecheckInterval)
        {
            return false;
        }
        var roles = await KuroBbsClient.Instance.SeekGameRoleAsync(account.Token, 2);
        account.PgrRoleCheckedAt = DateTimeOffset.UtcNow;
        Log.LogInformation("PGR character query: code={Code} success={Success} count={Count}",
            roles?.Code, roles?.IsSuccess, roles?.Data?.Count);
        if (roles is { IsSuccess: true, Data: { Count: > 0 } })
        {
            var role = roles.Data.FirstOrDefault(r => string.Equals(r.RoleId, account.PgrRoleId, StringComparison.OrdinalIgnoreCase))
                ?? roles.Data[0];
            account.PgrRoleId = role.RoleId;
            account.PgrServerId = role.ServerId;
            account.PgrRoleName = role.RoleName;
            account.PgrLevel = role.Level;
            account.HasPgrRole = true;

            AddOrUpdate(account, setCurrent: false);
            return true;
        }

        if (roles is { IsSuccess: true })
        {
            account.HasPgrRole = false;
            Log.LogInformation("PGR character query: {Cuid} has no PGR characters, removed from account switch list", account.UserId);
            AddOrUpdate(account, setCurrent: false);
        }
        return false;
    }

    public static async Task EnsurePgrRolesAsync()
    {
        EnsureLoaded();
        foreach (var account in _accounts.ToList())
        {
            try
            {
                await EnsurePgrRoleAsync(account);
            }
            catch
            {

            }
        }
    }


    public static void RemoveAt(int index)
    {
        EnsureLoaded();
        if (index < 0 || index >= _accounts.Count)
        {
            return;
        }
        _accounts.RemoveAt(index);

        _currentIndex = FixIndexAfterRemove(_currentIndex, index);
        _pgrIndex = FixIndexAfterRemove(_pgrIndex, index);
        Persist();
    }

    private static int FixIndexAfterRemove(int currentIndex, int removedIndex)
    {
        if (currentIndex > removedIndex)
        {
            return currentIndex - 1;
        }
        return Math.Min(currentIndex, Math.Max(_accounts.Count - 1, 0));
    }

    public static void Clear()
    {
        _accounts = [];
        _currentIndex = 0;
        _pgrIndex = 0;
        AppConfig.SetConfig(KeyAccounts, null);
        AppConfig.SetConfig(KeyCurrent, null);
        AppConfig.SetConfig(KeyCurrentPgr, null);
    }


    private static void Persist()
    {
        try
        {
            string json = JsonSerializer.Serialize(_accounts, JsonOptions);
            AppConfig.SetConfig(KeyAccounts, DpapiProtector.Protect(json));
            AppConfig.SetConfig(KeyCurrent, _currentIndex.ToString());
            AppConfig.SetConfig(KeyCurrentPgr, _pgrIndex.ToString());
        }
        catch { }
    }

    private static bool _loaded;

    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;
        try
        {
            string? protectedJson = AppConfig.GetConfig(KeyAccounts);
            string? json = DpapiProtector.Unprotect(protectedJson ?? "");
            if (!string.IsNullOrEmpty(json))
            {
                _accounts = JsonSerializer.Deserialize<List<KuroBbsAccount>>(json) ?? [];

                _accounts = _accounts
                    .GroupBy(a => $"{a.Token}|{a.RoleId}", StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }
            _ = int.TryParse(AppConfig.GetConfig(KeyCurrent), out _currentIndex);

            _pgrIndex = _currentIndex;
            _ = int.TryParse(AppConfig.GetConfig(KeyCurrentPgr), out _pgrIndex);
        }
        catch
        {
            _accounts = [];
        }
    }
}
