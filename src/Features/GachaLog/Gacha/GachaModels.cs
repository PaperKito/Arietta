using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Arietta.Features.GachaLog;




public class GachaRecord
{
    [JsonPropertyName("cardPoolType")]
    public string CardPoolType { get; set; } = "";

    [JsonPropertyName("resourceId")]
    public int ResourceId { get; set; }

    [JsonPropertyName("qualityLevel")]
    public int QualityLevel { get; set; }

    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; } = 1;

    [JsonPropertyName("time")]
    public string Time { get; set; } = "";
}





public class GachaParams
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = "";

    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = "";

    [JsonPropertyName("cardPoolId")]
    public string CardPoolId { get; set; } = "";

    [JsonPropertyName("cardPoolType")]
    public string CardPoolType { get; set; } = "";

    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = "";

    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = "";
}





public class GachaQueryResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public List<GachaRecord>? Data { get; set; }
}






public sealed record GachaFetchResult(Dictionary<string, List<GachaRecord>> Data, int Code, string? Message);





public static class GachaPools
{
    public const string CharacterEvent = "1";      
    public const string WeaponEvent = "2";         
    public const string CharacterStandard = "3";   
    public const string WeaponStandard = "4";      
    public const string Novice = "5";              
    public const string NoviceChoice = "6";        
    public const string NoviceGratitude = "7";     
    public const string CharacterRookie = "8";     
    public const string WeaponRookie = "9";        
    public const string CharacterCollab = "10";    
    public const string WeaponCollab = "11";       
    public const string CharacterRevisit = "12";   
    public const string WeaponRevisit = "13";      

    
    
    
    
    public static readonly string[] DisplayOrder =
    [
        CharacterEvent, WeaponEvent, CharacterStandard, WeaponStandard,
        Novice, NoviceChoice, NoviceGratitude,
        CharacterCollab, WeaponCollab, CharacterRookie, WeaponRookie,
        CharacterRevisit, WeaponRevisit,
    ];

    public static readonly Dictionary<string, string> Names = new()
    {
        [CharacterEvent] = Localization.Lang.Instance.GachaPool_CharacterEvent,
        [WeaponEvent] = Localization.Lang.Instance.GachaPool_WeaponEvent,
        [CharacterStandard] = Localization.Lang.Instance.GachaPool_CharacterStandard,
        [WeaponStandard] = Localization.Lang.Instance.GachaPool_WeaponStandard,
        [Novice] = Localization.Lang.Instance.GachaPool_Novice,
        [NoviceChoice] = Localization.Lang.Instance.GachaPool_NoviceChoice,
        [NoviceGratitude] = Localization.Lang.Instance.GachaPool_NoviceGratitude,
        [CharacterRookie] = Localization.Lang.Instance.GachaPool_CharacterRookie,
        [WeaponRookie] = Localization.Lang.Instance.GachaPool_WeaponRookie,
        [CharacterCollab] = Localization.Lang.Instance.GachaPool_CharacterCollab,
        [WeaponCollab] = Localization.Lang.Instance.GachaPool_WeaponCollab,
        [CharacterRevisit] = Localization.Lang.Instance.GachaPool_CharacterRevisit,
        [WeaponRevisit] = Localization.Lang.Instance.GachaPool_WeaponRevisit,
    };

    public static string GetName(string poolId)
    {
        return Names.TryGetValue(poolId, out var name) ? name : string.Format(Localization.Lang.Instance.GachaPool_Unknown, poolId);
    }

    
    public static bool IsEventPool(string poolId)
    {
        return poolId is CharacterEvent or WeaponEvent or CharacterCollab or WeaponCollab
            or CharacterRookie or WeaponRookie or CharacterRevisit or WeaponRevisit;
    }


    private const string DisplayPoolsConfigKey = "display_gacha_pools_wuwa";


    
    
    
    
    public static HashSet<string>? GetVisiblePools()
    {
        string? saved = AppConfig.GetConfig(DisplayPoolsConfigKey);
        if (string.IsNullOrWhiteSpace(saved))
        {
            return null;
        }
        var set = saved.Split(',').ToHashSet();
        return set.Count == 0 ? null : set;
    }


    public static void SetVisiblePools(IEnumerable<string> poolIds)
    {
        AppConfig.SetConfig(DisplayPoolsConfigKey, string.Join(',', poolIds));
    }
}
