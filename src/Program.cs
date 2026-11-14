using System;
using System.IO;
using System.Text;

namespace Arietta;

public static class Program
{

    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {

        if (args.Length > 0 && string.Equals(args[0], Features.GamePacks.GamePackLauncher.Verb, StringComparison.OrdinalIgnoreCase))
        {
            Environment.Exit(Features.GamePacks.GamePackLauncher.RunVerb(args));
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            string logFolder = AppConfig.LogFolder;
            Directory.CreateDirectory(logFolder);
            string logFile = Path.Combine(logFolder, $"Arietta_{DateTime.Now:yyMMdd}.log");
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Program Crash:");
            sb.AppendLine(e.ExceptionObject.ToString());
            File.AppendAllText(logFile, sb.ToString());
        }
        catch { }
    }

}
