using System;
using System.Collections.Generic;
using Arietta.Core;

namespace Arietta.Features.GameLauncher;

public static class AnnouncementLanguageSettings
{
    public const string ConfigKey = "AnnouncementLanguage";

    public static readonly (string? Code, string Label)[] Options =
    [
        ("zh-Hans",  "简体中文"),
        ("zh-Hant",  "繁體中文"),
        ("en",       "English"),
        ("ja",       "日本語"),
        ("ko",       "한국어"),
        ("fr",       "Français"),
        ("de",       "Deutsch"),
        ("es",       "Español"),
        ("th",       "ไทย"),
        ("pt-BR",    "Português (Brasil)"),
    ];

    public static string Selected
    {
        get
        {
            string? value = AppConfig.GetConfig(ConfigKey);

            foreach (var (code, _) in Options)
            {
                if (string.Equals(code, value, StringComparison.OrdinalIgnoreCase))
                {
                    return code;
                }
            }
            return NormalizeSystemLanguage();
        }
    }

    public static string EffectiveLanguage => Normalize(Selected) ?? NormalizeSystemLanguage();


    public static void SetSelected(string? code)
    {
        AppConfig.SetConfig(ConfigKey, string.IsNullOrWhiteSpace(code) ? null : code);
    }

    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }
        string lang = language.Trim();

        foreach (var (code, _) in Options)
        {
            if (code is not null && string.Equals(code, lang, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        string lower = lang.ToLowerInvariant();

        if (lower.StartsWith("zh"))
        {
            bool traditional = lower.Contains("hant") || lower.Contains("tw") || lower.Contains("hk") || lower.Contains("mo");
            return traditional ? "zh-Hant" : "zh-Hans";
        }

        string primary = lower.Split('-', '_')[0];
        foreach (var (code, _) in Options)
        {
            if (code is null)
            {
                continue;
            }
            if (string.Equals(code.Split('-')[0], primary, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        return null;
    }

    private static string NormalizeSystemLanguage()
    {
        try
        {
            string name = System.Globalization.CultureInfo.CurrentUICulture.Name;
            return Normalize(name) ?? "en";
        }
        catch
        {
            return "en";
        }
    }
}
