using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Arietta.Core;
using Arietta.Features.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.PlayTime;

internal class PlayTimeRecordService
{
    private readonly ILogger<PlayTimeRecordService> _logger;

    private readonly PlayTimeStatsService _playTimeStatsService;

    public PlayTimeRecordService(ILogger<PlayTimeRecordService> logger, PlayTimeStatsService playTimeStatsService)
    {
        _logger = logger;
        _playTimeStatsService = playTimeStatsService;
    }


    
    
    
    
    
    public static PlayTimeRecordService Create()
    {
        return new PlayTimeRecordService(
            AppConfig.GetLogger<PlayTimeRecordService>(),
            AppConfig.GetService<PlayTimeStatsService>());
    }

    public async Task LogPlayTimeAsync(GameBiz biz, int pid)
    {
        try
        {
            var instance = AppInstance.FindOrRegisterForKey($"playtime_{pid}");
            if (!instance.IsCurrent)
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, pid, instance.ProcessId);
                return;
            }
            _logger.LogInformation("Start to log playtime ({biz}, {pid})", biz, pid);
            var process = Process.GetProcessById(pid);
            LogStartState(biz, process);
            var sw = Stopwatch.StartNew();
            long last = 0;
            while (true)
            {
                await Task.Delay(Random.Shared.Next(800, 1200));
                if (process.HasExited)
                {
                    var now = DateTimeOffset.Now;
                    Log(biz, pid, PlayTimeState.Stop, now.ToUnixTimeMilliseconds(), $"{process.ProcessName} [{now}]");
                    SavePlayTimeStats(biz, pid, process.StartTime, now.DateTime);
                    break;
                }
                else
                {
                    if (sw.ElapsedMilliseconds - last > 30000)
                    {
                        Log(biz, pid, PlayTimeState.Play);
                        last = sw.ElapsedMilliseconds;
                    }
                }
            }
            DatabaseService.SetValue($"playtime_total_{biz}", _playTimeStatsService.GetPlayTimeTotal(biz));
            _logger.LogInformation("End log playtime ({biz}, {pid})", biz, pid);
        }
        catch (Exception ex)
        {
            Log(biz, pid, PlayTimeState.Error, 0, ex.Message);
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}", biz, pid);
        }
    }

    private void LogStartState(GameBiz biz, Process process)
    {
        var startTime = new DateTimeOffset(process.StartTime);
        Log(biz, process.Id, PlayTimeState.Start, startTime.ToUnixTimeMilliseconds(), $"{process.ProcessName} [{startTime}]");
        using var dapper = DatabaseService.CreateConnection();
        
        
        var last = dapper.QueryFirstOrDefault<PlayTimeItemStruct>("SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND Pid = @Id ORDER BY TimeStamp DESC LIMIT 1;", new { biz, process.Id });
        DateTimeOffset time = startTime;
        if (last.TimeStamp > startTime.ToUnixTimeMilliseconds())
        {
            time = DateTimeOffset.FromUnixTimeMilliseconds(last.TimeStamp);
        }

        var now = DateTimeOffset.Now;
        if (now - time >= TimeSpan.FromSeconds(60))
        {
            List<PlayTimeItem> list = new List<PlayTimeItem>();
            while (true)
            {
                time = time.AddMilliseconds(Random.Shared.Next(30_000, 32_000));
                if (time < now)
                {
                    list.Add(new PlayTimeItem
                    {
                        TimeStamp = time.ToUnixTimeMilliseconds(),
                        GameBiz = biz,
                        Pid = process.Id,
                        State = PlayTimeState.Play,
                    });
                }
                else
                {
                    break;
                }
            }
            using var t = dapper.BeginTransaction();
            dapper.Execute("INSERT OR REPLACE INTO PlayTimeItem (TimeStamp, GameBiz, Pid, State, CursorPos, Message) VALUES (@TimeStamp, @GameBiz, @Pid, @State, @CursorPos, @Message);", list, t);
            t.Commit();
        }
    }

    private void Log(GameBiz biz, int pid, PlayTimeState state, long ts = 0, string? message = null)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            var item = new PlayTimeItem
            {
                TimeStamp = ts == 0 ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : ts,
                GameBiz = biz,
                Pid = pid,
                State = state,
                Message = message,
            };
            dapper.Execute("INSERT OR REPLACE INTO PlayTimeItem (TimeStamp, GameBiz, Pid, State, CursorPos, Message) VALUES (@TimeStamp, @GameBiz, @Pid, @State, @CursorPos, @Message);", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}, State {state}, Message {message}", biz, pid, state, message);
        }
    }

    private void SavePlayTimeStats(GameBiz biz, int pid, DateTime startTime, DateTime endTime)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            var stats = new PlayTimeStats
            {
                GameBiz = biz,
                Pid = pid,
                StartTime = new DateTimeOffset(startTime).ToUnixTimeMilliseconds(),
                EndTime = new DateTimeOffset(endTime).ToUnixTimeMilliseconds(),
            };
            using var t = dapper.BeginTransaction();
            dapper.Execute("INSERT OR REPLACE INTO PlayTimeStats (GameBiz, Pid, StartTime, EndTime, Interruption, Type) VALUES (@GameBiz, @Pid, @StartTime, @EndTime, @Interruption, @Type);", stats);
            dapper.Execute("DELETE FROM PlayTimeItem WHERE GameBiz = @GameBiz AND Pid = @Pid AND TimeStamp >= @StartTime AND TimeStamp <= @EndTime;", stats);
            t.Commit();
            _logger.LogInformation("Save play time stats: GameBiz {biz}, Pid {pid}, StartTime {startTime}, EndTime {endTime}, Interruption {interruption}", biz, pid, startTime, endTime, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save play time stats: GameBiz {biz}, Pid {pid}, StartTime {startTime}, EndTime {endTime}, Interruption {interruption}", biz, pid, startTime, endTime, false);
        }
    }

    
    
    
    
    
    
    public Task<Process?> StartProcessToLogAsync(GameId gameId, int? anchorPid = null)
    {
        try
        {
            _logger.LogInformation("Start to log playtime ({biz}), waiting for the game process in the helper process", gameId.GameBiz);
            StartPlayTimeLoggerProcess(gameId.GameBiz, null, anchorPid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start process to log play time: {biz}", gameId.GameBiz);
        }
        return Task.FromResult<Process?>(null);
    }


    
    
    
    
    
    
    
    
    
    
    
    public Task StartProcessToLogAsync(GameId gameId, int launchedPid, string launchedProcessName)
    {
        string[] names = GetGameExeNames(gameId);
        bool launchedIsGame = names.Length > 0
            && string.Equals(launchedProcessName, Path.GetFileNameWithoutExtension(names[0]), StringComparison.OrdinalIgnoreCase);
        if (launchedIsGame)
        {
            return StartProcessToLogAsync(gameId, launchedPid);
        }
        StartProcessToLogAsync(gameId, anchorPid: launchedPid);
        return Task.CompletedTask;
    }


    
    
    
    
    
    
    
    
    
    
    
    public async Task WaitGameProcessAndLogAsync(GameBiz biz, int anchorPid = 0)
    {
        try
        {
            
            var waiter = AppInstance.FindOrRegisterForKey($"playtime_wait_{biz}");
            if (!waiter.IsCurrent)
            {
                _logger.LogWarning("Another helper process is waiting for the game process ({biz}, {pid})", biz, waiter.ProcessId);
                return;
            }
            string[] names = GetGameExeNames(new GameId(biz, biz.ToString()));
            DateTime deadline = DateTime.Now.AddSeconds(30);
            DateTime hardCap = DateTime.Now.AddMinutes(10);
            Process? lastResort = null;
            while (true)
            {
                await Task.Delay(2000);
                var parents = anchorPid > 0 ? ProcessTree.BuildParentMap() : null;
                
                
                var process = parents is { Count: > 0 }
                    ? FindLoggableProcess(biz, names, anchorPid, anchorPid, parents)
                    : FindLoggableProcess(biz, names, anchorPid, 0, null);
                if (process is not null)
                {
                    await LogPlayTimeAsync(biz, process.Id);
                    return;
                }
                
                if (anchorPid > 0)
                {
                    lastResort?.Dispose();
                    lastResort = FindLoggableProcess(biz, names, anchorPid, 0, null);
                }
                
                if (anchorPid > 0 && IsProcessAlive(anchorPid) && DateTime.Now < hardCap)
                {
                    continue;
                }
                if (DateTime.Now < deadline)
                {
                    continue;
                }
                if (lastResort is not null)
                {
                    
                    _logger.LogWarning("Anchored wait found no descendant ({biz}, anchor {anchor}); falling back to pid {pid}",
                        biz, anchorPid, lastResort.Id);
                    await LogPlayTimeAsync(biz, lastResort.Id);
                    return;
                }
                _logger.LogWarning("30 秒内没找到游戏进程（{biz}），本次不记录时长：{names}", biz, string.Join("/", names));
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wait game process to log play time: {biz}", biz);
        }
    }


    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }


    
    
    
    
    private static IEnumerable<Process> EnumerateSessionProcesses(string[] names)
    {
        int session = Process.GetCurrentProcess().SessionId;
        foreach (string name in names)
        {
            foreach (Process process in Process.GetProcessesByName(name))
            {
                bool inSession;
                try
                {
                    inSession = process.SessionId == session;
                }
                catch
                {
                    inSession = true;
                }
                if (inSession)
                {
                    yield return process;
                }
                else
                {
                    process.Dispose();
                }
            }
        }
    }


    
    
    
    
    
    
    public static bool HasOwnStartRecord(GameBiz biz, int pid)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            
            
            return dapper.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM PlayTimeItem WHERE GameBiz = @biz AND Pid = @pid AND State = @state;",
                new { biz, pid, state = (int)PlayTimeState.Start }) > 0;
        }
        catch
        {
            return false;
        }
    }


    
    
    
    
    
    
    public static bool HasStatsRecord(GameBiz biz, long minEndTimeMs)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            
            return dapper.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM PlayTimeStats WHERE GameBiz = @biz AND EndTime >= @minEndTimeMs;",
                new { biz, minEndTimeMs }) > 0;
        }
        catch
        {
            return false;
        }
    }


    
    
    
    
    
    
    private Process? FindLoggableProcess(GameBiz biz, string[] names, int excludePid = 0, int anchorPid = 0, Dictionary<int, int>? parents = null)
    {
        bool requireDescendant = anchorPid > 0 && parents is { Count: > 0 };
        foreach (var process in EnumerateSessionProcesses(names))
        {
            try
            {
                int id = process.Id;
                if (excludePid > 0 && id == excludePid)
                {
                    
                    process.Dispose();
                    continue;
                }
                if (requireDescendant && !ProcessTree.IsSelfOrDescendant(id, anchorPid, parents!))
                {
                    
                    process.Dispose();
                    continue;
                }
                if (HasStartRecordForOtherBiz(biz, id))
                {
                    
                    process.Dispose();
                    continue;
                }
                if (AppInstance.GetInstances().Any(x => x.Key == $"playtime_{id}"))
                {
                    
                    process.Dispose();
                    continue;
                }
                string? image = ProcessImage.GetPath(process);
                if (image is not null && IsUnderOtherGameDir(biz, image))
                {
                    
                    process.Dispose();
                    continue;
                }
                return process;
            }
            catch
            {
                
            }
        }
        return null;
    }


    
    
    
    
    
    
    private void StartPlayTimeLoggerProcess(GameBiz biz, int? pid, int? anchorPid = null)
    {
        try
        {
            string exe = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, $"{AppConfig.AppName}.exe");
            string arguments = pid is null
                ? $"playtime --biz {biz}"
                : $"playtime --biz {biz} --pid {pid}";
            if (anchorPid is { } anchor)
            {
                arguments += $" --anchor-pid {anchor}";
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start playtime logger process: GameBiz {biz}, Pid {pid}", biz, pid);
        }
    }

    public async Task StartProcessToLogAsync(GameId gameId, int pid)
    {
        try
        {
            Process process = Process.GetProcessById(pid);
            var biz = gameId.GameBiz;
            string[] names = GetGameExeNames(gameId);
            if (!names.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) is not the expected process ({name})", biz, pid, process.ProcessName);
                return;
            }
            if (AppInstance.GetInstances().Any(x => x.Key == $"playtime_{pid}"))
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) has been recorded by another process", biz, pid);
                return;
            }

            _logger.LogInformation("Start process to log play time: GameBiz {biz}, Pid {pid}", biz, pid);
            StartPlayTimeLoggerProcess(biz, pid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start process to log play time: GameBiz {biz}, Pid {pid}", gameId.GameBiz, pid);
        }
    }

    public string[] GetGameExeNames(GameId gameId)
    {
        
        var names = GameRegistry.GetEntry(gameId.GameBiz).ExeNames
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
        string? path = GameRegistry.GetConfiguredExePath(gameId.GameBiz);
        if (!string.IsNullOrWhiteSpace(path))
        {
            string configured = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                names.Add(configured);
            }
        }
        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }


    
    
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _candidateDiagLogged = new();

    private static void LogCandidateOnce(int pid, string kind, Action log)
    {
        if (_candidateDiagLogged.TryAdd($"{pid}:{kind}", 0))
        {
            log();
        }
    }


    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public Process? FindRunningClient(GameBiz biz)
    {
        try
        {
            string[] names = GetGameExeNames(new GameId(biz, biz.ToString()));
            string? root = Path.GetDirectoryName(GameRegistry.GetConfiguredExePath(biz));
            
            
            int anchor = GameLaunchAnchor.Get(biz);
            var parents = anchor > 0 ? ProcessTree.BuildParentMap() : null;
            if (parents is { Count: 0 })
            {
                parents = null;
            }
            Process? anchored = null;
            Process? fallback = null;
            foreach (Process process in EnumerateSessionProcesses(names))
            {
                if (HasStartRecordForOtherBiz(biz, process.Id))
                {
                    
                    
                    process.Dispose();
                    continue;
                }
                string? image = ProcessImage.GetPath(process);
                if (root is not null && IsUnderGameDir(image, root))
                {
                    
                    LogCandidateOnce(process.Id, "match", () => AppConfig.GetLogger<PlayTimeRecordService>().LogInformation(
                        "FindRunningClient ({biz}) matched own dir: pid {pid}, image {image}", biz, process.Id, image));
                    anchored?.Dispose();
                    fallback?.Dispose();
                    return process;
                }
                
                
                
                
                
                
                if (image is not null && IsUnderOtherGameDir(biz, image))
                {
                    LogCandidateOnce(process.Id, "other", () => AppConfig.GetLogger<PlayTimeRecordService>().LogInformation(
                        "FindRunningClient ({biz}) excluded other-server process: pid {pid}, image {image}", biz, process.Id, image));
                    process.Dispose();
                    continue;
                }
                
                
                if (parents is not null && ProcessTree.IsSelfOrDescendant(process.Id, anchor, parents))
                {
                    if (anchored is null)
                    {
                        anchored = process;
                    }
                    else
                    {
                        process.Dispose();
                    }
                    continue;
                }
                
                
                if (fallback is null)
                {
                    if (image is null)
                    {
                        LogCandidateOnce(process.Id, "unreadable", () => AppConfig.GetLogger<PlayTimeRecordService>().LogWarning(
                            "FindRunningClient ({biz}) pid {pid}: image path unreadable, fallback to name-only match (multi-install attribution unreliable)",
                            biz, process.Id));
                    }
                    fallback = process;
                }
                else
                {
                    process.Dispose();
                }
            }
            
            if (anchored is not null)
            {
                fallback?.Dispose();
                return anchored;
            }
            return fallback;
        }
        catch
        {
            
        }
        return null;
    }


    
    
    
    
    
    
    
    public static bool HasStartRecordForOtherBiz(GameBiz biz, int pid)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            return dapper.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM PlayTimeItem WHERE Pid = @pid AND State = @state AND GameBiz != @biz;",
                new { pid, state = (int)PlayTimeState.Start, biz }) > 0;
        }
        catch
        {
            return false;
        }
    }


    
    
    
    
    private static bool IsUnderOtherGameDir(GameBiz biz, string imagePath)
    {
        foreach (GameRegistry.GameEntry entry in GameRegistry.Games)
        {
            if (entry.Biz == biz)
            {
                continue;
            }
            string? otherRoot = Path.GetDirectoryName(GameRegistry.GetConfiguredExePath(entry.Biz));
            if (otherRoot is not null && IsUnderGameDir(imagePath, otherRoot))
            {
                return true;
            }
        }
        return false;
    }


    
    private static bool IsUnderGameDir(string? imagePath, string gameRoot)
    {
        string? imageDir = string.IsNullOrEmpty(imagePath) ? null : Path.GetDirectoryName(imagePath);
        if (string.IsNullOrEmpty(imageDir))
        {
            return false;
        }
        string relative = Path.GetRelativePath(gameRoot, imageDir);
        return relative == "." || (!Path.IsPathRooted(relative) && !(relative == ".." || relative.StartsWith(@"..\")));
    }
}
