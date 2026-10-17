using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Dapper;
using Arietta.Core;
using Arietta.Features.Database;

namespace Arietta.Features.KuroBbs;

internal static class ResourceRecordStore
{
    private const string WuwaBiz = nameof(GameBiz.WutheringWaves);
    private const string PgrBiz = nameof(GameBiz.PunishingGrayRaven);
    private const string PgrPeriodType = "month";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);


    #region 鸣潮 · 资源简报（/aki/resource/period：月 / 周 / 版本）

    public static void SaveBriefing(string roleId, string periodType, int periodIndex, string? title, KuroBriefingRecord record)
        => UpsertResourceRecord(WuwaBiz, roleId, periodType, periodIndex, title,
            JsonSerializer.Serialize(record, JsonOptions));

    public static KuroBriefingRecord? GetBriefing(string roleId, string periodType, int periodIndex)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            string? json = connection.QueryFirstOrDefault<string>(
                $"SELECT DataJson FROM {DatabaseSchema.Tables.ResourceRecord} " +
                "WHERE GameBiz = @biz AND RoleId = @roleId AND PeriodType = @periodType AND PeriodIndex = @periodIndex;",
                new { biz = WuwaBiz, roleId, periodType, periodIndex });
            return json is null ? null : JsonSerializer.Deserialize<KuroBriefingRecord>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static List<BriefingRow> ListBriefing(string roleId, string periodType)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            var rows = connection.Query<RecordRow>(
                $"SELECT PeriodIndex, Title, DataJson FROM {DatabaseSchema.Tables.ResourceRecord} " +
                "WHERE GameBiz = @biz AND RoleId = @roleId AND PeriodType = @periodType ORDER BY PeriodIndex DESC;",
                new { biz = WuwaBiz, roleId, periodType });
            return rows
                .Select(r => (Row: r, Record: ParseBriefing(r.DataJson)))
                .Where(x => x.Record is not null)
                .Select(x => new BriefingRow(x.Row.PeriodIndex, x.Row.Title, x.Record!))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public sealed record BriefingRow(int PeriodIndex, string? Title, KuroBriefingRecord Record);

    private static KuroBriefingRecord? ParseBriefing(string? json)
    {
        try
        {
            return json is null ? null : JsonSerializer.Deserialize<KuroBriefingRecord>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    #endregion


    #region 战双 · 资源月报（/haru/resource/halfOfYear：近半年逐月）

    public static void SavePgrReport(string roleId, KuroPgrResourceReport report)
    {
        foreach (var m in report.PerMonthList ?? [])
        {
            SavePgrMonth(roleId, m.CurrentYear, m.CurrentMonth, m.Month,
                m.MonthBlackCard, m.MonthDevelopResource, m.MonthTradeCredit);
        }
    }

    public static void SavePgrMonth(string roleId, int year, int month, string? title,
        int blackCard, int developResource, int tradeCredit)
        => UpsertResourceRecord(PgrBiz, roleId, PgrPeriodType, year * 100 + month, title,
            JsonSerializer.Serialize(new PgrMonthPayload(blackCard, developResource, tradeCredit), JsonOptions));

    private static void UpsertResourceRecord(string biz, string roleId, string periodType, int periodIndex,
        string? title, string json)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            connection.Execute($"""
                INSERT INTO {DatabaseSchema.Tables.ResourceRecord} (GameBiz, RoleId, PeriodType, PeriodIndex, Title, DataJson, UpdateTime)
                VALUES (@Biz, @RoleId, @PeriodType, @PeriodIndex, @Title, @DataJson, @UpdateTime)
                ON CONFLICT (GameBiz, RoleId, PeriodType, PeriodIndex) DO UPDATE SET
                    Title = excluded.Title,
                    DataJson = excluded.DataJson,
                    UpdateTime = excluded.UpdateTime;
                """, new
            {
                Biz = biz,
                RoleId = roleId,
                PeriodType = periodType,
                PeriodIndex = periodIndex,
                Title = title,
                DataJson = json,
                UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            });
        }
        catch { }
    }

    public static List<PgrMonthRow> ListPgrMonths(string roleId)
    {
        try
        {
            using var connection = DatabaseService.CreateConnection();
            var rows = connection.Query<RecordRow>(
                $"SELECT PeriodIndex, Title, DataJson FROM {DatabaseSchema.Tables.ResourceRecord} " +
                "WHERE GameBiz = @biz AND RoleId = @roleId AND PeriodType = @periodType ORDER BY PeriodIndex DESC;",
                new { biz = PgrBiz, roleId, periodType = PgrPeriodType });
            var result = new List<PgrMonthRow>();
            foreach (var r in rows)
            {
                var payload = ParsePgrMonth(r.DataJson);
                if (payload is null)
                {
                    continue;
                }
                result.Add(new PgrMonthRow(r.PeriodIndex / 100, r.PeriodIndex % 100, r.Title,
                    payload.BlackCard, payload.DevelopResource, payload.TradeCredit));
            }
            return result;
        }
        catch
        {
            return [];
        }
    }

    public sealed record PgrMonthRow(int Year, int Month, string? Title,
        int BlackCard, int DevelopResource, int TradeCredit);

    private sealed record PgrMonthPayload(int BlackCard, int DevelopResource, int TradeCredit);

    private static PgrMonthPayload? ParsePgrMonth(string? json)
    {
        try
        {
            return json is null ? null : JsonSerializer.Deserialize<PgrMonthPayload>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    #endregion


    private sealed class RecordRow
    {
        public int PeriodIndex { get; set; }
        public string? Title { get; set; }
        public string? DataJson { get; set; }
    }
}
