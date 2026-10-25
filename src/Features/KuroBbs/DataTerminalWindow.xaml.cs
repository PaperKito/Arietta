using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Arietta.Core;
using Arietta.Features.ViewHost;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

public sealed partial class DataTerminalWindow : Window
{

    private const string WuwaTerminalUrl = "https://web-static.kurobbs.com/mcbox/index.html#/mc-role-box";

    private const string PgrTerminalUrl = "https://web-static.kurobbs.com/pns-box/index.html#/pns-role-box";

    private const string AppUserAgent = "Mozilla/5.0 (Linux; Android 13; 23049RAD8C Build/TQ3A.230805.001; wv) "
        + "AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/124.0.0.0 Mobile Safari/537.36 "
        + "Kuro/3.1.2 KuroGameBox/3.1.2";

    private static readonly string[] BridgeHandlers =
    [
        "KJQKeyboardChange", "activityChangeTitle", "appLogout", "appShare", "appShareFailed",
        "appShareSuccess", "bindAccount", "cacheData", "changeAddress", "chooseActivityPhoto",
        "choosePhoto", "chooseRole", "finishPage", "getAccessToken", "getAuthToken",
        "getCacheData", "getSystemStatus", "getToolInfo", "getUserDefaults", "getUserInfo",
        "listenerKJQKeyboardChange", "onTitleChanged", "openGame", "refreshToken",
        "refreshTokenV2", "response", "selectRole", "selected", "send", "setAppNavbarText",
        "setAppShareData", "setKrStatusBar", "setNavigationBarHidden", "setScreenOrientation",
        "setToolInfo", "share", "showWidgetGuide", "toSkip", "updateTitle", "useSystemSetting",
    ];

    private static DataTerminalWindow? _current;

    private bool _coreReady;
    private string? _scriptId;
    private string _url = "";
    private bool _isPgr;


    private DataTerminalWindow()
    {
        this.InitializeComponent();
        this.Activated += (_, _) => _ = ReloadAsync();
        this.Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, this))
            {
                _current = null;
            }
            MainView.GameChanged -= MainView_GameChanged;
            KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        };
        MainView.GameChanged += MainView_GameChanged;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;

        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(620, 1140));

        try
        {
            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<DataTerminalWindow>().LogWarning(ex, "Failed to set data terminal window title bar");
        }
    }

    public static void ShowTerminal()
    {
        if (_current is not null)
        {
            _current.Activate();
            _ = _current.ReloadAsync();
            return;
        }
        _current = new DataTerminalWindow();
        _current.Activate();
    }


    private bool IsPgr => MainView.CurrentGameBiz.IsPunishingGrayRaven();


    private void MainView_GameChanged(object? sender, GameBiz biz)
    {
        DispatcherQueue.TryEnqueue(async () => await ReloadAsync());
    }


    private void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () => await ReloadAsync());
    }

    private async Task ReloadAsync()
    {
        bool pgr = IsPgr;
        _isPgr = pgr;
        Title = pgr ? Lang.Instance.KuroBbs_CombatData : Lang.Instance.KuroBbs_DataTerminal;

        var account = KuroBbsAccountStore.Load(pgr);
        if (account is null || string.IsNullOrEmpty(account.Token))
        {
            ShowPlaceholder(Lang.Instance.DataTerminal_NoAccount);
            return;
        }
        if (pgr)
        {
            if (!await KuroBbsAccountStore.EnsurePgrRoleAsync(account))
            {
                ShowPlaceholder(string.Format(Lang.Instance.DataTerminal_NoRole, Lang.Instance.GameBiz_ShortPgr));
                return;
            }
        }
        else if (string.IsNullOrEmpty(account.RoleId))
        {
            ShowPlaceholder(string.Format(Lang.Instance.DataTerminal_NoRole, Lang.Instance.GameBiz_Wuwa));
            return;
        }

        string roleId = pgr ? account.PgrRoleId : account.RoleId;
        string serverId = pgr ? account.PgrServerId : KuroBbsClient.ParamServerId;
        string gameId = pgr ? "2" : "3";
        string baseUrl = pgr ? PgrTerminalUrl : WuwaTerminalUrl;

        _url = $"{baseUrl}?accessType=1&roleId={Uri.EscapeDataString(roleId)}&serverId={Uri.EscapeDataString(serverId)}";

        HidePlaceholder();
        try
        {
            await EnsureCoreAsync();
            if (Web.CoreWebView2 is null)
            {
                return;
            }

            await InstallBootstrapAsync(account, roleId, serverId, gameId, pgr);
            Web.CoreWebView2.Navigate(_url);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<DataTerminalWindow>().LogError(ex, "Failed to open data terminal");
            ShowPlaceholder(string.Format(Lang.Instance.DataTerminal_LoadFailed, ex.Message));
        }
    }


    private async Task EnsureCoreAsync()
    {
        if (_coreReady && Web.CoreWebView2 is not null)
        {
            return;
        }

        Arietta.Helpers.WebviewHost.UseDataFolder();
        await Web.EnsureCoreWebView2Async();
        if (Web.CoreWebView2 is null)
        {
            return;
        }
        Web.CoreWebView2.Settings.UserAgent = AppUserAgent;
        Web.CoreWebView2.Settings.IsStatusBarEnabled = false;
        Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        Web.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            if (Web.CoreWebView2?.DocumentTitle is { Length: > 0 } title)
            {
                string prefix = _isPgr ? Lang.Instance.KuroBbs_CombatData : Lang.Instance.KuroBbs_DataTerminal;
                DispatcherQueue.TryEnqueue(() =>
                {
                    Title = $"{prefix} - {title}";
                });
            }
            if (!args.IsSuccess)
            {
                AppConfig.GetLogger<DataTerminalWindow>().LogWarning(
                    "数据终端页面加载失败：{Status}", args.WebErrorStatus);
            }
        };

        Web.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            try
            {
                string message = args.TryGetWebMessageAsString();
                if (message.Contains("__mwl:"))
                {
                    AppConfig.GetLogger<DataTerminalWindow>().LogWarning("[Data terminal JS] {Message}", message);
                }
            }
            catch { }
        };
        _coreReady = true;
    }

    private async Task InstallBootstrapAsync(KuroBbsAccount account, string roleId, string serverId, string gameId, bool pgr)
    {
        if (Web.CoreWebView2 is null)
        {
            return;
        }
        if (_scriptId is not null)
        {
            try
            {
                Web.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(_scriptId);
            }
            catch { }
            _scriptId = null;
        }
        _scriptId = await Web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            BuildBootstrapScript(account, roleId, serverId, gameId, pgr));

        ApplyAuthCookies(account);
    }

    private void ApplyAuthCookies(KuroBbsAccount account)
    {
        if (Web.CoreWebView2 is null)
        {
            return;
        }
        try
        {
            var manager = Web.CoreWebView2.CookieManager;
            foreach (var host in new[] { "web-static.kurobbs.com", "www.kurobbs.com" })
            {
                foreach (var (name, value) in new[]
                {
                    ("token", account.Token), ("did", account.Did), ("userId", account.UserId),
                })
                {
                    var cookie = manager.CreateCookie(name, value ?? "", host, "/");
                    cookie.IsHttpOnly = false;
                    cookie.IsSecure = true;
                    cookie.SameSite = CoreWebView2CookieSameSiteKind.None;
                    manager.AddOrUpdateCookie(cookie);
                }
            }
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<DataTerminalWindow>().LogWarning(ex, "Failed to write KuroBBS cookies");
        }
    }


    #region 登录态注入

    private static string BuildBootstrapScript(KuroBbsAccount account, string roleId, string serverId, string gameId, bool pgr)
    {
        var auth = new Dictionary<string, object?>
        {
            ["token"] = account.Token,
            ["did"] = account.Did,
            ["userId"] = account.UserId,
            ["roleId"] = roleId,
            ["serverId"] = serverId,
            ["roleName"] = pgr ? account.PgrRoleName : account.RoleName,
            ["serverName"] = pgr ? account.PgrServerId : "",
            ["headUrl"] = KuroBbsPage.GetDisplayAvatar(account, pgr),
            ["userName"] = pgr ? account.PgrRoleName : account.RoleName,
            ["gameId"] = gameId,
            ["storage"] = BuildStorage(account, roleId, serverId, gameId, pgr),
        };
        string json = JsonSerializer.Serialize(auth, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default,
        });
        string handlers = JsonSerializer.Serialize(BridgeHandlers);
        return $$"""
        (() => {
            const auth = {{json}};
            const HANDLERS = {{handlers}};

            const write = (k, v) => {
                if (v === undefined || v === null) { return; }
                try { window.localStorage.setItem(k, typeof v === 'string' ? v : JSON.stringify(v)); } catch {}
            };
            for (const [k, v] of Object.entries(auth.storage || {})) { write(k, v); }

            const ok = (p) => JSON.stringify(p ?? {});
            const userInfo = () => ok({
                appVersion: '3.1.2', os: 'Android', headUrl: auth.headUrl, userName: auth.userName,
                ua: 'KuroGameBox', userId: auth.userId, did: auth.did, channelId: '2',
                enterSource: '12', token: auth.token
            });
            const roleInfo = () => ok({
                userId: auth.userId, gameId: auth.gameId, serverId: auth.serverId,
                serverName: auth.serverName, roleId: auth.roleId, roleName: auth.roleName, token: auth.token
            });
            const createResponse = (name, data) => {
                switch (name) {

                    case 'getUserInfo':
                        return auth.storage && auth.storage.initUserInfo ? auth.storage.initUserInfo : userInfo();
                    case 'getAuthToken':
                    case 'getAccessToken':
                        return ok({ token: auth.token, did: auth.did, userId: auth.userId });
                    case 'refreshToken':
                    case 'refreshTokenV2':
                        return ok({ code: 0, token: auth.token, did: auth.did, userId: auth.userId });
                    case 'selectRole':
                    case 'getToolInfo':
                    case 'setToolInfo':
                        return ok({ code: 0, gameId: auth.gameId, roleId: auth.roleId, serverId: auth.serverId,
                                    roleName: auth.roleName, serverName: auth.serverName, headPhotoUrl: auth.headUrl });
                    case 'getSystemStatus':
                        return ok({ darkMode: false, theme: 'light' });
                    case 'getUserDefaults':
                    case 'getCacheData':
                        return ok({});
                    default:
                        return ok({ result: true, echo: data ?? null, handlerName: name });
                }
            };

            const bridge = window.WebViewJavascriptBridge || {};
            bridge.inited = true;
            bridge.init = bridge.init || (() => {});
            bridge.registerHandler = bridge.registerHandler || function (name, cb) {
                (this._handlers = this._handlers || {})[name] = cb;
            };
            bridge.callHandler = bridge.callHandler || function (name, data, cb) {
                const resp = createResponse(name, data);
                if (typeof cb === 'function') { setTimeout(() => cb(resp), 0); }
                return resp;
            };
            for (const n of HANDLERS) {
                bridge[n] = bridge[n] || ((data, cb) => bridge.callHandler(n, data, cb));
            }
            window.WebViewJavascriptBridge = bridge;

            window.jsBridge = window.jsBridge || {};
            window.jsBridge.utils = window.jsBridge.utils || {};
            window.jsBridge.pcBridge = window.jsBridge.pcBridge || {};
            window.jsBridge.platform = 'android';
            window.jsBridge.platformEnum = { android: 'android', ios: 'ios', pc: 'pc' };
            window.jsBridge.callHandler = bridge.callHandler;
            window.jsBridge.registerHandler = bridge.registerHandler;
            window.WVJBCallbacks = window.WVJBCallbacks || [];

            const report = (m) => { try { window.chrome.webview.postMessage('__mwl: ' + m); } catch {} };
            window.addEventListener('error', (e) => report('error: ' + e.message + ' @' + e.filename + ':' + e.lineno));
            window.addEventListener('unhandledrejection', (e) => report('rejection: ' + (e.reason && e.reason.message ? e.reason.message : e.reason)));
            const origError = console.error.bind(console);
            console.error = (...a) => {
                report('console.error: ' + a.map((x) => (typeof x === 'object' ? JSON.stringify(x) : String(x))).join(' ').slice(0, 500));
                origError(...a);
            };
            setTimeout(() => { try { document.dispatchEvent(new Event('WebViewJavascriptBridgeReady')); } catch {} }, 0);
        })();
        """;
    }

    private static Dictionary<string, string?> BuildStorage(
        KuroBbsAccount account, string roleId, string serverId, string gameId, bool pgr)
    {
        var storage = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["token"] = account.Token,
            ["did"] = account.Did,
            ["userId"] = account.UserId,
            ["REQUEST_IP"] = "127.127.127.127",
            ["roleId"] = roleId,
            ["serverId"] = serverId,
        };
        if (pgr)
        {

            storage["zs_box_master_roleId"] = roleId;
            storage["zs_box_master_serverId"] = serverId;
            storage["zs_userInfo"] = JsonSerializer.Serialize(new Dictionary<string, string?>
            {
                ["roleId"] = roleId,
                ["serverId"] = serverId,
                ["gameId"] = gameId,
                ["userId"] = account.UserId,
            });
        }
        else
        {

            var userInfo = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["appVersion"] = "3.1.2",
                ["os"] = "Android",
                ["headUrl"] = KuroBbsPage.GetDisplayAvatar(account, pgr),
                ["userName"] = account.RoleName,
                ["ua"] = "KuroGameBox",
                ["userId"] = account.UserId,
                ["did"] = account.Did,
                ["channelId"] = "2",
                ["enterSource"] = "12",
                ["token"] = account.Token,
            });
            var roleInfo = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["userId"] = account.UserId,
                ["gameId"] = gameId,
                ["serverId"] = serverId,
                ["serverName"] = "",
                ["roleId"] = roleId,
                ["roleName"] = account.RoleName,
                ["token"] = account.Token,
            });
            storage["initUserInfo"] = userInfo;
            storage["mcResMonReport_APP_USER_INFO"] = userInfo;
            storage["mcResMonReport_ROLE_INFO"] = roleInfo;
            storage["mcResMonReport_GUIDE_ETSRC"] = "12";
            storage["mc_userInfo"] = roleInfo;
            storage["mc_serverId"] = serverId;
            storage["mc_roleId"] = roleId;
            storage["mc_roleName"] = account.RoleName;
        }
        return storage;
    }

    #endregion


    private void ShowPlaceholder(string message)
    {
        TextBlock_Placeholder.Text = message;
        Border_Placeholder.Visibility = Visibility.Visible;
        Web.Visibility = Visibility.Collapsed;
    }


    private void HidePlaceholder()
    {
        Border_Placeholder.Visibility = Visibility.Collapsed;
        Web.Visibility = Visibility.Visible;
    }
}
