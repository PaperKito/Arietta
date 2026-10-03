using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Arietta.Features.GachaLog;










public static class GachaTableImporter
{
    public static (string PlayerId, Dictionary<string, List<GachaRecord>> Pool) Import(string fileName, byte[] data)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        List<Dictionary<string, string>> rows = ext switch
        {
            ".xlsx" => ReadXlsx(data),
            ".csv" => ReadCsv(data),
            _ => throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat),
        };
        if (rows.Count == 0)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat);
        }
        return BuildPool(rows);
    }


    private static (string PlayerId, Dictionary<string, List<GachaRecord>> Pool) BuildPool(List<Dictionary<string, string>> rows)
    {
        var header = rows[0];
        
        var map = new Dictionary<string, string>();
        foreach (var col in header)
        {
            string name = col.Value.Trim().ToLowerInvariant();
            string field = name switch
            {
                "time" or "时间" or "日期" or "获取时间" => "time",
                "name" or "名称" or "物品" or "物品名称" or "资源名" => "name",
                "rank" or "rank_type" or "星级" or "品质" or "稀有度" => "rank",
                "item_type" or "类型" or "物品类型" or "资源类型" => "type",
                "gacha_type" or "pool" or "卡池" or "卡池名称" or "抽卡类型" or "唤取类型" => "pool",
                "count" or "数量" => "count",
                "item_id" or "resource_id" or "物品id" or "资源id" => "id",
                _ => "",
            };
            if (field != "" && !map.ContainsKey(field))
            {
                map[field] = col.Key;
            }
        }
        if (!map.ContainsKey("name") || !map.ContainsKey("time"))
        {
            throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat);
        }

        var pool = new Dictionary<string, List<GachaRecord>>();
        foreach (var row in rows.Skip(1))
        {
            string Get(string field) => map.TryGetValue(field, out var key) && row.TryGetValue(key, out var v) ? v.Trim() : "";
            if (Get("name").Length == 0)
            {
                continue;
            }
            string poolId = ResolvePoolId(Get("pool"));
            var record = new GachaRecord
            {
                CardPoolType = poolId,
                Name = Get("name"),
                ResourceType = Get("type"),
                QualityLevel = int.TryParse(Get("rank"), out var rank) ? rank : 0,
                Count = int.TryParse(Get("count"), out var count) && count > 0 ? count : 1,
                Time = NormalizeTime(Get("time")),
            };
            int.TryParse(Get("id"), out var rid);
            record.ResourceId = rid;
            if (!pool.TryGetValue(poolId, out var records))
            {
                records = [];
                pool[poolId] = records;
            }
            records.Add(record);
        }
        if (pool.Count == 0)
        {
            throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat);
        }
        return ("", pool);
    }


    
    private static string ResolvePoolId(string value)
    {
        if (value.Length == 0)
        {
            return GachaPools.CharacterEvent;
        }
        if (GachaPools.IsPoolId(value))
        {
            return value;
        }
        string mapped = UgfService.ResolvePoolId(value);
        return mapped.Length > 0 ? mapped : GachaPools.CharacterEvent;
    }


    
    private static string NormalizeTime(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
            && serial is > 20000 and < 80000)
        {
            return DateTime.FromOADate(serial).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        return value;
    }


    

    
    private static readonly string[] ExportHeader = ["卡池", "时间", "名称", "物品ID", "类型", "星级", "数量"];


    
    public static string ExportCsv(string playerId, Dictionary<string, List<GachaRecord>> pool)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"玩家ID,{playerId}");
        sb.AppendLine(string.Join(",", ExportHeader));
        foreach (var r in ExportRows(pool))
        {
            sb.AppendLine(string.Join(",", r.Select(EscapeCsvField)));
        }
        return sb.ToString();
    }


    
    public static byte[] ExportXlsx(string playerId, Dictionary<string, List<GachaRecord>> pool)
    {
        List<string[]> cells = [[$"玩家ID: {playerId}"], ExportHeader];
        cells.AddRange(ExportRows(pool));
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(zip, "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            WriteEntry(zip, "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            WriteEntry(zip, "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="唤取记录" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            WriteEntry(zip, "xl/_rels/workbook.xml.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            WriteEntry(zip, "xl/worksheets/sheet1.xml", BuildSheetXml(cells));
        }
        return ms.ToArray();
    }


    
    private static IEnumerable<string[]> ExportRows(Dictionary<string, List<GachaRecord>> pool)
    {
        foreach (var (poolId, records) in pool)
        {
            string poolName = GachaPools.GetName(poolId);
            foreach (var r in records.OrderBy(r => r.Time))
            {
                yield return
                [
                    poolName,
                    r.Time,
                    r.Name,
                    r.ResourceId.ToString(CultureInfo.InvariantCulture),
                    r.ResourceType,
                    r.QualityLevel.ToString(CultureInfo.InvariantCulture),
                    Math.Max(r.Count, 1).ToString(CultureInfo.InvariantCulture),
                ];
            }
        }
    }


    private static string EscapeCsvField(string field)
    {
        return field.Contains(',') || field.Contains('"') || field.Contains('\n')
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;
    }


    private static void WriteEntry(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }


    
    private static string BuildSheetXml(List<string[]> cells)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" + "\n");
        sb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        for (int r = 0; r < cells.Count; r++)
        {
            sb.Append($"""<row r="{r + 1}">""");
            for (int c = 0; c < cells[r].Length; c++)
            {
                string reference = $"{ColumnIndexToLetters(c)}{r + 1}";
                string value = System.Security.SecurityElement.Escape(cells[r][c]) ?? "";
                sb.Append($"""<c r="{reference}" t="inlineStr"><is><t xml:space="preserve">{value}</t></is></c>""");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }


    

    private static List<Dictionary<string, string>> ReadXlsx(byte[] data)
    {
        using var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);

        
        var shared = new List<string>();
        var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
        if (ssEntry is not null)
        {
            var ss = XDocument.Load(ssEntry.Open()).Root!
                .Elements(Ns("si"))
                .Select(si => string.Concat(si.Descendants(Ns("t")).Select(t => t.Value)))
                .ToList();
            shared.AddRange(ss);
        }

        
        string sheetPath = "xl/worksheets/sheet1.xml";
        var wbEntry = zip.GetEntry("xl/workbook.xml");
        if (wbEntry is not null)
        {
            var first = XDocument.Load(wbEntry.Open()).Root!
                .Elements(Ns("sheets"))
                .First()
                .Elements(Ns("sheet"))
                .First();
            string rid = first.Attribute(NsR("id"))?.Value ?? "";
            if (rid.Length > 0)
            {
                var rels = XDocument.Load(zip.GetEntry("xl/_rels/workbook.xml.rels")!.Open()).Root!
                    .Elements(Ns("Relationship"))
                    .FirstOrDefault(r => r.Attribute("Id")?.Value == rid);
                string target = rels?.Attribute("Target")?.Value ?? "";
                if (target.Length > 0)
                {
                    target = target.StartsWith("/xl/", StringComparison.OrdinalIgnoreCase) ? target[1..]
                        : target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target
                        : "xl/" + target;
                    sheetPath = target;
                }
            }
        }

        var entry = zip.GetEntry(sheetPath) ?? throw new InvalidOperationException(Localization.Lang.Instance.Ugf_ErrUnknownFormat);
        var sheet = XDocument.Load(entry.Open()).Root!.Element(Ns("sheetData"))!;
        var rows = new List<Dictionary<string, string>>();
        foreach (var row in sheet.Elements(Ns("row")))
        {
            var cells = new Dictionary<string, string>();
            foreach (var c in row.Elements(Ns("c")))
            {
                string colRef = ColumnLetters(c.Attribute("r")?.Value ?? "");
                string type = c.Attribute("t")?.Value ?? "";
                string value;
                if (type == "inlineStr")
                {
                    value = string.Concat(c.Descendants(Ns("t")).Select(t => t.Value));
                }
                else
                {
                    var v = c.Element(Ns("v"));
                    value = v?.Value ?? "";
                    if (type == "s" && int.TryParse(value, out var idx) && idx < shared.Count)
                    {
                        value = shared[idx];
                    }
                }
                cells[colRef] = value;
            }
            if (cells.Count > 0)
            {
                rows.Add(cells);
            }
        }
        return rows;
    }


    private static XName Ns(string name) => XName.Get(name, "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
    private static XName NsR(string name) => XName.Get(name, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

    
    private static string ColumnLetters(string cellRef)
    {
        return new string(cellRef.TakeWhile(char.IsLetter).ToArray());
    }


    

    private static List<Dictionary<string, string>> ReadCsv(byte[] data)
    {
        
        string text = DecodeText(data);
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in SplitLines(text))
        {
            var fields = SplitCsvLine(line);
            var row = new Dictionary<string, string>();
            for (int i = 0; i < fields.Count; i++)
            {
                row[ColumnIndexToLetters(i)] = fields[i];
            }
            rows.Add(row);
        }
        return rows;
    }


    
    private static string DecodeText(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            return new UTF8Encoding(false).GetString(data, 3, data.Length - 3);
        }
        if (data.Length >= 2 && ((data[0] == 0xFF && data[1] == 0xFE) || (data[0] == 0xFE && data[1] == 0xFF)))
        {
            return Encoding.Unicode.GetString(data);
        }
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch {  }
            return Encoding.GetEncoding("gbk").GetString(data);
        }
    }


    private static string ColumnIndexToLetters(int index)
    {
        var sb = new StringBuilder();
        index++;
        while (index > 0)
        {
            (int div, int rem) = int.DivRem(index - 1, 26);
            sb.Insert(0, (char)('A' + rem));
            index = div;
        }
        return sb.ToString();
    }


    private static IEnumerable<string> SplitLines(string text)
    {
        
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Where(l => l.Trim().Length > 0);
    }


    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else if (ch == '"')
            {
                inQuotes = true;
            }
            else if (ch == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
