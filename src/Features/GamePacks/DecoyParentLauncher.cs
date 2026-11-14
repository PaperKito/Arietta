using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.GamePacks;

public static class DecoyParentLauncher
{

    private const string StagedParentName = "Games.exe";

    private const uint CreateSuspended = 0x00000004;
    private const uint CreateNoWindow = 0x08000000;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const nint ProcThreadAttributeParentProcess = 0x00020000;
    private const uint Infinite = 0xFFFFFFFF;

    public static bool IsElevated
    {
        get
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public static int RunAndWait(string gameExe, string? arguments, string? workDir, string? decoySource)
    {
        string fullGame = Path.GetFullPath(gameExe);
        string gameDir = Path.GetDirectoryName(fullGame)
            ?? throw new InvalidOperationException("game exe has no directory");

        string source = Path.GetFullPath(
            string.IsNullOrWhiteSpace(decoySource) ? "PlatformProcess.exe" : decoySource, gameDir);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"decoy parent source not found: {source}", source);
        }
        if (!HasMatchingSigner(source, fullGame))
        {
            throw new InvalidDataException("decoy parent is unsigned or signed by a different publisher than the game");
        }

        string sessionDir = Path.Combine(gameDir, "parent", Environment.ProcessId.ToString(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sessionDir);
        string staged = Path.Combine(sessionDir, StagedParentName);

        nint hProcess = 0;
        nint hThread = 0;
        try
        {
            File.Copy(source, staged, true);
            File.SetAttributes(staged, FileAttributes.Normal);
            if (!HasMatchingSigner(staged, fullGame))
            {
                throw new InvalidDataException("staged decoy lost its signature");
            }

            (hProcess, hThread) = Suspend(staged);
            (nint gameProcess, nint gameThread, int pid) = StartWithParent(hProcess, fullGame, arguments, workDir ?? gameDir);
            AppConfig.GetLogger("GamePack").LogInformation(
                "Game started gamePid={Pid} decoyPid={Decoy} exe={Exe}", pid, GetProcessId(hProcess), fullGame);

            CloseHandle(gameThread);
            WaitForSingleObject(gameProcess, Infinite);
            int exitCode = 0;
            if (GetExitCodeProcess(gameProcess, out uint nativeExit))
            {
                exitCode = unchecked((int)nativeExit);
            }
            CloseHandle(gameProcess);

            WaitForLeftoverProcesses(Path.GetFileNameWithoutExtension(fullGame));
            return exitCode;
        }
        finally
        {
            CloseHandle(hThread);
            if (hProcess != 0)
            {
                TerminateProcess(hProcess, 0);

                WaitForSingleObject(hProcess, 2000);
                CloseHandle(hProcess);
            }
            Cleanup(sessionDir);
        }
    }


    private static void WaitForLeftoverProcesses(string processName)
    {
        DateTime deadline = DateTime.UtcNow.AddMinutes(10);
        while (DateTime.UtcNow < deadline)
        {
            Process[] running = Process.GetProcessesByName(processName);
            try
            {
                if (running.Length == 0)
                {
                    return;
                }
            }
            finally
            {
                foreach (Process p in running)
                {
                    p.Dispose();
                }
            }
            Thread.Sleep(1000);
        }
    }


    private static int GetProcessId(nint hProcess)
    {
        try
        {
            return GetProcessIdNative(hProcess);
        }
        catch
        {
            return 0;
        }
    }


    private static void Cleanup(string directory)
    {

        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
                RemoveIfEmpty(Path.GetDirectoryName(directory));
                RemoveIfEmpty(Path.GetDirectoryName(Path.GetDirectoryName(directory)));
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == 4)
                {
                    AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot remove decoy session {Dir}", directory);
                    return;
                }
                Thread.Sleep(300);
            }
        }
    }


    private static void RemoveIfEmpty(string? dir)
    {
        if (dir is null || !Directory.Exists(dir))
        {
            return;
        }
        if (Directory.GetFileSystemEntries(dir).Length == 0)
        {
            Directory.Delete(dir);
        }
    }

    private static bool HasMatchingSigner(string executablePath, string gameExecutablePath)
    {
        if (!File.Exists(executablePath))
        {
            return false;
        }
        try
        {
#pragma warning disable SYSLIB0057
            using X509Certificate certificate = X509Certificate.CreateFromSignedFile(executablePath);
            using X509Certificate gameCertificate = X509Certificate.CreateFromSignedFile(gameExecutablePath);
#pragma warning restore SYSLIB0057
            bool matches = CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(certificate.GetRawCertData()), SHA256.HashData(gameCertificate.GetRawCertData()));
            if (!matches)
            {
                AppConfig.GetLogger("GamePack").LogError("Decoy signer differs from game signer: {Path}", executablePath);
            }
            return matches;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot read signer certificate of {Path}", executablePath);
            return false;
        }
    }


    private static (nint Process, nint Thread) Suspend(string executablePath)
    {
        STARTUPINFOEX startupInfo = default;
        startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFO>();

        if (!CreateProcess(executablePath, 0, 0, 0, false, CreateSuspended | CreateNoWindow, 0,
                Path.GetDirectoryName(executablePath), in startupInfo, out PROCESS_INFORMATION info))
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"cannot suspend decoy process: {new Win32Exception(error).Message}");
        }
        return (info.hProcess, info.hThread);
    }


    private static (nint Process, nint Thread, int Pid) StartWithParent(nint parentProcess, string targetExe, string? arguments, string? workDir)
    {
        nint size = 0;
        InitializeProcThreadAttributeList(0, 1, 0, ref size);
        nint list = Marshal.AllocHGlobal(size);
        nint commandLine = 0;
        bool initialized = false;
        try
        {
            if (!InitializeProcThreadAttributeList(list, 1, 0, ref size))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed");
            }
            initialized = true;

            unsafe
            {
                nint parent = parentProcess;
                if (!UpdateProcThreadAttribute(list, 0, ProcThreadAttributeParentProcess,
                        (nint)(&parent), (nint)sizeof(nint), 0, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute failed");
                }
            }

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                commandLine = Marshal.StringToCoTaskMemUni($"\"{targetExe}\" {arguments}");
            }

            STARTUPINFOEX startupInfo = default;
            startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            startupInfo.lpAttributeList = list;

            if (!CreateProcess(targetExe, commandLine, 0, 0, false, ExtendedStartupInfoPresent, 0,
                    workDir, in startupInfo, out PROCESS_INFORMATION info))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error,
                    error == 740
                        ? "game requires elevation; this launcher is not elevated"
                        : $"CreateProcess failed: {new Win32Exception(error).Message}");
            }

            return (info.hProcess, info.hThread, info.dwProcessId);
        }
        finally
        {
            if (commandLine != 0)
            {
                Marshal.FreeCoTaskMem(commandLine);
            }
            if (initialized)
            {
                DeleteProcThreadAttributeList(list);
            }
            Marshal.FreeHGlobal(list);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public nint lpReserved;
        public nint lpDesktop;
        public nint lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public nint lpReserved2;
        public nint hStdInput;
        public nint hStdOutput;
        public nint hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public nint lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public nint hProcess;
        public nint hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true,
        CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcess(string? applicationName, nint commandLine,
        nint processAttributes, nint threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint creationFlags, nint environment,
        string? currentDirectory, in STARTUPINFOEX startupInfo, out PROCESS_INFORMATION processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(nint attributeList, int attributeCount,
        int flags, ref nint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(nint attributeList, uint flags, nint attribute,
        nint value, nint size, nint previousValue, nint returnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(nint attributeList);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(nint process, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(nint handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(nint process, out uint exitCode);

    [DllImport("kernel32.dll", EntryPoint = "GetProcessId", SetLastError = true)]
    private static extern int GetProcessIdNative(nint process);

}
