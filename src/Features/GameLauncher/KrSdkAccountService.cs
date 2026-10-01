using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;










public sealed class KrSdkAccount
{
    
    [JsonPropertyName("cuid")]
    public string Cuid { get; set; } = "";

    
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    
    
    
    
    
    [JsonPropertyName("id")]
    public double Id { get; set; }

    
    [JsonIgnore]
    public string? Nickname { get; set; }

    
    [JsonIgnore]
    public string? RoleId { get; set; }

    
    [JsonIgnore]
    public string? RoleName { get; set; }

    
    
    
    
    
    
    
    
    
    
    [JsonIgnore]
    public bool? HasRole { get; set; }

    
    [JsonIgnore]
    public int Level { get; set; }

    
    [JsonIgnore]
    public string? ServerName { get; set; }

    
    
    
    
    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Nickname) ? (RoleName ?? "") : Nickname!;

    
    
    
    
    [JsonIgnore]
    public string DisplayUid => RoleId ?? "";

    
    
    
    
    [JsonIgnore]
    public string UidSeparator => DisplayUid.Length == 0 ? "" : "  ";
}





internal sealed class KrSdkUserCacheData
{
    [JsonPropertyName("account_list")]
    public List<KrSdkAccount>? AccountList { get; set; }

    [JsonPropertyName("last_login_cuid")]
    public string? LastLoginCuid { get; set; }
}






internal sealed class KrSdkLauncherUser
{
    [JsonPropertyName("cuid")]
    public string Cuid { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("oauthCode")]
    public string OauthCode { get; set; } = "";

    
    [JsonIgnore]
    public string DecodedOauthCode => new string(OauthCode.Select(c => (char)(c ^ 5)).ToArray());
}





public sealed record KrSdkRoleInfo(string ServerName, string RoleId, string RoleName, int Level);














public sealed class KuroSdkBriefing
{
    
    public bool IsPgr { get; init; }

    public string RoleName { get; init; } = "";

    public int Level { get; init; }

    
    public int Energy { get; init; }
    public int MaxEnergy { get; init; }
    public int StoreEnergy { get; init; }
    public int MaxStoreEnergy { get; init; }

    
    public long EnergyRecoverMs { get; init; }

    public int Liveness { get; init; }
    public int LivenessMax { get; init; }
    public int WeeklyInstCount { get; init; }
    public int BattlePassLevel { get; init; }
    public int BattlePassWeekExp { get; init; }
    public int BattlePassWeekMaxExp { get; init; }
    public bool BattlePassOpen { get; init; }

    
    public int ActionPoint { get; init; }
    public int ActionPointTotal { get; init; }

    
    public long ActionPointFullSeconds { get; init; }

    public int Activeness { get; init; }
    public int ActivenessTotal { get; init; }
    public int BossReward { get; init; }
    public int BossRewardTotal { get; init; }
    public bool BossUnlocked { get; init; }
    public int ArenaReward { get; init; }
    public int ArenaRewardTotal { get; init; }
    public bool ArenaUnlocked { get; init; }


    
    public static KuroSdkBriefing? Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        
        if (root.TryGetProperty("Base", out var b) && b.ValueKind == JsonValueKind.Object)
        {
            root.TryGetProperty("BattlePass", out var bp);
            return new KuroSdkBriefing
            {
                IsPgr = false,
                RoleName = Str(b, "Name"),
                Level = Int(b, "Level"),
                Energy = Int(b, "Energy"),
                MaxEnergy = Int(b, "MaxEnergy"),
                StoreEnergy = Int(b, "StoreEnergy"),
                MaxStoreEnergy = Int(b, "MaxStoreEnergy"),
                EnergyRecoverMs = Long(b, "EnergyRecoverTime"),
                Liveness = Int(b, "Liveness"),
                LivenessMax = Int(b, "LivenessMaxCount"),
                WeeklyInstCount = Int(b, "WeeklyInstCount"),
                BattlePassLevel = Int(bp, "Level"),
                BattlePassWeekExp = Int(bp, "WeekExp"),
                BattlePassWeekMaxExp = Int(bp, "WeekMaxExp"),
                BattlePassOpen = Bool(bp, "IsOpen"),
            };
        }
        if (root.TryGetProperty("actionPoint", out _) || root.TryGetProperty("playerName", out _))
        {
            return new KuroSdkBriefing
            {
                IsPgr = true,
                RoleName = Str(root, "playerName"),
                Level = Int(root, "playerLevel"),
                ActionPoint = Int(root, "actionPoint"),
                ActionPointTotal = Int(root, "actionPointTotal"),
                ActionPointFullSeconds = Long(root, "actionPointFullTime"),
                Activeness = Int(root, "activeness"),
                ActivenessTotal = Int(root, "activenessTotal"),
                BossReward = Int(root, "bossSingleRewardCount"),
                BossRewardTotal = Int(root, "bossSingleRewardCountTotal"),
                BossUnlocked = Bool(root, "bossSingleUnlocked"),
                ArenaReward = Int(root, "arenaRewardCount"),
                ArenaRewardTotal = Int(root, "arenaRewardCountTotal"),
                ArenaUnlocked = Bool(root, "arenaUnlocked"),
            };
        }
        return null;
    }


    private static string Str(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? ""
            : "";

    private static int Int(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.TryGetInt32(out int i) ? i : 0;

    private static long Long(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) && v.TryGetInt64(out long l) ? l : 0;

    private static bool Bool(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v)
           && v.ValueKind == JsonValueKind.True;
}
















public static class KrSdkAccountService
{
    private const byte XorKey = 9;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    
    private static readonly Dictionary<GameBiz, string?> _cachePaths = [];

    
    
    
    
    private static readonly GameBiz[] LauncherBizOrder =
    {
        GameBiz.PunishingGrayRaven,
        GameBiz.PunishingGrayRavenGlobal,
        GameBiz.PunishingGrayRavenKorea,
        GameBiz.PunishingGrayRavenJapan,
        GameBiz.PunishingGrayRavenTaiwan,
        GameBiz.WutheringWaves,
        GameBiz.WutheringWavesGlobal,
        GameBiz.WutheringWavesBilibili,
    };

    
    
    
    
    
    
    
    
    
    
    
    private static (string Folder, string PkgId, string ApiBase) GetSdkProfile(GameBiz biz) => biz switch
    {
        GameBiz.WutheringWaves => ("KR_G152", "A1381", "https://pc-launcher-sdk-api.kurogame.com"),
        GameBiz.WutheringWavesBilibili => ("KR_G152", "A1421", "https://pc-launcher-sdk-api.kurogame.com"),
        GameBiz.WutheringWavesGlobal => ("KR_G153", "A1730", "https://pc-launcher-sdk-api.kurogame.net"),
        GameBiz.PunishingGrayRaven or GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
            or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => ("KR_G148", "A1393", "https://pc-launcher-sdk-haru-api.kurogames.com"),
        
        _ => throw new ArgumentOutOfRangeException(nameof(biz), biz, "未知的游戏服区，没有对应的 SDK 缓存配置"),
    };

    
    
    
    
    
    private static bool HasSdkCache(GameBiz biz) => !biz.IsPunishingGrayRaven()
        || biz == GameBiz.PunishingGrayRaven;

    
    
    
    public static string GetApiBase(GameBiz biz) => GetSdkProfile(biz).ApiBase;


    
    
    
    
    
    
    
    
    public static string? GetOauthCode(GameBiz biz, KrSdkAccount? account)
        => GetOauthCode(biz, account?.Cuid);


    
    
    
    
    
    
    
    
    
    
    
    
    public static string? GetOauthCode(GameBiz biz, string? cuid)
    {
        if (string.IsNullOrEmpty(cuid))
        {
            return null;
        }
        string? code = FindOauthCode(biz, cuid);
        if (!string.IsNullOrEmpty(code))
        {
            return code;
        }
        foreach (var other in LauncherBizOrder)
        {
            if (other == biz)
            {
                continue;
            }
            code = FindOauthCode(other, cuid);
            if (!string.IsNullOrEmpty(code))
            {
                return code;
            }
        }
        return null;
    }


    private static string? FindOauthCode(GameBiz biz, string cuid)
    {
        string? code = GetLauncherUsers(biz).FirstOrDefault(u => u.Cuid == cuid)?.DecodedOauthCode;
        return string.IsNullOrEmpty(code) ? null : code;
    }


    
    public static KrSdkAccount? GetCurrentAccount(GameBiz biz)
    {
        var data = Load(biz);
        return data?.AccountList?.FirstOrDefault(a => a.Cuid == data.LastLoginCuid)
               ?? data?.AccountList?.FirstOrDefault();
    }


    
    public static string? GetCurrentCuid(GameBiz biz) => Load(biz)?.LastLoginCuid;


    
    public static List<KrSdkAccount> GetAccounts(GameBiz biz) => Load(biz)?.AccountList ?? [];


    
    
    
    
    
    
    
    
    
    
    
    
    
    public static void SwitchTo(GameBiz biz, string cuid)
    {
        string path = GetCacheFilePath(biz)
            ?? throw new InvalidOperationException(Localization.Lang.Instance.KrSdk_ErrNoUserCache);

        byte[] encrypted = File.ReadAllBytes(path);
        string original = Encoding.UTF8.GetString(Xor(encrypted, XorKey));
        JsonNode root;
        try
        {
            root = JsonNode.Parse(original) ?? throw new JsonException(Localization.Lang.Instance.KrSdk_ErrEmptyContent);
        }
        catch (Exception ex)
        {
            
            throw new InvalidOperationException(Localization.Lang.Instance.KrSdk_ErrParseFailed, ex);
        }

        if (root["account_list"] is not JsonArray list)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.KrSdk_ErrNoAccountList);
        }
        if (!list.Any(node => node?["cuid"]?.GetValue<string>() == cuid))
        {
            throw new InvalidOperationException(Localization.Lang.Instance.KrSdk_ErrAccountNotInCache);
        }
        if (root["last_login_cuid"]?.GetValue<string>() == cuid)
        {
            return;
        }

        
        BackupOnce(path, encrypted);

        
        string? patched = TryPatchLastLoginCuid(original, cuid);
        if (patched is not null)
        {
            File.WriteAllBytes(path, Xor(Encoding.UTF8.GetBytes(patched), XorKey));
        }
        else
        {
            
            root["last_login_cuid"] = cuid;
            File.WriteAllBytes(path, Xor(SerializeCache(root), XorKey));
        }

        
        if (!Verify(path, cuid, list.Count))
        {
            Restore(path);
            throw new InvalidOperationException(Localization.Lang.Instance.KrSdk_ErrVerifyFailed);
        }
    }


    
    
    
    
    
    private static string? TryPatchLastLoginCuid(string json, string cuid)
    {
        if (cuid.Length == 0 || cuid.IndexOfAny(['"', '\\']) >= 0)
        {
            return null;
        }
        const string name = "\"last_login_cuid\"";
        int at = json.IndexOf(name, StringComparison.Ordinal);
        if (at < 0 || json.IndexOf(name, at + name.Length, StringComparison.Ordinal) >= 0)
        {
            return null;
        }

        int i = at + name.Length;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != ':') return null;
        i++;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != '"') return null;

        int start = i;
        i++;
        while (i < json.Length && json[i] != '"')
        {
            i += json[i] == '\\' ? 2 : 1;
        }
        if (i >= json.Length) return null;

        return $"{json[..start]}\"{cuid}\"{json[(i + 1)..]}";
    }


    
    private static JsonNode? ParseCache(byte[] encrypted)
        => JsonNode.Parse(Encoding.UTF8.GetString(Xor(encrypted, XorKey)));


    
    
    
    
    private static byte[] SerializeCache(JsonNode root)
        => JsonSerializer.SerializeToUtf8Bytes(root, CacheWriteOptions);


    private static readonly JsonSerializerOptions CacheWriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };


    
    
    
    
    
    
    
    
    
    
    
    private static void BackupOnce(string path, byte[] original)
    {
        try
        {
            string backup = path + ".mwl-bak";
            if (!File.Exists(backup))
            {
                File.WriteAllBytes(backup, original);
                return;
            }
            byte[] existing = File.ReadAllBytes(backup);
            if (existing.SequenceEqual(original))
            {
                return;
            }
            string first = backup + ".first";
            if (!File.Exists(first))
            {
                File.Copy(backup, first, true);
            }
            File.WriteAllBytes(backup, original);
        }
        catch
        {
            
        }
    }


    
    private static void Restore(string path)
    {
        try
        {
            string backup = path + ".mwl-bak";
            if (File.Exists(backup))
            {
                File.Copy(backup, path, true);
            }
        }
        catch
        {
            
        }
    }


    
    private static bool Verify(string path, string cuid, int accountCount)
    {
        try
        {
            var root = ParseCache(File.ReadAllBytes(path));
            return root?["last_login_cuid"]?.GetValue<string>() == cuid
                && (root["account_list"] as JsonArray)?.Count == accountCount;
        }
        catch
        {
            return false;
        }
    }


    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public static async Task<bool> RefreshAccountInfosAsync(GameBiz biz, IReadOnlyList<KrSdkAccount> accounts, string? recentUid)
    {
        if (!HasSdkCache(biz))
        {
            return false;
        }
        var (_, _, apiBase) = GetSdkProfile(biz);
        string? currentCuid = GetCurrentCuid(biz);
        bool changed = false;
        int noCode = 0;
        foreach (var acc in accounts)
        {
            var stored = KrSdkAccountStore.Get(biz, acc.Cuid);
            
            bool stale = stored is null || stored.HasRole is null
                || DateTimeOffset.UtcNow - stored.QueryTime > TimeSpan.FromHours(24);
            bool forced = acc.Cuid == currentCuid
                && !string.IsNullOrEmpty(recentUid)
                && stored?.RoleId != recentUid;
            if (!stale && !forced)
            {
                continue;
            }
            string? code = GetOauthCode(biz, acc.Cuid);
            if (string.IsNullOrEmpty(code))
            {
                
                noCode++;
                continue;
            }
            var probe = await KuroSdkPlayerClient.ProbeRoleAsync(apiBase, code);
            switch (probe.State)
            {
                case KuroSdkRoleState.HasRole when probe.Role is not null:
                    KrSdkAccountStore.SetRole(biz, acc.Cuid, probe.Role);
                    changed = true;
                    break;
                case KuroSdkRoleState.NoRole:
                    KrSdkAccountStore.SetNoRole(biz, acc.Cuid);
                    changed = true;
                    break;
                default:
                    
                    break;
            }
        }
        if (accounts.Count > 0 && noCode == accounts.Count)
        {
            
            AppConfig.GetLogger<RefreshTag>().LogInformation(
                "判定角色信息跳过：{Biz} 下没有任何账号能取到 oauthCode（官方启动器缓存缺失）", biz);
        }
        return changed;
    }


    
    private static List<KrSdkLauncherUser> GetLauncherUsers(GameBiz biz)
    {
        string? folder = FindCacheFolder(biz);
        if (folder is null)
        {
            return [];
        }
        string path = Path.Combine(folder, "KRSDKUserLauncherCache.json");
        if (!File.Exists(path))
        {
            return [];
        }
        try
        {
            return JsonSerializer.Deserialize<List<KrSdkLauncherUser>>(File.ReadAllText(path), JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }


    
    
    
    
    public static string? GetRecentlyLoginUid(GameBiz biz)
    {
        string? db = FindLocalStorageDb(biz);
        if (db is null)
        {
            return null;
        }
        try
        {
            return ReadLocalStorageValue(db, "RecentlyLoginUID");
        }
        catch
        {
            return null;
        }
    }


    
    private static string? FindLocalStorageDb(GameBiz biz)
    {
        string? exe = GameRegistry.GetConfiguredExePath(biz);
        if (string.IsNullOrWhiteSpace(exe))
        {
            return null;
        }
        string? dir = Path.GetDirectoryName(exe);
        if (dir is null)
        {
            return null;
        }
        string db = Path.Combine(dir, "Client", "Saved", "LocalStorage", "LocalStorage.db");
        return File.Exists(db) ? db : null;
    }


    
    
    
    
    private static string? ReadLocalStorageValue(string dbPath, string key)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder { DataSource = dbPath, Mode = SqliteOpenMode.ReadOnly };
            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM LocalStorage WHERE key = $key";
            command.Parameters.AddWithValue("$key", key);
            return command.ExecuteScalar() as string;
        }
        catch (SqliteException)
        {
            string temp = Path.Combine(Path.GetTempPath(), $"MWL_LocalStorage_{Path.GetFileNameWithoutExtension(dbPath)}.db");
            File.Copy(dbPath, temp, true);
            try
            {
                var builder = new SqliteConnectionStringBuilder { DataSource = temp, Mode = SqliteOpenMode.ReadWrite };
                using var connection = new SqliteConnection(builder.ToString());
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT value FROM LocalStorage WHERE key = $key";
                command.Parameters.AddWithValue("$key", key);
                return command.ExecuteScalar() as string;
            }
            finally
            {
                try { File.Delete(temp); } catch { }
            }
        }
    }


    
    private static KrSdkUserCacheData? Load(GameBiz biz)
    {
        string? path = GetCacheFilePath(biz);
        if (path is null)
        {
            return null;
        }
        try
        {
            byte[] encrypted = File.ReadAllBytes(path);
            byte[] decrypted = Xor(encrypted, XorKey);
            return JsonSerializer.Deserialize<KrSdkUserCacheData>(decrypted, JsonOptions);
        }
        catch
        {
            return null;
        }
    }


    
    private static string? GetCacheFilePath(GameBiz biz)
    {
        lock (_cachePaths)
        {
            if (_cachePaths.TryGetValue(biz, out string? cached) && cached is not null)
            {
                return File.Exists(cached) ? cached : null;
            }
            
            string? found = FindCacheFile(biz);
            if (found is not null)
            {
                _cachePaths[biz] = found;
            }
            return found;
        }
    }


    
    
    
    
    
    
    
    private static string? FindCacheFile(GameBiz biz)
    {
        if (!HasSdkCache(biz))
        {
            return null;
        }
        var (folder, pkgId, _) = GetSdkProfile(biz);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string exact = Path.Combine(appData, folder, pkgId, "KRSDKUserCache.json");
        return File.Exists(exact) ? exact : null;
    }


    
    private static string? FindCacheFolder(GameBiz biz)
    {
        string? path = GetCacheFilePath(biz);
        return path is null ? null : Path.GetDirectoryName(path);
    }


    private static byte[] Xor(byte[] data, byte key)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key);
        }
        return result;
    }

    private sealed class RefreshTag;
}










public enum KuroSdkRoleState
{
    
    Unknown,

    
    HasRole,

    
    NoRole,
}



public sealed record KuroSdkRoleProbe(KuroSdkRoleState State, KrSdkRoleInfo? Role);










public static class KuroSdkPlayerClient
{
    private static readonly HttpClient _httpClient = SharedHttp.Sdk;

    private static readonly Microsoft.Extensions.Logging.ILogger _logger = AppConfig.GetLogger<LogTag>();

    private sealed class LogTag;

    
    private const int MaxAttempts = 6;

    
    
    
    
    private static async Task<T?> RetryAsync<T>(Func<Task<T?>> attempt)
    {
        for (int i = 0; i < MaxAttempts; i++)
        {
            if (i > 0)
            {
                await Task.Delay(500 * i);
            }
            try
            {
                if (await attempt() is { } result)
                {
                    return result;
                }
            }
            catch
            {
                
            }
        }
        return default;
    }

    
    
    
    
    
    
    
    
    
    
    
    
    
    public static async Task<KuroSdkRoleProbe> ProbeRoleAsync(string apiBase, string oauthCode)
    {
        var probe = await RetryAsync(async () =>
        {
            var p = await ProbeOnceAsync(apiBase, oauthCode);
            return p.State == KuroSdkRoleState.Unknown ? null : p;
        });
        if (probe is null)
        {
            _logger.LogWarning("queryPlayerInfo 重试 {Attempts} 次仍无结论，本轮按「未知」处理（{ApiBase}）", MaxAttempts, apiBase);
            return new KuroSdkRoleProbe(KuroSdkRoleState.Unknown, null);
        }
        return probe;
    }


    
    private static async Task<KuroSdkRoleProbe> ProbeOnceAsync(string apiBase, string oauthCode)
    {
        string url = $"{apiBase}/game/queryPlayerInfo?_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string body = JsonSerializer.Serialize(new Dictionary<string, string> { ["oauthCode"] = oauthCode });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content);
        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int code = root.TryGetProperty("code", out var codeEl) && codeEl.TryGetInt32(out int c) ? c : -1;

        
        if (code == 1005)
        {
            return new KuroSdkRoleProbe(KuroSdkRoleState.Unknown, null);
        }
        
        
        
        if (code == 1003)
        {
            return new KuroSdkRoleProbe(KuroSdkRoleState.NoRole, null);
        }
        
        
        if (code != 0 || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return new KuroSdkRoleProbe(KuroSdkRoleState.Unknown, null);
        }

        bool anyServer = false;
        foreach (var server in data.EnumerateObject())
        {
            anyServer = true;
            
            if (server.Value.ValueKind != JsonValueKind.String || server.Value.GetString() is not { Length: > 0 } inner)
            {
                continue;
            }
            try
            {
                using var item = JsonDocument.Parse(inner);
                
                string? roleId = TryGetString(item.RootElement, "roleId") ?? TryGetString(item.RootElement, "playerId");
                string? roleName = TryGetString(item.RootElement, "roleName") ?? TryGetString(item.RootElement, "playerName") ?? "";
                int level = TryGetInt(item.RootElement, "level") ?? TryGetInt(item.RootElement, "playerLevel") ?? 0;
                if (!string.IsNullOrEmpty(roleId))
                {
                    return new KuroSdkRoleProbe(KuroSdkRoleState.HasRole,
                        new KrSdkRoleInfo(server.Name, roleId, roleName ?? "", level));
                }
            }
            catch
            {
                
            }
        }
        
        return new KuroSdkRoleProbe(anyServer ? KuroSdkRoleState.Unknown : KuroSdkRoleState.NoRole, null);
    }


    
    
    
    
    
    
    
    
    
    
    
    
    
    public static async Task<KuroSdkBriefing?> QueryBriefingAsync(string apiBase, string oauthCode, string playerId, string region)
    {
        if (string.IsNullOrEmpty(oauthCode) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(region))
        {
            return null;
        }
        var briefing = await RetryAsync(() => QueryBriefingOnceAsync(apiBase, oauthCode, playerId, region));
        if (briefing is null)
        {
            _logger.LogWarning("queryRole 重试 {Attempts} 次仍失败，本轮放弃（{ApiBase} region={Region}）", MaxAttempts, apiBase, region);
        }
        return briefing;
    }


    private static async Task<KuroSdkBriefing?> QueryBriefingOnceAsync(string apiBase, string oauthCode, string playerId, string region)
    {
        string url = $"{apiBase}/game/queryRole?_t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string body = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["oauthCode"] = oauthCode,
            ["playerId"] = playerId,
            ["region"] = region,
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content);
        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("code", out var codeEl) || !codeEl.TryGetInt32(out int code) || code != 0
            || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        string? inner = null;
        if (data.TryGetProperty(region, out var raw) && raw.ValueKind == JsonValueKind.String)
        {
            inner = raw.GetString();
        }
        if (string.IsNullOrEmpty(inner))
        {
            
            foreach (var fallback in data.EnumerateObject())
            {
                if (fallback.Value.ValueKind == JsonValueKind.String && fallback.Value.GetString() is { Length: > 0 } alt)
                {
                    inner = alt;
                    break;
                }
            }
        }
        if (string.IsNullOrEmpty(inner))
        {
            return null;
        }
        using var item = JsonDocument.Parse(inner);
        return KuroSdkBriefing.Parse(item.RootElement);
    }


    private static string? TryGetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                
                
                JsonValueKind.Number => v.GetRawText(),
                _ => null,
            }
            : null;


    private static int? TryGetInt(JsonElement element, string name)
        => element.TryGetProperty(name, out var v) && v.TryGetInt32(out int i) ? i : null;
}











public static class KrSdkAccountStore
{
    private const string Key = "krsdk_accounts";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static Dictionary<string, KrSdkAccountInfo> _infos = [];
    private static bool _loaded;

    public sealed class KrSdkAccountInfo
    {
        
        
        
        
        public bool? HasRole { get; set; }

        public string? Nickname { get; set; }
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public int Level { get; set; }
        public string? ServerName { get; set; }

        
        public DateTimeOffset QueryTime { get; set; }
    }


    
    private static string MakeKey(GameBiz biz, string cuid) => $"{(int)biz}|{cuid}";


    
    public static KrSdkAccountInfo? Get(GameBiz biz, string cuid)
    {
        EnsureLoaded();
        return _infos.GetValueOrDefault(MakeKey(biz, cuid));
    }


    
    public static void ApplyTo(GameBiz biz, KrSdkAccount account)
    {
        var info = Get(biz, account.Cuid);
        if (info is null)
        {
            return;
        }
        account.Nickname = info.Nickname;
        account.RoleId = info.RoleId;
        account.RoleName = info.RoleName;
        account.Level = info.Level;
        account.ServerName = info.ServerName;
        account.HasRole = info.HasRole;
    }


    public static void SetNickname(GameBiz biz, string cuid, string? nickname)
    {
        EnsureLoaded();
        var info = GetOrCreate(biz, cuid);
        info.Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname;
        Save();
    }


    public static void SetRole(GameBiz biz, string cuid, KrSdkRoleInfo role)
    {
        EnsureLoaded();
        var info = GetOrCreate(biz, cuid);
        info.HasRole = true;
        info.RoleId = role.RoleId;
        info.RoleName = role.RoleName;
        info.Level = role.Level;
        info.ServerName = role.ServerName;
        info.QueryTime = DateTimeOffset.UtcNow;
        Save();
    }


    
    
    
    
    public static void SetNoRole(GameBiz biz, string cuid)
    {
        EnsureLoaded();
        var info = GetOrCreate(biz, cuid);
        info.HasRole = false;
        info.RoleId = null;
        info.RoleName = null;
        info.Level = 0;
        info.ServerName = null;
        info.QueryTime = DateTimeOffset.UtcNow;
        Save();
    }


    
    public static void Remove(GameBiz biz, string cuid)
    {
        EnsureLoaded();
        if (_infos.Remove(MakeKey(biz, cuid)))
        {
            Save();
        }
    }


    private static KrSdkAccountInfo GetOrCreate(GameBiz biz, string cuid)
    {
        string key = MakeKey(biz, cuid);
        if (_infos.TryGetValue(key, out var existing))
        {
            return existing;
        }
        return _infos[key] = new KrSdkAccountInfo();
    }


    private static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;
        try
        {
            string? json = Database.DatabaseService.GetValue<string>(Key);
            if (!string.IsNullOrEmpty(json))
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, KrSdkAccountInfo>>(json, JsonOptions) ?? [];
                var migrated = new Dictionary<string, KrSdkAccountInfo>();
                foreach (var (key, value) in raw)
                {
                    
                    string newKey = key.Contains('|') ? key : MakeKey(GameBiz.WutheringWaves, key);
                    migrated.TryAdd(newKey, value);
                }
                _infos = migrated;
                if (migrated.Count != raw.Count || raw.Keys.Any(k => !k.Contains('|')))
                {
                    Save();
                }
            }
        }
        catch
        {
            _infos = [];
        }
    }


    private static void Save()
    {
        try
        {
            Database.DatabaseService.SetValue(Key, JsonSerializer.Serialize(_infos, JsonOptions));
        }
        catch { }
    }
}
