using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Arietta.Features.GachaLog;

public static class GachaNameTable
{

    private const int ColZhHans = 0;
    private const int ColZhHant = 1;
    private const int ColEn = 2;

    private static readonly Dictionary<string, int> ColumnByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zh-Hans"] = ColZhHans,
        ["zh-Hant"] = ColZhHant,
        ["en"] = ColEn,
        ["ja"] = 3,
        ["ko"] = 4,
        ["fr"] = 5,
        ["es"] = 6,
        ["pt-BR"] = 7,
        ["th"] = 8,
        ["de"] = ColEn,
    };

    private static readonly int[] LookupColumnOrder =
    [
        ColZhHans, ColZhHant, ColEn, 3, 4, 5, 6, 7, 8,
    ];

    private static readonly Dictionary<int, GachaNameRow> RowById = BuildRowById();

    private static readonly Dictionary<string, GachaNameRow> RowByName = BuildRowByName();

    private static Dictionary<int, GachaNameRow> BuildRowById()
    {
        var byId = new Dictionary<int, GachaNameRow>();
        foreach (GachaNameRow row in GachaNameTableData.Rows)
        {
            if (row.Id > 0)
            {
                byId.TryAdd(row.Id, row);
            }
        }
        return byId;
    }

    private static Dictionary<string, GachaNameRow> BuildRowByName()
    {
        var byName = new Dictionary<string, GachaNameRow>(StringComparer.Ordinal);

        foreach (int column in LookupColumnOrder)
        {
            foreach (GachaNameRow row in GachaNameTableData.Rows)
            {
                string normalized = Normalize(row.Get(column));
                if (normalized.Length > 0)
                {
                    byName.TryAdd(normalized, row);
                }
            }
        }
        return byName;
    }

    private static GachaNameRow? FindRow(int resourceId, string? storedName)
    {
        GachaNameRow? row = null;
        if (resourceId > 0)
        {
            RowById.TryGetValue(resourceId, out row);
        }
        if (row is null && !string.IsNullOrWhiteSpace(storedName))
        {
            RowByName.TryGetValue(Normalize(storedName), out row);
        }
        return row;
    }

    public static string? Translate(int resourceId, string? storedName, string targetCode)
    {
        GachaNameRow? row = FindRow(resourceId, storedName);
        if (row is null)
        {
            return null;
        }

        bool targetHasOwnColumn = !string.Equals(targetCode, "de", StringComparison.OrdinalIgnoreCase);
        int column = ColumnByLanguage.TryGetValue(targetCode, out int c) ? c : ColEn;
        string value = row.Get(column);

        bool needsEnFallback = string.IsNullOrWhiteSpace(value) || !targetHasOwnColumn;
        if (!needsEnFallback)
        {
            return value;
        }

        string en = row.En;
        if (string.IsNullOrWhiteSpace(en))
        {
            return null;
        }
        return !string.IsNullOrWhiteSpace(storedName) && RowContainsName(row, storedName) ? en : null;
    }

    private static bool RowContainsName(GachaNameRow row, string storedName)
    {
        string normalized = Normalize(storedName);
        foreach (int column in LookupColumnOrder)
        {
            if (Normalize(row.Get(column)) == normalized)
            {
                return true;
            }
        }
        return false;
    }

    public static string GetIconLookupName(int resourceId, string storedName)
    {
        GachaNameRow? row = FindRow(resourceId, storedName);
        if (row is null)
        {
            return storedName;
        }
        string zh = row.ZhHans;
        return string.IsNullOrWhiteSpace(zh) ? storedName : zh;
    }

    private static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "";
        }
        string source = name.Trim();
        string folded;
        try
        {
            folded = source.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            folded = source.ToLowerInvariant();
        }
        var sb = new StringBuilder(folded.Length);
        foreach (char c in folded)
        {
            sb.Append(c switch
            {
                '·' or '・' or '･' or '．' or '.' or '•' or '：' or ':' or '－' or '–' or '-' => '|',
                _ => c,
            });
        }
        return sb.ToString();
    }

}
