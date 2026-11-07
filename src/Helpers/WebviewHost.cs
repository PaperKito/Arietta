using System;
using System.IO;

namespace Arietta.Helpers;

internal static class WebviewHost
{
    private const string GpuSandboxConfigKey = "WebView2GpuSandbox";


    public static void UseDataFolder()
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER",
            Path.Combine(AppConfig.LocalFolder, "webview"));
        ApplyBrowserArguments();
    }

    private static void ApplyBrowserArguments()
    {
        if (string.Equals(AppConfig.GetConfig(GpuSandboxConfigKey), "on", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        const string flag = "--no-sandbox";
        string? current = Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS");
        if (current is not null && current.Contains(flag, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
            string.IsNullOrWhiteSpace(current) ? flag : $"{current} {flag}");
    }
}
