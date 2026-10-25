using System;
using System.Buffers;
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
using System.Threading.Channels;
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

    public string? CurrentFile { get; init; }

    public int CompletedFiles { get; init; }

    public int TotalFiles { get; init; }
}

public sealed class GameUpdateDownloader
{

    private readonly int _parallelism = GameUpdateService.DownloadThreads;

    private const long SegmentThreshold = 64L << 20;

    private readonly int _maxSegments = Math.Clamp(GameUpdateService.DownloadThreads, 1, 16);

    private const long MinSegmentSize = 16L << 20;

    private const int ReadBlockSize = 1 << 20;

    private const long MinStripeChunkSize = 8L << 20;

    private const long MaxStripeChunkSize = 64L << 20;

    private const int TargetBlocksPerSegment = 12;

    private const int SegmentBufferBlocks = 2;

    private const long WriteBurstBytes = 8L << 20;

    private const string TempSuffix = ".part";

    private readonly SemaphoreSlim _diskWriteGate = new(1, 1);

    private const int MaxAttempts = 3;

    private static readonly TimeSpan ReadStallTimeout = TimeSpan.FromSeconds(60);

    private readonly string _baseUrl;

    private readonly string? _fallbackBaseUrl;
    private readonly string _destDir;
    private readonly List<GameResourceEntry> _files;
    private readonly long _totalOverride;

    private DownloadChannelSet _channelSet;

    private readonly Func<CancellationToken, Task<DownloadChannelSet>>? _rebuildChannels;

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
        Func<CancellationToken, Task<DownloadChannelSet>>? rebuildChannels = null)
    {
        _channelSet = channels;
        _rebuildChannels = rebuildChannels;
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

    private DownloadChannel[] AssignSegments(GameResourceEntry file, int segCount)
    {
        DownloadChannel[] ranked = OrderedChannels();
        int use = Math.Min(ranked.Length, segCount);
        DownloadChannel[] top = ranked.Take(use).ToArray();
        double[] credit = top.Select(c => Math.Max(c.ScoreBytesPerSec, 1)).ToArray();
        int[] taken = new int[use];
        var assignment = new DownloadChannel[segCount];
        for (int j = 0; j < segCount; j++)
        {
            int pick = 0;
            double best = double.NegativeInfinity;
            for (int k = 0; k < use; k++)
            {
                double score = credit[k] / (taken[k] + 1);
                if (score > best)
                {
                    best = score;
                    pick = k;
                }
            }
            taken[pick]++;
            assignment[j] = top[pick];
        }
        if (use > 1)
        {
            _logger.LogInformation("Swarm: {Dest}, {Segs} segments on {Hosts} hosts: {Alloc}",
                file.Dest, segCount, use, string.Join(", ", top.Select((c, i) => $"{c.Label}×{taken[i]}")));
        }
        return assignment;
    }

    private long PartBytes(GameResourceEntry file)
    {
        try
        {
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
                "丢弃旧版切段残件（预分配空壳），本次重新下载：{Dest}（{Bytes} 字节）", file.Dest, len);
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
            if (!force && throttle.ElapsedMilliseconds < 200)
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

            bool segmented = file.Size >= SegmentThreshold;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (segmented)
                    {
                        await DownloadSegmentedAsync(file, limiter, speedMeter, speedLimit, ct);
                    }
                    else
                    {
                        await DownloadWholeAsync(file, limiter, speedMeter, speedLimit, ct);
                    }
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (RangeUnsupportedException ex) when (segmented)
                {

                    segmented = false;
                    _logger.LogWarning(ex, "{Dest}: server does not support Range, falling back to single stream", file.Dest);
                }
                catch (StallException ex) when (attempt < MaxAttempts)
                {

                    _logger.LogWarning(ex, "Download stalled, switching host and retry {Attempt}/{Max}: {Dest}",
                        attempt, MaxAttempts - 1, file.Dest);
                    await MaybeRebuildChannelsAsync(ct);
                    await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    _logger.LogWarning(ex, "Download failed, retry {Attempt}/{Max}: {Dest}", attempt, MaxAttempts - 1, file.Dest);
                    await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                }
            }

            Interlocked.Exchange(ref _consecutiveStalls, 0);
            Interlocked.Increment(ref completed);
            Report(Path.GetFileName(file.Dest), force: true);
        }, ct));
        await Task.WhenAll(tasks);
        reporterCts.Cancel();
        await reporter;
        Report(null, force: true);
        return Interlocked.Read(ref _newBytes);
    }

    private async Task WriteGatedAsync(Func<ValueTask> write, CancellationTokenSource stallCts, CancellationToken ct)
    {
        await _diskWriteGate.WaitAsync(ct);
        try
        {
            stallCts.CancelAfter(ReadStallTimeout);
            await write();
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

        if (File.Exists(dest) && FileLengthMatches(dest, file.Size) && VerifyMd5(dest, file.Md5, ct))
        {
            return;
        }

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
            try
            {
                resp = await channel.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                NoteStall(channel.Host);
                throw new StallException($"下载连接超时（{ReadStallTimeout.TotalSeconds:0}s 无响应）：{file.Dest}");
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
                long downloadedNow = 0;
                await using (var source = await resp.Content.ReadAsStreamAsync(ct))

                await using (var output = new FileStream(temp, resuming ? FileMode.Append : FileMode.Create,
                    FileAccess.Write, FileShare.None, ReadBlockSize, FileOptions.Asynchronous))
                {
                    var buffer = new byte[ReadBlockSize];
                    int read;
                    try
                    {
                        while ((read = await source.ReadAsync(buffer, stallCts.Token)) > 0)
                        {
                            await speedLimit.WaitAsync(read, ct);
                            await WriteGatedAsync(() => output.WriteAsync(buffer.AsMemory(0, read), ct), stallCts, ct);
                            inlineMd5?.AppendData(buffer.AsSpan(0, read));
                            downloadedNow += read;
                            Interlocked.Add(ref _received, read);
                            Interlocked.Add(ref _newBytes, read);
                            speedMeter.Add(read);
                            stallCts.CancelAfter(ReadStallTimeout);
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        NoteStall(channel.Host);
                        throw new StallException($"下载停滞超时（{ReadStallTimeout.TotalSeconds:0}s 无数据）：{file.Dest}（已收 {downloadedNow} 字节）");
                    }
                }

            if (new FileInfo(temp).Length != file.Size)
            {
                throw new InvalidDataException($"下载大小不符：{file.Dest}（期望 {file.Size}，实际 {new FileInfo(temp).Length}）");
            }
            if (file.Md5 is not null &&
                !(inlineMd5 is not null ? Md5Matches(inlineMd5.GetHashAndReset(), file.Md5) : VerifyMd5(temp, file.Md5, ct)))
            {

                long bad = new FileInfo(temp).Length;
                File.Delete(temp);
                Interlocked.Add(ref _received, -bad);
                Interlocked.Add(ref _newBytes, -downloadedNow);
                throw new InvalidDataException($"下载 MD5 校验失败：{file.Dest}");
            }
            if (File.Exists(dest))
            {
                File.Delete(dest);
            }
            File.Move(temp, dest);
            }
        }
        finally
        {
            limiter.Release();
        }
    }

    private async Task DownloadSegmentedAsync(GameResourceEntry file, SemaphoreSlim limiter, SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        string dest = Path.Combine(_destDir, file.Dest);
        string temp = dest + TempSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        if (File.Exists(dest) && FileLengthMatches(dest, file.Size) && VerifyMd5(dest, file.Md5, ct))
        {
            return;
        }

        int segCount = SegmentCount(file.Size);
        long chunkSize = ComputeStripeChunkSize(file.Size, segCount);
        long blockCount = (file.Size + chunkSize - 1) / chunkSize;
        long initial = File.Exists(temp) ? new FileInfo(temp).Length : 0;
        if (initial > file.Size)
        {

            Interlocked.Add(ref _received, -initial);
            try { File.Delete(temp); } catch { }
            initial = 0;
        }

        long aligned = initial - initial % chunkSize;
        if (aligned != initial)
        {
            using (var fs = new FileStream(temp, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(aligned);
            }
            Interlocked.Add(ref _received, aligned - initial);
            initial = aligned;
        }
        long startBlock = initial / chunkSize;

        DownloadChannel[] segChannels = AssignSegments(file, segCount);

        var channels = new Channel<SegmentBlock>[segCount];
        for (int i = 0; i < segCount; i++)
        {
            channels[i] = Channel.CreateBounded<SegmentBlock>(new BoundedChannelOptions(SegmentBufferBlocks)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true,
            });
        }

        var writer = Task.Run(async () =>
        {
            try
            {
                return await WriteSequentiallyAsync(temp, file, segCount, startBlock, blockCount, chunkSize, channels, ct);
            }
            catch
            {

                foreach (var ch in channels)
                {
                    ch.Writer.TryComplete();
                }
                throw;
            }
        }, ct);

        var segments = new Task[segCount];
        for (int i = 0; i < segCount; i++)
        {
            int index = i;
            segments[index] = Task.Run(async () =>
            {
                await limiter.WaitAsync(ct);
                try
                {
                    await DownloadRangeAsync(segChannels[index], file, index, segCount, startBlock, blockCount, chunkSize,
                        channels[index], speedMeter, speedLimit, ct);
                }
                finally
                {

                    channels[index].Writer.TryComplete();
                    limiter.Release();
                }
            }, ct);
        }
        try
        {
            await Task.WhenAll(segments).ConfigureAwait(false);
        }
        catch
        {

            try { await writer.ConfigureAwait(false); } catch { }
            throw;
        }
        byte[]? inlineHash = await writer.ConfigureAwait(false);

        if (new FileInfo(temp).Length != file.Size)
        {
            throw new InvalidDataException($"分段下载大小不符：{file.Dest}（期望 {file.Size}，实际 {new FileInfo(temp).Length}）");
        }
        if (file.Md5 is not null &&
            !(inlineHash is not null ? Md5Matches(inlineHash, file.Md5) : VerifyMd5(temp, file.Md5, ct)))
        {

            File.Delete(temp);
            Interlocked.Add(ref _received, -file.Size);
            throw new InvalidDataException($"下载 MD5 校验失败：{file.Dest}");
        }
        if (File.Exists(dest))
        {
            File.Delete(dest);
        }
        File.Move(temp, dest);
    }

    private async Task<byte[]?> WriteSequentiallyAsync(string temp, GameResourceEntry file, int segCount,
        long startBlock, long blockCount, long chunkSize, Channel<SegmentBlock>[] channels, CancellationToken ct)
    {

        IncrementalHash? md5 = startBlock == 0 && file.Md5 is not null
            ? IncrementalHash.CreateHash(HashAlgorithmName.MD5)
            : null;

        FileStream? output = null;
        try
        {

            for (long b = startBlock; b < blockCount; b++)
            {
                int seg = (int)(b % segCount);
                long need = Math.Min(chunkSize, file.Size - b * chunkSize);
                long done = 0;
                while (done < need)
                {
                    SegmentBlock block;
                    try
                    {
                        block = await channels[seg].Reader.ReadAsync(ct).ConfigureAwait(false);
                    }
                    catch (ChannelClosedException)
                    {

                        throw new InvalidDataException($"分段下载不完整：{file.Dest}（块 {b} 缺 {need - done} 字节）");
                    }

                    output ??= new FileStream(temp, FileMode.Append, FileAccess.Write, FileShare.None,
                        ReadBlockSize, FileOptions.Asynchronous);

                    await _diskWriteGate.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        await output.WriteAsync(block.Buffer.AsMemory(0, block.Length), ct).ConfigureAwait(false);
                        md5?.AppendData(block.Buffer.AsSpan(0, block.Length));
                        ArrayPool<byte>.Shared.Return(block.Buffer);
                        done += block.Length;
                        Interlocked.Add(ref _received, block.Length);
                        Interlocked.Add(ref _newBytes, block.Length);

                        long burst = block.Length;
                        while (done < need && burst < WriteBurstBytes
                               && channels[seg].Reader.TryRead(out SegmentBlock extra))
                        {
                            await output.WriteAsync(extra.Buffer.AsMemory(0, extra.Length), ct).ConfigureAwait(false);
                            md5?.AppendData(extra.Buffer.AsSpan(0, extra.Length));
                            ArrayPool<byte>.Shared.Return(extra.Buffer);
                            done += extra.Length;
                            burst += extra.Length;
                            Interlocked.Add(ref _received, extra.Length);
                            Interlocked.Add(ref _newBytes, extra.Length);
                        }
                    }
                    finally
                    {
                        _diskWriteGate.Release();
                    }
                }
            }

            return md5?.GetHashAndReset();
        }
        finally
        {
            if (output is not null)
            {
                await output.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task DownloadRangeAsync(DownloadChannel ch, GameResourceEntry file, int segIndex, int segCount,
        long startBlock, long blockCount, long chunkSize, Channel<SegmentBlock> channel,
        SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {

        long first = startBlock + ((segIndex - startBlock % segCount) + segCount) % segCount;
        for (long b = first; b < blockCount; b += segCount)
        {
            long from = b * chunkSize;
            long len = Math.Min(chunkSize, file.Size - from);
            await DownloadBlockAsync(ch, file, segIndex, from, len, channel, speedMeter, speedLimit, ct);
        }
    }

    private async Task DownloadBlockAsync(DownloadChannel ch, GameResourceEntry file, int segIndex,
        long from, long len, Channel<SegmentBlock> channel,
        SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl(ch, file));
        request.Headers.Range = new RangeHeaderValue(from, from + len - 1);

        using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stallCts.CancelAfter(ReadStallTimeout);
        HttpResponseMessage resp;
        try
        {
            resp = await ch.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            NoteStall(ch.Host);
            throw new StallException($"下载连接超时（{ReadStallTimeout.TotalSeconds:0}s 无响应）：{file.Dest}");
        }
        using (resp)
        {
            if (resp.StatusCode != HttpStatusCode.PartialContent)
            {

                throw new RangeUnsupportedException($"服务器不认 Range（HTTP {(int)resp.StatusCode}）：{file.Dest}");
            }
            await using var source = await resp.Content.ReadAsStreamAsync(ct);
            long remaining = len;
            try
            {
                while (remaining > 0)
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(ReadBlockSize);

                    int want = (int)Math.Min(ReadBlockSize, remaining);
                    int read = await source.ReadAsync(buffer.AsMemory(0, want), stallCts.Token).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        break;
                    }
                    await speedLimit.WaitAsync(read, ct).ConfigureAwait(false);

                    await channel.Writer.WriteAsync(new SegmentBlock(buffer, read), ct).ConfigureAwait(false);
                    speedMeter.Add(read);
                    remaining -= read;
                    stallCts.CancelAfter(ReadStallTimeout);
                }
            }
            catch (ChannelClosedException)
            {

                return;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                NoteStall(ch.Host);
                throw new StallException($"下载停滞超时（{ReadStallTimeout.TotalSeconds:0}s 无数据）：{file.Dest}（段 {segIndex}）");
            }
        }
    }

    private static long ComputeStripeChunkSize(long fileSize, int segCount)
    {
        long raw = fileSize / (long)Math.Max(segCount * TargetBlocksPerSegment, 1);
        return Math.Clamp(raw, MinStripeChunkSize, MaxStripeChunkSize);
    }

    private int SegmentCount(long size)
    {
        return size >= SegmentThreshold && _maxSegments >= 2
            ? Math.Clamp((int)(size / MinSegmentSize), 2, _maxSegments)
            : 1;
    }

    private sealed class RangeUnsupportedException : IOException
    {
        public RangeUnsupportedException(string message) : base(message) { }
    }

    private sealed class StallException : IOException
    {
        public StallException(string message) : base(message) { }
    }

    private readonly record struct SegmentBlock(byte[] Buffer, int Length);

    private static bool Md5Matches(byte[] hash, string expected)
    {
        return string.Equals(Convert.ToHexStringLower(hash), expected, StringComparison.OrdinalIgnoreCase);
    }

    public static bool VerifyMd5(string path, string? expectedMd5, CancellationToken ct = default)
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
            int read;
            ct.ThrowIfCancellationRequested();
            while ((read = stream.Read(buffer)) > 0)
            {
                ct.ThrowIfCancellationRequested();
                md5.TransformBlock(buffer, 0, read, null, 0);
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
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _bytesInWindow;
        private long _speed;
        private readonly object _gate = new();

        public long Speed
        {
            get
            {
                lock (_gate)
                {

                    if (_clock.ElapsedMilliseconds > 3000)
                    {
                        _speed = 0;
                    }
                    return _speed;
                }
            }
        }

        public void Add(long bytes)
        {
            lock (_gate)
            {
                _bytesInWindow += bytes;
                if (_clock.ElapsedMilliseconds >= 1000)
                {
                    _speed = (long)(_bytesInWindow / _clock.Elapsed.TotalSeconds);
                    _bytesInWindow = 0;
                    _clock.Restart();
                }
            }
        }
    }
}
