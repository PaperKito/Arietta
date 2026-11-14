using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arietta.Core;

public sealed class GamePack
{

    public string Id { get; set; } = "";

    public Dictionary<string, string>? Names { get; set; }

    public string? Vendor { get; set; }

    public string? Version { get; set; }

    public int Order { get; set; } = 100;

    public string? Icon { get; set; }

    public string? ServerCode { get; set; }

    public string[] ExeNames { get; set; } = Array.Empty<string>();

    public string[] MarkerDirs { get; set; } = Array.Empty<string>();

    public string[] RegistryKeys { get; set; } = Array.Empty<string>();

    public GamePackLaunch Launch { get; set; } = new();

    public GamePackCapabilities Capabilities { get; set; } = new();

    public GamePackProvider? Provider { get; set; }

    public GamePackAssets Assets { get; set; } = new();

    public string? Icons { get; set; }

    public Dictionary<string, string>? Ui { get; set; }

    public GamePackUpdate? Update { get; set; }

    public Dictionary<string, string>? Strings { get; set; }

    public GamePackSettings? Settings { get; set; }

    [JsonIgnore]
    public string Folder { get; set; } = "";

    [JsonIgnore]
    public string? LoadError { get; set; }

    [JsonIgnore]
    public string? Author { get; set; }

    [JsonIgnore]
    public bool AuthorVerified { get; set; }

    [JsonIgnore]
    public string? SignatureReason { get; set; }

    [JsonIgnore]
    public bool IsValid => LoadError is null;

    [JsonIgnore]
    public string? ExePath { get; set; }


    public string IconPath => string.IsNullOrEmpty(Icon) ? "" : Path.Combine(Folder, Icon);

    public string DisplayName
    {
        get
        {
            string tag = LanguageTagFor(Localization.UiLanguage.Current);
            return Names is not null && Names.TryGetValue(tag, out string? value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : Id;
        }
    }


    private static string LanguageTagFor(Localization.UiLanguageKind kind) => kind switch
    {
        Localization.UiLanguageKind.SimplifiedChinese => "zh-Hans",
        Localization.UiLanguageKind.TraditionalChinese => "zh-Hant",
        Localization.UiLanguageKind.English => "en",
        Localization.UiLanguageKind.Japanese => "ja",
        Localization.UiLanguageKind.Korean => "ko",
        Localization.UiLanguageKind.French => "fr",
        Localization.UiLanguageKind.German => "de",
        Localization.UiLanguageKind.Spanish => "es",
        Localization.UiLanguageKind.Thai => "th",
        Localization.UiLanguageKind.PortugueseBrazil => "pt-BR",
        _ => "en",
    };


    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static GamePack Load(string manifestPath)
    {
        var pack = new GamePack { Folder = Path.GetDirectoryName(manifestPath) ?? "" };
        try
        {
            pack = JsonSerializer.Deserialize<GamePack>(File.ReadAllText(manifestPath), JsonOptions) ?? pack;
            pack.Folder = Path.GetDirectoryName(manifestPath) ?? "";
            pack.Validate();
        }
        catch (Exception ex)
        {
            pack.LoadError = ex.Message;
        }
        return pack;
    }


    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            LoadError = "id is required";
            return;
        }
        foreach (char c in Id)
        {
            bool ok = c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-';
            if (!ok)
            {
                LoadError = $"invalid id '{Id}' (lowercase ascii and hyphen only)";
                return;
            }
        }
        if (!string.IsNullOrEmpty(Icon) && !File.Exists(IconPath))
        {

            Icon = null;
        }
    }

}

public sealed class GamePackLaunch
{

    public string? Exe { get; set; }

    public string? ExePath { get; set; }

    public string? Args { get; set; }

    public string? DecoyParent { get; set; }

    public bool RequireAdmin { get; set; } = true;

}


public sealed class GamePackCapabilities
{
    public bool Launch { get; set; } = true;
    public bool Gacha { get; set; }
    public bool Settings { get; set; }
    public bool Update { get; set; }

    public bool Accounts { get; set; }
}


public sealed class GamePackAssets
{

    public string? Background { get; set; }

    public string? Poster { get; set; }

    public string? Logo { get; set; }
}


public sealed class GamePackUpdate
{

    public string? ProviderMethod { get; set; }

    public string? BaseUrl { get; set; }
}


public sealed class GamePackProvider
{
    public string? Command { get; set; }
    public string[] Args { get; set; } = Array.Empty<string>();
    public int Protocol { get; set; } = 1;
}


public sealed class GamePackSettings
{
    public GamePackSettingStore Store { get; set; } = new();
    public List<GamePackSettingGroup> Groups { get; set; } = new();
}


public sealed class GamePackSettingStore
{

    public string Type { get; set; } = "registry";

    public string Path { get; set; } = "";
}


public sealed class GamePackSettingGroup
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public List<GamePackSettingItem> Items { get; set; } = new();
}


public sealed class GamePackSettingItem
{

    public string Key { get; set; } = "";
    public string Label { get; set; } = "";

    public string Type { get; set; } = "int";
    public List<GamePackSettingOption>? Options { get; set; }
    public long Min { get; set; }
    public long Max { get; set; } = 100;
}


public sealed class GamePackSettingOption
{
    public long Value { get; set; }
    public string Label { get; set; } = "";
}
