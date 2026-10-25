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

        m_MainWindow = new MainWindow();
        m_MainWindow.Activate();

        _ = Features.Setting.SelfUpdateService.CleanupLeftoversAsync();

        _ = Features.Setting.SelfUpdateService.AutoCheckAsync();

        if (AppConfig.CommandLineArgs.Contains("--fake-update"))
        {
            var fakeTimer = new System.Timers.Timer(3000) { AutoReset = false };
            fakeTimer.Elapsed += (_, _) => ShowUpdateDialog(new Features.Setting.SelfUpdateInfo(
                "9.9.9", "https://github.com/PaperKito/Arietta/releases/latest",
                "## Arietta v9.9.9（模拟更新）\n" +
                "- 模拟用的假版本说明，Markdown 渲染预览\n" +
                "- 支持**加粗**、[链接](https://github.com)与列表\n" +
                "- 点「立即更新」在开发树只会提示开发布局，不会真下载",
                "https://example.invalid/Arietta-portable.zip", 0, null));
            fakeTimer.Start();
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

    public void ShowUpdateDialog(Features.Setting.SelfUpdateInfo info)
    {
        _uiDispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (m_updateWindow is null)
                {
                    m_updateWindow = new Features.Setting.UpdateWindow(info);
                    m_updateWindow.Closed += (_, _) => m_updateWindow = null;
                }
                m_updateWindow.Show();
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<App>().LogWarning(ex, "Failed to show update window");
            }
        });
    }


    public void EnsureMainWindow()
    {
        m_MainWindow ??= new MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();

        _ = Features.KuroBbs.KuroBbsAutoSignService.RunDailyOnceAsync();
    }

    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {
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
