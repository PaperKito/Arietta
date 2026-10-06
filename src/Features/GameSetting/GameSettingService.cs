using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Arietta.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Arietta.Features.GameSetting;









public static class GameSettingService
{

    
    
    public const string PcWindowMode = "PcWindowMode";
    
    public const string PcResolutionIndex = "PcResolutionIndex";
    
    public const string VoiceLanguage = "VoiceLanguage";

    private const string IniSectionGameUserSettings = "[/Script/Engine.GameUserSettings]";

    
    
    
    
    
    private static readonly (int Width, int Height)[] StandardResolutions =
    {
        (3840, 2160),
        (3200, 1800),
        (3072, 1728),
        (2560, 1440),
        (2048, 1536),
        (1920, 1200),
        (1920, 1080),
        (1760, 990),
        (1680, 1050),
        (1600, 1200),
        (1600, 900),
        (1366, 768),
        (1280, 960),
        (1280, 720),
        (1152, 864),
        (1024, 768),
    };

    public record GameResolutionSetting(bool IsFullScreen, bool UseBorderless, int Width, int Height, int ResolutionIndex);



    #region 路径

    public static string? GetGameDir(GameBiz biz)
    {
        string? exe = GameRegistry.GetConfiguredExePath(biz);
        if (string.IsNullOrWhiteSpace(exe))
        {
            return null;
        }
        return Path.GetDirectoryName(exe);
    }


    public static bool IsGameInstalled(GameBiz biz)
    {
        return GetGameDir(biz) is { } dir
            && biz switch
            {
                
                var b when b.IsPunishingGrayRaven() => Directory.Exists(dir),
                _ => File.Exists(Path.Combine(dir, "Client", "Saved", "LocalStorage", "LocalStorage.db")),
            };
    }


    #endregion



    #region 分辨率 / 显示模式

    public static GameResolutionSetting? GetGameResolutionSetting(GameBiz biz)
    {
        int mode = GetLocalStorageInt(biz, PcWindowMode) ?? 0;
        int index = GetLocalStorageInt(biz, PcResolutionIndex) ?? -1;
        string? ini = GetGameUserSettingsPath(biz);
        int width = ParseInt(GetIniValue(ini, IniSectionGameUserSettings, "ResolutionSizeX")) ?? 0;
        int height = ParseInt(GetIniValue(ini, IniSectionGameUserSettings, "ResolutionSizeY")) ?? 0;
        
        return new GameResolutionSetting(mode == 0, false, width, height, index);
    }


    
    
    
    
    
    public static void SetGameResolutionSetting(GameBiz biz, bool isFullScreen, bool useBorderless, int width, int height)
    {
        bool fullscreenMode = isFullScreen || useBorderless;
        var values = new Dictionary<string, string>
        {
            [PcWindowMode] = fullscreenMode ? "0" : "1",
        };
        SetLocalStorageValues(biz, values);

        if (fullscreenMode)
        {
            (width, height) = GetMonitorResolution();
        }
        int iniMode = fullscreenMode ? 1 : 2;
        SetIniValues(GetGameUserSettingsPath(biz), IniSectionGameUserSettings, new Dictionary<string, string>
        {
            ["ResolutionSizeX"] = width.ToString(),
            ["ResolutionSizeY"] = height.ToString(),
            ["LastUserConfirmedResolutionSizeX"] = width.ToString(),
            ["LastUserConfirmedResolutionSizeY"] = height.ToString(),
            ["FullscreenMode"] = iniMode.ToString(),
            ["LastConfirmedFullscreenMode"] = iniMode.ToString(),
            ["PreferredFullscreenMode"] = iniMode.ToString(),
        });
    }


    
    
    
    
    public static List<(int Width, int Height)> GetAvailableResolutions()
    {
        return FilterByMonitor(StandardResolutions);
    }


    
    private static List<(int Width, int Height)> FilterByMonitor(IEnumerable<(int Width, int Height)> candidates)
    {
        var supported = GetMonitorSupportedModes();
        var (nativeW, nativeH) = GetMonitorResolution();
        var list = new List<(int Width, int Height)>();
        foreach (var (w, h) in candidates)
        {
            
            if (supported.Count == 0 && (w > nativeW || h > nativeH))
            {
                continue;
            }
            if (supported.Count > 0 && !supported.Contains((w, h)) && (w != nativeW || h != nativeH))
            {
                continue;
            }
            if (!list.Contains((w, h)))
            {
                list.Add((w, h));
            }
        }
        
        if (nativeW > 0 && nativeH > 0 && !list.Contains((nativeW, nativeH)))
        {
            list.Add((nativeW, nativeH));
        }
        return list;
    }


    
    private static HashSet<(int Width, int Height)> GetMonitorSupportedModes()
    {
        var set = new HashSet<(int Width, int Height)>();
        try
        {
            for (int i = 0; EnumDisplaySettings(null, i, out DEVMODE dm); i++)
            {
                if (dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
                {
                    set.Add(((int)dm.dmPelsWidth, (int)dm.dmPelsHeight));
                }
            }
        }
        catch { }
        return set;
    }


    private static (int Width, int Height) GetMonitorResolution()
    {
        
        return (GetSystemMetrics(0), GetSystemMetrics(1));
    }


    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);


    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, out DEVMODE devMode);


    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    private struct DEVMODE
    {
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel;
        public uint dmPelsWidth;
        public uint dmPelsHeight;
        public uint dmDisplayFlags;
        public uint dmDisplayFrequency;
        public uint dmICMMethod;
        public uint dmICMIntent;
        public uint dmMediaType;
        public uint dmDitherType;
        public uint dmReserved1;
        public uint dmReserved2;
        public uint dmPanningWidth;
        public uint dmPanningHeight;
    }


    #endregion



    #region 语音语言

    public static int? GetVoiceLanguage(GameBiz biz)
    {
        return GetLocalStorageInt(biz, VoiceLanguage);
    }


    public static void SetVoiceLanguage(GameBiz biz, int lang)
    {
        SetLocalStorageValues(biz, new Dictionary<string, string> { [VoiceLanguage] = lang.ToString() });
    }


    #endregion



    #region 战双（Unity / 注册表）

    
    
    private const string PgrRegistryPath = @"Software\kurogame\战双帕弥什";

    
    private const string PgrKeyFullscreenMode = "Screenmanager Fullscreen mode_h3630240806";
    private const string PgrKeyResolutionWidth = "Screenmanager Resolution Width_h182942802";
    private const string PgrKeyResolutionHeight = "Screenmanager Resolution Height_h2627697771";
    private const string PgrKeyResolutionWindowWidth = "Screenmanager Resolution Window Width_h2524650974";
    private const string PgrKeyResolutionWindowHeight = "Screenmanager Resolution Window Height_h1684712807";
    private const string PgrKeyResolutionUseNative = "Screenmanager Resolution Use Native_h1405027254";
    
    private const string PgrKeyLastScreen = "LastScreen_h2228486371";                
    private const string PgrKeyLastResolution = "LastResolution_h1893709025";        
    private const string PgrKeyLastFullScreen = "LastFullScreen_h577202448";         
    private const string PgrKeyLastFullScreenMode = "LastFullScreenMode_h1799216915"; 
    
    private const string PgrKeyUseHdr = "___X_QUALITY_USE_HDR___h2944275789";

    

    
    
    
    
    
    private static readonly (int Width, int Height)[] PgrResolutions =
    {
        (2560, 2048),
        (2560, 1600),
        (2560, 1440),
        (2048, 1536),
        (2048, 1280),
        (2048, 1152),
        (1920, 1440),
        (1920, 1200),
        (1920, 1080),
        (1680, 1050),
        (1600, 1200),
        (1600, 1024),
        (1600, 900),
        (1440, 900),
        (1366, 768),
        (1360, 768),
        (1280, 1440),
        (1280, 1024),
        (1280, 960),
        (1280, 800),
        (1280, 768),
        (1280, 720),
        (1176, 664),
        (1152, 864),
        (1024, 768),
        (800, 600),
        (720, 576),
        (720, 480),
        (640, 480),
    };


    public static List<(int Width, int Height)> GetPgrAvailableResolutions()
    {
        return new List<(int Width, int Height)>(PgrResolutions);
    }


    
    
    
    
    public static bool IsPgrFullscreenBlocked()
    {
        var (w, h) = GetMonitorResolution();
        return w > 0 && (w > 2560 || h > 2048);
    }


    public static GameResolutionSetting? GetPgrResolutionSetting(GameBiz biz)
    {
        int mode = GetPgrInt(PgrKeyFullscreenMode) ?? 3;
        int width = GetPgrInt(PgrKeyResolutionWidth) ?? 0;
        int height = GetPgrInt(PgrKeyResolutionHeight) ?? 0;
        
        return new GameResolutionSetting(mode == 0, mode == 1, width, height, -1);
    }


    
    
    
    
    public static void SetPgrResolutionSetting(GameBiz biz, bool isFullScreen, bool useBorderless, int width, int height)
    {
        int mode = isFullScreen ? 0 : useBorderless ? 1 : 3;
        using var key = Registry.CurrentUser.CreateSubKey(PgrRegistryPath, writable: true);
        key.SetValue(PgrKeyFullscreenMode, mode, RegistryValueKind.DWord);
        key.SetValue(PgrKeyResolutionWidth, width, RegistryValueKind.DWord);
        key.SetValue(PgrKeyResolutionHeight, height, RegistryValueKind.DWord);
        key.SetValue(PgrKeyResolutionWindowWidth, width, RegistryValueKind.DWord);
        key.SetValue(PgrKeyResolutionWindowHeight, height, RegistryValueKind.DWord);
        key.SetValue(PgrKeyResolutionUseNative, 0, RegistryValueKind.DWord);
        key.SetValue(PgrKeyLastFullScreen, isFullScreen || useBorderless ? 1 : 0, RegistryValueKind.DWord);
        key.SetValue(PgrKeyLastFullScreenMode, useBorderless ? 1 : 0, RegistryValueKind.DWord);
        byte[] resolution = Encoding.ASCII.GetBytes($"{width},{height}\0");
        key.SetValue(PgrKeyLastScreen, resolution, RegistryValueKind.Binary);
        key.SetValue(PgrKeyLastResolution, resolution, RegistryValueKind.Binary);
    }


    public static bool GetPgrHdrEnabled(GameBiz biz)
    {
        return (GetPgrInt(PgrKeyUseHdr) ?? 1) == 1;
    }


    public static void SetPgrHdrEnabled(GameBiz biz, bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(PgrRegistryPath, writable: true);
        key.SetValue(PgrKeyUseHdr, enable ? 1 : 0, RegistryValueKind.DWord);
    }


    private static int? GetPgrInt(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(PgrRegistryPath);
        return key?.GetValue(name) as int?;
    }


    #endregion



    #region HDR（GameUserSettings.ini）

    public static bool GetHdrEnabled(GameBiz biz)
    {
        return GetIniValue(GetGameUserSettingsPath(biz), IniSectionGameUserSettings, "bUseHDRDisplayOutput") is "True" or "true" or "1";
    }


    public static void SetHdrEnabled(GameBiz biz, bool enable)
    {
        SetIniValues(GetGameUserSettingsPath(biz), IniSectionGameUserSettings, new Dictionary<string, string>
        {
            ["bUseHDRDisplayOutput"] = enable ? "True" : "False",
        });
    }


    #endregion



    #region LocalStorage.db

    private static string? GetLocalStoragePath(GameBiz biz)
    {
        return GetGameDir(biz) is { } dir ? Path.Combine(dir, "Client", "Saved", "LocalStorage", "LocalStorage.db") : null;
    }


    private static int? GetLocalStorageInt(GameBiz biz, string key)
    {
        return ParseInt(GetLocalStorageValue(biz, key));
    }


    public static string? GetLocalStorageValue(GameBiz biz, string key)
    {
        string? path = GetLocalStoragePath(biz);
        if (path is null || !File.Exists(path))
        {
            return null;
        }
        var builder = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite };
        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM LocalStorage WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string;
    }


    public static void SetLocalStorageValues(GameBiz biz, IReadOnlyDictionary<string, string> values)
    {
        string? path = GetLocalStoragePath(biz);
        if (path is null || !File.Exists(path))
        {
            throw new FileNotFoundException(Localization.Lang.Instance.GameSetting_ErrNoLocalStorage, path);
        }
        var builder = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWrite };
        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        using var transaction = connection.BeginTransaction();
        foreach (var (key, value) in values)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO LocalStorage (key, value) VALUES ($key, $value)";
            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$value", value);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }


    #endregion



    #region GameUserSettings.ini

    private static string? GetGameUserSettingsPath(GameBiz biz)
    {
        return GetGameDir(biz) is { } dir ? Path.Combine(dir, "Client", "Saved", "Config", "WindowsNoEditor", "GameUserSettings.ini") : null;
    }


    private static string? GetIniValue(string? path, string section, string key)
    {
        if (path is null || !File.Exists(path))
        {
            return null;
        }
        string currentSection = "";
        foreach (var line in File.ReadLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed;
                continue;
            }
            if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            int split = trimmed.IndexOf('=');
            if (split > 0 && trimmed[..split].Trim() == key)
            {
                return trimmed[(split + 1)..].Trim();
            }
        }
        return null;
    }


    
    private static void SetIniValues(string? path, string section, IReadOnlyDictionary<string, string> values)
    {
        if (path is null || !File.Exists(path))
        {
            throw new FileNotFoundException(Localization.Lang.Instance.GameSetting_ErrNoGameUserSettings, path);
        }
        var missing = new Dictionary<string, string>(values);
        var lines = new List<string>(File.ReadAllLines(path));
        string currentSection = "";
        for (int i = 0; i < lines.Count; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed;
                continue;
            }
            if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            int split = trimmed.IndexOf('=');
            if (split <= 0)
            {
                continue;
            }
            string key = trimmed[..split].Trim();
            if (values.ContainsKey(key))
            {
                lines[i] = $"{key}={values[key]}";
                missing.Remove(key);
            }
        }
        
        for (int i = 0; i < lines.Count && missing.Count > 0; i++)
        {
            if (string.Equals(lines[i].Trim(), section, StringComparison.OrdinalIgnoreCase))
            {
                int insert = i + 1;
                foreach (var (key, value) in missing)
                {
                    lines.Insert(insert++, $"{key}={value}");
                }
                missing.Clear();
            }
        }
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }


    #endregion



    private static int? ParseInt(string? value)
    {
        return int.TryParse(value, out int result) ? result : null;
    }

}
