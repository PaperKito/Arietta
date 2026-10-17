using Arietta.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

public static class GameProcessMonitor
{
    private static readonly object Gate = new();

    private static readonly HashSet<GameBiz> Running = new();

    private static readonly Dictionary<GameBiz, int> Sessions = new();

    private static int _scanned;

    public static event Action? Changed;


    public static bool IsRunning(GameBiz biz)
    {
        lock (Gate)
        {
            return Running.Contains(biz);
        }
    }

    public static bool IsAnyRunning()
    {
        lock (Gate)
        {
            return Running.Count > 0;
        }
    }

    public static void SetRunning(GameBiz biz, bool running)
    {
        bool changed;
        lock (Gate)
        {
            if (running)
            {
                Sessions[biz] = Sessions.GetValueOrDefault(biz) + 1;
                changed = Running.Add(biz);
                int session = Sessions[biz];
                _ = Task.Run(() => PollUntilExitedAsync(biz, session));
            }
            else
            {
                changed = Running.Remove(biz);
            }
        }
        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public static void NotifyProcessExited(GameBiz biz)
    {
        if (!IsProcessAlive(biz))
        {
            SetRunning(biz, false);
        }
    }

    public static void EnsureScanned()
    {
        if (Interlocked.Exchange(ref _scanned, 1) == 1)
        {
            return;
        }
        foreach (var entry in GameRegistry.Games)
        {
            if (IsProcessAlive(entry.Biz))
            {
                SetRunning(entry.Biz, true);
            }
        }
    }

    private static bool IsSessionCurrent(GameBiz biz, int session)
    {
        lock (Gate)
        {
            return Sessions.GetValueOrDefault(biz) == session;
        }
    }

    private static async Task PollUntilExitedAsync(GameBiz biz, int session)
    {
        try
        {
            while (IsSessionCurrent(biz, session) && IsProcessAlive(biz))
            {
                await Task.Delay(20_000);
            }
        }
        catch
        {

        }
        finally
        {
            bool changed;
            lock (Gate)
            {
                changed = Sessions.GetValueOrDefault(biz) == session && Running.Remove(biz);
            }
            if (changed)
            {
                Changed?.Invoke();
            }
        }
    }

    private static bool IsProcessAlive(GameBiz biz)
    {
        using Process? process = FindRunningProcess(biz);
        return process is not null;
    }

    public static Process? FindRunningProcess(GameBiz biz)
    {
        string? configured = GameRegistry.GetConfiguredExePath(biz);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }
        string? dir = Path.GetDirectoryName(configured);
        if (string.IsNullOrEmpty(dir))
        {
            return null;
        }
        foreach (string exeName in GameRegistry.GetEntry(biz).ExeNames)
        {
            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName)))
            {
                bool matched = false;
                try
                {
                    string? image = Core.ProcessImage.GetPath(process);

                    matched = IsUnderDirectory(Path.GetDirectoryName(image), dir);
                }
                catch
                {

                }
                finally
                {

                    if (!matched)
                    {
                        process.Dispose();
                    }
                }
                if (matched)
                {
                    return process;
                }
            }
        }
        return null;
    }

    private static bool IsUnderDirectory(string? dir, string root)
    {
        if (string.IsNullOrEmpty(dir))
        {
            return false;
        }
        var sep = Path.DirectorySeparatorChar;
        dir = dir.Replace(Path.AltDirectorySeparatorChar, sep);
        root = root.Replace(Path.AltDirectorySeparatorChar, sep);
        if (string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var prefix = root.EndsWith(sep) ? root : root + sep;
        return dir.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
