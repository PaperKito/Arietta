using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arietta;




public static partial class AppConfig
{

    public const string AppName = "Arietta";

    public static string AppVersion =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";


    public static string LocalFolder { get; } = ResolveLocalFolder();

    
    
    
    
    
    
    
    private static string ResolveLocalFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && dir.GetFiles("*.csproj").Length == 0)
        {
            dir = dir.Parent;
        }
        if (dir != null)
        {
            return Path.Combine(dir.FullName, "Arietta");
        }
        string root = Environment.GetEnvironmentVariable("ARIETTA_ROOT") is { Length: > 0 } portable
            ? portable
            : AppContext.BaseDirectory;
        return Path.Combine(root, "data");
    }


    public static string LogFolder => Path.Combine(LocalFolder, "log");


    public static string BackgroundFolder => Path.Combine(LocalFolder, "bg");


    
    public static string DynamicBackgroundFolder => BackgroundFolder;


    public static string ConfigFile => Path.Combine(LocalFolder, "config.ini");


    
    public static string[] CommandLineArgs { get; } =
        Environment.GetCommandLineArgs().Skip(1).ToArray();


    
    
    
    
    public static bool IsPlayTimeHelperProcess =>
        CommandLineArgs.Length > 0
        && string.Equals(CommandLineArgs[0], "playtime", StringComparison.OrdinalIgnoreCase);


    
    private static readonly ConcurrentDictionary<string, string> _configCache = new();


    public static string? GetConfig(string key)
    {
        if (_configCache.Count == 0)
        {
            LoadConfig();
        }
        return _configCache.GetValueOrDefault(key);
    }


    public static void SetConfig(string key, string? value)
    {
        if (_configCache.Count == 0)
        {
            LoadConfig();
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            _configCache.TryRemove(key, out _);
        }
        else
        {
            _configCache[key] = value;
        }
        SaveConfig();
    }


    private static void LoadConfig()
    {
        try
        {
            Directory.CreateDirectory(LocalFolder);
            if (File.Exists(ConfigFile))
            {
                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    var seg = line.Split('=', 2);
                    if (seg.Length == 2 && !string.IsNullOrWhiteSpace(seg[0]))
                    {
                        _configCache[seg[0].Trim()] = seg[1].Trim();
                    }
                }
            }
        }
        catch { }
    }


    private static void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(LocalFolder);
            var sb = new System.Text.StringBuilder();
            foreach (var (key, value) in _configCache)
            {
                sb.AppendLine($"{key}={value}");
            }
            File.WriteAllText(ConfigFile, sb.ToString());
        }
        catch { }
    }

}
