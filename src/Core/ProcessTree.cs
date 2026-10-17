using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Arietta.Core;

internal static class ProcessTree
{
    private const uint TH32CS_SNAPPROCESS = 0x00000002;
    private const long INVALID_HANDLE_VALUE = -1;
    private const int MaxDepth = 16;


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32W
    {
        public int dwSize;
        public int cntUsage;
        public int th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public int th32ModuleID;
        public int cntThreads;
        public int th32ParentProcessID;
        public int pcPriClassBase;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    public static Dictionary<int, int> BuildParentMap()
    {
        var map = new Dictionary<int, int>();
        IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == IntPtr.Zero || snapshot.ToInt64() == INVALID_HANDLE_VALUE)
        {
            return map;
        }
        try
        {
            var entry = new PROCESSENTRY32W { dwSize = Marshal.SizeOf<PROCESSENTRY32W>() };
            if (!Process32FirstW(snapshot, ref entry))
            {
                return map;
            }
            do
            {
                map[entry.th32ProcessID] = entry.th32ParentProcessID;
            }
            while (Process32NextW(snapshot, ref entry));
        }
        catch
        {

        }
        finally
        {
            CloseHandle(snapshot);
        }
        return map;
    }

    public static bool IsSelfOrDescendant(int candidatePid, int ancestorPid, Dictionary<int, int> parentMap)
    {
        if (ancestorPid <= 0 || parentMap.Count == 0)
        {
            return false;
        }
        int pid = candidatePid;
        for (int depth = 0; depth < MaxDepth; depth++)
        {
            if (pid == ancestorPid)
            {
                return true;
            }
            if (!parentMap.TryGetValue(pid, out int parent) || parent <= 0)
            {
                return false;
            }
            pid = parent;
        }
        return false;
    }


    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32FirstW(IntPtr snapshot, ref PROCESSENTRY32W entry);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32NextW(IntPtr snapshot, ref PROCESSENTRY32W entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
