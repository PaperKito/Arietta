using System;
using System.IO;

namespace Arietta.Helpers;

internal static class WebviewHost
{
    public static void UseDataFolder()
    {
        Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER",
            Path.Combine(AppConfig.LocalFolder, "webview"));
    }
}
