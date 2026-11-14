using Microsoft.Extensions.Logging;
using Arietta.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.GamePacks;

public static class GamePackService
{

    private const string ManifestFileName = "game.json";

    private static readonly List<GamePack> _packs = new();

    public static string Root => Path.Combine(AppConfig.LocalFolder, "games");


    public static IReadOnlyList<GamePack> Packs => _packs;

    public static event Action? Changed;

    public static void Reload()
    {
        _packs.Clear();
        GameRegistry.ExternalEntriesProvider ??= BuildExternalEntries;
        try
        {
            Directory.CreateDirectory(Root);
            foreach (string dir in Directory.EnumerateDirectories(Root))
            {
                string manifest = Path.Combine(dir, ManifestFileName);
                if (!File.Exists(manifest))
                {
                    continue;
                }
                GamePack pack = GamePack.Load(manifest);
                GamePackSignatureVerifier.Result sig = GamePackSignatureVerifier.Verify(pack.Folder);
                pack.AuthorVerified = sig.Verified;
                pack.Author = sig.Verified ? sig.Author : null;
                pack.SignatureReason = sig.Verified ? null : sig.Reason;
                pack.ExePath = ResolveExe(pack);
                _packs.Add(pack);
            }
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Failed to enumerate game packs under {Root}", Root);
        }
        _packs.Sort((a, b) =>
        {
            int byOrder = a.Order.CompareTo(b.Order);
            return byOrder != 0 ? byOrder : string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture);
        });

        GameRegistry.Invalidate();
    }

    public static string? ResolveExe(GamePack pack)
    {
        string? stored = AppConfig.GetConfig(ExeConfigKey(pack.Id));
        if (!string.IsNullOrWhiteSpace(stored) && File.Exists(stored))
        {
            return stored;
        }
        if (!string.IsNullOrWhiteSpace(pack.Launch.ExePath) && File.Exists(pack.Launch.ExePath))
        {
            return pack.Launch.ExePath;
        }
        return null;
    }


    public static void SetExePath(GamePack pack, string exePath)
    {
        AppConfig.SetConfig(ExeConfigKey(pack.Id), exePath);
        pack.ExePath = exePath;
    }


    private static string ExeConfigKey(string id) => $"PackExePath_{id}";

    public static bool IsEnabled(GamePack pack) => AppConfig.GetConfig(EnabledConfigKey(pack.Id)) != "0";


    public static void SetEnabled(GamePack pack, bool enabled)
    {
        AppConfig.SetConfig(EnabledConfigKey(pack.Id), enabled ? null : "0");
        GameRegistry.Invalidate();
        Changed?.Invoke();
    }


    private static string EnabledConfigKey(string id) => $"PackEnabled_{id}";

    public static string? SearchExe(GamePack pack, Action<string>? progress = null)
    {
        string[] exeNames = pack.ExeNames.Length > 0
            ? pack.ExeNames
            : new[] { pack.Launch.Exe ?? "" }.Where(s => s.Length > 0).ToArray();
        if (exeNames.Length == 0 || pack.MarkerDirs.Length == 0)
        {
            return null;
        }

        var drives = new List<DriveInfo>();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    drives.Add(drive);
                }
            }
            catch (IOException)
            {
            }
        }

        using var cts = new CancellationTokenSource();
        string? hit = null;
        var gate = new object();
        var tasks = new List<Task>();
        foreach (DriveInfo drive in drives)
        {
            string root = drive.RootDirectory.FullName;
            tasks.Add(Task.Run(() =>
            {
                progress?.Invoke(root);
                string? found = Scan(root, exeNames, pack.MarkerDirs, cts.Token);
                if (found is null)
                {
                    return;
                }
                lock (gate)
                {
                    if (hit is null)
                    {
                        hit = found;
                        cts.Cancel();
                    }
                }
            }, cts.Token));
        }

        try
        {
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromMinutes(2));
        }
        catch (AggregateException)
        {

        }
        return hit;
    }


    private static readonly string[] SkipNames =
    {
        "$RECYCLE.BIN", "Windows", "ProgramData", "System Volume Information", "AppData",
        "$WinREAgent", "Recovery", "PerfLogs", "node_modules", ".git",
    };


    private static string? Scan(string root, string[] exeNames, string[] markerDirs, CancellationToken token)
    {
        var queue = new Queue<(string Dir, int Depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }
            (string dir, int depth) = queue.Dequeue();
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
            if (depth > 0 && markerDirs.Any(m => name.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                string? exe = FindExe(dir, exeNames);
                if (exe is not null)
                {
                    return exe;
                }
            }
            if (depth >= 4)
            {
                continue;
            }
            try
            {
                foreach (string sub in Directory.EnumerateDirectories(dir))
                {
                    if (token.IsCancellationRequested)
                    {
                        return null;
                    }
                    string subName = Path.GetFileName(sub);
                    if (SkipNames.Contains(subName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    queue.Enqueue((sub, depth + 1));
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {

            }
        }
        return null;
    }


    private static string? FindExe(string dir, string[] exeNames)
    {
        try
        {
            foreach (string candidate in exeNames)
            {
                string direct = Path.Combine(dir, candidate);
                if (File.Exists(direct))
                {
                    return direct;
                }
            }

            foreach (string sub in Directory.EnumerateDirectories(dir))
            {
                foreach (string candidate in exeNames)
                {
                    string nested = Path.Combine(sub, candidate);
                    if (File.Exists(nested))
                    {
                        return nested;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
        }
        return null;
    }

    private static IEnumerable<GameRegistry.GameEntry> BuildExternalEntries()
    {
        foreach (GamePack pack in _packs)
        {
            if (!pack.IsValid)
            {
                continue;
            }
            if (!IsEnabled(pack))
            {
                continue;
            }
            yield return new GameRegistry.GameEntry(GameBiz.External, pack.DisplayName, pack.Capabilities.Gacha)
            {
                PackId = pack.Id,
                Pack = pack,
            };
        }
    }

}
