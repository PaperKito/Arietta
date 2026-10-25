using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arietta.Helpers;

namespace Arietta.Features.GachaLog;

public static class CloudGameClient
{

    public const string CloudHomePage = "https://mc.kurogames.com/cloud/index.html";

    public const string CloudApiBase = "https://cloud-game-sh.aki-game.com/";

    public const string CloudCardPoolId = "5c13a63f85465e9fcc0f24d6efb15083";

    public const string CloudServerId = "76402e5b20be2c39f095a152090afddc";

    public const string CloudLanguageCode = "zh-Hans";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36 Edg/139.0.0.0";

    private static readonly HttpClient Client = SharedHttp.Default;

    public static Task<(GachaParams? Params, string? Error)> TryBuildParamsAsync(IReadOnlyDictionary<string, string> cookies)
    {
        cookies.TryGetValue("token", out string? cloudToken);
        string cookieHeader = string.Join("; ", cookies.Select(kv => $"{kv.Key}={kv.Value}"));
        return TryRequestRecordInfoAsync(cookieHeader, cloudToken, null);
    }

    public static Task<(GachaParams? Params, string? Error)> TryBuildParamsWithCapturedHeadersAsync(
        string? cookieHeader, string? xToken, string? traceId)
    {
        return TryRequestRecordInfoAsync(cookieHeader ?? "", xToken, traceId);
    }

    private static async Task<(GachaParams?, string?)> TryRequestRecordInfoAsync(
        string cookieHeader, string? xToken, string? traceId)
    {
        if (string.IsNullOrWhiteSpace(cookieHeader) && string.IsNullOrWhiteSpace(xToken))
        {
            return (null, Localization.Lang.Instance.CloudGame_ErrNoLogin);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, CloudApiBase + "Message/GameRecordInfo");
        var headers = request.Headers;
        headers.Accept.ParseAdd("application/json");
        headers.Accept.ParseAdd("text/plain");
        headers.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9");
        headers.Referrer = new Uri(CloudHomePage);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }
        headers.TryAddWithoutValidation("Origin", "https://mc.kurogames.com");
        headers.TryAddWithoutValidation("x-os", "web");
        if (!string.IsNullOrEmpty(xToken))
        {
            headers.TryAddWithoutValidation("x-token", xToken);
        }
        headers.TryAddWithoutValidation("x-b3-traceid",
            string.IsNullOrEmpty(traceId) ? Guid.NewGuid().ToString("N") : traceId);

        string body;
        try
        {
            using var response = await Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (null, string.Format(Localization.Lang.Instance.CloudGame_ErrHttp, (int)response.StatusCode));
            }
            body = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            return (null, string.Format(Localization.Lang.Instance.CloudGame_ErrRequest, e.Message));
        }

        CloudRecordResponse? model;
        try
        {
            model = JsonSerializer.Deserialize<CloudRecordResponse>(body, JsonOpts.CaseInsensitive);
        }
        catch (JsonException)
        {
            return (null, string.Format(Localization.Lang.Instance.CloudGame_ErrParse, Truncate(body)));
        }
        if (model is null)
        {
            return (null, string.Format(Localization.Lang.Instance.CloudGame_ErrEmpty, Truncate(body)));
        }
        if (model.Code != 0 || model.Data is null || string.IsNullOrEmpty(model.Data.RecordId))
        {
            return (null, string.Format(Localization.Lang.Instance.CloudGame_ErrCode, model.Code, model.Msg ?? Truncate(body)));
        }

        var parameters = new GachaParams
        {
            PlayerId = model.Data.PlayerId.ToString(),
            RecordId = model.Data.RecordId,
            CardPoolId = CloudCardPoolId,
            CardPoolType = "1",
            ServerId = CloudServerId,
            LanguageCode = CloudLanguageCode,
        };
        return (parameters, null);
    }


    private static string Truncate(string s) => s.Length <= 120 ? s : s[..120] + "…";
}

public class CloudRecordResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public CloudRecordData? Data { get; set; }
}


public class CloudRecordData
{
    [JsonPropertyName("playerId")]
    public long PlayerId { get; set; }

    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = "";
}
