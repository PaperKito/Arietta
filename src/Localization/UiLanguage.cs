using System;
using System.Globalization;

namespace Arietta.Localization;



















public static class UiLanguage
{

    
    public const string AnnouncementLanguageConfigKey = "AnnouncementLanguage";


    
    public static event Action? Changed;


    private static bool _loaded;
    private static UiLanguageKind _kind;


    
    public static UiLanguageKind Current
    {
        get
        {
            EnsureLoaded();
            return _kind;
        }
    }


    
    public static UiLanguageKind KindFor(string? announcementLanguage)
    {
        return Resolve(announcementLanguage);
    }


    
    
    
    
    public static void SetLanguage(UiLanguageKind kind)
    {
        EnsureLoaded();
        if (_kind == kind)
        {
            return;
        }
        _kind = kind;
        Changed?.Invoke();
    }


    
    public static void SetLanguageFromCode(string? announcementLanguage)
    {
        SetLanguage(KindFor(announcementLanguage));
    }


    private static void EnsureLoaded()
    {
        if (!_loaded)
        {
            _loaded = true;
            _kind = Resolve(AppConfig.GetConfig(AnnouncementLanguageConfigKey));
        }
    }


    private static UiLanguageKind Resolve(string? announcementLanguage)
    {
        try
        {
            
            if (!string.IsNullOrWhiteSpace(announcementLanguage))
            {
                return FromLanguageCode(announcementLanguage.Trim());
            }
            
            return FromLanguageCode(CultureInfo.CurrentUICulture.Name);
        }
        catch
        {
            return UiLanguageKind.SimplifiedChinese;
        }
    }


    
    private static UiLanguageKind FromLanguageCode(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return UiLanguageKind.English;
        }
        if (language.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.Japanese;
        }
        if (language.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.Korean;
        }
        if (language.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.French;
        }
        if (language.StartsWith("de", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.German;
        }
        if (language.StartsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.Spanish;
        }
        if (language.StartsWith("th", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.Thai;
        }
        if (language.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.PortugueseBrazil;
        }
        if (!language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguageKind.English;
        }
        string lower = language.ToLowerInvariant();
        bool traditional = lower.Contains("hant") || lower.Contains("tw") || lower.Contains("hk") || lower.Contains("mo");
        return traditional ? UiLanguageKind.TraditionalChinese : UiLanguageKind.SimplifiedChinese;
    }

}



public enum UiLanguageKind
{
    SimplifiedChinese,
    TraditionalChinese,
    English,
    Japanese,
    Korean,
    French,
    German,
    Spanish,
    Thai,
    PortugueseBrazil,
}
