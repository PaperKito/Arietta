using Dapper;
using Microsoft.Data.Sqlite;
using Arietta.Features.Database;
using System;
using System.Collections.Generic;

namespace Arietta.Features.Sync;


internal sealed record MergeResult(bool Success, string Message, int RowsAdded, int SettingInserted, IReadOnlyList<string> ConflictingKeys);









internal static class DatabaseMergeService
{
    private static readonly string[] PlayTimeItemColumns =
        ["TimeStamp", "GameBiz", "Pid", "State", "CursorPos", "Message"];

    private static readonly string[] PlayTimeStatsColumns =
        ["GameBiz", "Pid", "StartTime", "EndTime", "Interruption", "Type"];

    private static readonly string[] GachaLogItemColumns =
        ["PlayerId", "CardPoolType", "ResourceId", "QualityLevel", "ResourceType", "Name", "Count", "Time", "SubSeq"];


    
    
    
    
    public static MergeResult MergeFromFile(string remoteDatabaseFile, SyncPreference preference = SyncPreference.LocalFirst)
    {
        using var connection = DatabaseService.CreateConnection();
        connection.Execute("ATTACH DATABASE @path AS remote;", new { path = remoteDatabaseFile });
        try
        {
            Dictionary<string, int> before = CountRows(connection, "main");
            int settingInserted;

            using (var transaction = connection.BeginTransaction())
            {
                
                MergeAppendOnly(connection, transaction, DatabaseSchema.Tables.PlayTimeItem, PlayTimeItemColumns);
                MergeAppendOnly(connection, transaction, DatabaseSchema.Tables.PlayTimeStats, PlayTimeStatsColumns);
                MergeAppendOnly(connection, transaction, DatabaseSchema.Tables.GachaLogItem, GachaLogItemColumns);

                
                MergeResourceRecord(connection, transaction);
                MergeCombatRecord(connection, transaction);
                MergeTowerFloorRecord(connection, transaction);

                
                settingInserted = MergeSetting(connection, transaction, preference);

                transaction.Commit();
            }

            Dictionary<string, int> after = CountRows(connection, "main");
            int added = 0;
            foreach (var pair in after)
            {
                added += pair.Value - before.GetValueOrDefault(pair.Key);
            }

            string message = added == 0
                ? "本地已是最新，无需合并"
                : $"合并完成：新增 {added} 条记录";
            return new MergeResult(true, message, added, settingInserted, ConflictingKeys(connection));
        }
        finally
        {
            try
            {
                connection.Execute("DETACH DATABASE remote;");
            }
            catch { }
        }
    }


    private static void MergeAppendOnly(SqliteConnection connection, SqliteTransaction transaction, string table, string[] columns)
    {
        if (!RemoteTableExists(connection, table))
        {
            
            return;
        }
        string columnList = string.Join(", ", columns);
        connection.Execute(
            $"INSERT OR IGNORE INTO main.{table} ({columnList}) SELECT {columnList} FROM remote.{table};",
            transaction: transaction);
    }


    
    
    
    
    
    
    
    
    private static void MergeResourceRecord(SqliteConnection connection, SqliteTransaction transaction)
    {
        string table = DatabaseSchema.Tables.ResourceRecord;
        if (!RemoteTableExists(connection, table))
        {
            return;
        }
        connection.Execute($"""
            INSERT INTO main.{table} (GameBiz, RoleId, PeriodType, PeriodIndex, Title, DataJson, UpdateTime)
            SELECT GameBiz, RoleId, PeriodType, PeriodIndex, Title, DataJson, UpdateTime FROM remote.{table}
            WHERE true
            ON CONFLICT (GameBiz, RoleId, PeriodType, PeriodIndex) DO UPDATE SET
                Title = excluded.Title,
                DataJson = excluded.DataJson,
                UpdateTime = excluded.UpdateTime
            WHERE excluded.UpdateTime > main.{table}.UpdateTime;
            """, transaction: transaction);
    }


    
    
    
    
    
    private static void MergeCombatRecord(SqliteConnection connection, SqliteTransaction transaction)
    {
        string table = DatabaseSchema.Tables.CombatRecord;
        if (!RemoteTableExists(connection, table))
        {
            return;
        }
        connection.Execute($"""
            INSERT INTO main.{table} (Mode, RoleId, PeriodEnd, Star, MaxStar, Score, Detail, Chain, UpdateTime)
            SELECT Mode, RoleId, PeriodEnd, Star, MaxStar, Score, Detail, Chain, UpdateTime FROM remote.{table}
            WHERE true
            ON CONFLICT (Mode, RoleId, PeriodEnd) DO UPDATE SET
                Star = excluded.Star,
                MaxStar = excluded.MaxStar,
                Score = excluded.Score,
                Detail = excluded.Detail,
                Chain = excluded.Chain,
                UpdateTime = excluded.UpdateTime
            WHERE excluded.UpdateTime > main.{table}.UpdateTime;
            """, transaction: transaction);
    }


    
    
    
    
    
    private static void MergeTowerFloorRecord(SqliteConnection connection, SqliteTransaction transaction)
    {
        string table = DatabaseSchema.Tables.TowerFloorRecord;
        if (!RemoteTableExists(connection, table))
        {
            return;
        }
        connection.Execute($"""
            INSERT INTO main.{table} (Mode, RoleId, PeriodEnd, Difficulty, AreaId, AreaName, Floor, Star, Roles, UpdateTime)
            SELECT Mode, RoleId, PeriodEnd, Difficulty, AreaId, AreaName, Floor, Star, Roles, UpdateTime FROM remote.{table}
            WHERE true
            ON CONFLICT (Mode, RoleId, PeriodEnd, Difficulty, AreaId, Floor) DO UPDATE SET
                AreaName = excluded.AreaName,
                Star = excluded.Star,
                Roles = excluded.Roles,
                UpdateTime = excluded.UpdateTime
            WHERE excluded.UpdateTime > main.{table}.UpdateTime;
            """, transaction: transaction);
    }


    private static int MergeSetting(SqliteConnection connection, SqliteTransaction transaction, SyncPreference preference)
    {
        if (!RemoteTableExists(connection, DatabaseSchema.Tables.Setting))
        {
            return 0;
        }

        
        int inserted = connection.Execute("""
            INSERT OR IGNORE INTO main.Setting (Key, Value, UpdateTime)
            SELECT r.Key, r.Value, r.UpdateTime FROM remote.Setting r;
            """, transaction: transaction);

        
        
        
        int updated = 0;
        if (preference == SyncPreference.RemoteFirst)
        {
            updated = connection.Execute("""
                UPDATE main.Setting
                SET Value = (SELECT r.Value FROM remote.Setting r WHERE r.Key = main.Setting.Key),
                    UpdateTime = (SELECT r.UpdateTime FROM remote.Setting r WHERE r.Key = main.Setting.Key)
                WHERE EXISTS (
                    SELECT 1 FROM remote.Setting r
                    WHERE r.Key = main.Setting.Key AND r.Value IS NOT main.Setting.Value
                );
                """, transaction: transaction);
        }

        return inserted + updated;
    }


    private static IReadOnlyList<string> ConflictingKeys(SqliteConnection connection)
    {
        var keys = new List<string>();
        if (!RemoteTableExists(connection, DatabaseSchema.Tables.Setting))
        {
            return keys;
        }
        try
        {
            keys.AddRange(connection.Query<string>("""
                SELECT r.Key FROM remote.Setting r
                JOIN main.Setting m ON m.Key = r.Key
                WHERE r.Value IS NOT m.Value;
                """));
        }
        catch { }
        return keys;
    }


    private static bool RemoteTableExists(SqliteConnection connection, string table)
    {
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM remote.sqlite_master WHERE type = 'table' AND name = @table;",
            new { table }) > 0;
    }


    private static Dictionary<string, int> CountRows(SqliteConnection connection, string schema)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string table in new[]
                 {
                     DatabaseSchema.Tables.Setting,
                     DatabaseSchema.Tables.PlayTimeItem,
                     DatabaseSchema.Tables.PlayTimeStats,
                     DatabaseSchema.Tables.GachaLogItem,
                     DatabaseSchema.Tables.ResourceRecord,
                     DatabaseSchema.Tables.CombatRecord,
                     DatabaseSchema.Tables.TowerFloorRecord,
                 })
        {
            try
            {
                counts[table] = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {schema}.{table};");
            }
            catch
            {
                counts[table] = 0;
            }
        }
        return counts;
    }
}
