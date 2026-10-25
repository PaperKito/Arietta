using Arietta.Helpers;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arietta.Features.Sync;

public enum SyncPreference
{

    LocalFirst = 0,

    RemoteFirst = 1,
}

internal sealed class CloudSyncConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #region 用户配置

    public string ServerUrl { get; set; } = "";

    public string UserName { get; set; } = "";

    public const string RemoteFolder = "AriettaSync";

    public bool AutoSync { get; set; } = true;

    public SyncPreference Preference { get; set; } = SyncPreference.LocalFirst;

    public string ProtectedPassword { get; set; } = "";

    #endregion

    #region 同步锚点

    public string LastLocalVersion { get; set; } = "";

    public string LastRemoteETag { get; set; } = "";

    public DateTimeOffset? LastSyncTime { get; set; }

    #endregion

    [JsonIgnore]
    public string Password
    {
        get => DpapiProtector.Unprotect(ProtectedPassword) ?? "";
        set => ProtectedPassword = DpapiProtector.Protect(value ?? "");
    }


    [JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ServerUrl) &&
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Password);

    public const string RemoteFileName = "Arietta.db";


    public static string ConfigFolder => Path.Combine(AppConfig.LocalFolder, "CloudSync");

    public static string ConfigFile => Path.Combine(ConfigFolder, "sync.json");

    public static string BackupFolder => Path.Combine(ConfigFolder, "backup");


    public static CloudSyncConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                string json = File.ReadAllText(ConfigFile);
                var config = JsonSerializer.Deserialize<CloudSyncConfig>(json, JsonOptions);
                if (config is not null)
                {
                    return config;
                }
            }
        }
        catch { }
        return new CloudSyncConfig();
    }


    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigFolder);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch { }
    }

}
