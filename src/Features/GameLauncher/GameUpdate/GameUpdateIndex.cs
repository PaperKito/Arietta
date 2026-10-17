using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Arietta.Features.GameLauncher.GameUpdate;

public sealed class GameUpdateIndex
{
    [JsonPropertyName("default")]
    public GameVersionNode? Default { get; set; }

    [JsonPropertyName("predownloadSwitch")]
    public int PredownloadSwitch { get; set; }

    [JsonPropertyName("predownload")]
    public GameVersionNode? PredownloadNode { get; set; }

    [JsonPropertyName("resourcePacks")]
    public Dictionary<string, GameResourcePack>? ResourcePacks { get; set; }

    [JsonPropertyName("bundles")]
    public Dictionary<string, GameBundle>? Bundles { get; set; }

    [JsonPropertyName("keyFileCheckList")]
    public List<string>? KeyFileCheckList { get; set; }

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public GameVersionNode? Predownload => PredownloadNode ?? FindPredownloadInExtension();

    private GameVersionNode? FindPredownloadInExtension()
    {
        if (ExtensionData is null)
        {
            return null;
        }
        foreach (var (_, value) in ExtensionData)
        {
            if (value.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                continue;
            }
            try
            {
                var node = System.Text.Json.JsonSerializer.Deserialize<GameVersionNode>(value.GetRawText());
                if (node is { Version: not null, Config: not null })
                {
                    return node;
                }
            }
            catch
            {

            }
        }
        return null;
    }
}

public sealed class GameVersionNode
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("changelog")]
    public Dictionary<string, string>? Changelog { get; set; }

    [JsonPropertyName("cdnList")]
    public List<GameCdnItem>? CdnList { get; set; }

    [JsonPropertyName("config")]
    public GameVersionConfig? Config { get; set; }
}

public sealed class GameVersionConfig
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("indexFile")]
    public string? IndexFile { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("patchConfig")]
    public List<GamePatchConfig>? PatchConfig { get; set; }
}

public sealed class GamePatchConfig
{

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("indexFile")]
    public string? IndexFile { get; set; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("ext")]
    public GamePatchConfigExt? Ext { get; set; }
}

public sealed class GamePatchConfigExt
{
    [JsonPropertyName("requiredDiskSpace")]
    public long RequiredDiskSpace { get; set; }
}

public sealed class GameResourcePack
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("indexFile")]
    public string? IndexFile { get; set; }

    [JsonPropertyName("indexFileMd5")]
    public string? IndexFileMd5 { get; set; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("unCompressSize")]
    public long UnCompressSize { get; set; }

    [JsonPropertyName("patchConfig")]
    public List<GamePatchConfig>? PatchConfig { get; set; }
}

public sealed class GameBundle
{

    [JsonPropertyName("resourcePacks")]
    public List<string>? ResourcePacks { get; set; }
}

public sealed class GameCdnItem
{

    [JsonPropertyName("P")]
    public int P { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed class GameResourceEntry
{
    [JsonPropertyName("dest")]
    public string Dest { get; set; } = "";

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("fromFolder")]
    public string? FromFolder { get; set; }
}

public sealed class GameResourceList
{
    [JsonPropertyName("resource")]
    public List<GameResourceEntry> Resource { get; set; } = new();

    [JsonPropertyName("zipInfos")]
    public List<GameResourceZipInfo>? ZipInfos { get; set; }

    [JsonPropertyName("deleteFiles")]
    public List<string>? DeleteFiles { get; set; }

    [JsonPropertyName("groupInfos")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GameResourceGroupInfo>? GroupInfos { get; set; }

    [JsonPropertyName("applyTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ApplyTypes { get; set; }

    [JsonIgnore]
    public bool HasZipPackages => ZipInfos is { Count: > 0 };
}

public sealed class GameResourceGroupInfo
{

    [JsonPropertyName("dest")]
    public string Dest { get; set; } = "";

    [JsonPropertyName("srcFiles")]
    public List<GameResourceEntry>? SrcFiles { get; set; }

    [JsonPropertyName("dstFiles")]
    public List<GameResourceEntry>? DstFiles { get; set; }
}

public sealed class GameResourceZipInfo
{

    [JsonPropertyName("dest")]
    public string Dest { get; set; } = "";

    [JsonPropertyName("entries")]
    public List<GameResourceEntry>? Entries { get; set; }
}

public sealed class LauncherDownloadConfig
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("reUseVersion")]
    public string? ReUseVersion { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("isPreDownload")]
    public bool IsPreDownload { get; set; }

    [JsonPropertyName("appId")]
    public string? AppId { get; set; }
}
