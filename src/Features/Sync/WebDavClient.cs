using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Arietta.Features.Sync;


internal sealed record WebDavResource(string Href, string? ETag, DateTimeOffset? LastModified);


internal sealed record WebDavUploadResult(bool Success, string? ETag, int StatusCode)
{
    public bool PreconditionFailed => StatusCode is 412;
}















internal sealed class WebDavClient : IDisposable
{
    private const string PropFindBody = """
        <?xml version="1.0" encoding="utf-8"?>
        <d:propfind xmlns:d="DAV:">
          <d:prop>
            <d:getetag/>
            <d:getlastmodified/>
            <d:getcontentlength/>
          </d:prop>
        </d:propfind>
        """;

    private readonly HttpClient _http;
    private readonly Uri _baseUri;

    public WebDavClient(string serverUrl, string userName, string password)
    {
        _baseUri = NormalizeBaseUrl(serverUrl);
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        };
        _http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(90),
        };
        string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Arietta");
    }

    
    
    
    
    
    
    
    private Uri BuildUri(string relativePath, bool collection = false)
    {
        string encoded = Encode(relativePath);
        if (collection && encoded.Length > 0 && !encoded.EndsWith('/'))
        {
            encoded += "/";
        }
        return new Uri(_baseUri, encoded);
    }

    private static string Encode(string relativePath)
    {
        return string.Join('/', relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
    }

    private static Uri NormalizeBaseUrl(string serverUrl)
    {
        string url = serverUrl.Trim();
        if (!url.Contains("://", StringComparison.Ordinal))
        {
            url = "https://" + url;
        }
        if (!url.EndsWith('/'))
        {
            url += "/";
        }
        return new Uri(url);
    }

    
    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> factory, CancellationToken ct)
    {
        const int maxAttempts = 3;
        HttpResponseMessage? response = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            response?.Dispose();
            var request = factory();
            try
            {
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(4, attempt)), ct);
                continue;
            }
            if (response.StatusCode is not (HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests)
                || attempt == maxAttempts - 1)
            {
                return response;
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(4, attempt)), ct);
        }
        return response!;
    }

    
    public async Task<(bool Success, string Message)> TestAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Options, _baseUri), ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return (false, "认证失败：请检查账号与第三方应用密码（坚果云需要单独生成应用密码）");
            }
            if (!response.IsSuccessStatusCode)
            {
                return (false, $"服务器返回 {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            bool supportsDav = response.Headers.TryGetValues("DAV", out IEnumerable<string>? values)
                               && values.Any();
            return supportsDav
                ? (true, "连接成功")
                : (true, "已连通，但服务端未声明 DAV 支持，可能不是标准 WebDAV 目录");
        }
        catch (Exception ex)
        {
            return (false, $"连接失败：{ex.Message}");
        }
    }

    
    public async Task<WebDavResource?> GetResourceAsync(string relativePath, CancellationToken ct = default)
    {
        (HttpStatusCode status, string? xml, string reason) = await PropFindAsync(relativePath, collection: false, ct);

        
        if ((status is HttpStatusCode.Unauthorized or HttpStatusCode.MovedPermanently or HttpStatusCode.Found)
            && !relativePath.EndsWith('/'))
        {
            (status, xml, reason) = await PropFindAsync(relativePath, collection: true, ct);
        }

        if (status == HttpStatusCode.NotFound)
        {
            return null;
        }
        if (status is not (HttpStatusCode.OK or HttpStatusCode.MultiStatus))
        {
            throw new WebDavException($"PROPFIND {relativePath} 失败：{(int)status} {reason}");
        }
        return xml is null ? null : ParseResource(xml, relativePath);
    }

    private async Task<(HttpStatusCode Status, string? Body, string Reason)> PropFindAsync(string relativePath, bool collection, CancellationToken ct)
    {
        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), BuildUri(relativePath, collection));
            request.Headers.Add("Depth", "0");
            request.Content = new StringContent(PropFindBody, Encoding.UTF8, "application/xml");
            return request;
        }, ct);
        string? body = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
        return (response.StatusCode, body, response.ReasonPhrase ?? "");
    }

    
    
    
    
    
    private static List<WebDavResource> ParseResources(string xml, string hrefFallback)
    {
        var list = new List<WebDavResource>();
        if (string.IsNullOrWhiteSpace(xml))
        {
            return list;
        }
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch
        {
            return list;
        }

        foreach (var response in document.Descendants().Where(e => e.Name.LocalName == "response"))
        {
            string href = response.Descendants().FirstOrDefault(e => e.Name.LocalName == "href")?.Value ?? hrefFallback;
            string? etag = response.Descendants().FirstOrDefault(e => e.Name.LocalName == "getetag")?.Value;
            string? modified = response.Descendants().FirstOrDefault(e => e.Name.LocalName == "getlastmodified")?.Value;
            DateTimeOffset? lastModified = DateTimeOffset.TryParse(modified, out var parsed) ? parsed.ToUniversalTime() : null;
            list.Add(new WebDavResource(href, NormalizeETag(etag), lastModified));
        }
        return list;
    }


    private static WebDavResource? ParseResource(string xml, string relativePath)
    {
        return ParseResources(xml, relativePath).FirstOrDefault();
    }

    
    public static string? NormalizeETag(string? etag)
    {
        if (string.IsNullOrWhiteSpace(etag))
        {
            return null;
        }
        return etag.Trim().TrimStart('W', '/').Trim('"');
    }

    
    
    
    
    
    
    
    public async Task EnsureFolderAsync(string relativePath, CancellationToken ct = default)
    {
        using (var response = await SendAsync(() => new HttpRequestMessage(new HttpMethod("MKCOL"), BuildUri(relativePath)), ct))
        {
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                return;
            }
        }

        if (await GetResourceAsync(relativePath, ct) is not null)
        {
            return;
        }

        
        using (var retry = await SendAsync(() => new HttpRequestMessage(new HttpMethod("MKCOL"), BuildUri(relativePath, collection: true)), ct))
        {
            if (retry.IsSuccessStatusCode || retry.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                return;
            }
            if (await GetResourceAsync(relativePath, ct) is not null)
            {
                return;
            }
            throw new WebDavException($"创建目录 {relativePath} 失败：{(int)retry.StatusCode} {retry.ReasonPhrase}");
        }
    }

    public async Task<(byte[]? Content, string? ETag)> DownloadAsync(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, BuildUri(relativePath)), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return (null, null);
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new WebDavException($"下载 {relativePath} 失败：{(int)response.StatusCode} {response.ReasonPhrase}");
        }
        byte[] content = await response.Content.ReadAsByteArrayAsync(ct);
        return (content, NormalizeETag(response.Headers.ETag?.Tag));
    }

    
    
    
    
    
    public async Task<WebDavUploadResult> UploadAsync(string relativePath, byte[] content, string? ifMatch, CancellationToken ct = default)
    {
        int statusCode;
        string? etag;
        using (var response = await SendAsync(() =>
               {
                   var request = new HttpRequestMessage(HttpMethod.Put, BuildUri(relativePath));
                   var body = new ByteArrayContent(content);
                   body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                   request.Content = body;
                   if (!string.IsNullOrWhiteSpace(ifMatch))
                   {
                       request.Headers.TryAddWithoutValidation("If-Match", $"\"{NormalizeETag(ifMatch)}\"");
                   }
                   return request;
               }, ct))
        {
            statusCode = (int)response.StatusCode;
            etag = NormalizeETag(response.Headers.ETag?.Tag);
            if (!response.IsSuccessStatusCode)
            {
                return new WebDavUploadResult(false, etag, statusCode);
            }
        }

        
        
        if (etag is null)
        {
            try
            {
                etag = (await GetResourceAsync(relativePath, ct))?.ETag;
            }
            catch { }
        }
        return new WebDavUploadResult(true, etag, statusCode);
    }

    
    public async Task<bool> DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, BuildUri(relativePath)), ct);
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
    }

    
    public async Task<bool> DeleteCollectionAsync(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, BuildUri(relativePath, collection: true)), ct);
        return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
    }

    
    public async Task<List<WebDavResource>> ListAsync(string relativePath, CancellationToken ct = default)
    {
        using var response = await SendAsync(() =>
        {
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), BuildUri(relativePath, collection: true));
            request.Headers.Add("Depth", "1");
            request.Content = new StringContent(PropFindBody, Encoding.UTF8, "application/xml");
            return request;
        }, ct);

        var list = new List<WebDavResource>();
        if (!response.IsSuccessStatusCode)
        {
            return list;
        }
        string xml = await response.Content.ReadAsStringAsync(ct);
        return ParseResources(xml, hrefFallback: "");
    }

    public void Dispose() => _http.Dispose();
}


internal sealed class WebDavException : Exception
{
    public WebDavException(string message) : base(message) { }
}
