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
    
    private const int Parallelism = 3;

    private const string TempSuffix = ".part";

    
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

        using var limiter = new SemaphoreSlim(Parallelism);
        
        var speedLimit = new SpeedLimiter();
        var tasks = _files.Select(file => Task.Run(async () =>
        {
            await limiter.WaitAsync(ct);
            try
            {
                
                
                long got = 0;
                for (int attempt = 1; ; attempt++)
                {
                    try
                    {
                        got = await DownloadOneAsync(file, speedMeter, speedLimit, ct);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (attempt < MaxAttempts)
                    {
                        _logger.LogWarning(ex, "下载失败，重试 {Attempt}/{Max}：{Dest}", attempt, MaxAttempts - 1, file.Dest);
                        await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
                    }
                }
                if (got > 0)
                {
                    Interlocked.Add(ref _received, got);
                    Interlocked.Add(ref _newBytes, got);
                }
                Interlocked.Increment(ref completed);
                Report(Path.GetFileName(file.Dest), force: true);
            }
            finally
            {
                limiter.Release();
            }
        }, ct));
        await Task.WhenAll(tasks);
        Report(null, force: true);
        return Interlocked.Read(ref _newBytes);
    }

    
    private async Task<long> DownloadOneAsync(GameResourceEntry file, SpeedMeter speedMeter, SpeedLimiter speedLimit, CancellationToken ct)
    {
        string dest = Path.Combine(_destDir, file.Dest);
        string temp = dest + TempSuffix;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

        
        
        
        if (File.Exists(dest) && FileLengthMatches(dest, file.Size) && VerifyMd5(dest, file.Md5))
        {
            return 0;
        }

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
            throw new IOException($"下载连接超时（{ReadStallTimeout.TotalSeconds:s}s 无响应）：{file.Dest}");
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
                        await output.WriteAsync(buffer.AsMemory(0, read), ct);
                        downloadedNow += read;
                        speedMeter.Add(read);
                        stallCts.CancelAfter(ReadStallTimeout);
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new IOException($"下载停滞超时（{ReadStallTimeout.TotalSeconds:s}s 无数据）：{file.Dest}（已收 {downloadedNow} 字节）");
                }
            }

        
        if (new FileInfo(temp).Length != file.Size)
        {
            throw new InvalidDataException($"下载大小不符：{file.Dest}（期望 {file.Size}，实际 {new FileInfo(temp).Length}）");
        }
        if (file.Md5 is not null && !VerifyMd5(temp, file.Md5))
        {
            File.Delete(temp);
            throw new InvalidDataException($"下载 MD5 校验失败：{file.Dest}");
        }
        if (File.Exists(dest))
        {
            File.Delete(dest);
        }
        File.Move(temp, dest);
            return downloadedNow;
        }
    }

    
    public static bool VerifyMd5(string path, string? expectedMd5)
    {
        if (expectedMd5 is null)
        {
            return true;
        }
        try
        {
            using var stream = File.OpenRead(path);
            string actual = Convert.ToHexStringLower(MD5.HashData(stream));
            return string.Equals(actual, expectedMd5, StringComparison.OrdinalIgnoreCase);
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
