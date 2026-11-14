using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Arietta.Core;

public sealed class GamePackSignature
{
    public const string FileName = "signature.json";

    public string Alg { get; set; } = "";

    public string Author { get; set; } = "";

    public string PublicKey { get; set; } = "";

    public string Signature { get; set; } = "";
}


public static class GamePackSignatureVerifier
{

    public const string AlgEcdsaP256 = "ecdsa-p256-sha256";


    public sealed record Result(bool Verified, string? Author, string Reason);

    public static Result Verify(string packFolder)
    {
        string sigPath = Path.Combine(packFolder, GamePackSignature.FileName);
        if (!File.Exists(sigPath))
        {
            return new Result(false, null, "no-signature");
        }

        GamePackSignature? sig;
        try
        {
            sig = JsonSerializer.Deserialize<GamePackSignature>(File.ReadAllText(sigPath), GamePack.JsonOptions);
        }
        catch (Exception ex)
        {
            return new Result(false, null, $"bad-signature-file: {ex.Message}");
        }
        if (sig is null || !string.Equals(sig.Alg, AlgEcdsaP256, StringComparison.OrdinalIgnoreCase))
        {
            return new Result(false, null, "unsupported-alg");
        }
        if (sig.Author.Length == 0 || sig.PublicKey.Length == 0 || sig.Signature.Length == 0)
        {
            return new Result(false, null, "incomplete-signature");
        }

        try
        {
            byte[] payload = BuildPayload(packFolder);
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(sig.PublicKey), out _);
            bool ok = ecdsa.VerifyData(payload, Convert.FromBase64String(sig.Signature),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return ok ? new Result(true, sig.Author, "") : new Result(false, null, "digest-mismatch");
        }
        catch (Exception ex)
        {
            return new Result(false, null, $"verify-failed: {ex.Message}");
        }
    }

    public static byte[] BuildPayload(string packFolder)
    {
        string root = Path.GetFullPath(packFolder).TrimEnd(Path.DirectorySeparatorChar);
        var entries = new List<(string Path, string Hash)>();

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (string.Equals(rel, GamePackSignature.FileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            using var stream = File.OpenRead(file);
            entries.Add((rel.ToLowerInvariant(), Convert.ToHexString(SHA256.HashData(stream))));
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));

        var sb = new StringBuilder();
        foreach ((string path, string hash) in entries)
        {
            sb.Append(path).Append('\n').Append(hash).Append('\n');
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

}
