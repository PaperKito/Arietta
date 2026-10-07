using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Arietta.Core;











internal static class ProcessImage
{
    private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    
    public static string? GetPath(Process process)
    {
        try
        {
            string? path = process.MainModule?.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
        }
        catch
        {
            
        }
        return GetPathViaApi(process.Id);
    }

    private static string? GetPathViaApi(int pid)
    {
        IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            return QueryFullProcessImageNameW(handle, 0, buffer, ref size) ? buffer.ToString(0, size) : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }


    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageNameW(IntPtr handle, int flags, StringBuilder buffer, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
