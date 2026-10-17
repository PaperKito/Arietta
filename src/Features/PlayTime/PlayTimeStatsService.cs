using Dapper;
using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Features.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arietta.Features.PlayTime;

internal sealed class PlayTimeStatsService
{
    private readonly ILogger<PlayTimeStatsService> _logger;

    private const long MAX_INTERVAL = 60_000;

    public PlayTimeStatsService(ILogger<PlayTimeStatsService> logger)
    {
        _logger = logger;
    }

    public int ConvertItemToStats()
    {
        try
        {
            long ago = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            using var dapper = DatabaseService.CreateConnection();
            List<PlayTimeItemStruct> items = dapper.Query<PlayTimeItemStruct>("SELECT * FROM PlayTimeItem ORDER BY TimeStamp;").ToList();
            List<PlayTimeStats> stats = GetPlayTimeStats(items);
            List<PlayTimeStats> filteredStats = stats.Where(x => x.EndTime < ago).ToList();
            if (filteredStats.Count > 0)
            {
                using var t = dapper.BeginTransaction();
                dapper.Execute("INSERT OR IGNORE INTO PlayTimeStats (GameBiz, Pid, StartTime, EndTime, Interruption, Type) VALUES (@GameBiz, @Pid, @StartTime, @EndTime, @Interruption, @Type);", filteredStats, t);
                dapper.Execute("DELETE FROM PlayTimeItem WHERE GameBiz = @GameBiz AND Pid = @Pid AND TimeStamp >= @StartTime AND TimeStamp <= @EndTime;", filteredStats, t);
                t.Commit();
            }
            return stats.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convert playtime items to stats");
            return 0;
        }
    }

    public List<PlayTimeStats> GetPlayTimeStats(List<PlayTimeItemStruct> items)
    {
        List<PlayTimeStats> list = new();
        Dictionary<(GameBiz, int), PlayTimeStats> sessionByKey = new();

        void CloseSession((GameBiz, int) key, bool interruption)
        {
            if (sessionByKey.TryGetValue(key, out var s))
            {
                s.Interruption = interruption;
                list.Add(s);
                sessionByKey.Remove(key);
            }
        }

        void StartSession((GameBiz, int) key, long ts)
        {
            sessionByKey[key] = new PlayTimeStats
            {
                GameBiz = key.Item1,
                Pid = key.Item2,
                StartTime = ts,
                EndTime = ts,
            };
        }

        foreach (var item in items)
        {
            var key = (item.GameBiz, item.Pid);
            switch (item.State)
            {
                case PlayTimeState.Start:

                    CloseSession(key, true);
                    StartSession(key, item.TimeStamp);
                    break;

                case PlayTimeState.Play:
                    if (sessionByKey.TryGetValue(key, out var s))
                    {
                        if (item.TimeStamp - s.EndTime > MAX_INTERVAL)
                        {

                            CloseSession(key, true);
                            StartSession(key, item.TimeStamp);
                        }
                        else
                        {
                            s.EndTime = item.TimeStamp;
                        }
                    }
                    else
                    {

                        StartSession(key, item.TimeStamp);
                    }
                    break;

                case PlayTimeState.Stop:
                case PlayTimeState.Error:

                    if (sessionByKey.TryGetValue(key, out var st))
                    {
                        st.Interruption = item.State is PlayTimeState.Error;
                        st.EndTime = item.TimeStamp;
                        list.Add(st);
                        sessionByKey.Remove(key);
                    }
                    break;
            }
        }

        foreach (var kv in sessionByKey)
        {
            kv.Value.Interruption = true;
            list.Add(kv.Value);
        }
        sessionByKey.Clear();
        return list;
    }

    public TimeSpan GetPlayTimeTotal(GameBiz biz)
    {
        long total = 0;
        foreach (var session in GetPlayTimeInRange(biz, DateTimeOffset.FromUnixTimeMilliseconds(0), DateTimeOffset.Now))
        {
            total += session.EndTime - session.StartTime;
        }
        return TimeSpan.FromMilliseconds(total);
    }

    public List<PlayTimeStats> GetPlayTimeInRange(GameBiz biz, DateTimeOffset start, DateTimeOffset end)
    {
        List<PlayTimeStats> result = new();
        long ts_start = start.ToUnixTimeMilliseconds();
        long ts_end = end.ToUnixTimeMilliseconds();
        if (ts_end <= ts_start)
        {
            return result;
        }

        using var dapper = DatabaseService.CreateConnection();

        var stats = dapper.Query<PlayTimeStats>(
            "SELECT * FROM PlayTimeStats WHERE GameBiz = @biz AND EndTime >= @ts_start AND StartTime < @ts_end AND StartTime >= 0 AND EndTime >= StartTime;",
            new { biz, ts_start, ts_end });
        foreach (var stat in stats)
        {
            stat.StartTime = Math.Max(stat.StartTime, ts_start);
            stat.EndTime = Math.Min(stat.EndTime, ts_end);
            result.Add(stat);
        }

        var items = dapper.Query<PlayTimeItemStruct>(
            "SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND TimeStamp >= @ts_start AND TimeStamp < @ts_end ORDER BY TimeStamp;",
            new { biz, ts_start, ts_end }).ToList();
        stats = GetPlayTimeStats(items);
        foreach (var stat in stats)
        {
            stat.StartTime = Math.Max(stat.StartTime, ts_start);
            stat.EndTime = Math.Min(stat.EndTime, ts_end);
            result.Add(stat);
        }

        return result.OrderBy(x => x.StartTime).ToList();
    }
}