using Arietta.Features.Database;
using System;
using System.IO;
using System.Security.Cryptography;

namespace Arietta.Features.Sync;

internal static class DatabaseSnapshot
{
    private static readonly byte[] Magic = [0x4D, 0x57, 0x4C, 0x31];

    private const byte Version = 1;
    private const byte FlagEncrypted = 0x01;

    private const int HeaderSize = 6;

    public static byte[] Create(out string plainHash)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), "Arietta", "snapshot");
        Directory.CreateDirectory(tempFolder);
        string tempDatabase = Path.Combine(tempFolder, $"snapshot_{Guid.NewGuid():N}.db");
        try
        {

            DatabaseService.VacuumInto(tempDatabase, stripHostSpecificData: true);
            byte[] plain = File.ReadAllBytes(tempDatabase);
            plainHash = Convert.ToHexString(SHA256.HashData(plain));
            return Concat(BuildHeader(), plain);
        }
        finally
        {
            TryDelete(tempDatabase);
            TryDelete(tempDatabase + "-wal");
            TryDelete(tempDatabase + "-shm");
        }
    }

    public static byte[] Extract(byte[] payload)
    {
        if (payload.Length <= HeaderSize || payload[0] != Magic[0] || payload[1] != Magic[1]
            || payload[2] != Magic[2] || payload[3] != Magic[3])
        {
            throw new InvalidDataException("Not a valid backup file (missing MWL1 header)");
        }
        if (payload[4] != Version)
        {
            throw new InvalidDataException($"Backup file version {payload[4]} not supported, please upgrade the launcher");
        }
        if ((payload[5] & FlagEncrypted) != 0)
        {
            throw new InvalidDataException("Backup file is encrypted, restore it with an older launcher version that supports encrypted backups");
        }
        return payload[HeaderSize..];
    }


    private static byte[] BuildHeader()
    {
        byte[] header = new byte[HeaderSize];
        Magic.CopyTo(header, 0);
        header[4] = Version;
        header[5] = 0;
        return header;
    }


    private static byte[] Concat(byte[] head, byte[] tail) => [.. head, .. tail];

    internal static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
        catch { }
    }
}
