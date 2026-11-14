using Microsoft.Extensions.Logging;
using Arietta.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Arietta.Features.GamePacks;

public static class GamePackLauncher
{

    public const string Verb = "launchpack";

    public static string? Launch(GamePack pack)
    {
        string? exe = pack.ExePath;
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
        {
            return "game exe not set";
        }

        bool needsDecoy = !string.IsNullOrWhiteSpace(pack.Launch.DecoyParent);
        try
        {
            if (needsDecoy)
            {
                var args = new List<string>
                {
                    Verb,
                    "--exe", exe,
                    "--decoy", pack.Launch.DecoyParent!,
                };
                if (!string.IsNullOrWhiteSpace(pack.Launch.Args))
                {
                    args.Add("--args");
                    args.Add(pack.Launch.Args!);
                }
                args.Add("--cwd");
                args.Add(Path.GetDirectoryName(exe)!);

                SpawnSelf(args, elevate: pack.Launch.RequireAdmin || !DecoyParentLauncher.IsElevated);
                return null;
            }

            var psi = new ProcessStartInfo(exe)
            {
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = true,
            };
            if (!string.IsNullOrWhiteSpace(pack.Launch.Args))
            {
                psi.Arguments = pack.Launch.Args!;
            }
            Process.Start(psi);
            return null;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogError(ex, "Failed to launch pack {Id}", pack.Id);
            return ex.Message;
        }
    }


    private static void SpawnSelf(List<string> args, bool elevate)
    {
        var psi = new ProcessStartInfo(Environment.ProcessPath ?? "Arietta.exe")
        {
            UseShellExecute = true,
        };
        if (elevate)
        {
            psi.Verb = "runas";
        }
        foreach (string arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);
    }

    public static int RunVerb(string[] args)
    {
        string? exe = null;
        string? decoy = null;
        string? gameArgs = null;
        string? cwd = null;

        for (int i = 1; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--exe": exe = args[++i]; break;
                case "--decoy": decoy = args[++i]; break;
                case "--args": gameArgs = args[++i]; break;
                case "--cwd": cwd = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(exe))
        {
            return 2;
        }

        try
        {
            return DecoyParentLauncher.RunAndWait(exe, gameArgs, cwd, decoy);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogError(ex, "launchpack failed for {Exe}", exe);
            return 1;
        }
    }

}
