using System;
using System.Collections.Generic;
using System.Linq;

namespace Arietta.Features.GachaLog;

public static class GachaStatService
{

    private static readonly HashSet<int> BaseStandardIds =
    [
        1104, 1203, 1301, 1503, 1405,
        21010015, 21020015, 21030015, 21040015, 21050015,
        21010045, 21020045, 21030045, 21040045, 21050045,
    ];

    public static PoolStat ComputePool(
        string poolId,
        List<GachaRecord> recordsDesc,
        Func<string, List<GachaRecord>?>? standardPoolProvider = null)
    {
        var stat = new PoolStat { PoolId = poolId };

        if (recordsDesc.Count == 0)
        {
            return stat;
        }

        var chronological = recordsDesc.AsEnumerable().Reverse().ToList();
        stat.Total = chronological.Count;
        stat.StartTime = chronological[0].Time;
        stat.EndTime = chronological[^1].Time;

        bool isEventPool = GachaPools.IsEventPool(poolId);
        HashSet<int> standardIds = ResolveStandardIds(poolId, standardPoolProvider);

        int since5 = 0, since4 = 0;
        var items5 = new List<PoolItem>();
        var items4 = new List<PoolItem>();

        foreach (var record in chronological)
        {
            since5++;
            since4++;
            switch (record.QualityLevel)
            {
                case 5:
                    bool isUp = isEventPool && !standardIds.Contains(record.ResourceId);
                    items5.Add(new PoolItem(record.Name, record.ResourceId, since5, isUp, record.Time));
                    since5 = 0;

                    since4 = 0;
                    break;
                case 4:
                    items4.Add(new PoolItem(record.Name, record.ResourceId, since4, false, record.Time));
                    since4 = 0;
                    break;
            }
        }

        stat.Count5 = items5.Count;
        stat.Count4 = items4.Count;
        stat.Count3 = chronological.Count - items5.Count - items4.Count;
        stat.CurrentPity5 = since5;
        stat.CurrentPity4 = since4;
        stat.Avg5 = stat.Count5 > 0 ? (double)(stat.Total - since5) / stat.Count5 : 0;

        var upItems = items5.Where(i => i.IsUp).ToList();
        stat.UpCount = upItems.Count;
        stat.UpAvg = upItems.Count > 0 ? (double)upItems.Sum(i => i.Pity) / upItems.Count : 0;

        items5.Reverse();
        items4.Reverse();
        stat.Items5 = items5;
        stat.Items4 = items4;
        return stat;
    }

    private static HashSet<int> ResolveStandardIds(string poolId, Func<string, List<GachaRecord>?>? standardPoolProvider)
    {
        var ids = new HashSet<int>(BaseStandardIds);
        if (standardPoolProvider is null)
        {
            return ids;
        }
        foreach (string standardPool in new[] { GachaPools.CharacterStandard, GachaPools.WeaponStandard })
        {
            if (poolId == standardPool)
            {
                continue;
            }
            foreach (var r in standardPoolProvider(standardPool)?.Where(r => r.QualityLevel == 5) ?? [])
            {
                ids.Add(r.ResourceId);
            }
        }
        return ids;
    }
}


public class PoolStat
{
    public string PoolId { get; set; } = "";
    public int Total { get; set; }
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public int Count5 { get; set; }
    public int Count4 { get; set; }
    public int Count3 { get; set; }
    public int CurrentPity5 { get; set; }
    public int CurrentPity4 { get; set; }
    public double Avg5 { get; set; }
    public double UpAvg { get; set; }
    public int UpCount { get; set; }
    public List<PoolItem> Items5 { get; set; } = [];
    public List<PoolItem> Items4 { get; set; } = [];
}


public record PoolItem(string Name, int ResourceId, int Pity, bool IsUp, string Time);
