using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;
using Arietta.Core;

namespace Arietta.Helpers;




internal static class GameAutoSearch
{

    private static readonly string[] _skipDirPrefixes =
    {
        "Windows", "$Recycle.Bin", "$WinREAgent", "ProgramData\\Package", "System Volume Information", "PerfLogs",
    };

    
    public static string? FindGame(GameBiz gameBiz)
    {
        var entry = Core.GameRegistry.GetEntry(gameBiz);
        var key = Core.GameRegistry.GetExePathConfigKey(gameBiz);

        
        foreach (var keyName in entry.RegistryKeys)
        {
            try
            {
                string baseKey = keyName.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase) ? "HKEY_LOCAL_MACHINE" : "HKEY_CURRENT_USER";
                string subKey = keyName[(baseKey.Length + 1)..];
                using var reg = baseKey == "HKEY_LOCAL_MACHINE"
                    ? Registry.LocalMachine.OpenSubKey(subKey)
                    : Registry.CurrentUser.OpenSubKey(subKey);
                if (reg == null)
                {
                    continue;
                }
                
                foreach (var valueName in new[] { "SingleLauncherInstallPath", "GameInstallPath", "InstallPath", "Install Dir", null })
                {
                    if (Registry.GetValue(keyName, valueName ?? "", null) is string path && Directory.Exists(path))
                    {
                        string? exe = FindExeUnder(path, entry);
                        if (exe != null)
                        {
                            return exe;
                        }
                    }
                }
            }
            catch { }
        }

        
        foreach (var drive in Directory.GetLogicalDrives())
        {
            try
            {
                var di = new DriveInfo(drive);
                if (di.DriveType != DriveType.Fixed || !di.IsReady)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            string? found = ScanDrive(drive, entry);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }


    
    
    
    
    
    
    
    public static void WriteOfficialLedger(GameBiz gameBiz, string? gameDir, bool elevateForHklm = false)
    {
        foreach (var keyName in Core.GameRegistry.GetEntry(gameBiz).RegistryKeys)
        {
            
            
            bool hklm = keyName.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase);
            if (hklm)
            {
                if (gameDir is not null && elevateForHklm)
                {
                    WriteElevatedRegAdd(keyName["HKEY_LOCAL_MACHINE\\".Length..], "InstallPath", gameDir);
                }
                continue;
            }
            try
            {
                string valueName = keyName.Contains(@"kurogame\KRLauncher", StringComparison.OrdinalIgnoreCase)
                    ? "SingleLauncherInstallPath" : "InstallPath";
                if (gameDir is null)
                {
                    using var reg = Registry.CurrentUser.OpenSubKey(keyName["HKEY_CURRENT_USER\\".Length..], writable: true);
                    reg?.DeleteValue(valueName, throwOnMissingValue: false);
                }
                else
                {
                    Registry.SetValue(keyName, valueName, gameDir);
                }
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger("GameAutoSearch").LogWarning(ex, "写官方安装账本失败 {Key}", keyName);
            }
        }
    }

    
    private static void WriteElevatedRegAdd(string subKey, string valueName, string value)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("reg.exe")
            {
                Arguments = $"add \"{subKey}\" /v {valueName} /t REG_SZ /d \"{value}\" /f",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(15000);
        }
        catch (Exception ex)
        {
            
            AppConfig.GetLogger("GameAutoSearch").LogInformation(ex, "elevated 写 HKLM 账本被取消或失败 {SubKey}", subKey);
        }
    }


    
    private static readonly string[] _otherServerHints = { "bilibili", "overseas", "global" };

    private static string? ScanDrive(string drive, Core.GameRegistry.GameEntry entry)
    {
        var queue = new Queue<(string dir, int depth)>();
        queue.Enqueue((drive, 0));
        while (queue.Count > 0)
        {
            var (dir, depth) = queue.Dequeue();
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
            if (depth > 0 && entry.MarkerDirs.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                
                bool hintOk = entry.DirHint == null
                    ? !_otherServerHints.Any(h => dir.Contains(h, StringComparison.OrdinalIgnoreCase))
                    : dir.Contains(entry.DirHint, StringComparison.OrdinalIgnoreCase);
                if (hintOk)
                {
                    
                    string? exe = FindExeUnder(dir, entry);
                    if (exe != null)
                    {
                        return exe;
                    }
                }
            }
            if (depth >= 3 || _skipDirPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            try
            {
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    queue.Enqueue((sub, depth + 1));
                }
            }
            catch { }
        }
        return null;
    }


    private static string? FindExeUnder(string dir, Core.GameRegistry.GameEntry entry)
    {
        var searchDirs = new List<string> { dir };
        try
        {
            searchDirs.AddRange(Directory.EnumerateDirectories(dir, "* Game"));
            searchDirs.AddRange(Directory.EnumerateDirectories(dir, "*游戏*"));
        }
        catch { }

        foreach (var searchDir in searchDirs)
        {
            foreach (var exeName in entry.ExeNames)
            {
                try
                {
                    string candidate = Path.Combine(searchDir, exeName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
                catch { }
            }
        }
        return null;
    }

}
