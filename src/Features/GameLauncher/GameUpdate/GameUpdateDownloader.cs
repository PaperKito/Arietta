using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Arietta.Core;
using Microsoft.Extensions.Logging;

namespace Arietta.Features.GameLauncher.GameUpdate;

public enum DownloadPhase
{
    Downloading,
    Applying,

    Patching,

    Verifying,
}

public sealed class GameDownloadProgress
{
    public DownloadPhase Phase { get; init; }

    public long ReceivedBytes { get; init; }

    public long TotalBytes { get; init; }

    public long SpeedBytesPerSec { get; init; }

    public long SpeedFilesPerSec { get; init; }

    public string? CurrentFile { get; init; }

    public int CompletedFiles { get; init; }

    public int TotalFiles { get; init; }

    public double? OverallProgress { get; init; }
}

public sealed class GameUpdateDownloader
{

    private readonly int _parallelism = GameUpdateService.DownloadThreads;

    private int _diagLogged;

    private const int ReadBlockSize = 1 << 20;

    private const string TempSuffix = ".part";

    private const long SegmentUnitSize = 64L << 20;

    private const int MaxSegmentsPerFile = 16;

    private const string SegmentSuffix = ".seg";

    private const int SegmentWriteBlockSize = 2 << 20;

    private const long SegmentSlowRateBytesPerSec = 48 * 1024;

    private readonly SemaphoreSlim _diskWriteGate = new(1, 1);

    private const int MaxAttempts = 3;

    private static readonly TimeSpan ReadStallTimeout = TimeSpan.FromSeconds(45);

    private const int SlowRateWindowMs = 20_000;

    private const long SlowRateBytesPerSec = 256 * 1024;

    private readonly string _baseUrl;

    private readonly string? _fallbackBaseUrl;
    private readonly string _destDir;
    private readonly List<GameResourceEntry> _files;
    private readonly long _totalOverride;

    private DownloadChannelSet _channelSet;

    private readonly Func<CancellationToken, Task<DownloadChannelSet>>? _rebuildChannels;

    private readonly GameUpdateVerifiedLedger? _ledger;

    private int _cursor;

    private int _consecutiveStalls;

    private readonly ConcurrentDictionary<string, int> _stallCounts = new();

    private readonly SemaphoreSlim _rebuildGate = new(1, 1);

    private const int StallRebuildThreshold = 3;

    private static readonly ILogger _logger =
        AppConfig.GetLogger<GameUpdateDownloader>();

    private long _received;

    private long _newBytes;

    public GameUpdateDownloader(
        DownloadChannelSet channels, string baseUrl, string destDir, List<GameResourceEntry> files,
        long receivedBase = 0, long totalOverride = 0, string? fallbackBaseUrl = null,
        Func<CancellationToken, Task<DownloadChannelSet>>? rebuildChannels = null,
        GameUpdateVerifiedLedger? ledger = null)
    {
        _channelSet = channels;
        _rebuildChannels = rebuildChannels;
        _ledger = ledger;
        _baseUrl = baseUrl;
        _fallbackBaseUrl = fallbackBaseUrl;
        _destDir = destDir;
        _files = files;
        _totalOverride = totalOverride;
        _received = receivedBase + files.Sum(PartBytes);
    }

    private DownloadChannel[] OrderedChannels()
    {
        return Volatile.Read(ref _channelSet).Channels
            .Select((c, i) => (c, i, stalled: _stallCounts.ContainsKey(c.Host)))
            .OrderBy(t => t.stalled ? 1 : 0)
            .ThenBy(t => t.i)
            .Select(t => t.c)
            .ToArray();
    }

    private DownloadChannel PickChannel()
    {
        DownloadChannel[] channels = OrderedChannels();
        return channels[(int)((uint)Interlocked.Increment(ref _cursor) % (uint)channels.Length)];
    }

    private string BuildUrl(DownloadChannel channel, GameResourceEntry file)
    {
        return GameUpdateService.BuildFileUrl(channel.Host, file.FromFolder ?? _fallbackBaseUrl ?? _baseUrl, file.Dest);
    }

    private void NoteStall(string host)
    {
        int count = _stallCounts.AddOrUpdate(host, 1, (_, n) => n + 1);
        _logger.LogWarning("Host stalled {Count} time(s), sinking it in channel order: {Host}", count, host);
    }

    private async Task MaybeRebuildChannelsAsync(CancellationToken ct)
    {
        if (_rebuildChannels is null || Interlocked.Increment(ref _consecutiveStalls) < StallRebuildThreshold)
        {
            return;
        }
        await _rebuildGate.WaitAsync(ct);
        try
        {

            if (Volatile.Read(ref _consecutiveStalls) < StallRebuildThreshold)
            {
                return;
            }
            _logger.LogWarning("Download stalled {Count} times in a row, re-probing download channels",
                Volatile.Read(ref _consecutiveStalls));
            try
            {
                Volatile.Write(ref _channelSet, await _rebuildChannels(ct));
                _stallCounts.Clear();
                Interlocked.Exchange(ref _consecutiveStalls, 0);
                _logger.LogInformation("Download channels re-probed: {Channels}",
                    string.Join(", ", Volatile.Read(ref _channelSet).Channels
                        .Select(c => $"{c.Label}={c.ScoreBytesPerSec / 1048576.0:0.0}MB/s")));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Re-probing download channels failed; keeping current channels");
            }
        }
        finally
        {
            _rebuildGate.Release();
        }
    }

    private static int SegmentCountFor(long size)
    {
        if (size < SegmentUnitSize * 2)
        {
            return 1;
        }
        return (int)Math.Min(MaxSegmentsPerFile, size / SegmentUnitSize);
    }

    private static (long Start, long End) SegmentRange(long size, int segCount, int index)
    {
        long segLen = (size + segCount - 1) / segCount;
        long start = (long)index * segLen;
        return (start, Math.Min(size, start + segLen) - 1);
    }

    private string SegPath(GameResourceEntry file, int index)
        => Path.Combine(_destDir, file.Dest) + TempSuffix + SegmentSuffix + index.ToString();

    private static long SegmentBytes(string segPath, long expected)
    {
        try
        {
            return File.Exists(segPath) ? Math.Min(new FileInfo(segPath).Length, expected) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static bool SegmentComplete(string segPath, long expected)
    {
        try
        {
            return File.Exists(segPath) && new FileInfo(segPath).Length == expected;
        }
        catch
        {
            return false;
        }
    }

    private long SegmentBytesTotal(GameResourceEntry file, int segCount)
    {
        long total = 0;
        for (int i = 0; i < segCount; i++)
        {
            (long start, long end) = SegmentRange(file.Size, segCount, i);
            total += SegmentBytes(SegPath(file, i), end - start + 1);
        }
        return total;
    }

    private long PartBytes(GameResourceEntry file)
    {
        try
        {
            int segCount = SegmentCountFor(file.Size);
            if (segCount > 1)
            {
                long segmented = SegmentBytesTotal(file, segCount);
                if (segmented > 0)
                {
                    return segmented;
                }
            }
            string temp = Path.Combine(_destDir, file.Dest) + TempSuffix;
            return File.Exists(temp) ? Math.Min(new FileInfo(temp).Length, file.Size) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private void DropLegacySegmentState(GameResourceEntry file)
    {
        string temp = Path.Combine(_destDir, file.Dest) + TempSuffix;
        string sidecar = temp + ".s";
        if (!File.Exists(sidecar))
        {
            return;
        }
        long len = 0;
        try
        {
            len = File.Exists(temp) ? Math.Min(new FileInfo(temp).Length, file.Size) : 0;
        }
        catch
        {

        }
        try { File.Delete(temp); } catch { }
        try { File.Delete(sidecar); } catch { }
        if (len > 0)
        {

            Interlocked.Add(ref _received, -len);
            _logger.LogInformation(
                "Discarding stale segmented leftover (preallocated shell), re-downloading: {Dest} ({Bytes} bytes)", file.Dest, len);
        }
    }

    public async Task<long> DownloadAsync(IProgress<GameDownloadProgress>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(_destDir);
        long totalBytes = _totalOverride > 0 ? _totalOverride : _files.Sum(f => f.Size);
        int completed = 0;
        var speedMeter = new SpeedMeter();
        var throttle = new Stopwatch();
        throttle.Start();

        void Report(string? currentFile, bool force = false)
        {
            if (progress == null)
            {
                return;
            }

            if (!force && throttle.ElapsedMilliseconds < 120)
            {
                return;
            }
            throttle.Restart();
            progress.Report(new GameDownloadProgress
            {
                Phase = DownloadPhase.Downloading,
                ReceivedBytes = Interlocked.Read(ref _received),
                TotalBytes = totalBytes,
                SpeedBytesPerSec = speedMeter.Speed,
                CurrentFile = currentFile,
                CompletedFiles = Volatile.Read(ref completed),
                TotalFiles = _files.Count,
            });
        }

        using var reporterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var reporter = Task.Run(async () =>
        {
            try
            {
                while (!reporterCts.IsCancellationRequested)
                {
                    Report(null);
                    await Task.Delay(200, reporterCts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }, CancellationToken.None);

        using var limiter = new SemaphoreSlim(_parallelism);

        var speedLimit = new SpeedLimiter();

        var tasks = _files.OrderByDescending(f => f.Size).Select(file => Task.Run(async () =>
        {

            DropLegacySegmentState(file);

            if (await AlreadyDoneAsync(file, ct))
            {
                Interlocked.Increment(ref completed);
                Report(Path.GetFileName(file.Dest), force: true);
                return;
            }

            int segCount = SegmentCountFor(file.Size);
            await WithRetryAsync(file.Dest, token => segCount > 1
                ? DownloadSegmentedAsync(file, segCount, limiter, speedMeter, speedLimit, token)
                : DownloadWholeAsync(file, limiter, speedMeter, speedLimit, token), ct);

            Interlocked.Exchange(ref _consecutiveStalls, 0);
            Interlocked.Increment(ref completed);
            Report(Path.GetFileName(file.Dest), force: true);
        }, ct));
        try
        {
            await Task.WhenAll(tasks);
        }
        finally
        {

            reporterCts.Cancel();
            _ledger?.Flush(force: true);
        }
        await reporter;
        Report(null, force: true);
        return Interlocked.Read(ref _newBytes);
    }

    private static async Task DrainOnPauseAsync(Stream source)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await source.CopyToAsync(Stream.Null, cts.Token).ConfigureAwait(false);
        }
        catch
        {

        }
    }


    private async Task WriteGatedAsync(Func<CancellationToken, ValueTask> write, CancellationTokenSource stallCts)
    {

        await _diskWriteGate.WaitAsync(stallCts.Token);
        try
        {
            stallCts.CancelAfter(ReadStallTimeout);
            await write(stallCts.Token);
        }
        finally
        {
            _diskWriteGate.Release();
        }
    }

    private async Task DownloadWholeAsync(GameResourceEntry file, SemaphoreSlim limiter, SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        string dest = Path.Combine(_destDir, file.Dest);
        string temp = dest + TempSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        await limiter.WaitAsync(ct);
        try
        {
            long existing = File.Exists(temp) ? new FileInfo(temp).Length : 0;

            DownloadChannel channel = PickChannel();
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(channel, file));
            if (existing > 0 && existing < file.Size)
            {
                request.Headers.Range = new RangeHeaderValue(existing, null);
            }

            using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            stallCts.CancelAfter(ReadStallTimeout);
            HttpResponseMessage resp;
            var swHdr = Stopwatch.StartNew();
            try
            {
                resp = await channel.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                NoteStall(channel.Host);
                throw new StallException($"Download connect timeout ({ReadStallTimeout.TotalSeconds:0}s no response): {file.Dest}");
            }
            if (Interlocked.Increment(ref _diagLogged) <= 30)
            {
                _logger.LogInformation("HTTP {Status} len={Len} hdr={Ms}ms {Url} existing={Existing}",
                    (int)resp.StatusCode, resp.Content.Headers.ContentLength, swHdr.ElapsedMilliseconds,
                    request.RequestUri, existing);
            }
            using (resp)
            {
                resp.EnsureSuccessStatusCode();
                bool resuming = existing > 0 && resp.StatusCode == HttpStatusCode.PartialContent;
                if (!resuming && File.Exists(temp))
                {

                    File.Delete(temp);
                    Interlocked.Add(ref _received, -existing);
                    existing = 0;
                }

                IncrementalHash? inlineMd5 = !resuming && file.Md5 is not null
                    ? IncrementalHash.CreateHash(HashAlgorithmName.MD5)
                    : null;
                long downloadedNow;

                await using (var source = await resp.Content.ReadAsStreamAsync(ct))
                await using (var output = new FileStream(temp, resuming ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, ReadBlockSize, FileOptions.Asynchronous))
                {
                    downloadedNow = await PumpAsync(source, output, channel, new byte[ReadBlockSize],
                        speedMeter, speedLimit, SlowRateBytesPerSec, inlineMd5, stallCts, file.Dest, ct);
                }

            if (new FileInfo(temp).Length != file.Size)
            {
                throw new InvalidDataException($"Download size mismatch: {file.Dest} (expected {file.Size}, actual {new FileInfo(temp).Length})");
            }
            if (file.Md5 is not null &&
                !(inlineMd5 is not null ? Md5Matches(inlineMd5.GetHashAndReset(), file.Md5) : VerifyMd5(temp, file.Md5, ct)))
            {

                long bad = new FileInfo(temp).Length;
                File.Delete(temp);
                Interlocked.Add(ref _received, -bad);
                Interlocked.Add(ref _newBytes, -downloadedNow);
                throw new InvalidDataException($"Download MD5 mismatch: {file.Dest}");
            }
            if (File.Exists(dest))
            {
                File.Delete(dest);
            }
            File.Move(temp, dest);

            _ledger?.Record(file.Dest, dest, file.Size, file.Md5);
            }
        }
        finally
        {
            limiter.Release();
        }
    }

    private async Task<long> PumpAsync(
        Stream source, FileStream output, DownloadChannel channel, byte[] buffer,
        SpeedMeter speedMeter, SpeedLimiter speedLimit, long slowRateBytesPerSec,
        IncrementalHash? inlineMd5, CancellationTokenSource stallCts, string label, CancellationToken ct)
    {
        long written = 0;
        long windowBytes = 0;
        long windowStart = Stopwatch.GetTimestamp();
        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer, stallCts.Token)) > 0)
            {
                await speedLimit.WaitAsync(read, ct);
                await WriteGatedAsync(token => output.WriteAsync(buffer.AsMemory(0, read), token), stallCts);
                inlineMd5?.AppendData(buffer.AsSpan(0, read));
                written += read;
                Interlocked.Add(ref _received, read);
                Interlocked.Add(ref _newBytes, read);
                speedMeter.Add(read);
                stallCts.CancelAfter(ReadStallTimeout);
                windowBytes += read;
                long windowMs = (Stopwatch.GetTimestamp() - windowStart) * 1000 / Stopwatch.Frequency;
                if (windowMs >= SlowRateWindowMs)
                {
                    long windowRate = windowBytes * 1000 / windowMs;
                    if (windowRate < slowRateBytesPerSec)
                    {
                        NoteStall(channel.Host);
                        throw new StallException(
                            $"Download too slow ({windowRate / 1024.0:0}KB/s for {SlowRateWindowMs / 1000:0}s): {label}",
                            slowStream: true);
                    }
                    windowBytes = 0;
                    windowStart = Stopwatch.GetTimestamp();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {

            await DrainOnPauseAsync(source);
            throw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            NoteStall(channel.Host);
            throw new StallException(
                $"Download stall timeout ({ReadStallTimeout.TotalSeconds:0}s no data): {label} (received {written} bytes)");
        }
        return written;
    }

    private async Task<bool> AlreadyDoneAsync(GameResourceEntry file, CancellationToken ct)
    {
        string dest = Path.Combine(_destDir, file.Dest);
        if (!File.Exists(dest) || !FileLengthMatches(dest, file.Size))
        {
            return false;
        }
        bool ok = _ledger?.IsVerified(file.Dest, dest, file.Size, file.Md5) == true
            || await Task.Run(() => VerifyMd5(dest, file.Md5, ct), ct);
        if (ok)
        {
            _ledger?.Record(file.Dest, dest, file.Size, file.Md5);
        }
        return ok;
    }

    private async Task WithRetryAsync(string label, Func<CancellationToken, Task> action, CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await action(ct);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RangeUnsupportedException)
            {

                throw;
            }
            catch (StallException ex) when (attempt < MaxAttempts || ex.SlowStream)
            {
                _logger.LogWarning(ex, "Download stalled, switching host and retry {Attempt}: {Label}", attempt, label);
                await MaybeRebuildChannelsAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt, 3)), ct);
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Download failed, retry {Attempt}/{Max}: {Label}", attempt, MaxAttempts - 1, label);
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
        }
    }

    private async Task DownloadSegmentedAsync(
        GameResourceEntry file, int segCount, SemaphoreSlim limiter,
        SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        string dest = Path.Combine(_destDir, file.Dest);
        string temp = dest + TempSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        if (File.Exists(temp) && new FileInfo(temp).Length > file.Size)
        {
            File.Delete(temp);
        }

        var segments = new Task[segCount];
        for (int index = 0; index < segCount; index++)
        {
            int i = index;
            segments[i] = Task.Run(async () =>
            {
                (long start, long end) = SegmentRange(file.Size, segCount, i);
                string segPath = SegPath(file, i);
                long expected = end - start + 1;

                if (SegmentComplete(segPath, expected))
                {
                    return;
                }

                await limiter.WaitAsync(ct);
                try
                {
                    await WithRetryAsync($"{file.Dest} [seg {i + 1}/{segCount}]",
                        token => DownloadSegmentOnceAsync(file, i, start, end, segPath, expected,
                            speedMeter, speedLimit, token), ct);
                }
                finally
                {
                    limiter.Release();
                }
            }, ct);
        }

        try
        {
            await Task.WhenAll(segments);
        }
        catch (RangeUnsupportedException)
        {

            long staged = SegmentBytesTotal(file, segCount);
            CleanupSegments(file, segCount);
            Interlocked.Add(ref _received, -staged);
            _logger.LogWarning("Server does not honor Range, falling back to single stream: {Dest}", file.Dest);
            await DownloadWholeAsync(file, limiter, speedMeter, speedLimit, ct);
            return;
        }

        await MergeSegmentsAsync(file, segCount, temp, speedMeter, ct);
    }

    private async Task DownloadSegmentOnceAsync(
        GameResourceEntry file, int index, long start, long end, string segPath, long expected,
        SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        long raw;
        try
        {
            raw = File.Exists(segPath) ? new FileInfo(segPath).Length : 0;
        }
        catch
        {
            raw = 0;
        }
        if (raw > expected)
        {

            try { File.Delete(segPath); } catch { }
            Interlocked.Add(ref _received, -expected);
            raw = 0;
        }
        if (raw >= expected)
        {
            return;
        }
        long have = raw;

        DownloadChannel channel = PickChannel();
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(channel, file));

        request.Headers.Range = new RangeHeaderValue(start + have, end);

        using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stallCts.CancelAfter(ReadStallTimeout);
        HttpResponseMessage resp;
        var swHdr = Stopwatch.StartNew();
        try
        {
            resp = await channel.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            NoteStall(channel.Host);
            throw new StallException(
                $"Segment connect timeout ({ReadStallTimeout.TotalSeconds:0}s no response): {file.Dest} [seg {index + 1}]");
        }
        if (Interlocked.Increment(ref _diagLogged) <= 30)
        {
            _logger.LogInformation("HTTP {Status} len={Len} hdr={Ms}ms {Url} seg={Seg} at={At}",
                (int)resp.StatusCode, resp.Content.Headers.ContentLength, swHdr.ElapsedMilliseconds,
                request.RequestUri, index + 1, have);
        }
        using (resp)
        {
            if (resp.StatusCode != HttpStatusCode.PartialContent)
            {

                resp.EnsureSuccessStatusCode();
                throw new RangeUnsupportedException($"Server ignored Range (HTTP {resp.StatusCode}): {file.Dest}");
            }

            await using (var source = await resp.Content.ReadAsStreamAsync(ct))
            await using (var output = new FileStream(segPath, FileMode.Append, FileAccess.Write, FileShare.None,
                SegmentWriteBlockSize, FileOptions.Asynchronous))
            {
                await PumpAsync(source, output, channel, new byte[SegmentWriteBlockSize],
                    speedMeter, speedLimit, SegmentSlowRateBytesPerSec, inlineMd5: null, stallCts,
                    $"{file.Dest} [seg {index + 1}]", ct);
            }
        }

        long got = SegmentBytes(segPath, expected);
        if (got < expected)
        {
            throw new InvalidDataException(
                $"Segment short: {file.Dest} [seg {index + 1}] (expected {expected}, got {got})");
        }
    }

    private async Task MergeSegmentsAsync(
        GameResourceEntry file, int segCount, string temp, SpeedMeter speedMeter, CancellationToken ct)
    {

        long staged = SegmentBytesTotal(file, segCount);
        if (staged != file.Size)
        {
            throw new InvalidDataException(
                $"Segment coverage mismatch: {file.Dest} (expected {file.Size}, staged {staged})");
        }

        long merged = 0;
        try
        {
            merged = File.Exists(temp) ? new FileInfo(temp).Length : 0;
        }
        catch
        {
            merged = 0;
        }
        if (merged > file.Size)
        {
            File.Delete(temp);
            merged = 0;
        }

        bool inline = merged == 0 && file.Md5 is not null;
        var buffer = new byte[SegmentWriteBlockSize];
        using IncrementalHash? md5 = inline ? IncrementalHash.CreateHash(HashAlgorithmName.MD5) : null;

        await using (var output = new FileStream(temp, merged > 0 ? FileMode.Append : FileMode.Create,
            FileAccess.Write, FileShare.None, SegmentWriteBlockSize, FileOptions.Asynchronous))
        {
            long prefix = 0;
            for (int i = 0; i < segCount; i++)
            {
                (long start, long end) = SegmentRange(file.Size, segCount, i);
                string segPath = SegPath(file, i);
                long segLen = end - start + 1;
                long segEnd = prefix + segLen;
                if (segEnd <= merged)
                {

                    prefix = segEnd;
                    continue;
                }

                long offset = Math.Max(0, merged - prefix);
                long want = segLen - offset;
                long copied = 0;
                await using (var input = new FileStream(segPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    SegmentWriteBlockSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    if (offset > 0)
                    {
                        input.Seek(offset, SeekOrigin.Begin);
                    }
                    int read;
                    while ((read = await input.ReadAsync(buffer, ct)) > 0)
                    {
                        md5?.AppendData(buffer, 0, read);
                        int captured = read;

                        await _diskWriteGate.WaitAsync(ct);
                        try
                        {
                            await output.WriteAsync(buffer.AsMemory(0, captured), ct);
                        }
                        finally
                        {
                            _diskWriteGate.Release();
                        }

                        speedMeter.Add(captured);
                        copied += read;
                        ct.ThrowIfCancellationRequested();
                    }
                }
                if (copied != want)
                {

                    throw new InvalidDataException(
                        $"Segment truncated while merging: {file.Dest} [seg {i + 1}] (want {want}, got {copied})");
                }

                await output.FlushAsync(ct);
                try { File.Delete(segPath); } catch { }
                prefix = segEnd;
                merged = segEnd;
            }
        }

        if (new FileInfo(temp).Length != file.Size)
        {
            throw new InvalidDataException($"Merged size mismatch: {file.Dest}");
        }
        bool md5Ok = file.Md5 is null
            || (md5 is not null
                ? Md5Matches(md5.GetHashAndReset(), file.Md5)
                : VerifyMd5(temp, file.Md5, ct));
        if (!md5Ok)
        {

            File.Delete(temp);
            CleanupSegments(file, segCount);
            Interlocked.Add(ref _received, -file.Size);
            throw new InvalidDataException($"Download MD5 mismatch: {file.Dest}");
        }

        string dest = Path.Combine(_destDir, file.Dest);
        if (File.Exists(dest))
        {
            File.Delete(dest);
        }
        File.Move(temp, dest);
        CleanupSegments(file, segCount);

        _ledger?.Record(file.Dest, dest, file.Size, file.Md5);
    }

    private void CleanupSegments(GameResourceEntry file, int segCount)
    {
        for (int i = 0; i < segCount; i++)
        {
            try
            {
                File.Delete(SegPath(file, i));
            }
            catch
            {

            }
        }
    }

    private sealed class StallException : IOException
    {
        public StallException(string message, bool slowStream = false) : base(message)
        {
            SlowStream = slowStream;
        }

        public bool SlowStream { get; }
    }

    private sealed class RangeUnsupportedException : IOException
    {
        public RangeUnsupportedException(string message) : base(message) { }
    }

    private static bool Md5Matches(byte[] hash, string expected)
    {
        return string.Equals(Convert.ToHexStringLower(hash), expected, StringComparison.OrdinalIgnoreCase);
    }

    public static bool VerifyMd5(string path, string? expectedMd5, CancellationToken ct = default, Action<long>? onHashed = null)
    {
        if (expectedMd5 is null)
        {
            return true;
        }
        try
        {
            using var stream = File.OpenRead(path);
            using var md5 = MD5.Create();
            var buffer = new byte[1 << 20];
            long hashed = 0;
            int read;
            ct.ThrowIfCancellationRequested();
            while ((read = stream.Read(buffer)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                md5.TransformBlock(buffer, 0, read, null, 0);
                hashed += read;
                onHashed?.Invoke(hashed);
            }
            md5.TransformFinalBlock([], 0, 0);
            string actual = Convert.ToHexStringLower(md5.Hash!);
            return string.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public static bool FileLengthMatches(string path, long expectedSize)
    {
        if (expectedSize <= 0)
        {
            return true;
        }
        try
        {
            return new FileInfo(path).Length == expectedSize;
        }
        catch
        {
            return false;
        }
    }

    internal sealed class SpeedLimiter
    {
        private long _tokens;
        private long _lastStamp = Stopwatch.GetTimestamp();

        public async ValueTask WaitAsync(int bytes, CancellationToken ct)
        {
            long bytesPerSec = GameUpdateService.DownloadSpeedLimitBytesPerSec;
            if (bytesPerSec <= 0 || bytes <= 0)
            {
                return;
            }
            while (true)
            {
                long delayMs;
                lock (this)
                {
                    long now = Stopwatch.GetTimestamp();
                    _tokens += (long)((now - _lastStamp) / (double)Stopwatch.Frequency * bytesPerSec);
                    _lastStamp = now;
                    if (_tokens > bytesPerSec)
                    {
                        _tokens = bytesPerSec;
                    }
                    if (_tokens >= bytes)
                    {
                        _tokens -= bytes;
                        return;
                    }
                    delayMs = Math.Max(1, (bytes - _tokens) * 1000 / bytesPerSec);
                }
                await Task.Delay((int)Math.Min(delayMs, 1000), ct);
            }
        }
    }

    private sealed class SpeedMeter
    {
        private const int SampleMs = 200;
        private const int Slots = 15;

        private readonly long[] _slots = new long[Slots];
        private readonly object _gate = new();
        private long _pending;
        private long _stamp = Stopwatch.GetTimestamp();
        private int _cursor;
        private long _speed;

        public long Speed
        {
            get
            {
                lock (_gate)
                {
                    long idleMs = (Stopwatch.GetTimestamp() - _stamp) * 1000 / Stopwatch.Frequency;
                    return idleMs >= Slots * SampleMs ? 0 : _speed;
                }
            }
        }

        public void Add(long bytes)
        {
            lock (_gate)
            {
                _pending += bytes;
                long now = Stopwatch.GetTimestamp();
                long elapsedMs = (now - _stamp) * 1000 / Stopwatch.Frequency;
                if (elapsedMs < SampleMs)
                {
                    return;
                }

                int steps = (int)Math.Min(elapsedMs / SampleMs, Slots);
                for (int i = 0; i < steps - 1; i++)
                {
                    _slots[_cursor] = 0;
                    _cursor = (_cursor + 1) % Slots;
                }
                _slots[_cursor] = _pending;
                _cursor = (_cursor + 1) % Slots;
                _pending = 0;
                _stamp = now;

                long sum = 0;
                foreach (long slot in _slots)
                {
                    sum += slot;
                }
                _speed = sum * 1000 / (Slots * SampleMs);
            }
        }
    }
}
