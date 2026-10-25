using System;
using System.Globalization;
using Arietta.Features.GameLauncher;

namespace Arietta.Features.GachaLog;

public static class GachaLanguageSettings
{
    public const string ConfigKey = "GachaLanguage";

    private const string FallbackLanguage = "en";

    public static string Selected
    {
        get
        {

            string? raw = Raw;
            string? code = raw is null ? AnnouncementLanguageSettings.Selected : raw;

            return AnnouncementLanguageSettings.Normalize(code) ?? AnnouncementLanguageSettings.Normalize(CultureInfo.CurrentUICulture.Name) ?? FallbackLanguage;
        }
    }

    private static string? Raw
    {
        get
        {
            string? value = AppConfig.GetConfig(ConfigKey);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public static string Effective => Selected;

    public static void SetSelected(string? code)
    {
        AppConfig.SetConfig(ConfigKey, string.IsNullOrWhiteSpace(code) ? null : code.Trim());
    }

    public static string GetDisplayName(string code)
    {
        foreach (var (option, label) in AnnouncementLanguageSettings.Options)
        {
            if (option is not null && string.Equals(option, code, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }
        return code;
    }
}
