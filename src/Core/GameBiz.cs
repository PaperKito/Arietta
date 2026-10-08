using System;
using System.IO;

namespace Arietta.Core;

public enum GameBiz
{
    WutheringWaves = 0,
    WutheringWavesGlobal = 1,
    WutheringWavesBilibili = 2,
    PunishingGrayRaven = 3,
    PunishingGrayRavenGlobal,
    PunishingGrayRavenKorea,
    PunishingGrayRavenJapan,
    PunishingGrayRavenTaiwan,
}

public static class GameBizExtensions
{
    
    public static bool IsPunishingGrayRaven(this GameBiz biz)
    {
        return biz is GameBiz.PunishingGrayRaven
            or GameBiz.PunishingGrayRavenGlobal
            or GameBiz.PunishingGrayRavenKorea
            or GameBiz.PunishingGrayRavenJapan
            or GameBiz.PunishingGrayRavenTaiwan;
    }

    
    public static bool IsOverseas(this GameBiz biz)
    {
        return biz is GameBiz.WutheringWavesGlobal
            or GameBiz.PunishingGrayRavenGlobal
            or GameBiz.PunishingGrayRavenKorea
            or GameBiz.PunishingGrayRavenJapan
            or GameBiz.PunishingGrayRavenTaiwan;
    }

    
    
    
    
    
    public static GameBiz GetBaseBiz(this GameBiz biz)
    {
        if (biz.IsPunishingGrayRaven())
        {
            return GameBiz.PunishingGrayRaven;
        }
        return biz is GameBiz.WutheringWavesGlobal or GameBiz.WutheringWavesBilibili
            ? GameBiz.WutheringWaves
            : biz;
    }
}




public static class GameRegistry
{

    public record GameEntry(
        GameBiz Biz,
        string Name,
        string IconPath,
        bool HasGachaPage,
        string? CloudUrl = null,
        string? DirHint = null)
    {
        
        public string ShortName => Biz switch
        {
            GameBiz.WutheringWaves => Localization.Lang.Instance.GameBiz_ShortCn,
            GameBiz.WutheringWavesGlobal => Localization.Lang.Instance.GameBiz_ShortGlobal,
            GameBiz.WutheringWavesBilibili => Localization.Lang.Instance.GameBiz_ShortBilibili,
            GameBiz.PunishingGrayRaven => Localization.Lang.Instance.GameBiz_ShortPgr,
            GameBiz.PunishingGrayRavenGlobal => Localization.Lang.Instance.GameBiz_ShortGlobal,
            GameBiz.PunishingGrayRavenKorea => Localization.Lang.Instance.GameBiz_ShortKorea,
            GameBiz.PunishingGrayRavenJapan => Localization.Lang.Instance.GameBiz_ShortJapan,
            GameBiz.PunishingGrayRavenTaiwan => Localization.Lang.Instance.GameBiz_ShortTaiwan,
            _ => Name,
        };

        
        
        
        
        
        public string GameIconPath => Biz switch
        {
            GameBiz.PunishingGrayRaven or GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
                or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => "ms-appx:///Assets/Image/Game/gameicon_pgr.png",
            GameBiz.WutheringWavesGlobal => "ms-appx:///Assets/Image/Game/gameicon_wuwa.png",
            _ => "ms-appx:///Assets/Image/Game/gameicon_wuwa.png",
        };

        
        
        
        
        public string? ServerIconPath => Biz switch
        {
            GameBiz.WutheringWavesBilibili => "ms-appx:///Assets/Image/Game/gameicon_bilibili.png",
            _ => null,
        };

        
        
        
        
        public string ServerCode => Biz switch
        {
            GameBiz.WutheringWaves => "CN",
            GameBiz.WutheringWavesGlobal => "GL",
            GameBiz.PunishingGrayRaven => "CN",
            GameBiz.PunishingGrayRavenGlobal => "GL",
            GameBiz.PunishingGrayRavenKorea => "KR",
            GameBiz.PunishingGrayRavenJapan => "JP",
            GameBiz.PunishingGrayRavenTaiwan => "TW",
            _ => "",
        };

        
        public string[] ExeNames => Biz switch
        {
            GameBiz.PunishingGrayRaven or GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
                or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => new[] { "PGR.exe", "Client-Win64-Shipping.exe" },
            _ => new[] { "Client-Win64-Shipping.exe", "Wuthering Waves.exe" },
        };

        
        public string[] MarkerDirs => Biz switch
        {
            GameBiz.PunishingGrayRaven => new[] { "Punishing Gray Raven" },
            
            GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
                or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => Array.Empty<string>(),
            _ => new[] { "Wuthering Waves" },
        };

        
        
        
        
        public string DefaultInstallDirName => Biz switch
        {
            GameBiz.WutheringWavesGlobal => "Wuthering Waves Global",
            GameBiz.WutheringWavesBilibili => "Wuthering Waves bilibili",
            GameBiz.PunishingGrayRaven or GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
                or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => "Punishing Gray Raven",
            _ => "Wuthering Waves",
        };

        
        
        
        
        
        public string[] RegistryKeys => Biz switch
        {
            GameBiz.WutheringWaves => new[]
            {
                @"HKEY_CURRENT_USER\Software\kurogame\KRLauncher\Aki_G152_default_10003",
                @"HKEY_CURRENT_USER\Software\Kuro Games\Wuthering Waves",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Wuthering Waves",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\KuroGames\Wuthering Waves",
            },
            GameBiz.WutheringWavesBilibili => new[]
            {
                @"HKEY_CURRENT_USER\Software\kurogame\KRLauncher\Aki_G152_default_10004",
            },
            GameBiz.WutheringWavesGlobal => new[]
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\KRInstall Wuthering Waves Overseas",
                @"HKEY_CURRENT_USER\Software\Kuro Games\Wuthering Waves",
            },
            GameBiz.PunishingGrayRaven => new[]
            {
                @"HKEY_CURRENT_USER\Software\kurogame\KRLauncher\Haru_G148_default_10012",
                @"HKEY_CURRENT_USER\Software\Kuro Games\Punishing Gray Raven",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\KuroGames\Punishing Gray Raven",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Punishing Gray Raven",
            },
            
            
            GameBiz.PunishingGrayRavenGlobal or GameBiz.PunishingGrayRavenKorea
                or GameBiz.PunishingGrayRavenJapan or GameBiz.PunishingGrayRavenTaiwan
                => Array.Empty<string>(),
            _ => Array.Empty<string>(),
        };

        
        
        
        
        public string? DirHint => Biz switch
        {
            GameBiz.WutheringWavesGlobal => "global",
            GameBiz.WutheringWavesBilibili => "bilibili",
            _ => null,
        };
    }

    
    
    
    
    
    
    
    
    
    public static GameEntry[] Games => _games ??= BuildGames();

    private static GameEntry[]? _games;

    private static GameEntry[] BuildGames()
    {
        return
        [
            new(GameBiz.WutheringWaves, Localization.Lang.Instance.GameBiz_WuwaCn, "ms-appx:///Assets/Image/Game/gameicon_wuwa.png", true),
            new(GameBiz.WutheringWavesGlobal, Localization.Lang.Instance.GameBiz_WuwaGlobal, "ms-appx:///Assets/Image/Game/gameicon_wuwa.png", true),
            new(GameBiz.WutheringWavesBilibili, Localization.Lang.Instance.GameBiz_WuwaBilibili, "ms-appx:///Assets/Image/Game/gameicon_wuwa.png", true),
            new(GameBiz.PunishingGrayRaven, Localization.Lang.Instance.GameBiz_Pgr, "ms-appx:///Assets/Image/Game/gameicon_pgr.png", false),
            new(GameBiz.PunishingGrayRavenGlobal, Localization.Lang.Instance.GameBiz_PgrGlobal, "ms-appx:///Assets/Image/Game/gameicon_pgr.png", false),
            new(GameBiz.PunishingGrayRavenKorea, Localization.Lang.Instance.GameBiz_PgrKorea, "ms-appx:///Assets/Image/Game/gameicon_pgr.png", false),
            new(GameBiz.PunishingGrayRavenJapan, Localization.Lang.Instance.GameBiz_PgrJapan, "ms-appx:///Assets/Image/Game/gameicon_pgr.png", false),
            new(GameBiz.PunishingGrayRavenTaiwan, Localization.Lang.Instance.GameBiz_PgrTaiwan, "ms-appx:///Assets/Image/Game/gameicon_pgr.png", false),
        ];
    }

    public static GameEntry GetEntry(GameBiz biz)
    {
        foreach (var g in Games)
        {
            if (g.Biz == biz)
            {
                return g;
            }
        }
        return Games[0];
    }

    
    
    
    
    
    
    
    public record GameGroup(string Game, string IconPath, GameEntry[] Servers);

    
    
    
    
    
    
    public static string WuwaLogoPath => Localization.UiLanguage.Current switch
    {
        Localization.UiLanguageKind.Korean => "ms-appx:///Assets/Image/Game/logoWuwa_ko.png",
        Localization.UiLanguageKind.TraditionalChinese or Localization.UiLanguageKind.Japanese => "ms-appx:///Assets/Image/Game/logoWuwa_zh-Hant.png",
        Localization.UiLanguageKind.SimplifiedChinese => "ms-appx:///Assets/Image/Game/logoWuwa_zh-Hans.png",
        _ => "ms-appx:///Assets/Image/Game/logoWuwa_en.png",
    };

    
    
    
    
    public static string PgrLogoPath => Localization.UiLanguage.Current switch
    {
        Localization.UiLanguageKind.SimplifiedChinese => "ms-appx:///Assets/Image/Game/logoPgr_zh-Hans.png",
        Localization.UiLanguageKind.TraditionalChinese => "ms-appx:///Assets/Image/Game/logoPgr_zh-Hant.png",
        Localization.UiLanguageKind.Japanese => "ms-appx:///Assets/Image/Game/logoPgr_ja.png",
        Localization.UiLanguageKind.Korean => "ms-appx:///Assets/Image/Game/logoPgr_ko.png",
        _ => "ms-appx:///Assets/Image/Game/logoPgr_en.png",
    };

    
    public static GameGroup[] Groups => _groups ??= BuildGroups();

    private static GameGroup[]? _groups;

    private static GameGroup[] BuildGroups()
    {
        return
        [
            
            new(Localization.Lang.Instance.GameBiz_Wuwa, WuwaLogoPath, new[]
            {
                Games[1],
                Games[0],
                Games[2],
            }),
            new(Localization.Lang.Instance.GameBiz_Pgr, PgrLogoPath, new[]
            {
                Games[3],
                Games[4],
                Games[5],
                Games[6],
                Games[7],
            }),
        ];
    }

    static GameRegistry()
    {
        Localization.UiLanguage.Changed += () =>
        {
            _games = null;
            _groups = null;
        };
    }

    
    public static string GetExePathConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "GameExePath",
            _ => $"GameExePath_{biz}",
        };
    }

    
    
    
    
    public static string GetInstallDirConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "GameInstallDir",
            _ => $"GameInstallDir_{biz}",
        };
    }

    
    
    
    public static string GetCustomBgConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "CustomBackground",
            _ => $"CustomBackground_{biz}",
        };
    }

    
    
    
    public static string GetEnableCustomBgConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "EnableCustomBg",
            _ => $"EnableCustomBg_{biz}",
        };
    }

    
    
    
    public static string GetStartGameArgumentConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "StartGameArgument",
            _ => $"StartGameArgument_{biz}",
        };
    }

    
    public static string GetEnableThirdPartyToolConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "EnableThirdPartyTool",
            _ => $"EnableThirdPartyTool_{biz}",
        };
    }

    
    public static string GetThirdPartyToolPathConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "ThirdPartyToolPath",
            _ => $"ThirdPartyToolPath_{biz}",
        };
    }

    
    
    
    
    
    
    
    
    public static string? GetEnabledThirdPartyToolPath(GameBiz biz)
    {
        if (AppConfig.GetConfig(GetEnableThirdPartyToolConfigKey(biz)) != "1")
        {
            return null;
        }
        string? path = AppConfig.GetConfig(GetThirdPartyToolPathConfigKey(biz));
        return path is { Length: > 0 } && File.Exists(path) ? path : null;
    }

    
    
    
    public static string GetSelectedBgConfigKey(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.WutheringWaves => "SelectedBackground",
            _ => $"SelectedBackground_{biz}",
        };
    }

    
    
    
    
    
    
    public static string GetEnableDynamicBgConfigKey(GameBiz biz)
    {
        return biz.GetBaseBiz() switch
        {
            GameBiz.WutheringWaves => "EnableKuroDynamicBg",
            GameBiz.PunishingGrayRaven => "EnableKuroDynamicBg_PunishingGrayRaven",
            _ => $"EnableKuroDynamicBg_{biz.GetBaseBiz()}",
        };
    }

        
        
        
        
        
        public static bool IsExePathMatchBiz(GameBiz biz, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            string? hint = GetEntry(biz).DirHint;
            if (hint != null)
            {
                return path.Contains(hint, StringComparison.OrdinalIgnoreCase);
            }
            if (biz.IsPunishingGrayRaven())
            {
                
                return true;
            }
            return !path.Contains("bilibili", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("overseas", StringComparison.OrdinalIgnoreCase)
                && !path.Contains("global", StringComparison.OrdinalIgnoreCase);
        }

        
        
        
        
        public static string? GetConfiguredExePath(GameBiz biz)
        {
            string? path = AppConfig.GetConfig(GetExePathConfigKey(biz));
            return IsExePathMatchBiz(biz, path) ? path : null;
        }

        
    public static bool IsDynamicBgEnabled(GameBiz biz)
    {
        return AppConfig.GetConfig(GetEnableDynamicBgConfigKey(biz)) != "0";
    }

    
    
    
    public static string? GetCustomBgPath(GameBiz biz)
    {
        string? path = AppConfig.GetConfig(GetCustomBgConfigKey(biz));
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }
        
        if (AppConfig.GetConfig(GetEnableCustomBgConfigKey(biz)) == "0")
        {
            return null;
        }
        return path;
    }
}
