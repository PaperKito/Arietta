using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Arietta.Helpers;

namespace Arietta.Features.KuroBbs;

public sealed class KuroBbsClient
{
    private static readonly Lazy<KuroBbsClient> _instance = new(() => new KuroBbsClient());
    public static KuroBbsClient Instance => _instance.Value;

    private static readonly HttpClient Http = SharedHttp.Default;
    private static readonly ILogger _logger = AppConfig.GetLogger<KuroBbsClient>();

    public static async Task<byte[]?> DownloadBytesAsync(string url)
    {
        try
        {
            using var response = await Http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return null;
        }
    }

    private const string BaseDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/baseData";
    private const string RoleDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/roleData";
    private const string RoleDetailUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/getRoleDetail";
    private const string RoleAccessTokenUrl = "https://api.kurobbs.com/aki/roleBox/requestToken";
    private const string SmsCodeUrl = "https://api.kurobbs.com/user/getSmsCodeForH5";
    private const string LoginUrl = "https://api.kurobbs.com/user/sdkLogin";
    private const string BriefingListUrl = "https://api.kurobbs.com/aki/resource/period/list";
    private const string BriefingMonthUrl = "https://api.kurobbs.com/aki/resource/month";
    private const string BriefingWeekUrl = "https://api.kurobbs.com/aki/resource/week";
    private const string BriefingVersionUrl = "https://api.kurobbs.com/aki/resource/version";
    private const string SignInUrl = "https://api.kurobbs.com/encourage/signIn/v2";
    private const string SignInInitUrl = "https://api.kurobbs.com/encourage/signIn/initSignInV2";

    private const string CommunitySignInUrl = "https://api.kurobbs.com/user/signIn";

    private const string TowerDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/towerDataDetail";
    private const string NewTowerDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/newTowerDetail";
    private const string SlashDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/slashDetail";

    private const string RefreshBoxDataUrl = "https://api.kurobbs.com/aki/roleBox/akiBox/refreshData";

    private const string CalculatorRefreshUrl = "https://api.kurobbs.com/aki/calculator/refreshData";
    private const string CalculatorListRoleUrl = "https://api.kurobbs.com/aki/calculator/listRole";
    private const string CalculatorListWeaponUrl = "https://api.kurobbs.com/aki/calculator/listWeapon";
    private const string CalculatorBatchRoleCostUrl = "https://api.kurobbs.com/aki/calculator/batchRoleCost";

    private const string WuwaWidgetDataUrl = "https://api.kurobbs.com/gamer/widget/game3/getData";

    public const string ParamServerId = "76402e5b20be2c39f095a152090afddc";
    public const string ParamGameId = "3";

    private const string UserAgent = "Mozilla/5.0 (Linux; Android 9; 23116PN5BC Build/PQ3A.190605.02201427; wv) " +
        "AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/124.0.6367.82 Mobile Safari/537.36 Kuro/2.5.0 KuroGameBox/2.5.0";

    private const int TokenCacheSeconds = 3600;
    private const int TokenRefreshAdvance = 300;

    private readonly object _accessTokenLock = new();

    private readonly Dictionary<string, (string Token, long Timestamp)> _accessTokenCache = new();

    private string? _cachedIp;
    private long _ipTimestamp;
    private string? _devcode;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };


    #region 角色列表 / 详情 / 玩家总览

    public async Task<KuroBbsResponse<List<KuroRole>>> GetRoleListAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        body.Add(new StringContent(ParamGameId), "gameId");

        using var request = await BuildRequestAsync(RoleDataUrl, body, account, ct);
        var response = await ParseDataStringAsync<KuroRoleDataInner>(request, ct);
        var result = new KuroBbsResponse<List<KuroRole>> { Code = response.Code, Msg = response.Msg, Success = response.Success };
        if (response.Data?.RoleList is { } list)
        {
            result.Data = list.OrderByDescending(r => r.Level).ToList();
        }
        return result;
    }

    public async Task<KuroBbsResponse<KuroRoleDetail>> GetRoleDetailAsync(KuroBbsAccount account, int cardRoleId, CancellationToken ct = default)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        body.Add(new StringContent(ParamGameId), "gameId");
        body.Add(new StringContent(cardRoleId.ToString()), "id");
        body.Add(new StringContent("19"), "channelId");
        body.Add(new StringContent("1"), "countryCode");

        using var request = await BuildRequestAsync(RoleDetailUrl, body, account, ct);
        return await ParseDataStringAsync<KuroRoleDetail>(request, ct);
    }

    public async Task<KuroBbsResponse<JsonElement>> GetBaseDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        body.Add(new StringContent(ParamGameId), "gameId");

        using var request = await BuildRequestAsync(BaseDataUrl, body, account, ct);
        return await ParseRawDataAsync(request, ct);
    }

    #endregion


    #region token 换取与角色查找

    public async Task<KuroBbsResponse<List<KuroBbsAccount>>> SeekGameRoleAsync(string token, int gameId = 3, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.kurobbs.com/gamer/role/list?gameId={gameId}");
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            request.Headers.TryAddWithoutValidation("source", "android");
            request.Headers.TryAddWithoutValidation("token", token);
            request.Content = new StringContent("", new MediaTypeHeaderValue("application/x-www-form-urlencoded"));

            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var body = JsonSerializer.Deserialize<KuroBbsResponse<JsonElement>>(json, JsonOptions);
            var result = new KuroBbsResponse<List<KuroBbsAccount>> { Code = body?.Code ?? 0, Msg = body?.Msg, Success = body?.Success ?? false };
            if (body is { IsSuccess: true } successBody && successBody.Data.ValueKind == JsonValueKind.Array)
            {
                var list = new List<KuroBbsAccount>();
                foreach (var item in successBody.Data.EnumerateArray())
                {
                    list.Add(new KuroBbsAccount
                    {
                        Token = token,
                        UserId = GetStringSafe(item, "userId"),
                        RoleId = GetStringSafe(item, "roleId"),
                        RoleName = GetStringSafe(item, "roleName"),
                        AvatarUrl = GetStringSafe(item, "headPhotoUrl"),
                        ServerId = GetStringSafe(item, "serverId"),
                        Level = int.TryParse(GetStringSafe(item, "gameLevel"), out var lv) ? lv : 0,
                    });
                }
                result.Data = list;
                result.Success = true;
            }
            else if (result.Code == 0)
            {
                result.Code = (int)response.StatusCode;
                result.Msg = string.Format(Localization.Lang.Instance.KuroBbs_ErrConnection, (int)response.StatusCode);
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<List<KuroBbsAccount>> { Code = 1, Msg = e.Message };
        }
    }

    private async Task<string> GetAccessTokenAsync(KuroBbsAccount account, CancellationToken ct)
    {
        try
        {
            return await RequestAccessTokenAsync(account, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("10900"))
        {
            var roles = await SeekGameRoleAsync(account.Token, 3, ct);
            if (roles is { IsSuccess: true, Data: { Count: > 0 } })
            {
                var role = roles.Data[0];
                account.UserId = role.UserId;
                account.RoleId = role.RoleId;
                if (string.IsNullOrEmpty(account.RoleName))
                {
                    account.RoleName = role.RoleName;
                }
                if (string.IsNullOrEmpty(account.AvatarUrl))
                {
                    account.AvatarUrl = role.AvatarUrl;
                }
                if (account.Level <= 0)
                {
                    account.Level = role.Level;
                }

                KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
                InvalidateAccessToken();
                return await RequestAccessTokenAsync(account, ct);
            }
            throw;
        }
    }

    private async Task<string> RequestAccessTokenAsync(KuroBbsAccount account, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        lock (_accessTokenLock)
        {
            if (_accessTokenCache.TryGetValue(account.Token, out var cached)
                && now - cached.Timestamp < TokenCacheSeconds - TokenRefreshAdvance)
            {
                return cached.Token;
            }
        }

        string url = $"{RoleAccessTokenUrl}?serverId={ParamServerId}&roleId={account.RoleId}&userId={account.UserId}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("source", "android");
        request.Headers.TryAddWithoutValidation("B-At", "");
        request.Headers.TryAddWithoutValidation("token", account.Token);
        request.Content = new StringContent("", new MediaTypeHeaderValue("application/x-www-form-urlencoded"));

        using var response = await Http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        int code = doc.RootElement.GetProperty("code").GetInt32();
        if (response.StatusCode == System.Net.HttpStatusCode.OK && code is 200 or 10902)
        {

            var dataNode = JsonDocument.Parse(doc.RootElement.GetProperty("data").GetString() ?? "{}").RootElement;
            string accessToken = dataNode.GetProperty("accessToken").GetString() ?? "";
            if (accessToken.Length == 0)
            {
                throw new InvalidOperationException(Localization.Lang.Instance.KuroBbs_ErrAccessTokenEmpty);
            }
            lock (_accessTokenLock)
            {
                _accessTokenCache[account.Token] = (accessToken, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            return accessToken;
        }
        throw new InvalidOperationException(string.Format(Localization.Lang.Instance.KuroBbs_ErrAccessToken, code, doc.RootElement.GetProperty("msg").GetString()));
    }

    public void InvalidateAccessToken()
    {
        lock (_accessTokenLock)
        {
            _accessTokenCache.Clear();
        }
    }

    #endregion


    #region 登录 / 短信验证码（移植自 wwt-kuro-api KujiequAuthApi）

    public async Task<string?> SendSmsCodeAsync(string phone, CancellationToken ct = default)
    {
        try
        {
            string devCode = RandomDevCode();
            using var request = new HttpRequestMessage(HttpMethod.Post, SmsCodeUrl);
            request.Headers.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) KuroGameBox/3.1.3");
            request.Headers.TryAddWithoutValidation("source", "h5");
            request.Headers.TryAddWithoutValidation("devcode", devCode);
            request.Headers.TryAddWithoutValidation("version", "3.1.3");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["mobile"] = phone,
                ["geeTestData"] = "",
            });
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            bool success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True
                || (root.TryGetProperty("code", out var c) && c.GetInt32() is 0 or 200);
            if (success)
            {
                return null;
            }
            return root.TryGetProperty("msg", out var m) ? m.GetString() : Localization.Lang.Instance.KuroBbs_ErrSendCode;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    public async Task<KuroBbsResponse<KuroLoginResult>> LoginWithSmsAsync(string phone, string code, CancellationToken ct = default)
    {
        try
        {
            string did = Guid.NewGuid().ToString("N");
            string url = $"{LoginUrl}?code={Uri.EscapeDataString(code)}&mobile={Uri.EscapeDataString(phone)}&devCode={did}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
            request.Headers.TryAddWithoutValidation("source", "android");
            request.Headers.TryAddWithoutValidation("devcode", await GetDevcodeAsync(ct));
            request.Headers.TryAddWithoutValidation("did", did);
            request.Content = new StringContent("", new MediaTypeHeaderValue("application/x-www-form-urlencoded"));

            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int respCode = root.GetProperty("code").GetInt32();
            if (response.StatusCode == System.Net.HttpStatusCode.OK && respCode == 200)
            {
                var data = root.GetProperty("data");
                string token = data.GetProperty("token").GetString() ?? "";
                return new KuroBbsResponse<KuroLoginResult>
                {
                    Code = 200,
                    Success = true,
                    Data = new KuroLoginResult
                    {
                        Token = token,
                        Did = did,
                        HeadPhotoUrl = data.TryGetProperty("headPhotoUrl", out var photo) && photo.ValueKind == JsonValueKind.String
                            ? photo.GetString() ?? ""
                            : "",
                    },
                };
            }
            return new KuroBbsResponse<KuroLoginResult>
            {
                Code = respCode,
                Msg = root.TryGetProperty("msg", out var m) ? m.GetString() : Localization.Lang.Instance.KuroBbs_ErrLogin,
            };
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroLoginResult> { Code = 1, Msg = e.Message };
        }
    }

    private static string RandomDevCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var sb = new System.Text.StringBuilder(32);
        var random = Random.Shared;
        for (int i = 0; i < 32; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }
        return sb.ToString();
    }

    #endregion


    #region 资源简报（移植自 wwt-kuro-api KujiequSignApi 的 Briefing 任务）

    public async Task<KuroBbsResponse<KuroBriefing>> GetBriefingListAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BriefingListUrl);
            request.Headers.TryAddWithoutValidation("token", token);
            request.Headers.TryAddWithoutValidation("source", "android");
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<KuroBbsResponse<KuroBriefing>>(json, JsonOptions) ?? new();
            if (result.Code == 220)
            {
                InvalidateAccessToken();
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroBriefing> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<KuroBriefingRecord>> GetBriefingDetailAsync(
        KuroBbsAccount account, int period, string type, CancellationToken ct = default)
    {
        string url = type switch
        {
            "week" => BriefingWeekUrl,
            "version" => BriefingVersionUrl,
            _ => BriefingMonthUrl,
        };
        try
        {
            using var request = await BuildFormRequestAsync(url,
                new Dictionary<string, string>
                {
                    ["period"] = period.ToString(),
                    ["roleId"] = account.RoleId,
                    ["serverId"] = ParamServerId,
                }, account, ct);
            return await ParseDirectAsync<KuroBriefingRecord>(request, ct);
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroBriefingRecord> { Code = 1, Msg = e.Message };
        }
    }

    #endregion


    #region 签到 / 深塔 / 终焉矩阵 / 冥歌海墟（移植自 wwt-kuro-api KujiequSignApi、KujiequTowerApi 与 wwt-app SignService）

    private async Task<HttpRequestMessage> BuildSourceRequestAsync(string url, KuroBbsAccount account, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("source", "android");
        request.Headers.TryAddWithoutValidation("devcode", await GetDevcodeAsync(ct));
        request.Headers.TryAddWithoutValidation("token", account.Token);
        request.Content = new StringContent("", new MediaTypeHeaderValue("application/x-www-form-urlencoded"));
        return request;
    }

    public async Task<KuroBbsResponse<KuroSignInitData>> GetSignInInitAsync(KuroBbsAccount account, string gameId = ParamGameId, string? serverId = null, string? roleId = null, CancellationToken ct = default)
    {
        try
        {
            string url = $"{SignInInitUrl}?gameId={gameId}&serverId={serverId ?? ParamServerId}&roleId={roleId ?? account.RoleId}&userId={account.UserId}";
            using var request = await BuildSourceRequestAsync(url, account, ct);
            var result = await ParseDirectAsync<KuroSignInitData>(request, ct);
            if (result is { IsSuccess: true, Data: { } data })
            {

                if (data.SignInGoodsConfigs is { } goods)
                {
                    for (int i = 0; i < goods.Count; i++)
                    {
                        goods[i].IsSign = i < data.SignInNum;
                    }
                }
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroSignInitData> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<bool>> SignInAsync(KuroBbsAccount account, string gameId = ParamGameId, string? serverId = null, string? roleId = null, CancellationToken ct = default)
    {
        try
        {
            int month = DateTime.Now.Month;
            string url = $"{SignInUrl}?gameId={gameId}&serverId={serverId ?? ParamServerId}&roleId={roleId ?? account.RoleId}&userId={account.UserId}&reqMonth={month:00}";
            using var request = await BuildSourceRequestAsync(url, account, ct);
            using var response = await Http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var result = new KuroBbsResponse<bool>();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                if (body.Contains("请求成功"))
                {
                    result.Code = 200; result.Success = true; result.Data = true; result.Msg = Localization.Lang.Instance.KuroBbs_SignOk;
                }
                else if (body.Contains("请勿重复签到"))
                {
                    result.Code = 200; result.Success = true; result.Data = true; result.Msg = Localization.Lang.Instance.KuroBbs_SignAlready;
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        result.Code = doc.RootElement.GetProperty("code").GetInt32();
                        result.Msg = doc.RootElement.GetProperty("msg").GetString();
                    }
                    catch
                    {
                        result.Msg = Localization.Lang.Instance.KuroBbs_SignFail;
                    }
                }
            }
            else
            {
                result.Code = (int)response.StatusCode;
                result.Msg = string.Format(Localization.Lang.Instance.KuroBbs_ErrConnection, (int)response.StatusCode);
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<bool> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<bool>> CommunitySignInAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, CommunitySignInUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("source", "android");
            request.Headers.TryAddWithoutValidation("token", token);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["gameId"] = "2",
                ["geeTestData"] = "",
            });
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            int code = root.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
            string msg = root.TryGetProperty("msg", out var m) ? m.GetString() ?? "" : "";

            bool ok = code == 200 || code == 1511;
            if (msg.Length == 0)
            {
                msg = ok
                    ? Localization.Lang.Instance.KuroBbs_SignAlready
                    : $"code={code}";
            }
            return new KuroBbsResponse<bool>
            {
                Code = code,
                Success = ok,
                Msg = msg,
                Data = ok,
            };
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<bool> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<bool> RefreshBoxDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            using var body = new MultipartFormDataContent();
            body.Add(new StringContent(ParamServerId), "serverId");
            body.Add(new StringContent(account.RoleId), "roleId");
            body.Add(new StringContent(ParamGameId), "gameId");
            using var request = await BuildRequestAsync(RefreshBoxDataUrl, body, account, ct);
            var result = await ParseDirectAsync<bool>(request, ct);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    public async Task<KuroBbsResponse<KuroTowerData>> GetTowerDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        await RefreshBoxDataAsync(account, ct);
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        body.Add(new StringContent(ParamGameId), "gameId");
        using var request = await BuildRequestAsync(TowerDataUrl, body, account, ct, withToken: false);
        return await ParseDataStringAsync<KuroTowerData>(request, ct);
    }

    public async Task<KuroBbsResponse<KuroNewTowerData>> GetNewTowerDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        await RefreshBoxDataAsync(account, ct);
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        using var request = await BuildRequestAsync(NewTowerDataUrl, body, account, ct, withToken: false);
        return await ParseDataStringAsync<KuroNewTowerData>(request, ct);
    }

    public async Task<KuroBbsResponse<KuroSlashData>> GetSlashDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        await RefreshBoxDataAsync(account, ct);
        using var body = new MultipartFormDataContent();
        body.Add(new StringContent(ParamServerId), "serverId");
        body.Add(new StringContent(account.RoleId), "roleId");
        body.Add(new StringContent(ParamGameId), "gameId");
        using var request = await BuildRequestAsync(SlashDataUrl, body, account, ct, withToken: false);
        return await ParseDataStringAsync<KuroSlashData>(request, ct);
    }

    #endregion


    #region 养成计算器（移植自 wwt-kuro-api KujiequCalculatorApi）

    public async Task<KuroBbsResponse<bool>> RefreshCalculatorDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            string url = $"{CalculatorRefreshUrl}?userId={account.UserId}&serverId={ParamServerId}&roleId={account.RoleId}";
            using var request = await BuildFormRequestAsync(url, [], account, ct);
            using var response = await Http.SendAsync(request, ct);
            return new KuroBbsResponse<bool>
            {
                Code = (int)response.StatusCode == 200 ? 200 : 1,
                Success = response.StatusCode == System.Net.HttpStatusCode.OK,
                Data = response.StatusCode == System.Net.HttpStatusCode.OK,
            };
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<bool> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<List<KuroCalculatorRole>>> GetCalculatorRoleListAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            using var request = await BuildFormRequestAsync(CalculatorListRoleUrl, [], account, ct);
            var result = await ParseDirectAsync<List<KuroCalculatorRole>>(request, ct);
            if (result.Data is { Count: > 0 })
            {
                result.Data.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<List<KuroCalculatorRole>> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<List<KuroCalculatorWeapon>>> GetCalculatorWeaponListAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            using var request = await BuildFormRequestAsync(CalculatorListWeaponUrl, [], account, ct);
            var result = await ParseDirectAsync<List<KuroCalculatorWeapon>>(request, ct);
            if (result.Data is { Count: > 0 })
            {
                result.Data.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<List<KuroCalculatorWeapon>> { Code = 1, Msg = e.Message };
        }
    }

    public async Task<KuroBbsResponse<KuroCalculatorResult>> BatchRoleCostAsync(
        KuroBbsAccount account, IEnumerable<KuroCalculatorRoleAim> aims, CancellationToken ct = default)
    {
        try
        {
            string content = Uri.EscapeDataString(JsonSerializer.Serialize(aims));
            var form = new Dictionary<string, string>
            {
                ["userId"] = account.UserId,
                ["serverId"] = ParamServerId,
                ["roleId"] = account.RoleId,
                ["content"] = content,
            };
            using var request = await BuildFormRequestAsync(CalculatorBatchRoleCostUrl, form, account, ct);
            return await ParseDirectAsync<KuroCalculatorResult>(request, ct);
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroCalculatorResult> { Code = 1, Msg = e.Message };
        }
    }

    #endregion


    #region 战双 · 作战数据（web-static.kurobbs.com/pns-box，接口前缀 /haru）

    private const string PgrRefreshUrl = "https://api.kurobbs.com/haru/roleBox/refreshData";
    private const string PgrAccountDataUrl = "https://api.kurobbs.com/haru/roleBox/accountData";
    private const string PgrRoleIndexUrl = "https://api.kurobbs.com/haru/roleBox/roleIndex";
    private const string PgrRoleDetailUrl = "https://api.kurobbs.com/haru/roleBox/roleDetail";
    private const string PgrResourceUrl = "https://api.kurobbs.com/haru/resource/halfOfYear";
    private const string PgrCageUrl = "https://api.kurobbs.com/haru/roleBox/prisonerCage";
    private const string PgrWarAreaUrl = "https://api.kurobbs.com/haru/roleBox/area";
    private const string PgrTransfiniteUrl = "https://api.kurobbs.com/haru/roleBox/transfinite";

    private async Task<HttpRequestMessage> BuildPgrRequestAsync(string url, Dictionary<string, string> form, KuroBbsAccount account, CancellationToken ct)
    {
        var request = await BuildSourceRequestAsync(url, account, ct);
        request.Content = new FormUrlEncodedContent(form);
        return request;
    }

    private static Dictionary<string, string> PgrForm(KuroBbsAccount account, bool withServer = true)
    {
        var form = new Dictionary<string, string> { ["roleId"] = account.PgrRoleId };
        if (withServer)
        {
            form["serverId"] = account.PgrServerId;
        }
        return form;
    }

    public async Task<bool> RefreshPgrDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            using var request = await BuildPgrRequestAsync(PgrRefreshUrl, PgrForm(account), account, ct);
            var result = await ParseDirectAsync<bool>(request, ct);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    private async Task<KuroBbsResponse<T>> PostPgrAsync<T>(string url, Dictionary<string, string> form, KuroBbsAccount account, CancellationToken ct)
    {
        try
        {
            using var request = await BuildPgrRequestAsync(url, form, account, ct);
            return await ParseDirectAsync<T>(request, ct);
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<T> { Code = 1, Msg = e.Message };
        }
    }

    public Task<KuroBbsResponse<KuroPgrAccountData>> GetPgrAccountDataAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrAccountData>(PgrAccountDataUrl, PgrForm(account), account, ct);

    public Task<KuroBbsResponse<KuroPgrRoleIndex>> GetPgrRoleListAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrRoleIndex>(PgrRoleIndexUrl, PgrForm(account), account, ct);

    public async Task<KuroBbsResponse<KuroPgrRoleDetailData>> GetPgrRoleDetailAsync(KuroBbsAccount account, long bodyId, CancellationToken ct = default)
    {
        var form = PgrForm(account);
        form["characterId"] = bodyId.ToString();
        return await PostPgrAsync<KuroPgrRoleDetailData>(PgrRoleDetailUrl, form, account, ct);
    }

    public Task<KuroBbsResponse<KuroPgrResourceReport>> GetPgrResourceReportAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrResourceReport>(PgrResourceUrl, PgrForm(account, withServer: false), account, ct);

    public Task<KuroBbsResponse<KuroPgrCageData>> GetPgrCageDataAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrCageData>(PgrCageUrl, PgrForm(account), account, ct);

    public Task<KuroBbsResponse<KuroPgrWarAreaData>> GetPgrWarAreaDataAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrWarAreaData>(PgrWarAreaUrl, PgrForm(account), account, ct);

    public Task<KuroBbsResponse<KuroPgrTransfiniteData>> GetPgrTransfiniteDataAsync(KuroBbsAccount account, CancellationToken ct = default)
        => PostPgrAsync<KuroPgrTransfiniteData>(PgrTransfiniteUrl, PgrForm(account), account, ct);

    #endregion


    #region 鸣潮（gamer/widget/game3）

    public async Task<KuroBbsResponse<KuroWuwaWidgetData>> GetWuwaWidgetDataAsync(KuroBbsAccount account, CancellationToken ct = default)
    {
        try
        {
            string url = $"{WuwaWidgetDataUrl}?type=2&roleId={account.RoleId}&sizeType=1&gameId={ParamGameId}&serverId={account.ServerId}";
            var body = new StringContent("", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
            using var request = await BuildRequestAsync(url, body, account, ct);
            return await ParseDirectAsync<KuroWuwaWidgetData>(request, ct);
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<KuroWuwaWidgetData> { Code = 1, Msg = e.Message };
        }
    }

    #endregion


    #region 请求构造

    private async Task<HttpRequestMessage> BuildRequestAsync(string url, HttpContent? body, KuroBbsAccount account, CancellationToken ct, bool withToken = true)
    {
        string bAt = await GetAccessTokenAsync(account, ct);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = body };
        AddCommonHeaders(request, account, bAt, await GetDevcodeAsync(ct), withToken);
        return request;
    }

    private async Task<HttpRequestMessage> BuildFormRequestAsync(string url, Dictionary<string, string> form, KuroBbsAccount account, CancellationToken ct)
    {
        string bAt = await GetAccessTokenAsync(account, ct);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        AddCommonHeaders(request, account, bAt, await GetDevcodeAsync(ct));
        return request;
    }


    private void AddCommonHeaders(HttpRequestMessage request, KuroBbsAccount account, string bAt, string devcode, bool withToken = true)
    {
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("Source", "android");
        request.Headers.TryAddWithoutValidation("Devcode", devcode);
        request.Headers.TryAddWithoutValidation("B-At", bAt);
        request.Headers.TryAddWithoutValidation("Did", account.Did);

        if (withToken)
        {
            request.Headers.TryAddWithoutValidation("token", account.Token);
        }
    }

    private async Task<string> GetDevcodeAsync(CancellationToken ct)
    {
        if (_devcode is not null)
        {
            return _devcode;
        }
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (_cachedIp is null || now - _ipTimestamp > 86400)
        {
            string[] services =
            [
                "https://event.kurobbs.com/event/ip",
                "https://api.ipify.org/?format=json",
                "https://httpbin.org/ip",
            ];
            string ip = "127.127.127.127";
            foreach (var service in services)
            {
                try
                {
                    using var resp = await Http.GetAsync(service, ct);
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var text = (await resp.Content.ReadAsStringAsync(ct)).Trim();
                        if (text.Length > 0 && text.Length < 512)
                        {
                            ip = ExtractIp(text) ?? ip;
                            break;
                        }
                    }
                }
                catch { }
            }
            _cachedIp = ip;
            _ipTimestamp = now;
        }
        _devcode = $"{_cachedIp}, {UserAgent}";
        return _devcode;
    }

    private static string? ExtractIp(string text)
    {
        if (Uri.IsHexDigit(text[0]) || char.IsDigit(text[0]))
        {
            return text.Contains(':') ? null : text;
        }
        try
        {
            using var doc = JsonDocument.Parse(text);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("ip")) return prop.Value.GetString();
            }
        }
        catch { }
        return null;
    }

    #endregion


    #region 响应解析

    private async Task<KuroBbsResponse<T>> ParseDirectAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = DeserializeWithFallback<KuroBbsResponse<T>>(json, request.RequestUri?.ToString());
            if (result.Code == 220)
            {
                InvalidateAccessToken();
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<T> { Code = 1, Msg = e.Message, Success = false };
        }
    }

    private static T? DeserializeWithFallback<T>(string json, string? url)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException e)
        {
            _logger.LogWarning("库街区响应严格解析失败，降级宽松解析：{Url} | {Error}", url, e.Message);
            return JsonSerializer.Deserialize<T>(json, LenientJson.Options);
        }
    }

    private async Task<KuroBbsResponse<T>> ParseDataStringAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var result = new KuroBbsResponse<T>
            {
                Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32() : 0,
                Msg = root.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null,
                Success = root.TryGetProperty("success", out var successEl) && successEl.ValueKind == JsonValueKind.True,
            };
            if (result.Code == 220)
            {
                InvalidateAccessToken();
                return result;
            }
            if (result.IsSuccess)
            {
                string dataString = root.GetProperty("data").GetString() ?? "";
                if (dataString.Length > 0 && dataString != "null")
                {
                    result.Data = DeserializeWithFallback<T>(dataString, request.RequestUri?.ToString());
                }
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<T> { Code = 1, Msg = e.Message, Success = false };
        }
    }

    private async Task<KuroBbsResponse<JsonElement>> ParseRawDataAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            using var response = await Http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var result = new KuroBbsResponse<JsonElement>
            {
                Code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetInt32() : 0,
                Msg = root.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null,
                Success = root.TryGetProperty("success", out var successEl) && successEl.ValueKind == JsonValueKind.True,
            };
            if (result.Code == 220)
            {
                InvalidateAccessToken();
                return result;
            }
            if (result.IsSuccess && root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
            {
                result.Data = dataEl.Clone();
            }
            return result;
        }
        catch (Exception e)
        {
            return new KuroBbsResponse<JsonElement> { Code = 1, Msg = e.Message, Success = false };
        }
    }

    #endregion

    private static string GetStringSafe(JsonElement obj, string name)
    {
        return obj.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.String or JsonValueKind.Number
            ? v.ToString()
            : "";
    }

    private sealed class KuroRoleDataInner
    {
        [System.Text.Json.Serialization.JsonPropertyName("roleList")]
        public List<KuroRole>? RoleList { get; set; }
    }

}
