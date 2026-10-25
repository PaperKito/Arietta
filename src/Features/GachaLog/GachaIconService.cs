using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Arietta.Helpers;

namespace Arietta.Features.GachaLog;

public static class GachaIconService
{

    private static readonly HttpClient Http = SharedHttp.Default;

    private static readonly string CacheFolder = Path.Combine(AppConfig.LocalFolder, "GachaIcons");

    private static readonly string IndexFile = Path.Combine(CacheFolder, "index.json");

    private static readonly TimeSpan IndexLifetime = TimeSpan.FromHours(24);

    private const string WikiBaseUrl = "https://api.kurobbs.com/wiki/core/catalogue/item/getPage";

    private const string WikiType = "9";
    private static readonly int[] CatalogueIds = [1105, 1106];

    private static volatile Dictionary<string, string>? _index;

    private static readonly SemaphoreSlim _indexLoadLock = new(1, 1);

    internal static async Task<string?> LookupWikiUrlAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        Dictionary<string, string>? index = await EnsureIndexAsync().ConfigureAwait(false);
        return index is not null && index.TryGetValue(name.Trim(), out string? url) ? url : null;
    }

    private static async Task<Dictionary<string, string>?> EnsureIndexAsync()
    {
        var cached = _index;
        if (cached is not null)
        {
            return cached;
        }

        await _indexLoadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_index is not null)
            {
                return _index;
            }

            IndexFileModel? disk = await TryLoadDiskIndexAsync().ConfigureAwait(false);
            if (disk is not null && DateTime.UtcNow - disk.FetchedAt < IndexLifetime)
            {
                return _index = disk.Items;
            }

            Dictionary<string, string>? fresh = await FetchIndexAsync().ConfigureAwait(false);
            if (fresh is not null)
            {
                _index = fresh;
                await TrySaveDiskIndexAsync(fresh).ConfigureAwait(false);
                return fresh;
            }

            if (disk is not null)
            {
                return _index = disk.Items;
            }

            return null;
        }
        finally
        {
            _indexLoadLock.Release();
        }
    }


    private static async Task<Dictionary<string, string>?> FetchIndexAsync()
    {
        try
        {
            var result = new Dictionary<string, string>();
            foreach (int catalogueId in CatalogueIds)
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["catalogueId"] = catalogueId.ToString(CultureInfo.InvariantCulture),
                    ["page"] = "1",
                    ["limit"] = "200",
                });
                using var request = new HttpRequestMessage(HttpMethod.Post, WikiBaseUrl) { Content = content };
                request.Headers.TryAddWithoutValidation("wiki_type", WikiType);
                request.Headers.TryAddWithoutValidation("devCode", "e69cef1c-978f-452c-9e66-8a25b3a03d88");
                using HttpResponseMessage response = await Http.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                WikiPageModel? page = await JsonSerializer.DeserializeAsync(stream, WikiJsonContext.Default.WikiPageModel).ConfigureAwait(false);
                if (page?.Data?.Results?.Records is null)
                {
                    return null;
                }
                foreach (WikiRecordModel record in page.Data.Results.Records)
                {
                    string? url = record.Content?.ContentUrl;
                    if (!string.IsNullOrEmpty(record.Name) && !string.IsNullOrEmpty(url))
                    {
                        result.TryAdd(record.Name, url);
                    }
                }
            }
            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }


    private static async Task<IndexFileModel?> TryLoadDiskIndexAsync()
    {
        try
        {
            if (!File.Exists(IndexFile))
            {
                return null;
            }
            await using FileStream stream = File.OpenRead(IndexFile);
            return await JsonSerializer.DeserializeAsync(stream, WikiJsonContext.Default.IndexFileModel).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }


    private static async Task TrySaveDiskIndexAsync(Dictionary<string, string> items)
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);
            var model = new IndexFileModel { FetchedAt = DateTime.UtcNow, Items = items };
            await using FileStream stream = File.Create(IndexFile);
            await JsonSerializer.SerializeAsync(stream, model, WikiJsonContext.Default.IndexFileModel).ConfigureAwait(false);
        }
        catch
        {

        }
    }


    internal sealed class IndexFileModel
    {
        [JsonPropertyName("fetchedAt")]
        public DateTime FetchedAt { get; set; }

        [JsonPropertyName("items")]
        public Dictionary<string, string> Items { get; set; } = [];
    }


    internal sealed class WikiPageModel
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("data")]
        public WikiDataModel? Data { get; set; }
    }


    internal sealed class WikiDataModel
    {
        [JsonPropertyName("results")]
        public WikiResultsModel? Results { get; set; }
    }


    internal sealed class WikiResultsModel
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("records")]
        public List<WikiRecordModel>? Records { get; set; }
    }


    internal sealed class WikiRecordModel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content")]
        public WikiContentModel? Content { get; set; }
    }


    internal sealed class WikiContentModel
    {
        [JsonPropertyName("contentUrl")]
        public string? ContentUrl { get; set; }
    }
}

[JsonSerializable(typeof(GachaIconService.IndexFileModel))]
[JsonSerializable(typeof(GachaIconService.WikiPageModel))]
internal sealed partial class WikiJsonContext : JsonSerializerContext;
