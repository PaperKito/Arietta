using Arietta.Core;

namespace Arietta.Features.GameLauncher;

public static class GameNoticeSettings
{
    public const string EnableBannerAndPostKey = "EnableBannerAndPost";

    public static bool ShowBannerAndPost => AppConfig.GetConfig(EnableBannerAndPostKey) != "0";


    public static void SetShowBannerAndPost(bool value)
    {
        AppConfig.SetConfig(EnableBannerAndPostKey, value ? "1" : "0");
    }
}
