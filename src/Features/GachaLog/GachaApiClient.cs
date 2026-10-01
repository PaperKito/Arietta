using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Arietta.Helpers;

namespace Arietta.Features.GachaLog;





public class GachaApiClient
{
    private const string CnRequestUrl = "https://gmserver-api.aki-game2.com/gacha/record/query";
    private const string GlobalRequestUrl = "https://gmserver-api.aki-game2.net/gacha/record/query";

    private static readonly HttpClient Client = SharedHttp.Default;


    
    
    
    
    
    public async Task<GachaFetchResult> FetchAllPoolsAsync(GachaParams parameters)
    {
        string requestUrl = parameters.PlayerId.StartsWith('1') ? CnRequestUrl : GlobalRequestUrl;
        var result = new Dictionary<string, List<GachaRecord>>();
        int errorCode = 0;
        string? errorMessage = null;

        foreach (string poolId in GachaPools.DisplayOrder)
        {
            var response = await QueryPoolAsync(requestUrl, parameters, poolId);
            if (response is null)
            {
                continue;
            }
            if (response.Code == 0)
            {
                if (response.Data is not null)
                {
                    result[poolId] = response.Data;
                }
            }
            else if (errorCode == 0)
            {
                errorCode = response.Code;
                errorMessage = response.Message;
            }
        }
        
        return result.Count > 0
            ? new GachaFetchResult(result, 0, null)
            : new GachaFetchResult(result, errorCode, errorMessage);
    }


    private static async Task<GachaQueryResponse?> QueryPoolAsync(string requestUrl, GachaParams parameters, string poolId)
    {
        var body = new GachaParams
        {
            PlayerId = parameters.PlayerId,
            RecordId = parameters.RecordId,
            CardPoolId = parameters.CardPoolId,
            CardPoolType = poolId,
            ServerId = parameters.ServerId,
            LanguageCode = parameters.LanguageCode,
        };
        try
        {
            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await Client.PostAsync(requestUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<GachaQueryResponse>(stream, JsonOpts.CaseInsensitive);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
