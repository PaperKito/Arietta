using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arietta.Features.GachaLog;
















public static class UgfService
{
    private const string ExportApp = "Arietta";
    
    
    private const string GameId = "wuthering-waves";
    private const string LegacyGameId = "wuwa";

    
    private const string CnOffset = "+08:00";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    
    private static readonly Dictionary<string, string> NameToPoolId = GachaPools.Names
        .ToDictionary(kv => kv.Value, kv => kv.Key);


    
    public static string ResolvePoolId(string nameOrId)
    {
        if (GachaPools.Names.ContainsKey(nameOrId))
        {
            return nameOrId;
        }
        return NameToPoolId.TryGetValue(nameOrId, out var id) ? id : "";
    }


    
    
    
    
    
    public static string Export(string playerId, Dictionary<string, List<GachaRecord>> pool, GachaParams? parameters = null)
    {
        
        bool cn = playerId.StartsWith('1');
        string? lang = string.IsNullOrWhiteSpace(parameters?.LanguageCode)
            ? GachaLanguageSettings.Effective
            : parameters!.LanguageCode;

        var records = new List<UgfItem>();
        int seq = 0;
        foreach (var (poolId, list) in pool)
        {
            foreach (var r in list)
            {
                string itemId = r.ResourceId.ToString(CultureInfo.InvariantCulture);
                string time = r.Time.Replace(' ', 'T');
                records.Add(new UgfItem
                {
                    Pool = poolId,
                    
                    Time = cn ? time + CnOffset : time,
                    ItemId = itemId,
                    Name = r.Name,
                    ItemType = r.ResourceType,
                    Rarity = r.QualityLevel,
                    Count = Math.Max(r.Count, 1),
                    
                    Id = $"{ToUnixSeconds(r.Time, cn)}-{itemId}-{seq++}",
                });
            }
        }

        string? serverId = parameters?.ServerId;
        var model = new UgfFile
        {
            Exporter = new UgfExporter
            {
                Name = ExportApp,
                Version = AppConfig.AppVersion,
                ExportedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
            },
            Accounts =
            [
                new UgfAccount
                {
                    Game = GameId,
                    Uid = playerId,
                    Lang = string.IsNullOrWhiteSpace(lang) ? null : lang,
                    Timezone = cn ? CnOffset : null,
                    Server = string.IsNullOrWhiteSpace(serverId) ? null : serverId,
                    Records = records,
                },
            ],
        };
        return JsonSerializer.Serialize(model, JsonOptions);
    }


    
    
    
    
    private static long ToUnixSeconds(string time, bool cn)
    {
        string iso = cn ? time.Replace(' ', 'T') + CnOffset : time.Replace(' ', 'T');
        var style = cn ? DateTimeStyles.None : DateTimeStyles.AssumeUniversal;
        return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, style, out var dto)
            ? dto.ToUnixTimeSeconds()
            : DateTimeOffset.Now.ToUnixTimeSeconds();
    }


    
    
    
    public static (string PlayerId, Dictionary<string, List<GachaRecord>> Pool) Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        
        if (root.TryGetProperty("accounts", out var accounts) && accounts.ValueKind == JsonValueKind.Array)
        {
            
            
            JsonElement account = default;
            foreach (var candidate in accounts.EnumerateArray())
            {
                if (candidate.TryGetProperty("game", out var g)
                    && GetRaw(g) is { Length: > 0 } game && game != GameId && game != LegacyGameId)
                {
                    continue;
                }
                if (candidate.TryGetProperty("uid", out var u) && !string.IsNullOrWhiteSpace(GetRaw(u)))
                {
                    account = candidate;
                    break;
                }
            }
            string uid = account.ValueKind == JsonValueKind.Object ? GetRaw(account.GetProperty("uid")) : "";
            if (string.IsNullOrEmpty(uid))
            {
                throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrNoUid);
            }
            var pool = new Dictionary<string, List<GachaRecord>>();
            if (account.TryGetProperty("records", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in list.EnumerateArray())
                {
                    var record = RecordFromUgfItem(item);
                    if (!pool.TryGetValue(record.CardPoolType, out var rs))
                    {
                        rs = [];
                        pool[record.CardPoolType] = rs;
                    }
                    rs.Add(record);
                }
            }
            return (uid, pool);
        }

        
        
        foreach (var key in new[] { "wuwa", "x_uigf_compat_wuwa" })
        {
            if (!root.TryGetProperty(key, out var legacy) || legacy.ValueKind != JsonValueKind.Array)
            {
                continue;
            }
            JsonElement account = default;
            foreach (var candidate in legacy.EnumerateArray())
            {
                if (candidate.TryGetProperty("uid", out var u) && !string.IsNullOrWhiteSpace(GetRaw(u)))
                {
                    account = candidate;
                    break;
                }
            }
            string uid = account.ValueKind == JsonValueKind.Object ? GetRaw(account.GetProperty("uid")) : "";
            if (string.IsNullOrEmpty(uid))
            {
                throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrNoUid);
            }
            var pool = new Dictionary<string, List<GachaRecord>>();
            if (account.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in list.EnumerateArray())
                {
                    var record = RecordFromLegacyItem(item);
                    if (!pool.TryGetValue(record.CardPoolType, out var rs))
                    {
                        rs = [];
                        pool[record.CardPoolType] = rs;
                    }
                    rs.Add(record);
                }
            }
            return (uid, pool);
        }

        
        var byName = new Dictionary<string, List<GachaRecord>>();
        bool anyRecord = false;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }
            
            string poolId = NameToPoolId.TryGetValue(prop.Name, out var id) ? id : prop.Name;
            if (!GachaPools.Names.ContainsKey(poolId) && !int.TryParse(poolId, out _))
            {
                continue;
            }
            var records = JsonSerializer.Deserialize<List<GachaRecord>>(prop.Value.GetRawText());
            if (records is { Count: > 0 })
            {
                byName[poolId] = records;
                anyRecord = true;
            }
        }
        if (!anyRecord)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat);
        }
        
        return ("", byName);
    }


    
    private static string GetRaw(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            _ => "",
        };
    }


    private static GachaRecord RecordFromUgfItem(JsonElement item)
    {
        string Get(string name)
        {
            return item.TryGetProperty(name, out var v) ? GetRaw(v) : "";
        }
        
        string poolId = Get("pool");
        if (!GachaPools.Names.ContainsKey(poolId) && NameToPoolId.TryGetValue(poolId, out var mapped))
        {
            poolId = mapped;
        }
        return new GachaRecord
        {
            CardPoolType = poolId,
            ResourceId = int.TryParse(Get("item_id"), out var rid) ? rid : 0,
            Name = Get("name"),
            ResourceType = Get("item_type"),
            QualityLevel = int.TryParse(Get("rarity"), out var rank) ? rank : 0,
            Count = int.TryParse(Get("count"), out var count) ? count : 1,
            Time = NormalizeTime(Get("time")),
        };
    }


    
    private static GachaRecord RecordFromLegacyItem(JsonElement item)
    {
        string Get(string name)
        {
            return item.TryGetProperty(name, out var v) ? GetRaw(v) : "";
        }
        
        string poolId = Get("gacha_type");
        if (poolId.Length == 0)
        {
            poolId = Get("uigf_gacha_type");
        }
        if (!GachaPools.Names.ContainsKey(poolId) && NameToPoolId.TryGetValue(poolId, out var mapped))
        {
            poolId = mapped;
        }
        return new GachaRecord
        {
            CardPoolType = poolId,
            ResourceId = int.TryParse(Get("item_id"), out var rid) ? rid : 0,
            Name = Get("name"),
            ResourceType = Get("item_type"),
            QualityLevel = int.TryParse(Get("rank_type"), out var rank) ? rank : 0,
            Count = int.TryParse(Get("count"), out var count) ? count : 1,
            Time = Get("time"),
        };
    }


    
    
    
    private static string NormalizeTime(string raw)
    {
        raw = raw.Trim();
        if (raw.Length > 10 && (raw[10] is 'T' or 't'))
        {
            raw = raw[..10] + ' ' + raw[11..];
        }
        return raw.Length >= 19 ? raw[..19]
             : raw.Length == 10 ? raw + " 00:00:00"
             : raw;
    }
}


public class UgfFile
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "UGF";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.2";

    [JsonPropertyName("exporter")]
    public UgfExporter Exporter { get; set; } = new();

    [JsonPropertyName("accounts")]
    public List<UgfAccount> Accounts { get; set; } = [];
}


public class UgfExporter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    
    [JsonPropertyName("exported_at")]
    public string ExportedAt { get; set; } = "";
}


public class UgfAccount
{
    [JsonPropertyName("game")]
    public string Game { get; set; } = "wuthering-waves";

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = "";

    
    [JsonPropertyName("server")]
    public string? Server { get; set; }

    
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("records")]
    public List<UgfItem> Records { get; set; } = [];
}


public class UgfItem
{
    [JsonPropertyName("pool")]
    public string Pool { get; set; } = "";

    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("item_id")]
    public string? ItemId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("item_type")]
    public string? ItemType { get; set; }

    
    [JsonPropertyName("rarity")]
    public int? Rarity { get; set; }

    
    [JsonPropertyName("count")]
    public int? Count { get; set; }

    
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
