using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.Sync;


















internal static class CloudSyncHost
{
    
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(8);

    
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(30);

    
    private static readonly TimeSpan ExitBudget = TimeSpan.FromSeconds(2);

    private static readonly object _lock = new();

    private static bool _initialized;
    private static CancellationTokenSource? _loopCts;
    private static Task? _loopTask;
    private static ILogger? _logger;

    private static ILogger Logger
    {
        get
        {
            if (_logger is null)
            {
                try
                {
                    _logger = AppConfig.GetLogger<CloudSyncService>();
                }
                catch
                {
                    _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CloudSyncService>.Instance;
                }
            }
            return _logger;
        }
    }


    
    
    
    
    [ModuleInitializer]
    internal static void AutoStart()
    {
        try
        {
            
            if (AppConfig.IsPlayTimeHelperProcess)
            {
                return;
            }
            Initialize();
        }
        catch
        {
            
        }
    }


    
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
        }

        try
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => FlushOnExit();
        }
        catch { }

        try
        {
            if (CloudSyncConfig.Load().AutoSync)
            {
                StartLoop();
            }
        }
        catch { }
    }


    
    public static void SetAutoSync(bool enabled)
    {
        try
        {
            if (enabled)
            {
                StartLoop();
            }
            else
            {
                StopLoop();
            }
        }
        catch { }
    }


    
    
    
    public static void FlushOnExit()
    {
        try
        {
            CloudSyncService? service = GetService();
            if (service is null || !service.IsConfigured)
            {
                return;
            }
            Task<CloudSyncResult> task = service.SyncAsync();
            if (!task.Wait(ExitBudget))
            {
                Logger.LogInformation("退出时同步未在 {Budget} 内完成，已放弃本次同步", ExitBudget);
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "退出时同步失败（已忽略）");
        }
    }


    private static void StartLoop()
    {
        lock (_lock)
        {
            if (_loopTask is { IsCompleted: false })
            {
                return;
            }
            _loopCts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_loopCts.Token));
        }
    }


    private static void StopLoop()
    {
        lock (_lock)
        {
            try
            {
                _loopCts?.Cancel();
            }
            catch { }
            _loopCts = null;
            _loopTask = null;
        }
    }


    private static async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(StartupDelay, ct);
            await SyncQuietlyAsync(ct);

            using var timer = new PeriodicTimer(SyncInterval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                await SyncQuietlyAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "云同步循环已退出");
        }
    }


    
    private static async Task SyncQuietlyAsync(CancellationToken ct)
    {
        try
        {
            CloudSyncService? service = GetService();
            if (service is null || !service.IsConfigured)
            {
                return;
            }
            CloudSyncResult result = await service.SyncAsync(ct);
            if (result.Action != CloudSyncAction.Skipped || !result.Success)
            {
                Logger.LogInformation("后台同步：{Action} {Message}", result.Action, result.Message);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "后台同步失败（已忽略，等待下一次）");
        }
    }


    private static CloudSyncService? GetService()
    {
        try
        {
            return AppConfig.GetService<CloudSyncService>();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "获取云同步服务失败");
            return null;
        }
    }
}
