using Microsoft.Extensions.Logging;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

public static class KuroBbsAutoSignService
{

    private const string KeySwitchCommunity = "KuroBbs_AutoSign_Community";
    private const string KeySwitchWuwa = "KuroBbs_AutoSign_Wuwa";
    private const string KeySwitchPgr = "KuroBbs_AutoSign_Pgr";

    private const string PrefixDone = "KuroBbs_AutoSign_LastRun_";
    private const string ItemCommunity = "Community";
    private const string ItemWuwa = "Wuwa";
    private const string ItemPgr = "Pgr";
    private const string ItemResource = "Resource";

    private static readonly Microsoft.Extensions.Logging.ILogger Log = AppConfig.GetLogger("KuroBbsAutoSignService");

    private static int _running;

    private static string Today => DateTime.Now.ToString("yyyyMMdd");

    private static bool SwitchOn(string key) => AppConfig.GetConfig(key) != "0";

    private static bool DoneToday(KuroBbsAccount account, string item)
        => AppConfig.GetConfig(PrefixDone + item + "_" + AccountKey(account)) == Today;

    private static void MarkDone(KuroBbsAccount account, string item)
        => AppConfig.SetConfig(PrefixDone + item + "_" + AccountKey(account), Today);

    private static string AccountKey(KuroBbsAccount account)
    {
        if (!string.IsNullOrEmpty(account.UserId))
        {
            return account.UserId;
        }
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(account.Token));
        return Convert.ToHexString(hash)[..12];
    }

    private static string AccountLabel(KuroBbsAccount account, int index)
    {
        if (!string.IsNullOrEmpty(account.RoleName)) return account.RoleName;
        if (!string.IsNullOrEmpty(account.Phone)) return account.Phone;
        if (!string.IsNullOrEmpty(account.UserId)) return account.UserId;
        return index >= 0 ? $"#{index + 1}" : "?";
    }

    private sealed record SignResult(string Name, bool Ok, string Detail);


    private static void Collect(List<SignResult> results, SignResult? result)
    {
        if (result is not null)
        {
            results.Add(result);
        }
    }

    private static string SignMessage(string? msg, bool ok)
        => string.IsNullOrWhiteSpace(msg)
            ? (ok ? Lang.Instance.KuroBbs_SignOk : Lang.Instance.KuroBbs_SignFail)
            : msg;


    public static async Task RunDailyOnceAsync()
    {

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }
        try
        {

            var accounts = KuroBbsAccountStore.Accounts
                .Where(a => !string.IsNullOrEmpty(a.Token))
                .ToList();
            if (accounts.Count == 0)
            {

                return;
            }

            bool community = SwitchOn(KeySwitchCommunity);
            bool wuwa = SwitchOn(KeySwitchWuwa);
            bool pgr = SwitchOn(KeySwitchPgr);

            bool needSign = accounts.Any(a =>
                   (community && !DoneToday(a, ItemCommunity))
                || (wuwa && !DoneToday(a, ItemWuwa))
                || (pgr && !DoneToday(a, ItemPgr)));
            bool needResource = accounts.Any(a => !DoneToday(a, ItemResource));
            if (!needSign && !needResource)
            {
                return;
            }

            await Task.Delay(4000);

            bool multi = accounts.Count > 1;
            List<SignResult> results = new();

            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                string prefix = multi ? AccountLabel(account, i) + "·" : "";
                if (community)
                {
                    Collect(results, await RunCommunitySignInAsync(account, prefix));
                }
                if (wuwa)
                {
                    Collect(results, await RunWuwaSignInAsync(account, prefix));
                }
                if (pgr)
                {
                    Collect(results, await RunPgrSignInAsync(account, prefix));
                }
            }

            foreach (var account in accounts)
            {
                await RunResourceCaptureAsync(account);
            }

            if (results.Count == 0)
            {

                return;
            }

            var failed = results.Where(r => !r.Ok).ToList();
            if (failed.Count > 0)
            {

                string detail = string.Join("；", failed.Select(r => $"{r.Name}：{r.Detail}"));
                InAppToast.MainWindow?.Error(Lang.Instance.KuroBbs_AutoSignFailedToast, detail);
            }
            else
            {
                InAppToast.MainWindow?.Success(Lang.Instance.KuroBbs_AutoSignDoneToast);
            }
            KuroBbsEvents.RaiseAccountChanged();
        }
        catch (Exception ex)
        {

            Log.LogError(ex, "自动签到：本轮中途异常，未完成的项不记「今天已做」");
            KuroBbsUi.HandleException(ex, Lang.Instance.KuroBbs_AutoSignFailedToast);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private static async Task<SignResult?> RunCommunitySignInAsync(KuroBbsAccount account, string prefix)
    {
        if (DoneToday(account, ItemCommunity))
        {
            return null;
        }
        string name = prefix + "库街区";
        try
        {
            var result = await KuroBbsClient.Instance.CommunitySignInAsync(account.Token);
            bool ok = result.IsSuccess || result.Code == 1511;
            if (ok)
            {
                MarkDone(account, ItemCommunity);
            }
            Log.LogInformation("自动签到 · {Account} 库街区社区：code={Code} success={Ok} msg={Msg}",
                AccountLabel(account, -1), result.Code, ok, result.Msg);
            return new SignResult(name, ok, SignMessage(result.Msg, ok));
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "自动签到 · {Account} 库街区社区：请求异常，不记「今天已做」", AccountLabel(account, -1));
            return new SignResult(name, false, Lang.Instance.KuroBbs_SignFail);
        }
    }

    private static async Task<SignResult?> RunWuwaSignInAsync(KuroBbsAccount account, string prefix)
    {
        if (DoneToday(account, ItemWuwa))
        {
            return null;
        }
        string name = prefix + "鸣潮";
        try
        {
            if (string.IsNullOrEmpty(account.RoleId))
            {

                Log.LogInformation("自动签到 · {Account} 鸣潮：账号尚未查到角色 id，本轮跳过", AccountLabel(account, -1));
                return null;
            }
            var result = await KuroBbsClient.Instance.SignInAsync(account);
            if (result.IsSuccess)
            {
                MarkDone(account, ItemWuwa);
            }
            Log.LogInformation("自动签到 · {Account} 鸣潮：code={Code} success={Ok} msg={Msg}",
                AccountLabel(account, -1), result.Code, result.IsSuccess, result.Msg);
            return new SignResult(name, result.IsSuccess, SignMessage(result.Msg, result.IsSuccess));
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "自动签到 · {Account} 鸣潮：请求异常，不记「今天已做」", AccountLabel(account, -1));
            return new SignResult(name, false, Lang.Instance.KuroBbs_SignFail);
        }
    }

    private static async Task<SignResult?> RunPgrSignInAsync(KuroBbsAccount account, string prefix)
    {
        if (DoneToday(account, ItemPgr))
        {
            return null;
        }
        string label = AccountLabel(account, -1);
        string name = prefix + "战双";
        try
        {

            bool hasRole = await KuroBbsAccountStore.EnsurePgrRoleAsync(account);
            if (!hasRole)
            {

                bool definite = account.HasPgrRole == false;
                if (definite)
                {
                    MarkDone(account, ItemPgr);
                }
                Log.LogInformation("自动签到 · {Account} 战双：没有可用的战双角色（结论确定={Definite}）", label, definite);

                return null;
            }

            var result = await KuroBbsClient.Instance.SignInAsync(account, "2", account.PgrServerId, account.PgrRoleId);
            if (result.IsSuccess)
            {
                MarkDone(account, ItemPgr);
            }
            Log.LogInformation("自动签到 · {Account} 战双：code={Code} success={Ok} msg={Msg}",
                label, result.Code, result.IsSuccess, result.Msg);
            return new SignResult(name, result.IsSuccess, SignMessage(result.Msg, result.IsSuccess));
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "自动签到 · {Account} 战双：请求异常，不记「今天已做」", label);
            return new SignResult(name, false, Lang.Instance.KuroBbs_SignFail);
        }
    }

    private static async Task RunResourceCaptureAsync(KuroBbsAccount account)
    {
        if (DoneToday(account, ItemResource))
        {
            return;
        }
        await CaptureResourceRecordsAsync(account);
        MarkDone(account, ItemResource);
    }

    private static async Task CaptureResourceRecordsAsync(KuroBbsAccount account)
    {
        try
        {
            if (!string.IsNullOrEmpty(account.RoleId))
            {
                var list = await KuroBbsClient.Instance.GetBriefingListAsync(account.Token);
                if (list is { IsSuccess: true, Data: { } briefing })
                {
                    await CaptureBriefingPeriodAsync(account, "month", briefing.Months);
                    await CaptureBriefingPeriodAsync(account, "week", briefing.Weeks);
                    await CaptureBriefingPeriodAsync(account, "version", briefing.Versions);
                }
            }
            if (!string.IsNullOrEmpty(account.PgrRoleId))
            {
                var report = await KuroBbsClient.Instance.GetPgrResourceReportAsync(account);
                if (report is { IsSuccess: true, Data: { } data })
                {
                    ResourceRecordStore.SavePgrReport(account.PgrRoleId, data);
                }
            }
        }
        catch
        {

        }
    }

    private static async Task CaptureBriefingPeriodAsync(KuroBbsAccount account, string type, List<KuroBriefingTitle>? periods)
    {
        if (periods is not { Count: > 0 })
        {
            return;
        }
        try
        {
            var detail = await KuroBbsClient.Instance.GetBriefingDetailAsync(account, periods[0].Index, type);
            if (detail is { IsSuccess: true, Data: { } record })
            {
                ResourceRecordStore.SaveBriefing(account.RoleId, type, periods[0].Index, periods[0].Title, record);
            }
        }
        catch { }
    }

}
