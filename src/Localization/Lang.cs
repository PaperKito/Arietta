using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Arietta.Localization;

public sealed class Lang : INotifyPropertyChanged
{

    public static Lang Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void NotifyAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }


    static Lang()
    {
        UiLanguage.Changed += () => Instance.NotifyAll();
    }

    private static readonly IReadOnlyDictionary<UiLanguageKind, Dictionary<string, string>> Maps =
        new Dictionary<UiLanguageKind, Dictionary<string, string>>
        {
            [UiLanguageKind.SimplifiedChinese] = LangZh.Map,
            [UiLanguageKind.TraditionalChinese] = LangHant.Map,
            [UiLanguageKind.English] = LangEn.Map,
            [UiLanguageKind.Japanese] = LangJa.Map,
            [UiLanguageKind.Korean] = LangKo.Map,
            [UiLanguageKind.French] = LangFr.Map,
            [UiLanguageKind.German] = LangDe.Map,
            [UiLanguageKind.Spanish] = LangEs.Map,
            [UiLanguageKind.Thai] = LangTh.Map,
            [UiLanguageKind.PortugueseBrazil] = LangPtBr.Map,
        };

    private static string T([CallerMemberName] string? key = null)
    {
        return TFor(UiLanguage.Current, key);
    }

    private static string TFor(UiLanguageKind kind, string? key)
    {
        if (key is null)
        {
            return string.Empty;
        }
        if (Maps.TryGetValue(kind, out var map) && map.TryGetValue(key, out string? translated))
        {
            return translated;
        }

        return LangZh.Map.TryGetValue(key, out string? cn) ? cn : key;
    }

    public string MainPage_Launcer => T();
    public string LauncherPage_GameSetting => T();
    public string MainPage_GameScreenshot => T();
    public string GachaLogPage_ConveneRecords => T();
    public string Toolbox => T();

    public string ScreenshotPage_MultiSelect => T();
    public string ScreenshotPage_OpenFolder => T();
    public string ScreenshotPage_Total0Files => T();
    public string ScreenshotPage_Selected0Of1 => T();
    public string ScreenshotPage_Empty => T();
    public string ScreenshotPage_EmptyDesc => T();
    public string ScreenshotPage_Delete => T();
    public string ScreenshotPage_DeleteConfirm => T();
    public string ScreenshotPage_Deleted => T();
    public string ScreenshotPage_DeleteFailed => T();
    public string ScreenshotPage_SelectFirst => T();
    public string ScreenshotPage_OpenImage => T();
    public string ScreenshotPage_DeletedPartial => T();

    public string SettingPage_Gamepad => T();
    public string GamepadPage_Title => T();
    public string GamepadPage_Enable => T();
    public string GamepadPage_Guide => T();
    public string GamepadPage_Share => T();
    public string GamepadPage_KeysPlaceholder => T();
    public string GamepadPage_BadKey => T();
    public string GamepadPage_NeedRedist => T();
    public string GamepadPage_RedistOutdate => T();
    public string GamepadPage_RedistLink => T();
    public string GamepadPage_ShareNote => T();

    public string KuroBbs => T();
    public string KuroBbs_SignIn => T();
    public string KuroBbs_Role => T();
    public string KuroBbs_ResourceBrief => T();

    public string PgrData_NoRole => T();
    public string PgrData_Locked => T();
    public string PgrData_FullIn => T();

    public string KuroBbs_DataTerminal => T();
    public string KuroBbs_CombatData => T();
    public string DataTerminal_NoAccount => T();
    public string DataTerminal_NoRole => T();
    public string DataTerminal_LoadFailed => T();

    public string KuroBbs_PgrRole => T();
    public string KuroBbs_PgrResource => T();
    public string KuroBbs_PgrCage => T();
    public string KuroBbs_PgrWarArea => T();
    public string KuroBbs_PgrTransfinite => T();
    public string PgrRole_Fight => T();

    public string PgrRoleDetail_Grade => T();
    public string PgrRoleDetail_Fight => T();

    public string PgrRoleDetail_Overrun => T();
    public string PgrRoleDetail_Wiki => T();
    public string PgrDetail_Weapon => T();
    public string PgrDetail_WeaponSkill => T();
    public string PgrDetail_OverrunLevel => T();
    public string PgrDetail_WeaponSuit => T();
    public string PgrDetail_WeaponOverrun2 => T();
    public string PgrDetail_WeaponResonance => T();
    public string PgrDetail_Partner => T();
    public string PgrDetail_ChipSuit => T();
    public string PgrDetail_ChipResonance => T();
    public string PgrDetail_ChipExDamage => T();
    public string PgrDetail_SuperSlot => T();
    public string PgrDetail_SubSlot => T();
    public string PgrDetail_Inactive => T();
    public string PgrDetail_SuitCount => T();
    public string PgrDetail_Suit2 => T();
    public string PgrDetail_Suit4 => T();
    public string PgrDetail_Suit6 => T();
    public string PgrDetail_PartnerLevel => T();
    public string PgrDetail_PartnerBreak => T();
    public string PgrDetail_SkillLevel => T();
    public string PgrResource_Total => T();
    public string PgrResource_BlackCard => T();
    public string PgrResource_Develop => T();
    public string PgrResource_Trade => T();
    public string PgrResource_SelectPeriod => T();
    public string PgrResource_Composition => T();
    public string PgrResource_MonthlyData => T();
    public string PgrChallenge_NotOpen => T();
    public string PgrChallenge_Locked => T();
    public string PgrChallenge_NoArea => T();
    public string PgrChallenge_NoRecord => T();
    public string PgrChallenge_Level => T();
    public string PgrWarArea_TotalPointLabel => T();
    public string PgrWarArea_TotalTimesLabel => T();
    public string PgrWarArea_Waves => T();
    public string PgrCage_TotalPoint => T();
    public string PgrCage_FightTime => T();
    public string PgrCage_Point => T();
    public string PgrCage_Level => T();
    public string PgrTransfinite_MaxProgress => T();
    public string PgrTransfinite_TimeCost => T();
    public string PgrTransfinite_OperatorLabel => T();
    public string PgrTransfinite_TeamTitle => T();
    public string PgrCommon_LoadFailed => T();

    public string DailyBriefing => T();
    public string DailyBriefing_Desc => T();
    public string DailyBriefing_Refresh => T();

    public string DailyBriefing_NoGameAccount => T();

    public string DailyBriefing_NoRole => T();

    public string DailyBriefing_Error => T();

    public string Briefing_Liveness => T();

    public string Briefing_StoreEnergy => T();

    public string Briefing_BattlePass => T();

    public string Briefing_ActionPoint => T();

    public string Briefing_Level => T();

    public string KuroBbs_TowerOfAdversity => T();
    public string KuroBbs_SongOfVoidSea => T();
    public string KuroBbs_DoomsdayMatrix => T();
    public string KuroBbs_ComingSoon => T();
    public string KuroBbs_TokenExpired => T();
    public string KuroBbs_TokenExpiredDesc => T();

    public string KuroAccount_UsbTitle => T();
    public string KuroAccount_UsbShortDesc => T();
    public string KuroAccount_UsbDesc => T();
    public string KuroAccount_UsbDescTail => T();
    public string KuroAccount_UsbVerWarn => T();
    public string KuroAccount_AdbPlaceholder => T();
    public string KuroAccount_ApplyAdb => T();
    public string KuroAccount_SelectDevice => T();
    public string KuroAccount_RefreshDevices => T();
    public string KuroAccount_StartCapture => T();
    public string KuroAccount_StopCapture => T();
    public string KuroAccount_FieldsTitle => T();
    public string KuroAccount_FillHint => T();
    public string KuroAccount_Token => T();
    public string KuroAccount_Did => T();
    public string KuroAccount_Save => T();
    public string KuroAccount_Clear => T();
    public string KuroAccount_Log => T();
    public string KuroAccount_AdbInvalidTitle => T();
    public string KuroAccount_AdbInvalidDesc => T();
    public string KuroAccount_AdbApplied => T();
    public string KuroAccount_AdbApplyFailed => T();
    public string KuroAccount_NoDevice => T();
    public string KuroAccount_NoDeviceDesc => T();
    public string KuroAccount_RefreshDevicesFailed => T();
    public string KuroAccount_SelectDeviceFirst => T();
    public string KuroAccount_SelectDeviceFirstDesc => T();
    public string KuroAccount_NoPage => T();
    public string KuroAccount_NoPageDesc => T();
    public string KuroAccount_CaptureFailed => T();
    public string KuroAccount_CaptureStarted => T();
    public string KuroAccount_CaptureStartedDesc => T();
    public string KuroAccount_Disconnected => T();
    public string KuroAccount_DisconnectedDesc => T();
    public string KuroAccount_NoSocket => T();
    public string KuroAccount_NoSocketDesc => T();
    public string KuroAccount_DiagTitle => T();
    public string KuroAccount_DiagHint => T();
    public string KuroAccount_DiagCopy => T();
    public string KuroAccount_DiagClose => T();
    public string KuroAccount_DiagCopied => T();
    public string KuroAccount_Invalid => T();
    public string KuroAccount_SaveNeedTokenDid => T();
    public string KuroAccount_Saved => T();
    public string KuroAccount_Cleared => T();

    public string RolePage_Refresh => T();
    public string RolePage_NoAccount => T();
    public string RolePage_NoAccountDesc => T();
    public string RolePage_LoadFailed => T();
    public string RolePage_Back => T();
    public string RolePage_Phantoms => T();

    public string GachaLogPage_Refresh => T();
    public string GachaLogPage_ImportUrl => T();
    public string GachaLogPage_ImportUrlDesc => T();
    public string GachaLogPage_ImportUrlPlaceholder => T();
    public string GachaLogPage_UrlInvalid => T();
    public string GachaLogPage_SelectPlayer => T();
    public string GachaLogPage_CopyUrl => T();
    public string GachaLogPage_DeletePlayer => T();
    public string GachaLogPage_Delete => T();
    public string GachaLogPage_Avg5 => T();
    public string GachaLogPage_Up => T();
    public string GachaLogPage_Stat => T();
    public string GachaLogPage_Times => T();
    public string GachaLogPage_ItemStats => T();

    public string GachaLogPage_UpdateSavedUrl => T();
    public string GachaLogPage_UpdateAll => T();
    public string GachaLogPage_InputUrl => T();
    public string GachaLogPage_ViaCloudGame => T();

    public string CloudGame_Title => T();
    public string CloudGame_Grab => T();
    public string CloudGame_StatusDefault => T();
    public string CloudGame_StatusSessionReady => T();
    public string CloudGame_StatusGrabbing => T();
    public string CloudGame_StatusOk => T();
    public string CloudGame_StatusFailReason => T();
    public string CloudGame_StatusNoRuntime => T();

    public string GachaLogPage_ImportExport => T();
    public string GachaLogPage_HidePools => T();
    public string GachaLogPage_ExportAs => T();
    public string GachaLogPage_ExportFormatUgf => T();
    public string GachaLogPage_ExportFormatExcel => T();
    public string GachaLogPage_ExportFormatCsv => T();
    public string GachaLogPage_ImportRecords => T();
    public string GachaLogPage_ImportOk => T();
    public string GachaLogPage_ImportFail => T();
    public string GachaLogPage_ImportFailNoPlayer => T();

    public string GachaLogPage_OtherSettings => T();
    public string GachaLogPage_LinkHelp => T();
    public string GachaLogPage_LinkHelpDesc => T();
    public string GachaLogPage_ClearSavedUrl => T();
    public string GachaLogPage_ClearSavedUrlConfirm => T();
    public string GachaLogPage_ClearSavedUrlDone => T();
    public string GachaLogPage_Language => T();
    public string GachaLogPage_LanguageSaved => T();
    public string GachaLogPage_LanguageApplied => T();

    public string GachaLangOption_ZhHans => $"{Language_Chinese}（zh-Hans）";
    public string GachaLangOption_ZhHant => $"{Language_HantChinese}（zh-Hant）";
    public string GachaLangOption_En => $"{Language_English}（en）";
    public string GachaLangOption_Ja => $"{Language_Japanese}（ja）";
    public string GachaLangOption_Ko => $"{Language_Korean}（ko）";
    public string GachaLangOption_Fr => $"{Language_French}（fr）";
    public string GachaLangOption_De => $"{Language_German}（de）";
    public string GachaLangOption_Es => $"{Language_Spanish}（es）";
    public string GachaLangOption_Th => $"{Language_Thai}（th）";
    public string GachaLangOption_PtBr => $"{Language_PortugueseBrazil}（pt-BR）";
    public string GachaLogPage_NoSavedUrl => T();
    public string GachaLogPage_LinkExpired => T();
    public string GachaLogPage_LinkExpiredDesc => T();
    public string GachaLogPage_FetchEmpty => T();
    public string GachaLogPage_FetchEmptyDesc => T();
    public string GachaLogPage_RefetchFromCloudGame => T();
    public string GachaLogPage_UpdateFailed => T();
    public string GachaLogPage_FetchFailed => T();
    public string GachaLogPage_LogNotFound => T();
    public string GachaLogPage_UrlNotFound => T();
    public string GachaLogPage_UrlCopied => T();
    public string GachaLogPage_PlayerDeleted => T();
    public string GachaLogPage_DeleteConfirmText => T();
    public string GachaLogPage_EmptyTitle => T();
    public string GachaLogPage_EmptyDesc => T();

    public string LauncherPage_StartGame => T();
    public string LauncherPage_InstallGame => T();
    public string LauncherPage_ResumeInstall => T();

    public string AccountSwitcher_SwitchAccount => T();
    public string AccountSwitcher_AccountInfo => T();
    public string AccountSwitcher_Nickname => T();
    public string AccountSwitcher_RoleId => T();
    public string AccountSwitcher_Save => T();
    public string AccountSwitcher_Delete => T();
    public string AccountSwitcher_GameRunning => T();
    public string AccountSwitcher_Unsaved => T();

    public string Notice_Activity => T();
    public string Notice_NoticeTab => T();
    public string Notice_News => T();
    public string Notice_LoadFailed => T();
    public string LauncherPage_UpdateGame => T();

    public string SettingPage_Language => T();

    public string Language_HantChinese => "繁體中文";
    public string Language_French => "Français";
    public string Language_German => "Deutsch";
    public string Language_Spanish => "Español";
    public string Language_Thai => "ไทย";
    public string Language_PortugueseBrazil => "Português (Brasil)";

    public string GameUpdate_PredownloadAvailable => T();
    public string GameUpdate_Predownloaded => T();
    public string GameUpdate_PredownloadButton => T();
    public string GameUpdate_ContinuePredownload => T();
    public string GameUpdate_ConfirmPredownload => T();
    public string GameUpdate_PredownloadTitle => T();
    public string GameUpdate_PackageSize => T();
    public string GameUpdate_SpaceRequired => T();
    public string GameUpdate_PredownloadedSize => T();
    public string GameUpdate_Applying => T();

    public string GameUpdate_Pausing => T();

    public string GameUpdate_ApplyingProgress => T();
    public string GameUpdate_Patching => T();
    public string GameUpdate_GameRunning => T();
    public string GameUpdate_ErrPatchFailed => T();
    public string GameUpdate_ErrNotEnoughSpaceForPatch => T();
    public string GameUpdate_ErrPatchTool => T();
    public string GameUpdate_Predownloading => T();
    public string GameUpdate_Done => T();
    public string GameUpdate_InstallDone => T();
    public string GameUpdate_PredownloadDone => T();
    public string GameUpdate_PredownloadFinished => T();
    public string GameUpdate_NoGameDir => T();

    public string GameUpdate_Verifying => T();
    public string GameUpdate_VerifyReportTitle => T();
    public string GameUpdate_VerifyDoneNone => T();
    public string GameUpdate_VerifyDoneDamage => T();
    public string GameUpdate_VerifyNoManifest => T();
    public string GameUpdate_Repairing => T();
    public string GameUpdate_RepairDone => T();
    public string GameUpdate_RepairNoBaseUrl => T();

    public string GameLauncherPage_Gallery => T();
    public string Common_Copy => T();
    public string ImageViewWindow2_SaveAs => T();
    public string GameLauncherPage_PlayPause => T();
    public string GameLauncherSettingDialog_DragAndDropImageOrVideoFiles => T();

    public string Background_Official => T();
    public string Background_OfficialStatic => T();
    public string Background_OpenFolder => T();
    public string Background_AddFailed => T();
    public string InstallGameDialog_Installed => T();
    public string InstallGameDialog_LocateGame => T();
    public string InstallGameDialog_SelectInstallationPath => T();
    public string Common_Change => T();
    public string InstallGameDialog_SpaceRequiredToDownload => T();
    public string InstallGameDialog_SpaceAvailable => T();
    public string InstallGameDialog_InsufficientSpace => T();
    public string InstallGameDialog_HardLinkEstimateDesc => T();
    public string InstallGameDialog_HardLinkAfterSize => T();
    public string InstallGameDialog_EstimateOnly => T();
    public string InstallGameDialog_SpaceDesc => T();

    public string InstallGameDialog_TierUHD => T();
    public string InstallGameDialog_TierHD => T();
    public string InstallGameDialog_TierSD => T();
    public string GameLauncherPage_GameIsRunning => T();

    public string SettingPage_Download => T();

    public string DownloadGamePage_Downloading => T();
    public string DownloadGamePage_Verifying => T();
    public string DownloadGamePage_Pause => T();

    public string Common_Confirm => T();
    public string Common_Cancel => T();
    public string Common_Close => T();
    public string Common_Save => T();
    public string Common_Error => T();
    public string Common_Ok => T();

    public string SettingPage_AppSettings => T();
    public string SettingPage_About => T();
    public string SettingPage_General => T();
    public string SettingPage_FileManagement => T();

    public string FilePage_DataFolder => T();
    public string FilePage_DataFolderDesc => T();
    public string FilePage_ReselectDataFolder => T();
    public string FilePage_OpenDataFolder => T();
    public string FilePage_DeleteAllSettings => T();
    public string FilePage_ReselectFolderDone => T();
    public string FilePage_DeleteSettingsDone => T();
    public string FilePage_DeleteSettingsConfirm => T();
    public string FilePage_OpenLogFolder => T();
    public string FilePage_Cache => T();
    public string FilePage_CacheLog => T();
    public string FilePage_CacheImage => T();
    public string FilePage_CacheBackground => T();
    public string FilePage_CleanCache => T();
    public string FilePage_CleanBackgroundCache => T();

    public string FilePage_HardLink => T();
    public string FilePage_HardLinkDesc => T();
    public string FilePage_Enabled => T();
    public string FilePage_Disabled => T();

    public string FilePage_SpeedLimit => T();
    public string FilePage_SpeedLimitDesc => T();

    public string FilePage_DownloadThreads => T();
    public string FilePage_DownloadThreadsDesc => T();

    public string KuroBbs_RefreshInfo => T();
    public string KuroBbs_Login => T();

    public string KuroBbs_Settings => T();

    public string Login_Title => T();
    public string Login_ByToken => T();
    public string Login_BySms => T();
    public string Login_TokenPlaceholder => T();
    public string Login_DidPlaceholder => T();
    public string Login_TokenTip => T();
    public string Login_PhonePlaceholder => T();
    public string Login_CodePlaceholder => T();
    public string Login_CodeLabel => T();
    public string Login_SmsTip => T();
    public string Login_SendCode => T();
    public string Login_Confirm => T();
    public string Login_Cancel => T();
    public string Login_PhoneInvalid => T();
    public string Login_Sending => T();
    public string Login_CodeSent => T();
    public string Login_CodeSendFailed => T();
    public string Login_NeedCode => T();
    public string Login_TokenInvalid => T();
    public string Login_Working => T();
    public string Login_Failed => T();
    public string Login_SeekRoleFailed => T();
    public string Login_Success => T();

    public string ResourceBrief_Month => T();
    public string ResourceBrief_Week => T();
    public string ResourceBrief_Version => T();
    public string ResourceBrief_HistoryData => T();
    public string ResourceBrief_TotalGain => T();
    public string ResourceBrief_StarComposition => T();
    public string ResourceBrief_CoinComposition => T();
    public string ResourceBrief_Detail => T();
    public string ResourceBrief_Empty => T();

    public string SignPage_DoSignIn => T();
    public string SignPage_MonthlySign => T();
    public string SignPage_SignedToday => T();
    public string SignPage_NotSignedToday => T();
    public string SignPage_Done => T();
    public string SignPage_AutoSign => T();
    public string SignPage_SignedDaysFormat => T();
    public string SignPage_DayFormat => T();
    public string SignPage_Energy => T();
    public string TowerPage_TotalStarCount => T();
    public string TowerPage_Floor => T();
    public string TowerPage_NoChallengeData => T();
    public string TowerPage_RefreshAfter => T();
    public string MatrixPage_Round => T();
    public string MatrixPage_NoRecord => T();

    public string MatrixPage_TotalScore => T();
    public string MatrixPage_Progress => T();
    public string MatrixPage_BossFormat => T();

    public string MatrixPage_Overview => T();
    public string MatrixPage_Singularity => T();

    public string SlashPage_ScoreLabel => T();
    public string SlashPage_ChallengeFallback => T();
    public string SlashPage_GroupScore => T();
    public string SlashPage_GroupScoreNamed => T();
    public string SlashPage_Team1 => T();
    public string SlashPage_Team2 => T();
    public string SlashPage_TeamN => T();
    public string CombatPage_Locked => T();
    public string CombatPage_NoData => T();
    public string CombatPage_StatsPeriod => T();
    public string CombatPage_ClearRecords => T();
    public string CombatPage_ClearRecordsConfirm => T();
    public string CombatPage_Clear => T();

    public string PlayTimeStatsDialog_TotalPlaytime => T();
    public string PlayTimeStatsDialog_Started0Times => T();
    public string PlayTimeStatsDialog_AverageDailyPlaytime => T();
    public string PlayTimeStatsDialog_PlayedFor0Days => T();
    public string PlayTimeStatsDialog_LongestStreak => T();
    public string PlayTimeStatsDialog_0Days => T();
    public string PlayTimeStatsDialog_LongestSession => T();
    public string PlayTimeStatsDialog_LongestDailyPlaytime => T();
    public string PlayTimeButton_LastStartup => T();
    public string PlayTimeStatsDialog_Last15Days => T();
    public string PlayTimeStatsDialog_Last12Weeks => T();
    public string PlayTimeStatsDialog_Last12Months => T();
    public string PlayTimeStatsDialog_Total => T();

    public string ExperienceSettingPage_MinimizeToSystemTray => T();

    public string GameLauncherSettingDialog_BasicInformation => T();
    public string GameLauncherSettingDialog_ConfirmUninstall => T();
    public string GameLauncherSettingDialog_CustomBackground => T();
    public string GameLauncherSettingDialog_BgFps => T();
    public string GameLauncherSettingDialog_FpsUnlimited => T();
    public string GameLauncherSettingDialog_BgQuality => T();
    public string GameLauncherSettingDialog_QualityAuto => T();
    public string GameLauncherSettingDialog_QualityHigh => T();
    public string GameLauncherSettingDialog_QualityMedium => T();
    public string GameLauncherSettingDialog_QualityLow => T();
    public string GameLauncherSettingDialog_PauseOnGameRun => T();
    public string GameLauncherSettingDialog_RelocateGame => T();
    public string GameLauncherSettingDialog_Size => T();
    public string GameLauncherSettingDialog_StartRepairing => T();
    public string GameLauncherSettingDialog_StartupArguments => T();
    public string GameLauncherSettingDialog_CommandLineArgument => T();
    public string GameLauncherSettingDialog_UninstallGameWarning => T();
    public string GameLauncherSettingDialog_CannotDeleteDriveRoot => T();
    public string GameLauncherSettingDialog_ScreenshotsFound => T();
    public string GameLauncherSettingDialog_BackupAndUninstall => T();
    public string GameLauncherSettingDialog_Uninstalled => T();

    public string LauncherPage_RepairGame => T();
    public string LauncherPage_ShowGameAnnouncement => T();
    public string LauncherPage_UninstallGame => T();
    public string LauncherPage_CustomStartExecutable => T();

    public string InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes => T();

    public string Language_Chinese => "简体中文";
    public string Language_English => "English";
    public string Language_Japanese => "日本語";
    public string Language_Korean => "한국어";

    public string Common_Select => T();

    public string PlayTimeStatsDialog_PlaytimeStatistics => T();
    public string PlayTimeStatsDialog_StatsTip => T();

    public string SettingPage_BgSizeTip => T();
    public string SettingPage_CustomBackgroundSupportVideo => T();

    public string Common_Apply => T();
    public string GameSettingPage_Resolution => T();
    public string GameSettingPage_FullScreen => T();
    public string GameSettingPage_PgrFullscreenBlockedTip => T();
    public string GameSettingPage_CustomResolution => T();
    public string GameSettingPage_CustomResolutionTip => T();
    public string GameSettingPage_VoiceLanguage => T();
    public string GameSettingPage_VoiceLanguageDescription => T();
    public string GameSettingPage_HDR => T();
    public string GameSettingPage_EnableHDR => T();
    public string GameSettingPage_HDRNotSupported => T();
    public string GameSettingPage_HDRNotEnabled => T();
    public string GameSettingPage_GameNotInstalled => T();
    public string GameSettingPage_GameNotInstalledDesc => T();
    public string GameSettingPage_CloudGameNotSupport => T();
    public string GameSettingPage_SettingNotEffect => T();
    public string GameSettingPage_ResolutionMustBeGreaterThan0 => T();
    public string GameSettingPage_LoadFailed => T();
    public string GameSettingPage_ApplyFailed => T();
    public string GameSettingPage_Applied => T();

    public string SettingPage_WindowBehavior => T();
    public string SettingPage_CloseWindowOption => T();
    public string SettingPage_ExitApp => T();
    public string SettingPage_AfterLaunchOption => T();
    public string SettingPage_KeepWindow => T();
    public string SettingPage_AfterGameExitOption => T();
    public string SettingPage_RestoreMainWindow => T();
    public string SettingPage_KeepHidden => T();
    public string SettingPage_CloseLauncher => T();
    public string SettingPage_KuroCommunitySign => T();
    public string SettingPage_SuggestSystemFeatures => T();
    public string SettingPage_TransparencyEffects => T();
    public string SettingPage_AnimationEffects => T();

    public string SettingPage_CurrentVersion => T();

    public string SettingPage_About_Copyright => T();

    public string SettingPage_About_Links => T();
    public string SettingPage_ProjectHomepage => T();
    public string SettingPage_ReportIssue => T();
    public string SettingPage_ReleaseHistory => T();

    public string SettingPage_Donate => T();

    public string Common_Exit => T();

    public string GameSelector_Subtitle => T();
    public string GameSelector_AutoFind => T();
    public string GameSelector_SelectServer => T();
    public string GameSelector_InstalledSize => T();
    public string GameSelector_Calculating => T();

    public string KuroBbs_AccountLevelLabel => T();
    public string KuroBbs_SwitchRole => T();
    public string KuroBbs_ClearAccount => T();

    public string GameBiz_Wuwa => T();
    public string GameBiz_WuwaCn => T();
    public string GameBiz_WuwaGlobal => T();
    public string GameBiz_WuwaBilibili => T();
    public string GameBiz_Pgr => T();
    public string GameBiz_ShortCn => T();
    public string GameBiz_ShortGlobal => T();
    public string GameBiz_ShortBilibili => T();
    public string GameBiz_ShortPgr => T();
    public string GameBiz_PgrGlobal => T();
    public string GameBiz_PgrKorea => T();
    public string GameBiz_PgrJapan => T();
    public string GameBiz_PgrTaiwan => T();
    public string GameBiz_ShortKorea => T();
    public string GameBiz_ShortJapan => T();
    public string GameBiz_ShortTaiwan => T();

    public string GachaPool_CharacterEvent => T();
    public string GachaPool_WeaponEvent => T();
    public string GachaPool_CharacterStandard => T();
    public string GachaPool_WeaponStandard => T();
    public string GachaPool_Novice => T();
    public string GachaPool_NoviceChoice => T();
    public string GachaPool_NoviceGratitude => T();
    public string GachaPool_CharacterRookie => T();
    public string GachaPool_WeaponRookie => T();
    public string GachaPool_CharacterCollab => T();
    public string GachaPool_WeaponCollab => T();
    public string GachaPool_CharacterRevisit => T();
    public string GachaPool_WeaponRevisit => T();
    public string GachaPool_Unknown => T();

    public string Adb_DeviceOnline => T();
    public string Adb_DeviceUnauthorized => T();

    public string Sync_Title => T();
    public string Sync_Description => T();
    public string Sync_ServerUrl => T();
    public string Sync_UserName => T();
    public string Sync_Password => T();
    public string Sync_ServerEntry => T();
    public string Sync_EnableOff => T();
    public string Sync_EnableNeedServer => T();
    public string Sync_ConnectTitle => T();
    public string Sync_ConnectDesc => T();
    public string Sync_ConnectInvalid => T();
    public string Sync_SaveAndTest => T();
    public string Sync_Testing => T();
    public string Sync_ConnectOk => T();
    public string Sync_ConnectFail => T();
    public string Sync_SyncNow => T();
    public string Sync_RevealPassword => T();
    public string Sync_Configured => T();
    public string Sync_NotConfigured => T();
    public string Sync_NeverSynced => T();
    public string Sync_LastSync => T();
    public string Sync_Working => T();

    public string CloudGame_ErrNoLogin => T();
    public string CloudGame_ErrHttp => T();
    public string CloudGame_ErrRequest => T();
    public string CloudGame_ErrParse => T();
    public string CloudGame_ErrEmpty => T();
    public string CloudGame_ErrCode => T();

    public string GachaLogPage_ExportEmpty => T();

    public string GachaLog_PityBuilt => T();

    public string GameLauncherSettingDialog_PathRemoved => T();
    public string GameLauncherSettingDialog_WrongServerExe => T();
    public string GameLauncherSettingDialog_ExpectExe => T();

    public string KrSdk_ErrNoUserCache => T();
    public string KrSdk_ErrEmptyContent => T();
    public string KrSdk_ErrParseFailed => T();
    public string KrSdk_ErrNoAccountList => T();
    public string KrSdk_ErrAccountNotInCache => T();
    public string KrSdk_ErrVerifyFailed => T();

    public string GameUpdate_ErrNoPlan => T();
    public string GameUpdate_ErrNoTarget => T();
    public string GameUpdate_ErrNoGamePath => T();
    public string GameUpdate_ErrNoManifest => T();
    public string GameUpdate_ErrFileMissing => T();
    public string GameUpdate_ErrFileChecksum => T();

    public string GameSetting_ErrNoLocalStorage => T();
    public string GameSetting_ErrNoGameUserSettings => T();

    public string KuroBbs_ErrConnection => T();
    public string KuroBbs_ErrAccessTokenEmpty => T();
    public string KuroBbs_ErrAccessToken => T();
    public string KuroBbs_ErrSendCode => T();
    public string KuroBbs_ErrLogin => T();
    public string KuroBbs_SignOk => T();
    public string KuroBbs_SignAlready => T();
    public string KuroBbs_SignFail => T();

    public string Ugf_ErrNoUid => T();
    public string Ugf_ErrUnknownFormat => T();

    public string CombatPage_AreaFallback => T();

    public string KuroBbs_UnionLevel => T();
    public string KuroBbs_CommanderLevel => T();
    public string KuroBbs_NoLevel => T();
    public string KuroBbs_SelectRolePgr => T();
    public string KuroBbs_SelectRoleWuwa => T();
    public string KuroBbs_SwitchedPgr => T();
    public string KuroBbs_SwitchedWuwa => T();
    public string PgrData_DailySupply => T();
    public string KuroBbs_AutoSignDoneToast => T();
    public string KuroBbs_AutoSignFailedToast => T();

    public string RolePage_WeaponLevel => T();

    public string Common_UnknownError => T();

}
