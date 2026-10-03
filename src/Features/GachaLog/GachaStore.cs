using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dapper;
using Arietta.Core;
using Arietta.Features.Database;

namespace Arietta.Features.GachaLog;















public static class GachaStore
{
    
    private const string GameExePathConfigKey = "GameExePath";

    
    private const string ParamsKeyPrefix = "gacha_params_";

    
    private static string LegacyFolder => Path.Combine(AppConfig.LocalFolder, "gacha");

    
    private const string LegacyImportedKeyPrefix = "gacha_legacy_imported_";

    
    public static string DataFolder => DatabaseService.DatabaseFolder;


    
    public static void ImportAllLegacy()
    {
        try
        {
            if (!Directory.Exists(LegacyFolder))
            {
                return;
            }
            foreach (string dir in Directory.GetDirectories(LegacyFolder))
            {
                ImportLegacyIfNeeded(Path.GetFileName(dir) ?? "");
            }
        }
        catch { }
    }


    public static string[] LoadPlayers()
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            return connection
                .Query<string>($"SELECT DISTINCT PlayerId FROM {DatabaseSchema.Tables.GachaLogItem} ORDER BY PlayerId;")
                .ToArray();
        }
        catch
        {
            return [];
        }
    }


    
    
    
    
    public static List<(int ResourceId, string Name, int QualityLevel)> LoadDistinctItems()
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            var rows = connection.Query<GachaRow>(
                $"SELECT ResourceId, Name, QualityLevel FROM {DatabaseSchema.Tables.GachaLogItem} " +
                $"WHERE Name IS NOT NULL AND Name <> '' GROUP BY ResourceId, Name, QualityLevel;");
            return rows
                .Where(r => r.ResourceId > 0 && !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => (r.ResourceId, r.Name!, r.QualityLevel))
                .ToList();
        }
        catch
        {
            return [];
        }
    }


    public static Dictionary<string, List<GachaRecord>>? LoadPool(string playerId)
    {
        ImportLegacyIfNeeded(playerId);
        try
        {
            using var connection = DatabaseService.CreateConnection();
            
            
            
            var rows = connection.Query<GachaRow>(
                $"SELECT CardPoolType, ResourceId, QualityLevel, ResourceType, Name, Count, Time " +
                $"FROM {DatabaseSchema.Tables.GachaLogItem} WHERE PlayerId = @p ORDER BY Time DESC, rowid ASC;",
                new { p = playerId });
            if (!rows.Any())
            {
                return null;
            }
            var result = new Dictionary<string, List<GachaRecord>>();
            foreach (var r in rows)
            {
                if (!result.TryGetValue(r.CardPoolType, out var list))
                {
                    list = [];
                    result[r.CardPoolType] = list;
                }
                list.Add(new GachaRecord
                {
                    CardPoolType = r.CardPoolType,
                    ResourceId = r.ResourceId,
                    QualityLevel = r.QualityLevel,
                    ResourceType = r.ResourceType ?? "",
                    Name = r.Name ?? "",
                    Count = r.Count,
                    Time = r.Time,
                });
            }
            return result;
        }
        catch
        {
            return null;
        }
    }


    public static GachaParams? LoadParams(string playerId)
    {
        ImportLegacyIfNeeded(playerId);
        string? json = DatabaseService.GetValue<string>(ParamsKeyPrefix + playerId);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<GachaParams>(json);
        }
        catch
        {
            return null;
        }
    }


    
    
    
    
    
    
    
    public static int Save(string playerId, Dictionary<string, List<GachaRecord>> pool, GachaParams parameters)
    {
        SaveParams(playerId, parameters);

        
        
        
        const string sql =
            $"INSERT OR IGNORE INTO {DatabaseSchema.Tables.GachaLogItem} " +
            "(PlayerId, CardPoolType, ResourceId, QualityLevel, ResourceType, Name, Count, Time, SubSeq) " +
            "VALUES (@PlayerId, @CardPoolType, @ResourceId, @QualityLevel, @ResourceType, @Name, @Count, @Time, @SubSeq);";
        const string renameSql =
            $"UPDATE {DatabaseSchema.Tables.GachaLogItem} SET Name = @Name " +
            "WHERE PlayerId = @PlayerId AND CardPoolType = @CardPoolType AND ResourceId = @ResourceId " +
            "AND Time = @Time AND SubSeq = @SubSeq AND Name <> @Name;";

        var allParams = new List<object>();
        foreach (var (poolId, records) in pool)
        {
            if (records is not { Count: > 0 })
            {
                continue;
            }
            
            var subSeq = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                string key = $"{poolId}|{record.ResourceId}|{record.Time}";
                int seq = subSeq.TryGetValue(key, out int n) ? n : 0;
                subSeq[key] = seq + 1;
                allParams.Add(new
                {
                    PlayerId = playerId,
                    CardPoolType = poolId,
                    record.ResourceId,
                    record.QualityLevel,
                    record.ResourceType,
                    record.Name,
                    record.Count,
                    record.Time,
                    SubSeq = seq,
                });
            }
        }

        if (allParams.Count == 0)
        {
            return 0;
        }
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();
        connection.Execute(sql, allParams, transaction);
        int renamed = connection.Execute(renameSql, allParams, transaction);
        transaction.Commit();
        return renamed;
    }


    public static void SaveParams(string playerId, GachaParams parameters)
    {
        DatabaseService.SetValue(ParamsKeyPrefix + playerId, JsonSerializer.Serialize(parameters));
    }


    public static bool ClearParams(string playerId)
    {
        try
        {
            DatabaseService.RemoveValue(ParamsKeyPrefix + playerId);
            return true;
        }
        catch
        {
            return false;
        }
    }


    public static DateTime? GetParamsTime(string playerId)
    {
        return DatabaseService.GetUpdateTime(ParamsKeyPrefix + playerId)?.LocalDateTime;
    }


    public static void DeletePlayer(string playerId)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            connection.Execute($"DELETE FROM {DatabaseSchema.Tables.GachaLogItem} WHERE PlayerId = @p;", new { p = playerId });
        }
        catch { }
        DatabaseService.RemoveValue(ParamsKeyPrefix + playerId);
    }


    
    
    
    
    
    
    
    
    private static void ImportLegacyIfNeeded(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }
        try
        {
            if (DatabaseService.GetValue<string>(LegacyImportedKeyPrefix + playerId) == "1")
            {
                return;
            }
            string folder = Path.Combine(LegacyFolder, playerId);
            
            string poolFile = Path.Combine(folder, "pool.json");
            if (!File.Exists(poolFile))
            {
                poolFile = Path.Combine(folder, "pool.json.imported");
            }
            if (!File.Exists(poolFile))
            {
                DatabaseService.SetValue(LegacyImportedKeyPrefix + playerId, "1");
                return;
            }
            Dictionary<string, List<GachaRecord>>? pool = JsonSerializer.Deserialize<Dictionary<string, List<GachaRecord>>>(File.ReadAllText(poolFile));
            if (pool is null)
            {
                return;
            }
            GachaParams parameters = LoadLegacyParams(playerId);
            Save(playerId, pool, parameters);
            DatabaseService.SetValue(LegacyImportedKeyPrefix + playerId, "1");
            string plainFile = Path.Combine(folder, "pool.json");
            
            
            if (poolFile == plainFile)
            {
                try { File.Move(plainFile, plainFile + ".imported", true); } catch { }
                string paramsFile = Path.Combine(folder, "params.json");
                if (File.Exists(paramsFile))
                {
                    try { File.Move(paramsFile, paramsFile + ".imported", true); } catch { }
                }
            }
        }
        catch { }
    }


    private static GachaParams LoadLegacyParams(string playerId)
    {
        try
        {
            foreach (string name in new[] { "params.json", "params.json.imported" })
            {
                string file = Path.Combine(LegacyFolder, playerId, name);
                if (File.Exists(file))
                {
                    GachaParams? p = JsonSerializer.Deserialize<GachaParams>(File.ReadAllText(file));
                    if (p is not null)
                    {
                        return p;
                    }
                }
            }
        }
        catch { }
        return new GachaParams { PlayerId = playerId };
    }


    
    
    
    
    
    
    public static string? FindGameLogFile()
    {
        string? exe = GameRegistry.GetConfiguredExePath(Core.GameBiz.WutheringWaves);
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
        {
            return null;
        }
        string? dir = Path.GetDirectoryName(exe);
        for (int i = 0; i < 5 && dir is not null; i++)
        {
            string candidate = Path.Combine(dir, "Client", "Saved", "Logs", "Client.log");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }


    
    private sealed class GachaRow
    {
        public string CardPoolType { get; set; } = "";
        public int ResourceId { get; set; }
        public int QualityLevel { get; set; }
        public string? ResourceType { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
        public string Time { get; set; } = "";
    }
}
