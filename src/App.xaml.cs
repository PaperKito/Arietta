using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Arietta.Features.ViewHost;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Arietta;

public partial class App : Application
{

    private readonly DispatcherQueue _uiDispatcherQueue;

    public static new App Current => (App)Application.Current;


    public App()
    {
        this.InitializeComponent();

        RequestedTheme = Frameworks.WindowEx.ShouldAppsUseDarkMode() ? ApplicationTheme.Dark : ApplicationTheme.Light;
        _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += App_UnhandledException;
    }


    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(AppConfig.LogFolder);
            string logFile = Path.Combine(AppConfig.LogFolder, $"Arietta_{DateTime.Now:yyMMdd}.log");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] App Crash:");
            sb.AppendLine(e.Exception.ToString());
            File.AppendAllText(logFile, sb.ToString());
        }
        catch { }
    }


    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {

        if (AppConfig.IsPlayTimeHelperProcess)
        {
            _ = RunPlayTimeLoggerAsync();
            return;
        }

        m_AppInstance = AppInstance.GetCurrent();
        m_AppInstance.Activated += AppInstance_Activated;
        AppInstance main = AppInstance.FindOrRegisterForKey(MainInstanceKey);
        if (!main.IsCurrent)
        {
            await main.RedirectActivationToAsync(m_AppInstance.GetActivatedEventArgs());
            ExitProcess(0);
            return;
        }

        Features.GamePacks.GamePackService.Reload();
        m_MainWindow = new MainWindow();
        m_MainWindow.Activate();

        _ = Features.Setting.SelfUpdateService.CleanupLeftoversAsync();

        ShowWhatsNewIfJustUpdated();

        _ = Features.Setting.SelfUpdateService.AutoCheckAsync();

        if (AppConfig.CommandLineArgs.Contains("--fake-update"))
        {
            var fakeTimer = new System.Timers.Timer(3000) { AutoReset = false };
            fakeTimer.Elapsed += (_, _) => ShowUpdateDialog(new Features.Setting.SelfUpdateInfo(
                "9.9.9", "https://github.com/PaperKito/Arietta/releases/latest",
                "<img width=\"1280\" height=\"635\" src=\"https://github.com/user-attachments/assets/73a87cf2-b455-48cf-b149-b5f0dcf670ac\" />\n\n" +
                "Wallpaper: [LILY(@Joezunzun)](https://www.pixiv.net/artworks/151971871)\n\n" +
                "Fixed\\修复\n\n" +
                "- 首行是图 + 下面跟 Markdown 列表：这条链路以前会被当成 HTML、整段跳过渲染\n" +
                "- 支持**加粗**、[链接](https://github.com)与列表\n\n" +
                "Added\\新增\n\n" +
                "- 点「立即更新」在开发树只会提示开发布局，不会真下载",
                "https://example.invalid/Arietta-portable.zip", 0, null, NotesIsMarkdown: true));
            fakeTimer.Start();
        }

        if (AppConfig.CommandLineArgs.Contains("--fake-space-check")
            || Features.GameLauncher.SpaceCheckDialog.HasSimConfig())
        {
            var spaceTimer = new System.Timers.Timer(1500) { AutoReset = false };
            spaceTimer.Elapsed += (_, _) => _uiDispatcherQueue.TryEnqueue(ShowSpaceCheckSim);
            spaceTimer.Start();
        }

        if (AppConfig.CommandLineArgs.Contains("--fake-whatsnew"))
        {
            var whatsNewTimer = new System.Timers.Timer(3000) { AutoReset = false };
            whatsNewTimer.Elapsed += (_, _) => _uiDispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    m_whatsNewWindow = new Features.Setting.UpdateWindow(
                        new Features.Setting.SelfUpdateInfo(
                            "1.0.1", "https://github.com/PaperKito/Arietta/releases/tag/v1.0.1",
                            null, string.Empty, 0, null),
                        whatsNew: true, fromVersion: "1.0.0");
                    m_whatsNewWindow.Closed += (_, _) => m_whatsNewWindow = null;
                    m_whatsNewWindow.Show();
                    m_whatsNewWindow.BringToFront();
                }
                catch (Exception ex)
                {
                    AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show fake whats-new window");
                }
            });
            whatsNewTimer.Start();
        }

        _ = Features.KuroBbs.KuroBbsAutoSignService.RunDailyOnceAsync();

        m_TrayWindow = new Features.ViewHost.SystemTrayWindow();

        _ = Task.Run(Features.Gamepad.GamepadController.Initialize);

        _ = Features.DynamicBackground.DynamicBackgroundStore.SyncAllAsync(
            Features.ViewHost.MainView.CurrentGameBiz);
    }

    private static async Task RunPlayTimeLoggerAsync()
    {
        try
        {
            string[] cmd = AppConfig.CommandLineArgs;
            int pid = GetIntArgument(cmd, "--pid");
            int anchorPid = GetIntArgument(cmd, "--anchor-pid");
            string? bizName = GetStringArgument(cmd, "--biz");
            if (Enum.TryParse<Core.GameBiz>(bizName, true, out var biz))
            {
                var service = Features.PlayTime.PlayTimeRecordService.Create();
                if (pid > 0)
                {
                    await service.LogPlayTimeAsync(biz, pid);
                }
                else
                {

                    await service.WaitGameProcessAndLogAsync(biz, anchorPid > 0 ? anchorPid : 0);
                }
            }
        }
        catch
        {

        }
        finally
        {
            Environment.Exit(0);
        }
    }


    private static string? GetStringArgument(string[] args, string name)
    {
        int index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }


    private static int GetIntArgument(string[] args, string name)
    {
        return int.TryParse(GetStringArgument(args, name), out int value) ? value : 0;
    }

    private const string MainInstanceKey = "main";

    private AppInstance? m_AppInstance;

    private MainWindow m_MainWindow;

    public MainWindow? MainWindow => m_MainWindow;


    private Features.Setting.UpdateWindow? m_updateWindow;

    private Features.Setting.UpdateWindow? m_whatsNewWindow;

    private Features.Setting.SelfUpdateInfo? m_pendingUpdateInfo;

    public void ShowUpdateDialogWaiting()
    {
        _uiDispatcherQueue.TryEnqueue(() =>
        {
            try
            {

                if (m_updateWindow is not null || m_whatsNewWindow is not null)
                {
                    return;
                }
                m_updateWindow = new Features.Setting.UpdateWindow();
                m_updateWindow.Closed += (_, _) => m_updateWindow = null;
                m_updateWindow.Show();

                m_updateWindow.EnsureForeground();
                RaiseLater(m_updateWindow, 900);
                RaiseLater(m_updateWindow, 2000);
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show waiting update window");
            }
        });
    }

    public void ApplyUpdateInfo(Features.Setting.SelfUpdateInfo info)
    {
        _uiDispatcherQueue.TryEnqueue(() =>
        {

            if (m_whatsNewWindow is not null)
            {
                m_pendingUpdateInfo = info;
                return;
            }
            if (m_updateWindow is null)
            {
                ShowUpdateDialog(info);
                return;
            }
            m_updateWindow.ApplyInfo(info);
            m_updateWindow.EnsureForeground();
        });
    }

    public void CloseUpdateDialog()
    {

        _uiDispatcherQueue.TryEnqueue(() =>
        {
            if (m_updateWindow is { IsWaiting: true })
            {
                m_updateWindow.Close();
            }
        });
    }

    private static void RaiseLater(Features.Setting.UpdateWindow window, int delayMs)
    {
        Microsoft.UI.Dispatching.DispatcherQueueTimer timer = window.DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(delayMs);
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                window.EnsureForeground();
            }
            catch
            {

            }
        };
        timer.Start();
    }


    public void ShowUpdateDialog(Features.Setting.SelfUpdateInfo info)
    {
        _uiDispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (m_whatsNewWindow is not null)
                {

                    m_pendingUpdateInfo = info;
                    return;
                }
                if (m_updateWindow is null)
                {
                    m_updateWindow = new Features.Setting.UpdateWindow(info);
                    m_updateWindow.Closed += (_, _) => m_updateWindow = null;
                }
                else
                {

                    m_updateWindow.ApplyInfo(info);
                }
                m_updateWindow.Show();

                m_updateWindow.EnsureForeground();
                RaiseLater(m_updateWindow, 700);
                RaiseLater(m_updateWindow, 2000);
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show update window");
            }
        });
    }

    private void ShowWhatsNewIfJustUpdated()
    {
        string current = AppConfig.AppVersion;
        string? last = AppConfig.GetConfig("LastAppVersion");
        if (string.Equals(last, current, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        AppConfig.SetConfig("LastAppVersion", current);
        if (last is null)
        {
            return;
        }
        if (string.Equals(AppConfig.GetConfig("ShowUpdateContentAfterUpdateRestart"), "off", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        _uiDispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (m_whatsNewWindow is not null)
                {
                    return;
                }
                if (m_updateWindow is not null)
                {

                    AppConfig.GetLogger<App>().LogInformation(
                        "Whats-new skipped: update prompt window is already open");
                    return;
                }

                m_whatsNewWindow = new Features.Setting.UpdateWindow(
                    Features.Setting.SelfUpdateService.CurrentVersionInfo(), whatsNew: true, fromVersion: last);
                m_whatsNewWindow.Closed += (_, _) =>
                {
                    m_whatsNewWindow = null;

                    if (m_pendingUpdateInfo is { } pending)
                    {
                        m_pendingUpdateInfo = null;
                        ShowUpdateDialog(pending);
                    }
                };
                m_whatsNewWindow.Show();
                m_whatsNewWindow.EnsureForeground();

                RaiseLater(m_whatsNewWindow, 700);
                RaiseLater(m_whatsNewWindow, 2000);
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show whats-new window");
            }
        });
    }

    private void ShowSpaceCheckSim()
    {
        try
        {
            var cfg = Features.GameLauncher.SpaceCheckDialog.LoadConfig();
            if (cfg.Enough && !cfg.ForceShow)
            {
                AppConfig.GetLogger<App>().LogInformation(
                    "Space check sim: enough ({Available}GB >= {Required}GB), no dialog",
                    cfg.AvailableGB, cfg.RequiredGB);
                Arietta.Helpers.InAppToast.MainWindow?.Information(
                    Arietta.Localization.Lang.Instance.SpaceCheck_SimEnough);
                return;
            }
            var dialog = new Features.GameLauncher.SpaceCheckDialog
            {
                XamlRoot = ((FrameworkElement)m_MainWindow.Content).XamlRoot,
            };
            _ = dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show space check sim dialog");
            Arietta.Helpers.InAppToast.MainWindow?.Error(ex, "Space check sim");
        }
    }


    public void EnsureMainWindow()
    {
        m_MainWindow ??= new MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();

        m_MainWindow.RestoreAndBringToFront();
        RaiseMainWindowLater(900);

        _ = Features.KuroBbs.KuroBbsAutoSignService.RunDailyOnceAsync();
    }

    private void RaiseMainWindowLater(int delayMs)
    {
        MainWindow? window = m_MainWindow;
        if (window is null)
        {
            return;
        }
        Microsoft.UI.Dispatching.DispatcherQueueTimer raise = window.DispatcherQueue.CreateTimer();
        raise.Interval = TimeSpan.FromMilliseconds(delayMs);
        raise.IsRepeating = false;
        raise.Tick += (_, _) =>
        {
            raise.Stop();
            try { window.EnsureForeground(); }
            catch { }
        };
        raise.Start();
    }

    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {

        AppConfig.GetLogger<App>().LogInformation("Second instance activation forwarded, restoring main window");
        _uiDispatcherQueue.TryEnqueue(EnsureMainWindow);
    }


    private Features.ViewHost.SystemTrayWindow? m_TrayWindow;

    public void ExitApp()
    {
        try { m_TrayWindow?.DisposeTrayIcon(); } catch { }

        try { m_AppInstance?.UnregisterKey(); } catch { }

        try { Features.Gamepad.GamepadController.RestoreGameBarRegistry(); } catch { }
        ExitProcess(0);
    }


    [DllImport("kernel32.dll")]
    private static extern void ExitProcess(int exitCode);

}
