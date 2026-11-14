using Arietta.Core;

namespace Arietta.Features.GameLauncher;

public static class GameNoticeSettings
{
    public const string EnableBannerAndPostKey = "EnableBannerAndPost";

    public const string EnableBannerScaleUpKey = "EnableBannerScaleUp";

    public static bool ShowBannerAndPost => AppConfig.GetConfig(EnableBannerAndPostKey) != "0";


    public static void SetShowBannerAndPost(bool value)
    {
        AppConfig.SetConfig(EnableBannerAndPostKey, value ? "1" : "0");
    }

    public static bool ScaleUpOnHover => AppConfig.GetConfig(EnableBannerScaleUpKey) != "0";


    public static void SetScaleUpOnHover(bool value)
    {
        AppConfig.SetConfig(EnableBannerScaleUpKey, value ? "1" : "0");
    }
}
