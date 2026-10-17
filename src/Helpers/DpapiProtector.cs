using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Arietta.Helpers;

internal static class DpapiProtector
{
    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string? szDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DATA_BLOB pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return "";
        }
        byte[]? result = Transform(Encoding.UTF8.GetBytes(plainText), protect: true);
        return result is null ? "" : Convert.ToBase64String(result);
    }

    public static string? Unprotect(string protectedBase64)
    {
        if (string.IsNullOrWhiteSpace(protectedBase64))
        {
            return null;
        }
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(protectedBase64);
        }
        catch
        {
            return null;
        }
        byte[]? result = Transform(bytes, protect: false);
        return result is null ? null : Encoding.UTF8.GetString(result);
    }


    private static byte[]? Transform(byte[] data, bool protect)
    {
        if (data.Length == 0)
        {
            return null;
        }

        IntPtr buffer = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data, 0, buffer, data.Length);
            var dataIn = new DATA_BLOB { cbData = data.Length, pbData = buffer };

            bool ok;
            DATA_BLOB dataOut;
            if (protect)
            {
                ok = CryptProtectData(ref dataIn, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out dataOut);
            }
            else
            {
                ok = CryptUnprotectData(ref dataIn, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CRYPTPROTECT_UI_FORBIDDEN, out dataOut);
            }

            if (!ok || dataOut.cbData <= 0 || dataOut.pbData == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                byte[] result = new byte[dataOut.cbData];
                Marshal.Copy(dataOut.pbData, result, 0, dataOut.cbData);
                return result;
            }
            finally
            {
                LocalFree(dataOut.pbData);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
