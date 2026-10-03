using Microsoft.Extensions.Logging;
using Arietta.Core;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher.GameUpdate;
















public sealed class GameUpdateSession
{

    public static GameUpdateSession Current { get; } = new();


    
    public event EventHandler? Changed;


    private readonly object _gate = new();
    private CancellationTokenSource? _cts;

    
    private long _cancelRequestedTimestamp;


    public bool IsBusy { get; private set; }

    public GameBiz Biz { get; private set; }

    public GameUpdateService.UpdateKind Kind { get; private set; }

    public string? TargetVersion { get; private set; }

    
    public string? InstallDir { get; private set; }

    
    public string? InstallTier { get; private set; }

    
    public bool IsApplying { get; private set; }

    
    public double ProgressValue { get; private set; }

    public long ReceivedBytes { get; private set; }

    public long TotalBytes { get; private set; }

    
    public string? ProgressText { get; private set; }

    
    public GameDownloadProgress? LastProgress { get; private set; }


    
    public bool IsActiveFor(GameBiz biz) => IsBusy && Biz == biz;


    
    
    
    public bool Start(GameBiz biz, GameUpdateService.GameUpdateState state)
    {
        if (state.Target is null || state.TargetVersion is null)
        {
            return false;
        }
        return Begin(biz, state, installDir: null);
    }


    
    
    
    
    public bool StartInstall(GameBiz biz, GameUpdateService.GameUpdateState state, string installDir, string? tierName = null)
    {
        if (state.Target is null || state.TargetVersion is null || string.IsNullOrWhiteSpace(installDir))
        {
            return false;
        }
        return Begin(biz, state, installDir, tierName);
    }


    
    private bool Begin(GameBiz biz, GameUpdateService.GameUpdateState state, string? installDir, string? tierName = null)
    {
        lock (_gate)
        {
            if (IsBusy)
            {
                return false;
            }
            IsBusy = true;
            _cts = new CancellationTokenSource();
        }
        Biz = biz;
        Kind = installDir is null ? state.Kind : GameUpdateService.UpdateKind.Update;
        InstallDir = installDir;
        InstallTier = tierName;
        TargetVersion = state.TargetVersion;
        IsApplying = false;
        ProgressValue = 0;
        ReceivedBytes = 0;
        TotalBytes = state.TotalBytes;
        ProgressText = null;
        LastProgress = null;
        Changed?.Invoke(this, EventArgs.Empty);
        _ = RunAsync(biz, state);
        return true;
    }


    
    public void Cancel()
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            cts = _cts;
        }
        
        Interlocked.CompareExchange(ref _cancelRequestedTimestamp, Stopwatch.GetTimestamp(), 0);
        AppConfig.GetLogger<GameUpdateSession>().LogInformation("暂停请求已发出");
        try
        {
            cts?.Cancel();
        }
        catch
        {
            
        }
    }


    private async Task RunAsync(GameBiz biz, GameUpdateService.GameUpdateState state)
    {
        
        var progress = new Progress<GameDownloadProgress>(OnProgress);
        var ct = _cts?.Token ?? CancellationToken.None;
        try
        {
            if (InstallDir is { } installDir)
            {
                
                await Task.Run(() => GameUpdateService.InstallAsync(biz, installDir, state.Target!, progress, ct, InstallTier), ct);
                InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_InstallDone, state.TargetVersion));
            }
            else if (state.Kind == GameUpdateService.UpdateKind.Predownload)
            {
                await GameUpdateService.PredownloadAsync(biz, state, progress, ct);
                InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_PredownloadDone, state.TargetVersion));
            }
            else
            {
                
                await Task.Run(() => GameUpdateService.ApplyUpdateAsync(biz, state.Target!, progress, ct), ct);
                InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_Done, state.TargetVersion));
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            
        }
        catch (OperationCanceledException ex)
        {
            
            
            AppConfig.GetLogger<GameUpdateSession>().LogWarning(ex, "游戏更新非用户取消而中止 {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, Lang.Instance.LauncherPage_UpdateGame);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<GameUpdateSession>().LogError(ex, "游戏更新失败 {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, Lang.Instance.LauncherPage_UpdateGame);
        }
        finally
        {
            lock (_gate)
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
            IsApplying = false;
            long cancelTs = Interlocked.Exchange(ref _cancelRequestedTimestamp, 0);
            if (cancelTs != 0)
            {
                double ms = (Stopwatch.GetTimestamp() - cancelTs) * 1000.0 / Stopwatch.Frequency;
                AppConfig.GetLogger<GameUpdateSession>().LogInformation("会话结束（暂停响应 {Ms:0}ms）", ms);
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }


    private void OnProgress(GameDownloadProgress p)
    {
        LastProgress = p;
        if (p.Phase == DownloadPhase.Applying)
        {
            IsApplying = true;
            
            ProgressValue = p.TotalFiles > 0 ? p.CompletedFiles * 1000.0 / p.TotalFiles : 0;
            ProgressText = p.TotalFiles > 0
                ? string.Format(Lang.Instance.GameUpdate_ApplyingProgress, p.CompletedFiles, p.TotalFiles)
                : Lang.Instance.GameUpdate_Applying;
        }
        else if (p.Phase == DownloadPhase.Patching)
        {
            IsApplying = true;
            
            
            
            ProgressValue = p.TotalFiles > 0 ? p.CompletedFiles * 1000.0 / p.TotalFiles : 0;
            ProgressText = p.TotalFiles > 0
                ? string.Format(Lang.Instance.GameUpdate_Patching, p.CompletedFiles, p.TotalFiles)
                : Lang.Instance.GameUpdate_Applying;
        }
        else if (p.Phase == DownloadPhase.Verifying)
        {
            IsApplying = false;
            ProgressValue = p.TotalFiles > 0 ? p.CompletedFiles * 1000.0 / p.TotalFiles : 0;
            ProgressText = string.Format(Lang.Instance.GameUpdate_Verifying, p.CompletedFiles, p.TotalFiles);
        }
        else
        {
            IsApplying = false;
            ReceivedBytes = p.ReceivedBytes;
            TotalBytes = p.TotalBytes;
            ProgressValue = p.TotalBytes > 0 ? p.ReceivedBytes * 1000.0 / p.TotalBytes : 0;
            ProgressText = $"{FormatSize(p.ReceivedBytes)} / {FormatSize(p.TotalBytes)} · {FormatSize(p.SpeedBytesPerSec)}/s";
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }


    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1)
        {
            v /= 1024;
            i++;
        }
        return $"{v:0.#}{units[i]}";
    }


}
