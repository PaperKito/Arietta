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

    private long _lastApplyLogMs;


    public bool IsBusy { get; private set; }

    public bool IsRepair { get; private set; }

    public GameBiz Biz { get; private set; }

    public GameUpdateService.UpdateKind Kind { get; private set; }

    public string? TargetVersion { get; private set; }

    public string? InstallDir { get; private set; }

    public string? InstallTier { get; private set; }

    public bool IsApplying { get; private set; }

    public bool IsPausing { get; private set; }

    public double ProgressValue { get; private set; }

    public long ReceivedBytes { get; private set; }

    public long TotalBytes { get; private set; }

    public string? ProgressText { get; private set; }

    public GameDownloadProgress? LastProgress { get; private set; }

    public bool IsActiveFor(GameBiz biz) => IsBusy && Biz == biz;

    public bool Start(GameBiz biz, GameUpdateService.GameUpdateState state)
    {

        if (state.TargetVersion is null || (state.Target is null && state.TierName is null))
        {
            return false;
        }
        if (!Begin(biz, state.Kind, state.TargetVersion, state.TotalBytes, initialBytes: state.PredownloadedBytes))
        {
            return false;
        }
        _ = RunAsync(biz, state);
        return true;
    }

    public bool StartInstall(GameBiz biz, GameUpdateService.GameUpdateState state, string installDir, string? tierName = null)
    {
        if (state.Target is null || state.TargetVersion is null || string.IsNullOrWhiteSpace(installDir))
        {
            return false;
        }
        if (!Begin(biz, GameUpdateService.UpdateKind.Update, state.TargetVersion, state.TotalBytes, installDir, tierName, state.PredownloadedBytes))
        {
            return false;
        }
        _ = RunAsync(biz, state);
        return true;
    }

    public bool StartTierSwitch(GameBiz biz, string tierName, long totalBytes, string? installDir = null)
    {
        if (string.IsNullOrWhiteSpace(tierName))
        {
            return false;
        }
        if (!Begin(biz, GameUpdateService.UpdateKind.Update, null, totalBytes, installDir))
        {
            return false;
        }
        _ = RunCoreAsync(biz, async (progress, ct) =>
        {
            await Task.Run(() => GameUpdateService.SwitchTierAsync(biz, tierName, progress, ct, installDir), ct);
            InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_TierSwitchDone, GameUpdateService.TierDisplayName(tierName)));
        }, errorTitle: Lang.Instance.GameLauncherSettingDialog_ResourceTier);
        return true;
    }

    public bool StartRepair(GameBiz biz)
    {
        if (!Begin(biz, GameUpdateService.UpdateKind.Update, null, 0))
        {
            return false;
        }
        IsRepair = true;
        _ = RunCoreAsync(biz, async (progress, ct) =>
        {
            await GameUpdateService.RepairGameAsync(biz, progress, ct);
            InAppToast.MainWindow?.Success(Lang.Instance.GameUpdate_RepairDone);
        }, errorTitle: Lang.Instance.LauncherPage_RepairGame);
        return true;
    }

    private bool Begin(GameBiz biz, GameUpdateService.UpdateKind kind, string? targetVersion, long totalBytes,
        string? installDir = null, string? tierName = null, long initialBytes = 0)
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
        Kind = kind;
        IsRepair = false;
        InstallDir = installDir;
        InstallTier = tierName;
        TargetVersion = targetVersion;
        IsApplying = false;
        IsPausing = false;

        ProgressValue = totalBytes > 0 ? Math.Clamp(initialBytes * 1000.0 / totalBytes, 0, 1000) : 0;
        ReceivedBytes = initialBytes;
        TotalBytes = totalBytes;
        ProgressText = null;
        LastProgress = null;
        Changed?.Invoke(this, EventArgs.Empty);
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
        AppConfig.GetLogger<GameUpdateSession>().LogInformation("Pause requested");
        bool notify = false;
        lock (_gate)
        {
            if (IsBusy && !IsPausing)
            {
                IsPausing = true;
                notify = true;
            }
        }

        if (notify)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
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
        await RunCoreAsync(biz, async (progress, ct) =>
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

                await Task.Run(() => GameUpdateService.ApplyUpdateAsync(biz, state, progress, ct), ct);
                InAppToast.MainWindow?.Success(string.Format(Lang.Instance.GameUpdate_Done, state.TargetVersion));
            }
        });
    }

    private async Task RunCoreAsync(GameBiz biz, Func<IProgress<GameDownloadProgress>, CancellationToken, Task> work, string? errorTitle = null)
    {

        var progress = new Progress<GameDownloadProgress>(OnProgress);
        var ct = _cts?.Token ?? CancellationToken.None;
        try
        {
            await work(progress, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {

        }
        catch (OperationCanceledException ex)
        {

            AppConfig.GetLogger<GameUpdateSession>().LogWarning(ex, "Game update aborted by non-user cancellation {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, errorTitle ?? Lang.Instance.LauncherPage_UpdateGame);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<GameUpdateSession>().LogError(ex, "Game update failed {Biz}", biz);
            InAppToast.MainWindow?.Error(ex, errorTitle ?? Lang.Instance.LauncherPage_UpdateGame);
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
            IsPausing = false;
            IsRepair = false;
            long cancelTs = Interlocked.Exchange(ref _cancelRequestedTimestamp, 0);
            if (cancelTs != 0)
            {
                double ms = (Stopwatch.GetTimestamp() - cancelTs) * 1000.0 / Stopwatch.Frequency;
                AppConfig.GetLogger<GameUpdateSession>().LogInformation("Session ended (pause response {Ms:0}ms)", ms);
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }


    private void OnProgress(GameDownloadProgress p)
    {
        LastProgress = p;

        static string WithPercent(string text, double ratio)
            => $"{text} · {Math.Clamp(ratio, 0, 1):P0}";
        double fileRatio = p.TotalFiles > 0 ? (double)p.CompletedFiles / p.TotalFiles : 0;
        double overall = p.OverallProgress is { } o ? Math.Clamp(o, 0, 1) : fileRatio;

        if (p.Phase is DownloadPhase.Applying or DownloadPhase.Patching)
        {
            long nowMs = Environment.TickCount64;
            if (nowMs - _lastApplyLogMs >= 5000)
            {
                _lastApplyLogMs = nowMs;
                AppConfig.GetLogger<GameUpdateSession>().LogInformation(
                    "Apply progress: phase={Phase} {Done}/{Total} overall={Overall:P1} bytes={Bytes}/{TotalBytes} speed={Speed}/s",
                    p.Phase, p.CompletedFiles, p.TotalFiles, overall,
                    p.ReceivedBytes, p.TotalBytes, FormatSize(p.SpeedBytesPerSec));
            }
        }
        if (p.Phase == DownloadPhase.Applying)
        {
            IsApplying = true;
            ProgressValue = overall * 1000;
            ProgressText = p.TotalFiles > 0
                ? WithPercent(string.Format(Lang.Instance.GameUpdate_ApplyingProgress, p.CompletedFiles, p.TotalFiles), overall)
                : Lang.Instance.GameUpdate_Applying;
        }
        else if (p.Phase == DownloadPhase.Patching)
        {
            IsApplying = true;

            ProgressValue = overall * 1000;
            ProgressText = p.TotalFiles > 0
                ? WithPercent(string.Format(Lang.Instance.GameUpdate_Patching, p.CompletedFiles, p.TotalFiles), overall)
                : Lang.Instance.GameUpdate_Applying;
        }
        else if (p.Phase == DownloadPhase.Verifying)
        {
            IsApplying = false;
            ProgressValue = fileRatio * 1000;
            ProgressText = WithPercent(string.Format(Lang.Instance.GameUpdate_Verifying, p.CompletedFiles, p.TotalFiles), fileRatio);
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

    public async Task SimulateApplyProgressAsync(GameBiz biz)
    {
        const int moveFiles = 4;
        const int groups = 75;
        const int units = moveFiles + groups;
        const long totalBytes = 27L * 1024 * 1024 * 1024;
        if (!Begin(biz, GameUpdateService.UpdateKind.Update, "DEBUG", totalBytes))
        {
            AppConfig.GetLogger<GameUpdateSession>().LogWarning("Fake apply progress: another session is busy, skipped");
            return;
        }
        CancellationToken ct = _cts?.Token ?? CancellationToken.None;
        try
        {

            for (int i = 0; i <= moveFiles; i++)
            {
                ct.ThrowIfCancellationRequested();
                OnProgress(new GameDownloadProgress
                {
                    Phase = DownloadPhase.Applying,
                    CompletedFiles = i,
                    TotalFiles = moveFiles,
                    ReceivedBytes = (long)(totalBytes * 0.07 * i / moveFiles),
                    TotalBytes = totalBytes,
                    SpeedBytesPerSec = 540L * 1024 * 1024,
                    OverallProgress = (double)i / units,
                });
                await Task.Delay(400, ct);
            }

            for (int i = 0; i <= groups; i++)
            {
                ct.ThrowIfCancellationRequested();
                OnProgress(new GameDownloadProgress
                {
                    Phase = DownloadPhase.Patching,
                    CompletedFiles = i,
                    TotalFiles = groups,
                    ReceivedBytes = (long)(totalBytes * (0.07 + 0.93 * (double)i / groups)),
                    TotalBytes = totalBytes,
                    SpeedBytesPerSec = 300L * 1024 * 1024,
                    OverallProgress = (double)(moveFiles + i) / units,
                });
                await Task.Delay(220, ct);
            }
        }
        catch (OperationCanceledException)
        {

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
            IsPausing = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
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
