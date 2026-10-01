using Dapper;
using Arietta.Features.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Arietta.Features.KuroBbs;


public sealed class CombatRecordEntry
{
    
    public string Mode { get; init; } = "";

    
    public string RoleId { get; init; } = "";

    
    public long PeriodEnd { get; init; }

    
    public int Star { get; init; }

    
    public int MaxStar { get; init; }

    
    public int Score { get; init; }

    
    public string Detail { get; init; } = "";

    
    public string? Chain { get; init; }

    public long UpdateTime { get; init; }
}



public sealed class TowerFloorRecordEntry
{
    public string Mode { get; init; } = "";
    public string RoleId { get; init; } = "";

    
    public long PeriodEnd { get; init; }

    public int Difficulty { get; init; }
    public int AreaId { get; init; }
    public string? AreaName { get; init; }
    public int Floor { get; init; }
    public int Star { get; init; }

    
    public string? Roles { get; init; }

    public long UpdateTime { get; init; }
}














public static class CombatRecordStore
{
    public const string ModeTower = "tower";
    public const string ModeNewTower = "newtower";
    public const string ModeSlash = "slash";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    
    
    
    
    
    
    
    
    
    
    
    private static readonly Dictionary<string, int> FallbackSeasonDays = new()
    {
        [ModeTower] = 28,
        [ModeNewTower] = 42,
        [ModeSlash] = 28,
    };


    
    public static void Save(CombatRecordEntry entry)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            connection.Execute($"""
                INSERT INTO {DatabaseSchema.Tables.CombatRecord} (Mode, RoleId, PeriodEnd, Star, MaxStar, Score, Detail, Chain, UpdateTime)
                VALUES (@Mode, @RoleId, @PeriodEnd, @Star, @MaxStar, @Score, @Detail, @Chain, @UpdateTime)
                ON CONFLICT(Mode, RoleId, PeriodEnd) DO UPDATE SET
                    Star = excluded.Star,
                    MaxStar = excluded.MaxStar,
                    Score = excluded.Score,
                    Detail = excluded.Detail,
                    Chain = excluded.Chain,
                    UpdateTime = excluded.UpdateTime;
                """, entry);
        }
        catch
        {
            
        }
    }


    
    public static List<CombatRecordEntry> GetList(string mode, string roleId)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            return connection.Query<CombatRecordEntry>($"""
                SELECT Mode, RoleId, PeriodEnd, Star, MaxStar, Score, Detail, Chain, UpdateTime
                FROM {DatabaseSchema.Tables.CombatRecord}
                WHERE Mode = @mode AND RoleId = @roleId
                ORDER BY PeriodEnd DESC, UpdateTime DESC;
                """, new { mode, roleId }).AsList();
        }
        catch
        {
            return [];
        }
    }


    
    public static void Delete(string mode, string roleId)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            connection.Execute(
                $"DELETE FROM {DatabaseSchema.Tables.CombatRecord} WHERE Mode = @mode AND RoleId = @roleId;",
                new { mode, roleId });
        }
        catch { }
        try
        {
            using var connection = DatabaseService.CreateConnection();
            connection.Execute(
                $"DELETE FROM {DatabaseSchema.Tables.TowerFloorRecord} WHERE Mode = @mode AND RoleId = @roleId;",
                new { mode, roleId });
        }
        catch { }
    }


    
    
    
    
    
    public static void SaveTowerFloors(string roleId, long periodEnd, KuroTowerData? data)
    {
        if (data?.DifficultyList is null || periodEnd <= 0)
        {
            return;
        }
        try
        {
            
            using var cleanup = DatabaseService.CreateConnection();
            cleanup.Execute(
                $"DELETE FROM {DatabaseSchema.Tables.TowerFloorRecord} WHERE Mode = @mode AND RoleId = @roleId AND Difficulty != 3;",
                new { mode = ModeTower, roleId });
        }
        catch { }
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var difficulty in data.DifficultyList)
        {
            if (difficulty.Difficulty != 3)
            {
                continue; 
            }
            foreach (var area in difficulty.TowerAreaList ?? [])
            {
                foreach (var floor in area.FloorList ?? [])
                {
                    int floorNo = floor.Floor ?? 0;
                    if (floorNo <= 0)
                    {
                        continue;
                    }
                    try
                    {
                        using var connection = DatabaseService.CreateConnection();
                        connection.Execute($"""
                            INSERT INTO {DatabaseSchema.Tables.TowerFloorRecord}
                                (Mode, RoleId, PeriodEnd, Difficulty, AreaId, AreaName, Floor, Star, Roles, UpdateTime)
                            VALUES (@mode, @roleId, @periodEnd, @difficulty, @areaId, @areaName, @floor, @star, @roles, @now)
                            ON CONFLICT(Mode, RoleId, PeriodEnd, Difficulty, AreaId, Floor) DO UPDATE SET
                                Star = excluded.Star,
                                Roles = excluded.Roles,
                                AreaName = excluded.AreaName,
                                UpdateTime = excluded.UpdateTime;
                            """, new
                        {
                            mode = ModeTower,
                            roleId,
                            periodEnd,
                            difficulty = difficulty.Difficulty,
                            areaId = area.AreaId ?? 0,
                            areaName = area.AreaName,
                            floor = floorNo,
                            star = floor.Star,
                            roles = floor.RoleList is { Count: > 0 } ? JsonSerializer.Serialize(floor.RoleList, JsonOptions) : null,
                            now,
                        });
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    }


    
    
    
    
    public static void MergeAccumulatedFloors(KuroTowerData? data, string roleId, long periodEnd)
    {
        if (data?.DifficultyList is null || periodEnd <= 0)
        {
            return;
        }
        List<TowerFloorRecordEntry> rows;
        try
        {
            using var connection = DatabaseService.CreateConnection();
            rows = connection.Query<TowerFloorRecordEntry>($"""
                SELECT Mode, RoleId, PeriodEnd, Difficulty, AreaId, AreaName, Floor, Star, Roles, UpdateTime
                FROM {DatabaseSchema.Tables.TowerFloorRecord}
                WHERE Mode = @mode AND RoleId = @roleId AND PeriodEnd = @periodEnd;
                """, new { mode = ModeTower, roleId, periodEnd }).AsList();
        }
        catch
        {
            return;
        }
        if (rows.Count == 0)
        {
            return;
        }

        foreach (var difficulty in data.DifficultyList)
        {
            foreach (var area in difficulty.TowerAreaList ?? [])
            {
                var known = rows.Where(r => r.Difficulty == difficulty.Difficulty && r.AreaId == (area.AreaId ?? 0));
                var existing = (area.FloorList ?? [])
                    .Where(f => (f.Floor ?? 0) > 0)
                    .Select(f => f.Floor ?? 0)
                    .ToHashSet();
                foreach (var row in known)
                {
                    if (existing.Contains(row.Floor))
                    {
                        continue;
                    }
                    (area.FloorList ??= []).Add(new KuroTowerFloor
                    {
                        Floor = row.Floor,
                        Star = row.Star,
                        RoleList = string.IsNullOrEmpty(row.Roles)
                            ? null
                            : JsonSerializer.Deserialize<List<KuroTowerSimpleRole>>(row.Roles, JsonOptions),
                    });
                }
                if (area.FloorList is { Count: > 0 })
                {
                    area.FloorList.Sort((a, b) => (a.Floor ?? 0) - (b.Floor ?? 0));
                    
                    int mergedStar = area.FloorList.Sum(f => Math.Clamp(f.Star, 0, 3));
                    area.Star = Math.Max(area.Star, mergedStar);
                }
            }
        }
    }


    
    
    
    
    
    
    
    public static long[] ComputePeriodStarts(IReadOnlyList<CombatRecordEntry> records, string mode)
    {
        var starts = new long[records.Count];
        int fallbackDays = FallbackSeasonDays.GetValueOrDefault(mode, 42);
        for (int i = 0; i < records.Count; i++)
        {
            long end = records[i].PeriodEnd;
            if (end <= 0)
            {
                
                starts[i] = DateTimeOffset.FromUnixTimeMilliseconds(records[i].UpdateTime).ToUnixTimeSeconds();
                continue;
            }

            
            long prev = records.Skip(i + 1).Select(r => r.PeriodEnd).FirstOrDefault(e => e > 0);
            long gapDays = prev > 0 ? (end - prev) / 86400 : long.MaxValue;
            long start = prev > 0 && gapDays is > 0 and <= 200 ? prev : end - fallbackDays * 86400L;
            
            
            
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            while (start > now && start > end - 400L * 86400)
            {
                start -= fallbackDays * 86400L;
            }
            starts[i] = start;
        }
        return starts;
    }
}
