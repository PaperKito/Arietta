using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.DynamicBackground;






public sealed record KuroLauncherApp(string GameId, string AppId, string AppKey)
{
    
    public string DirName => $"{AppId}_{AppKey}";

    
    public string LauncherConfigPath(string host) => $"{host}/launcher/launcher/{DirName}/{GameId}/index.json";

    
    public string BackgroundConfigPath(string host, string backgroundId, string lang)
        => $"{host}/launcher/{DirName}/{GameId}/background/{backgroundId}/{lang}.json";
}





public sealed class KuroBackgroundInfo
{
    [JsonPropertyName("functionSwitch")]
    public int FunctionSwitch { get; set; }

    
    [JsonPropertyName("backgroundFile")]
    public string? BackgroundFile { get; set; }

    
    [JsonPropertyName("backgroundFileType")]
    public int BackgroundFileType { get; set; }

    
    [JsonPropertyName("firstFrameImage")]
    public string? FirstFrameImage { get; set; }

    
    [JsonPropertyName("slogan")]
    public string? Slogan { get; set; }

    
    [JsonIgnore]
    public string? BackgroundId { get; set; }

    
    [JsonIgnore]
    public bool IsVideo => BackgroundFileType == 2;

    
    [JsonIgnore]
    public string? Identity => BackgroundFile;
}










public static class KuroLauncherBackgroundClient
{
    
    public const string Language = "zh-Hans";

    
    public static readonly string[] Hosts =
    {
        "https://prod-cn-alicdn-gamestarter.kurogame.com",
        "https://prod-volcdn-gamestarter.kurogame.xyz",
    };

    private static ILogger Log => AppConfig.GetLogger("KuroLauncherBackgroundClient");

    private static readonly HttpClient Http = SharedHttp.LauncherBackground;

    
    
    
    
    public static KuroLauncherApp? GetApp(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => new KuroLauncherApp("G152", "10003", "Y8xXrXk65DqFHEDgApn3cpK5lfczpFx5"),
            GameBiz.PunishingGrayRaven => new KuroLauncherApp("G148", "10012", "RnIUKs3r59Csliu3N0rl5uRWWBOFDaJL"),
            _ => null,
        };
    }

    
    
    
    
    
    
    public static KuroLauncherApp? GetSourceApp(GameBiz biz)
        => GetApp(biz) ?? GetApp(GetSourceBiz(biz));

    
    public static GameBiz GetSourceBiz(GameBiz biz)
    {
        if (GetApp(biz) is not null)
        {
            return biz;
        }
        if (biz.IsPunishingGrayRaven())
        {
            return GameBiz.PunishingGrayRaven;
        }
        return biz switch
        {
            GameBiz.WutheringWavesGlobal => GameBiz.WutheringWaves,
            GameBiz.WutheringWavesBilibili => GameBiz.WutheringWaves,
            _ => GameBiz.WutheringWaves,
        };
    }

    
    
    
    
    
    public static bool CanFetch(GameBiz biz) => GetSourceApp(biz) is not null;


    
    public static async Task<KuroBackgroundInfo?> GetBackgroundAsync(GameBiz biz, CancellationToken ct = default)
    {
        var app = GetSourceApp(biz);
        if (app is null)
        {
            return null;
        }

        foreach (string host in Hosts)
        {
            try
            {
                string indexUrl = app.LauncherConfigPath(host);
                using var index = JsonDocument.Parse(await Http.GetByteArrayAsync(indexUrl, ct));
                if (!index.RootElement.TryGetProperty("functionCode", out var functionCode) ||
                    !functionCode.TryGetProperty("background", out var backgroundProperty))
                {
                    continue;
                }
                string? backgroundId = backgroundProperty.GetString();
                if (string.IsNullOrWhiteSpace(backgroundId))
                {
                    continue;
                }

                string jsonUrl = app.BackgroundConfigPath(host, backgroundId, Language);
                string payload = await Http.GetStringAsync(jsonUrl, ct);
                var info = JsonSerializer.Deserialize<KuroBackgroundInfo>(payload);
                if (info?.BackgroundFile is not { Length: > 0 })
                {
                    continue;
                }

                info.BackgroundId = backgroundId;
                info.BackgroundFile = Absolute(host, info.BackgroundFile);
                info.FirstFrameImage = Absolute(host, info.FirstFrameImage);
                info.Slogan = Absolute(host, info.Slogan);
                Log.LogInformation("Kuro background {Biz}: id={Id} file={File}", biz, backgroundId, info.BackgroundFile);
                return info;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.LogWarning(ex, "Kuro background request failed on {Host}", host);
            }
        }
        return null;
    }


    
    public static async Task<long> DownloadAsync(string url, string destPath, IProgress<double>? progress, CancellationToken ct)
    {
        string? dir = Path.GetDirectoryName(destPath);
        if (dir is { Length: > 0 })
        {
            Directory.CreateDirectory(dir);
        }
        string partPath = destPath + ".part";

        using (var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            long? total = response.Content.Headers.ContentLength;

            await using (var input = await response.Content.ReadAsStreamAsync(ct))
            await using (var output = File.Create(partPath))
            {
                byte[] buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                    done += read;
                    if (total is > 0)
                    {
                        progress?.Report((double)done / total.Value);
                    }
                }
            }
        }

        File.Move(partPath, destPath, true);
        return new FileInfo(destPath).Length;
    }


    
    public static async Task<bool> TryDownloadAsync(string? url, string destPath, CancellationToken ct)
    {
        if (url is not { Length: > 0 })
        {
            return false;
        }
        try
        {
            await DownloadAsync(url, destPath, null, ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Download {Url} failed", url);
            return false;
        }
    }


    private static string? Absolute(string host, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }
        return url.StartsWith('/') ? host + url : $"{host}/{url}";
    }

}
