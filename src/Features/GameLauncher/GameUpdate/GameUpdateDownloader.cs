using System;
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

    public string? CurrentFile { get; init; }

    public int CompletedFiles { get; init; }

    public int TotalFiles { get; init; }
}

public sealed class GameUpdateDownloader
{

    private readonly int _parallelism = GameUpdateService.DownloadThreads;

    private const long SegmentThreshold = 64L << 20;

    private readonly int _maxSegments = GameUpdateService.DownloadThreads <= 1 ? 1 : 4;

    private const long MinSegmentSize = 16L << 20;

    private const long SegmentFlushBytes = 4L << 20;

    private const string TempSuffix = ".part";

    private readonly SemaphoreSlim _diskWriteGate = new(1, 1);

    private const int MaxAttempts = 3;

    private static readonly TimeSpan ReadStallTimeout = TimeSpan.FromSeconds(60);

    private readonly string _cdn;
    private readonly string _baseUrl;

    private readonly string? _fallbackBaseUrl;
    private readonly string _destDir;
    private readonly List<GameResourceEntry> _files;
    private readonly long _totalOverride;

    private static readonly ILogger _logger =
        AppConfig.GetLogger<GameUpdateDownloader>();

    private long _received;

    private long _newBytes;

    public GameUpdateDownloader(
        string cdn, string baseUrl, string destDir, List<GameResourceEntry> files,
        long receivedBase = 0, long totalOverride = 0, string? fallbackBaseUrl = null)
    {
        _cdn = cdn;
        _baseUrl = baseUrl;
        _fallbackBaseUrl = fallbackBaseUrl;
        _destDir = destDir;
        _files = files;
        _totalOverride = totalOverride;
        _received = receivedBase + files.Sum(PartBytes);
    }

    private long PartBytes(GameResourceEntry file)
    {
        try
        {
            string temp = Path.Combine(_destDir, file.Dest) + TempSuffix;
            int segCount = SegmentCount(file.Size);
            if (segCount > 1)
            {
                return ReadSegmentProgress(temp + ".s", segCount, file.Size)?.Sum() ?? 0;
            }
            return File.Exists(temp) ? Math.Min(new FileInfo(temp).Length, file.Size) : 0;
        }
        catch
        {
            return 0;
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

                    Interlocked.Add(ref _received, -DeleteSegmentState(file));
                    segmented = false;
                    _logger.LogWarning(ex, "{Dest} 服务器不支持 Range，改回单流下载", file.Dest);
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    _logger.LogWarning(ex, "下载失败，重试 {Attempt}/{Max}：{Dest}", attempt, MaxAttempts - 1, file.Dest);
                    await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                }
            }
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

            string baseUrl = file.FromFolder ?? _fallbackBaseUrl ?? _baseUrl;
            string url = GameUpdateService.BuildFileUrl(_cdn, baseUrl, file.Dest);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existing > 0 && existing < file.Size)
            {
                request.Headers.Range = new RangeHeaderValue(existing, null);
            }

            using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            stallCts.CancelAfter(ReadStallTimeout);
            HttpResponseMessage resp;
            try
            {
                resp = await GameUpdateService.SharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new IOException($"下载连接超时（{ReadStallTimeout.TotalSeconds:0}s 无响应）：{file.Dest}");
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

                long downloadedNow = 0;
                await using (var source = await resp.Content.ReadAsStreamAsync(ct))
                await using (var output = new FileStream(temp, resuming ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
                {
                    var buffer = new byte[1 << 16];
                    int read;
                    try
                    {
                        while ((read = await source.ReadAsync(buffer, stallCts.Token)) > 0)
                        {
                            await speedLimit.WaitAsync(read, ct);
                            await WriteGatedAsync(() => output.WriteAsync(buffer.AsMemory(0, read), ct), stallCts, ct);
                            downloadedNow += read;
                            Interlocked.Add(ref _received, read);
                            Interlocked.Add(ref _newBytes, read);
                            speedMeter.Add(read);
                            stallCts.CancelAfter(ReadStallTimeout);
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        throw new IOException($"下载停滞超时（{ReadStallTimeout.TotalSeconds:0}s 无数据）：{file.Dest}（已收 {downloadedNow} 字节）");
                    }
                }

            if (new FileInfo(temp).Length != file.Size)
            {
                throw new InvalidDataException($"下载大小不符：{file.Dest}（期望 {file.Size}，实际 {new FileInfo(temp).Length}）");
            }
            if (file.Md5 is not null && !VerifyMd5(temp, file.Md5, ct))
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
        string sidecar = temp + ".s";
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        if (File.Exists(dest) && FileLengthMatches(dest, file.Size) && VerifyMd5(dest, file.Md5, ct))
        {
            try { File.Delete(sidecar); } catch { }
            return;
        }

        int segCount = SegmentCount(file.Size);
        long segSize = file.Size / segCount;
        long[]? loaded = ReadSegmentProgress(sidecar, segCount, file.Size);
        if (loaded == null || !File.Exists(temp) || new FileInfo(temp).Length != file.Size)
        {

            Interlocked.Add(ref _received, -(loaded?.Sum() ?? 0));
            try { File.Delete(temp); } catch { }
            try { File.Delete(sidecar); } catch { }
            loaded = new long[segCount];
            using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(file.Size);
            }
        }
        long[] progress = loaded;
        long startDone = progress.Sum();

        var segments = new Task[segCount];
        for (int i = 0; i < segCount; i++)
        {
            int index = i;
            long from = index * segSize;
            long to = index == segCount - 1 ? file.Size : from + segSize;
            segments[index] = Task.Run(async () =>
            {
                long done = Math.Min(progress[index], to - from);
                if (done >= to - from)
                {
                    return;
                }
                await limiter.WaitAsync(ct);
                try
                {
                    await DownloadRangeAsync(file, temp, sidecar, index, from + done, to, progress, speedMeter, speedLimit, ct);
                }
                finally
                {
                    limiter.Release();
                }
            }, ct);
        }
        await Task.WhenAll(segments);

        for (int i = 0; i < segCount; i++)
        {
            long len = i == segCount - 1 ? file.Size - (segCount - 1) * segSize : segSize;
            if (progress[i] < len)
            {
                throw new InvalidDataException($"分段下载不完整：{file.Dest}（段 {i} 缺 {len - progress[i]} 字节）");
            }
        }
        if (file.Md5 is not null && !VerifyMd5(temp, file.Md5, ct))
        {

            File.Delete(temp);
            File.Delete(sidecar);
            Interlocked.Add(ref _received, -file.Size);
            Interlocked.Add(ref _newBytes, -(file.Size - startDone));
            throw new InvalidDataException($"下载 MD5 校验失败：{file.Dest}");
        }
        File.Delete(sidecar);
        if (File.Exists(dest))
        {
            File.Delete(dest);
        }
        File.Move(temp, dest);
    }

    private async Task DownloadRangeAsync(GameResourceEntry file, string temp, string sidecar, int segIndex,
        long from, long to, long[] progress, SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        string baseUrl = file.FromFolder ?? _fallbackBaseUrl ?? _baseUrl;
        string url = GameUpdateService.BuildFileUrl(_cdn, baseUrl, file.Dest);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(from, to - 1);

        using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        stallCts.CancelAfter(ReadStallTimeout);
        HttpResponseMessage resp;
        try
        {
            resp = await GameUpdateService.SharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stallCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new IOException($"下载连接超时（{ReadStallTimeout.TotalSeconds:0}s 无响应）：{file.Dest}");
        }
        using (resp)
        {
            if (resp.StatusCode != HttpStatusCode.PartialContent)
            {

                throw new RangeUnsupportedException($"服务器不认 Range（HTTP {(int)resp.StatusCode}）：{file.Dest}");
            }
            await using var source = await resp.Content.ReadAsStreamAsync(ct);

            await using var output = new FileStream(temp, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 1 << 16);
            output.Seek(from, SeekOrigin.Begin);

            var buffer = new byte[1 << 16];
            long pending = 0;
            int read;
            try
            {
                while ((read = await source.ReadAsync(buffer, stallCts.Token)) > 0)
                {
                    await speedLimit.WaitAsync(read, ct);
                    await WriteGatedAsync(() => output.WriteAsync(buffer.AsMemory(0, read), ct), stallCts, ct);
                    Interlocked.Add(ref _received, read);
                    Interlocked.Add(ref _newBytes, read);
                    speedMeter.Add(read);
                    progress[segIndex] += read;
                    pending += read;
                    if (pending >= SegmentFlushBytes)
                    {
                        WriteSegmentProgress(sidecar, progress);
                        pending = 0;
                    }
                    stallCts.CancelAfter(ReadStallTimeout);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new IOException($"下载停滞超时（{ReadStallTimeout.TotalSeconds:0}s 无数据）：{file.Dest}（段 {segIndex} 已收 {progress[segIndex]} 字节）");
            }
            WriteSegmentProgress(sidecar, progress);
        }
    }

    private int SegmentCount(long size)
    {
        return size >= SegmentThreshold && _maxSegments >= 2
            ? Math.Clamp((int)(size / MinSegmentSize), 2, _maxSegments)
            : 1;
    }

    private static long[]? ReadSegmentProgress(string sidecar, int segCount, long fileSize)
    {
        try
        {
            if (!File.Exists(sidecar))
            {
                return null;
            }
            byte[] bytes = File.ReadAllBytes(sidecar);
            if (bytes.Length != segCount * sizeof(long))
            {
                return null;
            }
            long segSize = fileSize / segCount;
            var progress = new long[segCount];
            for (int i = 0; i < segCount; i++)
            {
                long value = BitConverter.ToInt64(bytes, i * sizeof(long));
                long max = i == segCount - 1 ? fileSize - (segCount - 1) * segSize : segSize;
                if (value < 0 || value > max)
                {
                    return null;
                }
                progress[i] = value;
            }
            return progress;
        }
        catch
        {
            return null;
        }
    }

    private static void WriteSegmentProgress(string sidecar, long[] progress)
    {
        try
        {
            var bytes = new byte[progress.Length * sizeof(long)];
            for (int i = 0; i < progress.Length; i++)
            {
                BitConverter.GetBytes(progress[i]).CopyTo(bytes, i * sizeof(long));
            }
            lock (progress)
            {
                File.WriteAllBytes(sidecar, bytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "段进度落盘失败：{Sidecar}", sidecar);
        }
    }

    private long DeleteSegmentState(GameResourceEntry file)
    {
        string temp = Path.Combine(_destDir, file.Dest) + TempSuffix;
        string sidecar = temp + ".s";
        long sum = ReadSegmentProgress(sidecar, SegmentCount(file.Size), file.Size)?.Sum() ?? 0;
        try { File.Delete(temp); } catch { }
        try { File.Delete(sidecar); } catch { }
        return sum;
    }

    private sealed class RangeUnsupportedException : IOException
    {
        public RangeUnsupportedException(string message) : base(message) { }
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
