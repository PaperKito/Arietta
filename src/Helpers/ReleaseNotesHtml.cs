using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Arietta.Helpers;

internal static partial class ReleaseNotesHtml
{

    public static string Build(IReadOnlyList<(string Tag, string Date, string Body)> releases, bool dark,
        Func<string, string?>? heroImageResolver = null, bool deferHeroImage = false)
    {
        var sb = new StringBuilder(8192);
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");

        sb.Append("<base href=\"https://github.com/\" target=\"_blank\">");
        sb.Append("<style>").Append(dark ? DarkCss : LightCss).Append("</style>");
        sb.Append("</head><body>");
        for (int i = 0; i < releases.Count; i++)
        {
            (string tag, string date, string body) = releases[i];
            body = NormalizeImageUrls(body ?? "");

            string? hero = i == 0 ? TakeHeroImage(ref body, heroImageResolver, deferHeroImage) : null;

            sb.Append("<section class=\"release\">");
            sb.Append("<h1>").Append(Escape(tag));
            if (date.Length > 0)
            {
                sb.Append("<span class=\"date\">").Append(Escape(date)).Append("</span>");
            }
            sb.Append("</h1>");
            if (hero is not null)
            {
                sb.Append("<div class=\"hero\">").Append(hero).Append("</div>");
            }
            sb.Append("<div class=\"body\">").Append(body).Append("</div>");
            sb.Append("</section>");
        }

        sb.Append("""
            <script>
            (function () {
              // 正文里的配图也做成可点击：点开用系统浏览器看原图（和 Starward 一致）
              document.querySelectorAll('img').forEach(function (img) {
                if (img.closest('a')) { return; }
                var src = img.getAttribute('src');
                if (!src || src.indexOf('http') !== 0) { return; }
                var a = document.createElement('a');
                a.href = src;
                a.target = '_blank';
                a.rel = 'noreferrer';
                img.parentNode.insertBefore(a, img);
                a.appendChild(img);
              });
              document.querySelectorAll('.hero img').forEach(function (img) {
                // 加载失败只隐藏、不删除：主程序自己可能正在后台把图抓下来，
                // 抓完会用 JS 把 src 换成本地文件再显示出来。
                function hide() { img.style.display = 'none'; }
                if (img.complete && img.naturalWidth === 0) { hide(); }
                else { img.addEventListener('error', hide); }
              });
            })();
            </script>
            """);
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string? TakeHeroImage(ref string body, Func<string, string?>? resolver, bool defer)
    {
        Match match = ImgTagRegex().Match(body);
        if (!match.Success)
        {
            return null;
        }

        string textBefore = TagRegex().Replace(body[..match.Index], "").Trim();
        if (textBefore.Length > 120)
        {
            return null;
        }
        string hero = match.Value;
        Match src = ImgSrcRegex().Match(hero);
        string originalSrc = src.Success ? WebUtility.HtmlDecode(src.Groups[1].Value) : "";
        if (src.Success)
        {
            string? local = resolver?.Invoke(originalSrc);
            Group g = src.Groups[1];
            if (!string.IsNullOrEmpty(local))
            {

                hero = hero[..g.Index] + local + hero[(g.Index + g.Length)..];
            }
            else if (defer)
            {

                hero = hero[..g.Index] + TransparentPixel + hero[(g.Index + g.Length)..];
            }
        }
        if (originalSrc.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {

            hero = "<a href=\"" + Escape(originalSrc) + "\" target=\"_blank\" rel=\"noreferrer\">" + hero + "</a>";
        }
        body = body.Remove(match.Index, match.Length);
        body = EmptyAnchorRegex().Replace(body, "");
        body = EmptyParagraphRegex().Replace(body, "");
        body = LeadingBreakRegex().Replace(body, "$1");
        body = EmptyHeadingRegex().Replace(body, "");
        body = EmptyParagraphRegex().Replace(body, "");
        return hero;
    }

    private static string NormalizeImageUrls(string body)
    {
        if (body.Length == 0)
        {
            return body;
        }
        body = body.Replace("<img ", "<img loading=\"lazy\" ", StringComparison.OrdinalIgnoreCase);
        string html = PrivateUserImageSrcRegex().Replace(body,
            m => "src=\"https://github.com/user-attachments/assets/" + m.Groups["id"].Value + "\"");
        return ApplyImageMirror(html);
    }

    public const string ImageMirrorConfigKey = "NotesImageMirror";

    private static string ApplyImageMirror(string html)
    {
        string? mirror = AppConfig.GetConfig(ImageMirrorConfigKey)?.Trim();
        if (string.IsNullOrEmpty(mirror))
        {
            return html;
        }
        return html.Replace("src=\"https://github.com/user-attachments/assets/",
            $"src=\"{mirror.TrimEnd('/')}/github-attachments/assets/");
    }

    internal static string NormalizeImageUrl(string url)
    {
        Match m = PrivateUserImageUrlRegex().Match(url);
        return m.Success
            ? "https://github.com/user-attachments/assets/" + m.Groups["id"].Value
            : url;
    }

    private static string Escape(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public static string PlainTextBody(string markdown)
    {

        string text = TagRegex().Replace(markdown, "");
        text = HeadingMarkRegex().Replace(text, "");
        return "<div style=\"white-space:pre-wrap\">" + Escape(text) + "</div>";
    }


    [GeneratedRegex(@"<img\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"<img\b[^>]*\bsrc=""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ImgSrcRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("""src="https://private-user-images\.githubusercontent\.com/[^"]*?/\d+-(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\.[A-Za-z0-9]+(?:\?[^"]*)?""", RegexOptions.IgnoreCase)]
    private static partial Regex PrivateUserImageSrcRegex();

    [GeneratedRegex("""^https://private-user-images\.githubusercontent\.com/[^?#]*?/\d+-(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\.[A-Za-z0-9]+(?:\?.*)?$""", RegexOptions.IgnoreCase)]
    private static partial Regex PrivateUserImageUrlRegex();

    [GeneratedRegex(@"^[ \t]{0,3}#{1,6}[ \t]*", RegexOptions.Multiline)]
    private static partial Regex HeadingMarkRegex();

    private const string TransparentPixel =
        "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";


    [GeneratedRegex(@"<a\b[^>]*>\s*</a>", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyAnchorRegex();

    [GeneratedRegex(@"<p\b[^>]*>\s*(?:<br\s*/?>\s*)*</p>", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyParagraphRegex();

    [GeneratedRegex(@"(<p\b[^>]*>)\s*<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingBreakRegex();

    [GeneratedRegex(@"<h([1-6])>\s*</h\1>", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyHeadingRegex();


    private const string DarkCss = """
        html, body { margin: 0; padding: 0; background: transparent; }
        body {
            font: 14px/1.7 "Segoe UI", "Microsoft YaHei UI", "Microsoft YaHei", sans-serif;
            color: #e8edf3; word-wrap: break-word;
        }
        section.release { padding-bottom: 8px; }
        section.release + section.release { margin-top: 8px; border-top: 1px solid rgba(255, 255, 255, .08); }
        h1 {
            font-size: 24px; font-weight: 600; letter-spacing: .2px;
            margin: 6px 24px 16px; color: #ffffff;
        }
        h1 .date { font-size: 12px; font-weight: 400; color: #9aa4b2; margin-left: 12px; }
        /* 首图：通栏，顶到内容区左右边缘，无圆角、无最大高度（Starward 同款观感） */
        .hero { margin: 0 0 16px; }
        .hero img {
            display: block; width: 100%; height: auto;
            max-width: none; max-height: none; border-radius: 0; margin: 0;
        }
        /* 正文：24px 内边距只作用在文字上，主视觉通栏 */
        .body { padding: 0 24px; }
        .body > :first-child { margin-top: 0; }
        .body img {
            max-width: 100%; max-height: 380px; width: auto; height: auto; display: block;
            border-radius: 10px; margin: 14px auto; object-fit: contain; cursor: zoom-in;
        }
        a { color: #6cb2ff; text-decoration: none; }
        a:hover { text-decoration: underline; }
        ul, ol { padding-left: 24px; margin: 10px 0; }
        li { margin: 5px 0; }
        p { margin: 10px 0; }
        code {
            background: rgba(255, 255, 255, .1); padding: 2px 6px;
            border-radius: 5px; font-size: 12.5px; font-family: Consolas, monospace;
        }
        pre {
            background: rgba(255, 255, 255, .07); padding: 14px;
            border-radius: 10px; overflow: auto;
        }
        pre code { background: none; padding: 0; }
        blockquote {
            margin: 12px 0; padding-left: 14px; color: #9aa4b2;
            border-left: 3px solid rgba(255, 255, 255, .16);
        }
        hr { border: none; border-top: 1px solid rgba(255, 255, 255, .1); margin: 20px 0; }
        table { border-collapse: collapse; }
        td, th { border: 1px solid rgba(255, 255, 255, .12); padding: 6px 10px; }
        /* 细滚动条：默认那条 17px 的灰条压在通栏图上很扎眼（Starward 同款 6px） */
        ::-webkit-scrollbar { width: 8px; height: 8px; }
        ::-webkit-scrollbar-track { background: transparent; }
        ::-webkit-scrollbar-thumb { background: rgba(255, 255, 255, .18); border-radius: 1000px; }
        ::-webkit-scrollbar-thumb:hover { background: rgba(255, 255, 255, .3); }
        """;


    private const string LightCss = """
        html, body { margin: 0; padding: 0; background: transparent; }
        body {
            font: 14px/1.7 "Segoe UI", "Microsoft YaHei UI", "Microsoft YaHei", sans-serif;
            color: #1c1c1c; word-wrap: break-word;
        }
        section.release { padding-bottom: 8px; }
        section.release + section.release { margin-top: 8px; border-top: 1px solid rgba(0, 0, 0, .08); }
        h1 {
            font-size: 24px; font-weight: 600; letter-spacing: .2px;
            margin: 6px 24px 16px; color: #101010;
        }
        h1 .date { font-size: 12px; font-weight: 400; color: #6a6a6a; margin-left: 12px; }
        .hero { margin: 0 0 16px; }
        .hero img {
            display: block; width: 100%; height: auto;
            max-width: none; max-height: none; border-radius: 0; margin: 0;
        }
        .body { padding: 0 24px; }
        .body > :first-child { margin-top: 0; }
        .body img {
            max-width: 100%; max-height: 380px; width: auto; height: auto; display: block;
            border-radius: 10px; margin: 14px auto; object-fit: contain; cursor: zoom-in;
        }
        a { color: #0a66c2; text-decoration: none; }
        a:hover { text-decoration: underline; }
        ul, ol { padding-left: 24px; margin: 10px 0; }
        li { margin: 5px 0; }
        p { margin: 10px 0; }
        code {
            background: rgba(0, 0, 0, .07); padding: 2px 6px;
            border-radius: 5px; font-size: 12.5px; font-family: Consolas, monospace;
        }
        pre {
            background: rgba(0, 0, 0, .05); padding: 14px;
            border-radius: 10px; overflow: auto;
        }
        pre code { background: none; padding: 0; }
        blockquote {
            margin: 12px 0; padding-left: 14px; color: #6a6a6a;
            border-left: 3px solid rgba(0, 0, 0, .12);
        }
        hr { border: none; border-top: 1px solid rgba(0, 0, 0, .1); margin: 20px 0; }
        table { border-collapse: collapse; }
        td, th { border: 1px solid rgba(0, 0, 0, .12); padding: 6px 10px; }
        ::-webkit-scrollbar { width: 8px; height: 8px; }
        ::-webkit-scrollbar-track { background: transparent; }
        ::-webkit-scrollbar-thumb { background: rgba(0, 0, 0, .2); border-radius: 1000px; }
        ::-webkit-scrollbar-thumb:hover { background: rgba(0, 0, 0, .32); }
        """;
}
