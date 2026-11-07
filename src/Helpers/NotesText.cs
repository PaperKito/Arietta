using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Arietta.Helpers;

internal static class NotesText
{

    private const char HeadingMark = '\u0001';

    private static readonly Regex ScriptStyle = new(
        @"<(script|style)[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex ImageTag = new(@"<img[^>]*>", RegexOptions.IgnoreCase);

    private static readonly Regex HtmlHeadingOpen = new(@"<h[1-6][^>]*>", RegexOptions.IgnoreCase);

    private static readonly Regex HtmlBreakTag = new(@"<br\s*/?>", RegexOptions.IgnoreCase);

    private static readonly Regex HtmlListItem = new(@"<li[^>]*>\s*", RegexOptions.IgnoreCase);

    private static readonly Regex HtmlBoldTag = new(@"</?(strong|b)>", RegexOptions.IgnoreCase);

    private static readonly Regex HtmlBlockEnd = new(
        @"</(p|div|li|h[1-6]|tr|blockquote|pre)>", RegexOptions.IgnoreCase);

    private static readonly Regex AnyTag = new(@"<[^>]+>", RegexOptions.Singleline);

    private static readonly Regex MdImage = new(@"!\[[^\]]*\]\([^)]*\)");

    private static readonly Regex ImageSrc = new(@"<img[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase);

    private static readonly Regex MdLink = new(@"\[([^\]]*)\]\([^)]*\)");

    private static readonly Regex MdInlineCode = new(@"`([^`]*)`");

    private static readonly Regex MdHeading = new(@"^\s{0,3}#{1,6}\s*");

    private static readonly Regex MdBullet = new(@"^\s{0,3}[-*+]\s+");

    private static readonly Regex MdQuote = new(@"^\s{0,3}>\s?");

    private static readonly Regex MdRule = new(@"^\s{0,3}([-*_])\s*(\1\s*){2,}$");

    private static readonly Regex BlankRun = new(@"\n{3,}");

    public static IReadOnlyList<(string Text, bool Heading)> ToBlocks(string? raw)
    {
        var blocks = new List<(string Text, bool Heading)>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return blocks;
        }
        string text;
        try
        {
            text = LooksLikeHtml(raw) ? HtmlToText(raw) : MarkdownToText(raw);

            text = AnyTag.Replace(text, "");
        }
        catch
        {
            text = raw;
        }
        foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            bool heading = trimmed[0] == HeadingMark;
            string content = heading ? trimmed[1..].Trim() : trimmed;
            if (content.Length > 0)
            {
                blocks.Add((content, heading));
            }
        }
        return blocks;
    }

    public static IReadOnlyList<string> ExtractImages(string? raw)
    {
        var urls = new List<string>();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return urls;
        }
        foreach (Match m in MdImage.Matches(raw))
        {

            int open = m.Value.IndexOf("](", StringComparison.Ordinal);
            if (open >= 0)
            {
                Add(m.Value[(open + 2)..].TrimEnd(')').Trim());
            }
        }
        foreach (Match m in ImageSrc.Matches(raw))
        {
            Add(m.Groups[1].Value);
        }
        return urls;

        void Add(string url)
        {
            if (url.Length > 0 && url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                && !urls.Contains(url) && urls.Count < 3)
            {
                urls.Add(url);
            }
        }
    }

    public static IReadOnlyList<(string Text, bool Bold)> SplitBold(string text)
    {
        var runs = new List<(string Text, bool Bold)>();
        int i = 0;
        while (i < text.Length)
        {
            int open = text.IndexOf("**", i, StringComparison.Ordinal);
            if (open < 0)
            {
                runs.Add((text[i..], false));
                break;
            }
            int close = text.IndexOf("**", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                runs.Add((text[i..], false));
                break;
            }
            if (open > i)
            {
                runs.Add((text[i..open], false));
            }
            string bold = text[(open + 2)..close];
            if (bold.Length > 0)
            {
                runs.Add((bold, true));
            }
            i = close + 2;
        }
        if (runs.Count == 0)
        {
            runs.Add((text, false));
        }
        return runs;
    }


    private static bool LooksLikeHtml(string raw)
    {
        return raw.Contains("<p", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("<li", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("<h2", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("<div", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("<br", StringComparison.OrdinalIgnoreCase);
    }

    private static string HtmlToText(string html)
    {
        string s = ScriptStyle.Replace(html, "");
        s = ImageTag.Replace(s, "");
        s = HtmlBreakTag.Replace(s, "\n");
        s = HtmlHeadingOpen.Replace(s, "\n" + HeadingMark);
        s = HtmlListItem.Replace(s, "\n• ");

        s = HtmlBoldTag.Replace(s, "**");
        s = HtmlBlockEnd.Replace(s, "\n");
        s = AnyTag.Replace(s, "");
        s = WebUtility.HtmlDecode(s);
        return BlankRun.Replace(s, "\n\n");
    }

    private static string MarkdownToText(string markdown)
    {
        string s = MdImage.Replace(markdown, "");
        s = MdLink.Replace(s, "$1");
        s = MdInlineCode.Replace(s, "$1");

        var sb = new StringBuilder(s.Length);
        foreach (string line in s.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            if (MdRule.IsMatch(line))
            {
                continue;
            }
            string outLine = line;
            if (MdHeading.IsMatch(line))
            {
                outLine = HeadingMark + MdHeading.Replace(line, "");
            }
            else if (MdBullet.IsMatch(line))
            {
                outLine = MdBullet.Replace(line, "• ");
            }
            else if (MdQuote.IsMatch(line))
            {
                outLine = MdQuote.Replace(line, "");
            }

            if (outLine.Contains('|'))
            {
                outLine = outLine.Replace("|", "  ").TrimEnd();
            }
            sb.Append(outLine).Append('\n');
        }
        return BlankRun.Replace(sb.ToString(), "\n\n");
    }
}
