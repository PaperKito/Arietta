using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Arietta.Core;
using Microsoft.Extensions.Logging;

namespace Arietta.Features.GameLauncher.GameUpdate;

public sealed class GameUpdateVerifiedLedger
{

    private const string FileName = ".arietta-verified.json";

    private const long FlushIntervalMs = 3000;

    private static readonly ILogger _logger = AppConfig.GetLogger<GameUpdateVerifiedLedger>();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly string _stageDir;
    private readonly string _path;
    private readonly ConcurrentDictionary<string, Entry> _entries;
    private readonly object _flushLock = new();
    private readonly Stopwatch _sinceFlush = Stopwatch.StartNew();
    private int _dirty;

    private GameUpdateVerifiedLedger(string stageDir, ConcurrentDictionary<string, Entry> entries)
    {
        _stageDir = stageDir;
        _path = Path.Combine(stageDir, FileName);
        _entries = entries;
    }

    public static GameUpdateVerifiedLedger Load(string stageDir)
    {
        var entries = new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string path = Path.Combine(stageDir, FileName);
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(path), JsonOptions);
                if (loaded is not null)
                {
                    foreach (var pair in loaded)
                    {
                        if (pair.Value is { Size: > 0 } entry && !string.IsNullOrWhiteSpace(pair.Key))
                        {
                            entries[Normalize(pair.Key)] = entry;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read verify ledger in {Dir}; every file will be hashed once more", stageDir);
        }
        return new GameUpdateVerifiedLedger(stageDir, entries);
    }

    public bool IsVerified(string relPath, string absolutePath, long size, string? md5)
    {
        if (md5 is null || size <= 0)
        {
            return false;
        }
        if (!_entries.TryGetValue(Normalize(relPath), out var entry)
            || entry.Size != size
            || !string.Equals(entry.Md5, md5, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        long ticks;
        try
        {
            ticks = new FileInfo(absolutePath).LastWriteTimeUtc.Ticks;
        }
        catch
        {
            return false;
        }
        return ticks == entry.Ticks;
    }

    public void Record(string relPath, string absolutePath, long size, string? md5)
    {
        if (md5 is null || size <= 0 || string.IsNullOrWhiteSpace(relPath))
        {
            return;
        }
        long ticks;
        try
        {
            ticks = new FileInfo(absolutePath).LastWriteTimeUtc.Ticks;
        }
        catch
        {
            return;
        }
        _entries[Normalize(relPath)] = new Entry { Size = size, Md5 = md5, Ticks = ticks };
        Interlocked.Exchange(ref _dirty, 1);
        Flush();
    }

    public void Flush(bool force = false)
    {
        if (Volatile.Read(ref _dirty) == 0)
        {
            return;
        }
        if (!force && _sinceFlush.ElapsedMilliseconds < FlushIntervalMs)
        {
            return;
        }
        lock (_flushLock)
        {
            if (Interlocked.Exchange(ref _dirty, 0) == 0)
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(_stageDir);
                string temp = _path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(_entries, JsonOptions));
                File.Move(temp, _path, true);
                _sinceFlush.Restart();
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _dirty, 1);
                _logger.LogWarning(ex, "Failed to write verify ledger {Path}", _path);
            }
        }
    }

    private static string Normalize(string relPath) => relPath.Replace('\\', '/').TrimStart('/');

    internal sealed class Entry
    {

        [JsonPropertyName("s")]
        public long Size { get; set; }

        [JsonPropertyName("m")]
        public string Md5 { get; set; } = "";

        [JsonPropertyName("t")]
        public long Ticks { get; set; }
    }
}
