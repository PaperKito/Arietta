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
            [UiLanguageKind.English] = LangEn.Map,
            [UiLanguageKind.TraditionalChinese] = LangHant.Map,
            [UiLanguageKind.Japanese] = LangJa.Map,
            [UiLanguageKind.Korean] = LangKo.Map,
            [UiLanguageKind.French] = LangFr.Map,
            [UiLanguageKind.German] = LangDe.Map,
            [UiLanguageKind.Spanish] = LangEs.Map,
            [UiLanguageKind.Thai] = LangTh.Map,
            [UiLanguageKind.PortugueseBrazil] = LangPtBr.Map,
        };


    
    private static string T(string zh, [CallerMemberName] string? key = null)
    {
        return TFor(UiLanguage.Current, zh, key);
    }


    
    private static string TFor(UiLanguageKind kind, string zh, string? key)
    {
        return key is not null
            && Maps.TryGetValue(kind, out var map)
            && map.TryGetValue(key, out string? translated) ? translated : zh;
    }


    
    public string MainPage_Launcer => T("启动器");
    public string LauncherPage_GameSetting => T("游戏设置");
    public string MainPage_GameScreenshot => T("游戏截图");
    public string GachaLogPage_ConveneRecords => T("调频记录");
    public string Toolbox => T("工具箱");

    
    public string ScreenshotPage_MultiSelect => T("多选");
    public string ScreenshotPage_OpenFolder => T("打开文件夹");
    public string ScreenshotPage_Total0Files => T("共 {0} 张");
    public string ScreenshotPage_Selected0Of1 => T("已选 {0} / {1}");
    public string ScreenshotPage_Empty => T("未找到截图文件夹");
    public string ScreenshotPage_EmptyDesc => T("截图默认保存在 <游戏目录>\\Client\\Saved\\Screenshots，可先在设置里定位游戏目录");
    public string ScreenshotPage_Delete => T("删除");
    public string ScreenshotPage_DeleteConfirm => T("确定把选中的 {0} 张截图移到回收站吗？");
    public string ScreenshotPage_Deleted => T("已把 {0} 张截图移到回收站");
    public string ScreenshotPage_DeleteFailed => T("删除失败：部分文件可能正被其他程序使用");
    public string ScreenshotPage_SelectFirst => T("先勾选要删除的截图");
    public string ScreenshotPage_OpenImage => T("打开图片");
    public string ScreenshotPage_DeletedPartial => T("已删除 {0} / {1} 张，其余可能正被占用");

    
    public string SettingPage_Gamepad => T("手柄");
    public string GamepadPage_Title => T("手柄映射");
    public string GamepadPage_Enable => T("启用手柄功能");
    public string GamepadPage_Guide => T("Guide 键（Xbox 徽标键）");
    public string GamepadPage_Share => T("Share 键（分享键）");
    public string GamepadPage_KeysPlaceholder => T("多键需用空格分隔");
    public string GamepadPage_BadKey => T("无法识别按键：{0}");
    public string GamepadPage_NeedRedist => T("缺少 GameInput 运行库（Win11 系统自带，Win10 需手动安装）。请点击下方链接下载安装，完成后重启启动器即可启用手柄功能。");
    public string GamepadPage_RedistOutdate => T("已安装的 GameInput 运行库版本过旧，请点击下方链接安装新版，完成后重启启动器。");
    public string GamepadPage_RedistLink => T("下载 GameInputRedist.msi");
    public string GamepadPage_ShareNote => T("注意：由于系统限制，设置 Share 键后，Xbox Game Bar 的自带截图可能仍会同时触发，造成重复截图，建议优先使用 Guide 键。");

    
    public string KuroBbs => T("库街区");
    public string KuroBbs_SignIn => T("签到");
    public string KuroBbs_Role => T("角色");
    public string KuroBbs_ResourceBrief => T("资源简报");

    
    public string PgrData_NoRole => T("该库街区账号还没设置战双角色，请在库街区 App 的「我的」里设置后再查看。");
    public string PgrData_Locked => T("未解锁");
    public string PgrData_FullIn => T("{0} 后回满");

    
    public string KuroBbs_DataTerminal => T("数据终端");
    public string KuroBbs_CombatData => T("作战数据");
    public string DataTerminal_NoAccount => T("还没有登录库街区，请点上方账号箭头登录后查看。");
    public string DataTerminal_NoRole => T("该库街区账号还没有绑定{0}角色，请先在库街区 App 里绑定。");
    public string DataTerminal_LoadFailed => T("页面加载失败：{0}");

    
    public string KuroBbs_PgrRole => T("角色");
    public string KuroBbs_PgrResource => T("资源月报");
    public string KuroBbs_PgrCage => T("幻痛囚笼");
    public string KuroBbs_PgrWarArea => T("纷争战区");
    public string KuroBbs_PgrTransfinite => T("历战映射");
    public string PgrRole_Fight => T("战力 {0}");
    
    public string PgrRoleDetail_Grade => T("评级");
    public string PgrRoleDetail_Fight => T("战力");
    
    public string PgrRoleDetail_Overrun => T("增幅");
    public string PgrRoleDetail_Wiki => T("库街区 Wiki");
    public string PgrDetail_Weapon => T("专属武器");
    public string PgrDetail_WeaponSkill => T("武器技能");
    public string PgrDetail_OverrunLevel => T("超限 {0}/{1}");
    public string PgrDetail_WeaponSuit => T("武器共鸣套装");
    public string PgrDetail_WeaponOverrun2 => T("二次超限");
    public string PgrDetail_WeaponResonance => T("武器共鸣");
    public string PgrDetail_Partner => T("辅助机");
    public string PgrDetail_ChipSuit => T("意识套装");
    public string PgrDetail_ChipResonance => T("意识共鸣");
    public string PgrDetail_ChipExDamage => T("意识额外伤害");
    public string PgrDetail_SuperSlot => T("超频槽");
    public string PgrDetail_SubSlot => T("副槽");
    public string PgrDetail_Inactive => T("未激活");
    public string PgrDetail_SuitCount => T("{0} 件套");
    public string PgrDetail_Suit2 => T("2 件套");
    public string PgrDetail_Suit4 => T("4 件套");
    public string PgrDetail_Suit6 => T("6 件套");
    public string PgrDetail_PartnerLevel => T("等级 {0}");
    public string PgrDetail_PartnerBreak => T("突破 {0}");
    public string PgrDetail_SkillLevel => T("Lv.{0}");
    public string PgrResource_Total => T("近半年累计");
    public string PgrResource_BlackCard => T("黑卡");
    public string PgrResource_Develop => T("研发资源");
    public string PgrResource_Trade => T("贸易凭据");
    public string PgrResource_SelectPeriod => T("选择周期");
    public string PgrResource_Composition => T("收入组成");
    public string PgrResource_MonthlyData => T("每月数据");
    public string PgrChallenge_NotOpen => T("当前不在开放期");
    public string PgrChallenge_Locked => T("尚未解锁");
    public string PgrChallenge_NoArea => T("未选择分区");
    public string PgrChallenge_NoRecord => T("本期暂无挑战记录");
    public string PgrChallenge_Level => T("等级区间 {0}");
    public string PgrWarArea_TotalPointLabel => T("积分总值");
    public string PgrWarArea_TotalTimesLabel => T("挑战次数");
    public string PgrWarArea_Waves => T("波次 {0}");
    public string PgrCage_TotalPoint => T("讨伐总值");
    public string PgrCage_FightTime => T("用时 {0}s");
    public string PgrCage_Point => T("积分");
    public string PgrCage_Level => T("等级: {0}");
    public string PgrTransfinite_MaxProgress => T("最高进度");
    public string PgrTransfinite_TimeCost => T("耗时");
    public string PgrTransfinite_OperatorLabel => T("算符");
    public string PgrTransfinite_TeamTitle => T("出战构造体");
    public string PgrCommon_LoadFailed => T("数据获取失败");

    
    public string DailyBriefing => T("简报");
    public string DailyBriefing_Desc => T("游戏内日常数据");
    public string DailyBriefing_Refresh => T("刷新");

    
    public string DailyBriefing_NoGameAccount => T("未识别到登录账号，请登录后再试");

    
    public string DailyBriefing_NoRole => T("该账号还没识别出游戏角色，请先启动一次游戏（角色信息由官方 SDK 在线识别）。");

    public string DailyBriefing_Error => T("简报数据获取失败");

    public string Briefing_Liveness => T("每日活跃");

    public string Briefing_StoreEnergy => T("结晶单质");

    public string Briefing_BattlePass => T("先约电台");

    public string Briefing_ActionPoint => T("血清");

    
    public string Briefing_Level => T("等级");
    
    public string KuroBbs_TowerOfAdversity => T("逆境深塔");
    public string KuroBbs_SongOfVoidSea => T("冥歌海虚");
    public string KuroBbs_DoomsdayMatrix => T("终焉矩阵");
    public string KuroBbs_ComingSoon => T("{0}功能开发中");
    public string KuroBbs_TokenExpired => T("库街区登录已过期");
    public string KuroBbs_TokenExpiredDesc => T("请到 设置 → 库街区账号 重新抓取或填写 token");

    
    public string KuroAccount_UsbTitle => T("ADB 工具");
    public string KuroAccount_UsbShortDesc => T("连接安卓手机，抓取库街区 Token 和 Did");
    public string KuroAccount_UsbDesc => T("用数据线连接手机并开启 USB 调试：手机上打开库街区 App 并进入「数据终端」或「养成计算器」，然后点击「开始抓取」，工具会自动抓取 token 和 did。需提供 adb.exe（可从 ");
    public string KuroAccount_UsbDescTail => T(" 官方页面下载）。");
    public string KuroAccount_UsbVerWarn => T("⚠ 新版库街区 App 已不可用此方式抓取，请使用旧版本。");
    public string KuroAccount_AdbPlaceholder => T("adb.exe 路径（留空用 PATH）");
    public string KuroAccount_ApplyAdb => T("应用");
    public string KuroAccount_SelectDevice => T("选择设备");
    public string KuroAccount_RefreshDevices => T("刷新");
    public string KuroAccount_StartCapture => T("开始抓取");
    public string KuroAccount_StopCapture => T("停止抓取");
    public string KuroAccount_FieldsTitle => T("账号信息");
    public string KuroAccount_FillHint => T("抓取结果自动填入，也可手动粘贴");
    public string KuroAccount_Token => T("Token");
    public string KuroAccount_Did => T("Did");
    public string KuroAccount_Save => T("保存");
    public string KuroAccount_Clear => T("清除");
    public string KuroAccount_Log => T("抓取日志");
    public string KuroAccount_AdbInvalidTitle => T("ADB 路径无效");
    public string KuroAccount_AdbInvalidDesc => T("请填写有效的 adb.exe 完整路径");
    public string KuroAccount_AdbApplied => T("ADB 路径已应用");
    public string KuroAccount_AdbApplyFailed => T("应用 ADB 路径失败");
    public string KuroAccount_NoDevice => T("未找到安卓设备");
    public string KuroAccount_NoDeviceDesc => T("请连接手机并允许 USB 调试授权");
    public string KuroAccount_RefreshDevicesFailed => T("刷新设备失败");
    public string KuroAccount_SelectDeviceFirst => T("请先选择设备");
    public string KuroAccount_SelectDeviceFirstDesc => T("先点「刷新」再选设备");
    public string KuroAccount_NoPage => T("未找到可抓取的网页");
    public string KuroAccount_NoPageDesc => T("先在手机上进入「数据终端」等网页页面，再点「开始抓取」");
    public string KuroAccount_CaptureFailed => T("抓取连接失败");
    public string KuroAccount_CaptureStarted => T("已开始监控");
    public string KuroAccount_CaptureStartedDesc => T("请在手机上进入「数据终端」或「养成计算器」页面");
    public string KuroAccount_Disconnected => T("手机连接已断开");
    public string KuroAccount_DisconnectedDesc => T("WebView 页面可能已关闭，可重新点「开始抓取」");
    public string KuroAccount_NoSocket => T("未找到 WebView 调试接口");
    public string KuroAccount_NoSocketDesc => T("该版本库街区可能已关闭 WebView 远程调试，详见诊断报告");
    public string KuroAccount_DiagTitle => T("抓取诊断报告");
    public string KuroAccount_DiagHint => T("报告里 devtools socket 为 0，说明这个版本的库街区 App 不再开启 WebView 远程调试（Chrome 的 chrome://inspect 也看不到它），ADB 抓取这条路走不通。此时改用「库街区 → 账号 → 登录」里的手机号 + 验证码登录即可拿到 Token 和 Did，不需要手机、不需要 ADB。");
    public string KuroAccount_DiagCopy => T("复制报告");
    public string KuroAccount_DiagClose => T("关闭");
    public string KuroAccount_DiagCopied => T("诊断报告已复制");
    public string KuroAccount_Invalid => T("信息不完整");
    public string KuroAccount_SaveNeedTokenDid => T("Token 和 Did 必填，roleId 可自动查询");
    public string KuroAccount_Saved => T("账号已保存");
    public string KuroAccount_Cleared => T("已清除账号");

    
    public string RolePage_Refresh => T("刷新");
    public string RolePage_NoAccount => T("尚未配置库街区账号");
    public string RolePage_NoAccountDesc => T("请到 设置 → 库街区账号 完成配置后刷新");
    public string RolePage_LoadFailed => T("加载角色失败");
    public string RolePage_Back => T("返回列表");
    public string RolePage_Phantoms => T("声骸");

    
    public string GachaLogPage_Refresh => T("更新记录");
    public string GachaLogPage_ImportUrl => T("粘贴链接导入");
    public string GachaLogPage_ImportUrlDesc => T("粘贴唤取记录页链接（游戏内打开「唤取记录」后从日志获取，或从其他工具复制），云手机玩家也可使用");
    public string GachaLogPage_ImportUrlPlaceholder => T("https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...");
    public string GachaLogPage_UrlInvalid => T("链接无效：请粘贴包含 record_id 的唤取记录页完整链接");
    public string GachaLogPage_SelectPlayer => T("选择账号");
    public string GachaLogPage_CopyUrl => T("复制 URL");
    public string GachaLogPage_DeletePlayer => T("删除记录");
    public string GachaLogPage_Delete => T("删除");
    public string GachaLogPage_Avg5 => T("平均");
    public string GachaLogPage_Up => T("/ UP");
    public string GachaLogPage_Stat => T("统计");
    public string GachaLogPage_Times => T("次");
    public string GachaLogPage_ItemStats => T("物品统计");
    
    public string GachaLogPage_UpdateSavedUrl => T("更新（保存的 URL）");
    public string GachaLogPage_UpdateAll => T("更新（全部）");
    public string GachaLogPage_InputUrl => T("输入 URL");
    public string GachaLogPage_ViaCloudGame => T("通过云游戏获取");
    
    public string CloudGame_Title => T("云鸣潮 - 登录后一键获取唤取链接");
    public string CloudGame_Grab => T("获取唤取链接");
    public string CloudGame_StatusDefault => T("在此登录库洛账号、点「进入游戏」并打开一次「唤取记录」界面，然后点击「获取唤取链接」");
    public string CloudGame_StatusSessionReady => T("已识别到云游戏登录态，点击「获取唤取链接」完成同步");
    public string CloudGame_StatusGrabbing => T("正在读取登录信息…");
    public string CloudGame_StatusOk => T("获取成功（玩家 {0}），正在同步调频记录…");
    public string CloudGame_StatusFailReason => T("获取失败：{0}");
    public string CloudGame_StatusNoRuntime => T("初始化内置浏览器失败：未安装 WebView2 运行时，请到微软官网安装后重试");
    
    public string GachaLogPage_ImportExport => T("导入 & 导出");
    public string GachaLogPage_HidePools => T("隐藏卡池");
    public string GachaLogPage_ExportAs => T("导出为");
    public string GachaLogPage_ExportFormatUgf => T("JSON(.json)");
    public string GachaLogPage_ExportFormatExcel => T("Excel (.xlsx)");
    public string GachaLogPage_ExportFormatCsv => T("CSV (.csv)");
    public string GachaLogPage_ImportRecords => T("导入记录（选择文件）");
    public string GachaLogPage_ImportOk => T("导入成功：玩家 {0} 共 {1} 条记录（重复记录自动去重）");
    public string GachaLogPage_ImportFail => T("导入失败：{0}");
    public string GachaLogPage_ImportFailNoPlayer => T("文件不含账号信息且当前没有选中账号，无法确定归属");
    
    
    public string GachaLogPage_OtherSettings => T("其他设置");
    public string GachaLogPage_LinkHelp => T("总是出现 Authkey Timeout？");
    public string GachaLogPage_LinkHelpDesc => T("删除缓存文件夹后重启游戏");
    public string GachaLogPage_ClearSavedUrl => T("删除缓存文件夹");
    public string GachaLogPage_ClearSavedUrlConfirm => T("确定删除本地保存的唤取链接吗？（已拉取的抽卡记录不会删除）");
    public string GachaLogPage_ClearSavedUrlDone => T("已清除保存的唤取链接，请进入游戏重新打开「唤取记录」页面");
    public string GachaLogPage_Language => T("语言");
    public string GachaLogPage_LanguageSaved => T("语言已保存，物品名称已按内置翻译表即时切换（表内缺的新物品保持原名，下次更新记录生效）");
    public string GachaLogPage_LanguageApplied => T("已把 {0} 条记录的物品名称更改为 {1}");

    
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
    public string GachaLogPage_NoSavedUrl => T("没有已保存的链接，请先执行一次「更新（全部）」");
    public string GachaLogPage_LinkExpired => T("唤取链接已失效");
    public string GachaLogPage_LinkExpiredDesc => T("链接已失效（接口返回 {0}），请在游戏内重新打开「唤取记录」");
    public string GachaLogPage_RefetchFromCloudGame => T("重新从云游戏获取");
    public string GachaLogPage_UpdateFailed => T("更新调频记录失败：{0}");
    public string GachaLogPage_FetchFailed => T("获取调频数据失败，请重试");
    public string GachaLogPage_LogNotFound => T("未找到游戏日志或唤取链接：请先在设置中定位游戏（Wuthering Waves.exe）并在游戏内打开一次「唤取记录」页面，或在「更新记录」菜单中选择「粘贴链接导入」");
    public string GachaLogPage_UrlNotFound => T("未找到唤取链接，请在游戏内打开一次「唤取记录」页面");
    public string GachaLogPage_UrlCopied => T("已复制唤取链接到剪贴板");
    public string GachaLogPage_PlayerDeleted => T("已删除 UID {0} 的本地唤取记录");
    public string GachaLogPage_DeleteConfirmText => T("即将删除 UID {0} 的所有本地唤取记录，此操作不可恢复。");
    public string GachaLogPage_EmptyTitle => T("暂无调频记录");
    public string GachaLogPage_EmptyDesc => T("先在设置中定位游戏目录，然后进入游戏打开一次「唤取记录」页面，回到这里点击「更新记录」即可同步数据。");

    
    public string LauncherPage_StartGame => T("开始游戏");
    public string LauncherPage_InstallGame => T("安装游戏");
    public string LauncherPage_ResumeInstall => T("继续安装");
    
    public string AccountSwitcher_SwitchAccount => T("切换账号");
    public string AccountSwitcher_AccountInfo => T("游戏账号");
    public string AccountSwitcher_Nickname => T("昵称");
    public string AccountSwitcher_RoleId => T("游戏 UID");
    public string AccountSwitcher_Save => T("保存");
    public string AccountSwitcher_Delete => T("删除");
    public string AccountSwitcher_GameRunning => T("游戏运行中，暂不能切换账号");
    public string AccountSwitcher_Unsaved => T("当前登录的账号未保存");
    
    public string Notice_Activity => T("活动");
    public string Notice_NoticeTab => T("公告");
    public string Notice_News => T("资讯");
    public string Notice_LoadFailed => T("公告加载失败");
    public string LauncherPage_UpdateGame => T("更新游戏");

    
    public string SettingPage_Language => T("语言");
    
    public string Language_HantChinese => "繁體中文";
    public string Language_French => "Français";
    public string Language_German => "Deutsch";
    public string Language_Spanish => "Español";
    public string Language_Thai => "ไทย";
    public string Language_PortugueseBrazil => "Português (Brasil)";

    
    public string GameUpdate_PredownloadAvailable => T("新版本 {0} 开放预下载");
    public string GameUpdate_Predownloaded => T("{0} 已预下载，开放更新后可直接安装");
    public string GameUpdate_PredownloadButton => T("预下载");
    public string GameUpdate_ContinuePredownload => T("继续预下载");
    public string GameUpdate_ConfirmPredownload => T("预下载版本 {0}？共需下载 {1}，文件将暂存在游戏目录的 predownload 文件夹里，官方开放更新后应用时无需再下载。");
    public string GameUpdate_PredownloadTitle => T("预下载新版本 {0}");
    public string GameUpdate_PackageSize => T("安装包大小 ");
    public string GameUpdate_PredownloadedSize => T("已预下载 ");
    public string GameUpdate_Applying => T("正在应用更新…");
    
    public string GameUpdate_Pausing => T("正在暂停…");

    public string GameUpdate_ApplyingProgress => T("正在应用更新 {0} / {1}");
    public string GameUpdate_Patching => T("正在应用补丁 {0} / {1}");
    public string GameUpdate_GameRunning => T("游戏正在运行，请先退出游戏再更新");
    public string GameUpdate_ErrPatchFailed => T("补丁应用失败：{0}（退出码 {1}）");
    public string GameUpdate_ErrNotEnoughSpaceForPatch => T("磁盘空间不足：应用补丁还需要至少 {0} 的可用空间");
    public string GameUpdate_ErrPatchTool => T("补丁工具缺失（Assets\\Hpatchz\\hpatchz.exe），请重新安装启动器");
    public string GameUpdate_Predownloading => T("正在预下载 {0}…");
    public string GameUpdate_Done => T("已更新到 {0}");
    public string GameUpdate_InstallDone => T("{0} 安装完成");
    public string GameUpdate_PredownloadDone => T("{0} 预下载完成，开放更新后可直接安装");
    public string GameUpdate_PredownloadFinished => T("预下载完成");
    public string GameUpdate_NoGameDir => T("请先在设置里定位游戏");

    
    public string GameUpdate_Verifying => T("正在校验 {0} / {1}");
    public string GameUpdate_VerifyReportTitle => T("校验完成");
    public string GameUpdate_VerifyDoneNone => T("未发现损坏或缺失的文件");
    public string GameUpdate_VerifyDoneDamage => T("发现 {0} 个文件损坏 / 缺失，共 {1}");
    public string GameUpdate_VerifyNoManifest => T("无法获取校验清单，请检查网络或稍后重试");
    public string GameUpdate_Repairing => T("正在修复…");
    public string GameUpdate_RepairDone => T("已修复 {0} 个文件");
    public string GameUpdate_RepairNoBaseUrl => T("当前版本不支持在线修复，请先将游戏更新到最新版本后再试");

    public string GameLauncherPage_Gallery => T("图库");
    public string Common_Copy => T("复制");
    public string ImageViewWindow2_SaveAs => T("另存为");
    public string GameLauncherPage_PlayPause => T("播放/暂停");
    public string GameLauncherSettingDialog_DragAndDropImageOrVideoFiles => T("拖放图片文件以更换背景");
    
    public string Background_Official => T("官方动态背景");
    public string Background_OfficialStatic => T("官方静态背景");
    public string Background_OpenFolder => T("打开背景文件夹");
    public string Background_AddFailed => T("无法使用该背景文件");
    public string InstallGameDialog_Installed => T("已安装？");
    public string InstallGameDialog_LocateGame => T("定位游戏");
    public string InstallGameDialog_SelectInstallationPath => T("选择安装路径");
    public string Common_Change => T("更改");
    public string InstallGameDialog_SpaceRequiredToDownload => T("下载所需空间 ");
    public string InstallGameDialog_SpaceAvailable => T("可用空间 ");
    public string InstallGameDialog_InsufficientSpace => T("可用空间不足");
    public string InstallGameDialog_HardLinkEstimateDesc => T("同盘其它区服客户端的相同文件将直接硬链接复用，预计实际需下载约 {0}；该值仅为预估值，实际以下载时逐文件校验为准。");
    public string InstallGameDialog_SpaceDesc => T("本启动器直接按游戏最终文件落盘下载，无压缩包合并/解压环节，不消耗额外流量、也不需要额外临时空间；「下载所需空间」即最终占用大小。");
    
    public string InstallGameDialog_TierUHD => T("极致(UHD)");
    public string InstallGameDialog_TierHD => T("高清(HD)");
    public string InstallGameDialog_TierSD => T("流畅(SD)");
    public string GameLauncherPage_GameIsRunning => T("游戏正在运行");

    
    public string SettingPage_Download => T("下载");

    
    public string DownloadGamePage_Downloading => T("下载中");
    public string DownloadGamePage_Verifying => T("校验中");
    public string DownloadGamePage_Pause => T("暂停");

    
    public string Common_Confirm => T("确认");
    public string Common_Cancel => T("取消");
    public string Common_Close => T("关闭");
    public string Common_Save => T("保存");
    public string Common_Error => T("错误");
    public string Common_Ok => T("确定");

    
    public string SettingPage_AppSettings => T("设置");
    public string SettingPage_About => T("关于");
    public string SettingPage_General => T("常规");
    public string SettingPage_FileManagement => T("文件管理");

    
    public string FilePage_DataFolder => T("数据文件夹");
    public string FilePage_DataFolderDesc => T("您的个人数据保存在下面的文件夹中，它很重要，请不要随意删除。");
    public string FilePage_ReselectDataFolder => T("重新选择数据文件夹");
    public string FilePage_OpenDataFolder => T("打开数据目录");
    public string FilePage_DeleteAllSettings => T("删除所有设置");
    public string FilePage_ReselectFolderDone => T("已保存新的数据文件夹，重启应用后生效。新目录初始为空，可用「云同步」把旧数据拉取回来。");
    public string FilePage_DeleteSettingsDone => T("设置已删除，重启应用后生效。");
    public string FilePage_DeleteSettingsConfirm => T("将删除全部设置（config.ini），重启应用后恢复默认值。游戏时长与抽卡记录等数据库内容不受影响。确定继续？");
    public string FilePage_OpenLogFolder => T("打开日志文件夹");
    public string FilePage_Cache => T("缓存");
    public string FilePage_CacheLog => T("日志");
    public string FilePage_CacheImage => T("图片");
    public string FilePage_CacheBackground => T("启动器背景");
    public string FilePage_CleanCache => T("清理缓存");
    public string FilePage_CleanBackgroundCache => T("清理启动器背景文件");

    
    public string FilePage_HardLink => T("硬链接");
    public string FilePage_HardLinkDesc => T("同盘安装多个客户端时，硬链接相同文件，从而减少占用的储存空间");
    public string FilePage_Enabled => T("已启用");
    public string FilePage_Disabled => T("已禁用");

    
    public string FilePage_SpeedLimit => T("限速");
    public string FilePage_SpeedLimitDesc => T("0表示不限速");

    
    public string FilePage_DownloadThreads => T("多线程");
    public string FilePage_DownloadThreadsDesc => T("同时建立的下载连接数，机械硬盘上过高并行会因寻道而变慢。改动对下次生效");

    
    public string KuroBbs_RefreshInfo => T("刷新信息");
    public string KuroBbs_Login => T("登录");

    
    public string KuroBbs_Settings => T("库街区设置");

    
    public string Login_Title => T("登录库街区");
    public string Login_ByToken => T("Token 登录");
    public string Login_BySms => T("验证码登录");
    public string Login_TokenPlaceholder => T("粘贴库街区登录 Token");
    public string Login_DidPlaceholder => T("与 Token 一起抓取的 Did（留空自动生成）");
    public string Login_TokenTip => T("Token 和 Did 从手机库街区 App 抓取（工具箱 → USB 抓取），两者成对保存");
    public string Login_PhonePlaceholder => T("手机号");
    public string Login_CodePlaceholder => T("短信验证码");
    public string Login_CodeLabel => T("验证码");
    public string Login_SmsTip => T("点击「发送验证码」后填写手机收到的短信验证码，登录成功自动保存账号");
    public string Login_SendCode => T("发送验证码");
    public string Login_Confirm => T("登录");
    public string Login_Cancel => T("取消");
    public string Login_PhoneInvalid => T("手机号格式不正确");
    public string Login_Sending => T("正在发送验证码…");
    public string Login_CodeSent => T("验证码已发送，请查收短信");
    public string Login_CodeSendFailed => T("发送验证码失败：{0}");
    public string Login_NeedCode => T("请填写短信验证码");
    public string Login_TokenInvalid => T("Token 无效：请粘贴完整的库街区登录 Token");
    public string Login_Working => T("正在登录…");
    public string Login_Failed => T("登录失败：{0}");
    public string Login_SeekRoleFailed => T("查询绑定角色失败：{0}");
    public string Login_Success => T("库街区账号「{0}」登录成功");

    
    public string ResourceBrief_Month => T("月度");
    public string ResourceBrief_Week => T("周度");
    public string ResourceBrief_Version => T("版本");
    public string ResourceBrief_HistoryData => T("历史数据");
    public string ResourceBrief_TotalGain => T("当期共获取");
    public string ResourceBrief_StarComposition => T("星声收入组成");
    public string ResourceBrief_CoinComposition => T("贝币收入组成");
    public string ResourceBrief_Detail => T("获取明细");
    public string ResourceBrief_Empty => T("本周期暂无数据");

    
    public string SignPage_DoSignIn => T("签到");
    public string SignPage_MonthlySign => T("本月签到");
    public string SignPage_SignedToday => T("今日已签到");
    public string SignPage_NotSignedToday => T("今日尚未签到");
    public string SignPage_Done => T("签到完成");
    public string SignPage_AutoSign => T("自动签到");
    public string SignPage_SignedDaysFormat => T("本月已签 {0} 天");
    public string SignPage_DayFormat => T("第 {0} 天");
    public string SignPage_Energy => T("结晶波片");
    public string TowerPage_TotalStarCount => T("总星数：{0} / {1}");
    public string TowerPage_Floor => T("第{0}层");
    public string TowerPage_NoChallengeData => T("暂无挑战数据");
    public string TowerPage_RefreshAfter => T("{0}天{1}小时后刷新");
    public string MatrixPage_Round => T("第{0}轮");
    public string MatrixPage_NoRecord => T("本期无记录");
    
    public string MatrixPage_TotalScore => T("累计积分");
    public string MatrixPage_Progress => T("挑战进度");
    public string MatrixPage_BossFormat => T("{0}/{1}");
    
    public string MatrixPage_Overview => T("总览");
    public string MatrixPage_Singularity => T("奇点扩张");
    
    public string SlashPage_ScoreLabel => T("积分：{0}");
    public string SlashPage_ChallengeFallback => T("挑战 {0}");
    public string SlashPage_GroupScore => T("总积分：{0} / {1}");
    public string SlashPage_GroupScoreNamed => T("{0}总积分：{1} / {2}");
    public string SlashPage_Team1 => T("队伍一");
    public string SlashPage_Team2 => T("队伍二");
    public string SlashPage_TeamN => T("队伍{0}");
    public string CombatPage_Locked => T("{0} 本期未解锁或无数据");
    public string CombatPage_NoData => T("暂无数据：服务端尚未生成本期记录，请先在库街区 App 中打开「{0}」页面刷新数据后重试");
    public string CombatPage_StatsPeriod => T("统计周期 {0} - {1}");
    public string CombatPage_ClearRecords => T("清除战绩记录");
    public string CombatPage_ClearRecordsConfirm => T("将删除本机保存的「{0}」全部历史期次记录，无法恢复。确定清除？");
    public string CombatPage_Clear => T("清除");

    

    
    public string PlayTimeStatsDialog_TotalPlaytime => T("总游戏时长");
    public string PlayTimeStatsDialog_Started0Times => T("启动 {0} 次");
    public string PlayTimeStatsDialog_AverageDailyPlaytime => T("每日平均游戏时间");
    public string PlayTimeStatsDialog_PlayedFor0Days => T("游玩 {0} 天");
    public string PlayTimeStatsDialog_LongestStreak => T("最长连续游玩");
    public string PlayTimeStatsDialog_0Days => T("{0} 天");
    public string PlayTimeStatsDialog_LongestSession => T("最长单次游玩");
    public string PlayTimeStatsDialog_LongestDailyPlaytime => T("单日最长游玩");
    public string PlayTimeButton_LastStartup => T("上一次游玩");
    public string PlayTimeStatsDialog_Last15Days => T("最近 15 天");
    public string PlayTimeStatsDialog_Last12Weeks => T("最近 12 周");
    public string PlayTimeStatsDialog_Last12Months => T("最近 12 个月");
    public string PlayTimeStatsDialog_Total => T("合计");

    
    public string ExperienceSettingPage_MinimizeToSystemTray => T("最小化到系统托盘");

    public string GameLauncherSettingDialog_BasicInformation => T("基本信息");
    public string GameLauncherSettingDialog_ConfirmUninstall => T("确认卸载");
    public string GameLauncherSettingDialog_CustomBackground => T("背景");
    public string GameLauncherSettingDialog_BgFps => T("帧率上限");
    public string GameLauncherSettingDialog_FpsUnlimited => T("不限");
    public string GameLauncherSettingDialog_BgQuality => T("渲染等级");
    public string GameLauncherSettingDialog_QualityAuto => T("自动（推荐）");
    public string GameLauncherSettingDialog_QualityHigh => T("高（原画）");
    public string GameLauncherSettingDialog_QualityMedium => T("中");
    public string GameLauncherSettingDialog_QualityLow => T("低");
    public string GameLauncherSettingDialog_PauseOnGameRun => T("游戏运行时暂停背景");
    public string GameLauncherSettingDialog_RelocateGame => T("重新定位游戏");
    public string GameLauncherSettingDialog_Size => T("大小");
    public string GameLauncherSettingDialog_StartRepairing => T("开始修复");
    public string GameLauncherSettingDialog_StartupArguments => T("启动参数");
    public string GameLauncherSettingDialog_CommandLineArgument => T("命令行参数");
    public string GameLauncherSettingDialog_CustomProgramTip => T("该服游戏未配置或 exe 缺失时，点「开始游戏」将改为启动此程序；游戏本体可用时不生效。");
    public string GameLauncherSettingDialog_UninstallGameWarning => T("卸载游戏警告");
    public string GameLauncherSettingDialog_CannotDeleteDriveRoot => T("不能删除驱动器根目录");
    public string GameLauncherSettingDialog_ScreenshotsFound => T("游戏目录中发现 {0} 张截图，可先备份再卸载（「确认卸载」不备份截图）");
    public string GameLauncherSettingDialog_BackupAndUninstall => T("备份并卸载");
    public string GameLauncherSettingDialog_Uninstalled => T("游戏已卸载");

    public string LauncherPage_RepairGame => T("修复游戏");
    public string LauncherPage_ShowGameAnnouncement => T("显示游戏公告");
    public string LauncherPage_UninstallGame => T("卸载游戏");
    public string LauncherPage_CustomStartExecutable => T("自定义启动程序");

    public string InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes => T("不支持映射的网络驱动器，请使用以双反斜杠开头的网络共享路径");

    public string Language_Chinese => "简体中文";
    public string Language_English => "English";
    public string Language_Japanese => "日本語";
    public string Language_Korean => "한국어";

    public string Common_Select => T("选择");

    public string PlayTimeStatsDialog_PlaytimeStatistics => T("游戏时长统计");
    public string PlayTimeStatsDialog_StatsTip => T("统计仅包含通过本启动器启动游戏的时长。");

    public string SettingPage_BgSizeTip => T("背景图尺寸提示");
    public string SettingPage_CustomBackgroundSupportVideo => T("自定义背景支持视频");

    

    
    public string Common_Apply => T("应用");
    public string GameSettingPage_Resolution => T("分辨率");
    public string GameSettingPage_FullScreen => T("全屏");
    public string GameSettingPage_PgrFullscreenBlockedTip => T("显示器原生分辨率超过游戏支持的上限，游戏启动后会强制切回窗口模式，全屏无法生效。");
    public string GameSettingPage_CustomResolution => T("自定义分辨率");
    public string GameSettingPage_CustomResolutionTip => T("自定义分辨率在「窗口」显示模式下生效");
    public string GameSettingPage_VoiceLanguage => T("语音语言");
    public string GameSettingPage_VoiceLanguageDescription => T("首次启动游戏前设置此值可以避免下载部分语音资源");
    public string GameSettingPage_HDR => T("HDR");
    public string GameSettingPage_EnableHDR => T("启用 HDR");
    public string GameSettingPage_HDRNotSupported => T("当前显示器不支持 HDR");
    public string GameSettingPage_HDRNotEnabled => T("Windows HDR 尚未开启，启用后可能无法生效");
    public string GameSettingPage_GameNotInstalled => T("游戏未安装");
    public string GameSettingPage_GameNotInstalledDesc => T("请先在启动器页设置游戏路径");
    public string GameSettingPage_CloudGameNotSupport => T("云游戏无需本地游戏设置");
    public string GameSettingPage_SettingNotEffect => T("游戏运行时应用的设置无法生效");
    public string GameSettingPage_ResolutionMustBeGreaterThan0 => T("分辨率必须大于 0");
    public string GameSettingPage_LoadFailed => T("读取游戏设置失败");
    public string GameSettingPage_ApplyFailed => T("应用游戏设置失败");
    public string GameSettingPage_Applied => T("已应用，下次启动游戏时生效");

    

    
    public string SettingPage_WindowBehavior => T("窗口行为");
    public string SettingPage_CloseWindowOption => T("关闭窗口选项");
    public string SettingPage_ExitApp => T("完全退出");
    public string SettingPage_AfterLaunchOption => T("启动游戏后");
    public string SettingPage_KeepWindow => T("保持窗口");
    public string SettingPage_AfterGameExitOption => T("关闭游戏后");
    public string SettingPage_RestoreMainWindow => T("弹出主界面");
    public string SettingPage_KeepHidden => T("保持隐藏");
    public string SettingPage_CloseLauncher => T("关闭启动器");
    public string SettingPage_KuroCommunitySign => T("库街区社群签到（库洛币）");
    public string SettingPage_SuggestSystemFeatures => T("为了更好的体验，建议在系统设置中启用以下功能。");
    public string SettingPage_TransparencyEffects => T("透明效果");
    public string SettingPage_AnimationEffects => T("动画效果");

    
    public string SettingPage_CurrentVersion => T("当前版本");
    
    public string SettingPage_About_Copyright => T("Developed by Pizazz-stack. All rights reserved.");

    

    
    public string Common_Exit => T("退出");

    
    public string GameSelector_Subtitle => T("点击左上角的图标即可选择或切换游戏");
    public string GameSelector_AutoFind => T("自动查找本机游戏");
    public string GameSelector_SelectServer => T("选择服务器");
    public string GameSelector_InstalledSize => T("已安装游戏实际占用空间 ");
    public string GameSelector_Calculating => T("统计中");

    
    public string KuroBbs_AccountLevelLabel => T("联觉等级 ");
    public string KuroBbs_SwitchRole => T("切换角色");
    public string KuroBbs_ClearAccount => T("清除账号");

    
    public string GameBiz_Wuwa => T("鸣潮");
    public string GameBiz_WuwaCn => T("鸣潮 · 中国服");
    public string GameBiz_WuwaGlobal => T("鸣潮 · 国际服");
    public string GameBiz_WuwaBilibili => T("鸣潮 · 哔哩哔哩");
    public string GameBiz_Pgr => T("战双帕弥什");
    public string GameBiz_ShortCn => T("中国服");
    public string GameBiz_ShortGlobal => T("国际服");
    public string GameBiz_ShortBilibili => T("哔哩哔哩");
    public string GameBiz_ShortPgr => T("中国服");
    public string GameBiz_PgrGlobal => T("战双帕弥什 · 国际服");
    public string GameBiz_PgrKorea => T("战双帕弥什 · 韩服");
    public string GameBiz_PgrJapan => T("战双帕弥什 · 日服");
    public string GameBiz_PgrTaiwan => T("战双帕弥什 · 台服");
    public string GameBiz_ShortKorea => T("韩服");
    public string GameBiz_ShortJapan => T("日服");
    public string GameBiz_ShortTaiwan => T("台服");

    
    public string GachaPool_CharacterEvent => T("角色活动唤取");
    public string GachaPool_WeaponEvent => T("武器活动唤取");
    public string GachaPool_CharacterStandard => T("角色常驻唤取");
    public string GachaPool_WeaponStandard => T("武器常驻唤取");
    public string GachaPool_Novice => T("新手唤取");
    public string GachaPool_NoviceChoice => T("新手自选唤取");
    public string GachaPool_NoviceGratitude => T("新手自选唤取（感恩定向唤取）");
    public string GachaPool_CharacterRookie => T("角色新旅唤取");
    public string GachaPool_WeaponRookie => T("武器新旅唤取");
    public string GachaPool_CharacterCollab => T("角色联动唤取");
    public string GachaPool_WeaponCollab => T("武器联动唤取");
    public string GachaPool_CharacterRevisit => T("角色忆旅唤取");
    public string GachaPool_WeaponRevisit => T("武器忆旅唤取");
    public string GachaPool_Unknown => T("卡池 {0}");

    
    public string Adb_DeviceOnline => T("在线");
    public string Adb_DeviceUnauthorized => T("未授权");

    
    public string Sync_Title => T("WebDAV");
    public string Sync_Description => T("通过云端同步来保存数据");
    public string Sync_ServerUrl => T("服务器地址");
    public string Sync_UserName => T("用户名");
    public string Sync_Password => T("密码或应用授权码");
    public string Sync_ServerEntry => T("同步服务器");
    public string Sync_EnableOff => T("已关闭，开启后自动同步");
    public string Sync_EnableNeedServer => T("请先配置服务器");
    public string Sync_ConnectTitle => T("连接 WebDAV");
    public string Sync_ConnectDesc => T("填写云盘或服务器提供的连接信息。");
    public string Sync_ConnectInvalid => T("请输入完整的 http:// 或 https:// 地址");
    public string Sync_SaveAndTest => T("保存并测试");
    public string Sync_Testing => T("正在测试连接…");
    public string Sync_ConnectOk => T("连接成功，配置已保存");
    public string Sync_ConnectFail => T("连接失败，请检查服务器地址、账号和密码后重试。");
    public string Sync_SyncNow => T("立即同步");
    public string Sync_RevealPassword => T("显示或隐藏密码");
    public string Sync_Configured => T("已配置");
    public string Sync_NotConfigured => T("尚未配置");
    public string Sync_NeverSynced => T("尚未同步过");
    public string Sync_LastSync => T("上次同步");
    public string Sync_Working => T("正在处理…");

    
    public string CloudGame_ErrNoLogin => T("没有可用的登录信息");
    public string CloudGame_ErrHttp => T("云游戏服务返回 HTTP {0}");
    public string CloudGame_ErrRequest => T("请求云游戏服务失败：{0}");
    public string CloudGame_ErrParse => T("云游戏服务返回了无法解析的内容：{0}");
    public string CloudGame_ErrEmpty => T("云游戏服务返回为空：{0}");
    public string CloudGame_ErrCode => T("云游戏接口返回错误 code={0}：{1}");

    
    public string GachaLogPage_ExportEmpty => T("当前账号没有可导出的调频记录");
    
    public string GachaLog_PityBuilt => T("已垫");

    
    public string GameLauncherSettingDialog_PathRemoved => T("已移除游戏路径配置");
    public string GameLauncherSettingDialog_WrongServerExe => T("所选的游戏 exe 不属于当前服务器，请重新选择");
    public string GameLauncherSettingDialog_ExpectExe => T("请选择游戏主程序：{0}");

    
    public string KrSdk_ErrNoUserCache => T("未找到 KRSDKUserCache.json，请先在游戏内登录过一次。");
    public string KrSdk_ErrEmptyContent => T("内容为空");
    public string KrSdk_ErrParseFailed => T("KRSDKUserCache.json 解析失败，已放弃写入以免破坏登录状态。");
    public string KrSdk_ErrNoAccountList => T("KRSDKUserCache.json 里没有 account_list。");
    public string KrSdk_ErrAccountNotInCache => T("本地缓存中没有此账号。");
    public string KrSdk_ErrVerifyFailed => T("写入后校验失败，已还原为改动前的缓存。");

    
    public string GameUpdate_ErrNoPlan => T("下载计划缺失");
    public string GameUpdate_ErrNoTarget => T("目标版本缺失");
    public string GameUpdate_ErrNoGamePath => T("未配置游戏路径，无法更新");
    public string GameUpdate_ErrNoManifest => T("下载清单获取失败");
    public string GameUpdate_ErrFileMissing => T("更新文件缺失：{0}");
    public string GameUpdate_ErrFileChecksum => T("更新文件校验失败：{0}");

    
    public string GameSetting_ErrNoLocalStorage => T("未找到游戏的 LocalStorage.db");
    public string GameSetting_ErrNoGameUserSettings => T("未找到游戏的 GameUserSettings.ini");

    
    public string KuroBbs_ErrConnection => T("连接失败，响应状态码:{0}");
    public string KuroBbs_ErrAccessTokenEmpty => T("requestToken 返回的 accessToken 为空");
    public string KuroBbs_ErrAccessToken => T("获取 B-At accessToken 失败（code={0}）：{1}");
    public string KuroBbs_ErrSendCode => T("发送验证码失败");
    public string KuroBbs_ErrLogin => T("登录失败");
    public string KuroBbs_SignOk => T("签到成功");
    public string KuroBbs_SignAlready => T("今日已签到");
    public string KuroBbs_SignFail => T("签到失败");

    

    
    public string Ugf_ErrNoUid => T("UGF 文件缺少账号 uid");
    public string Ugf_ErrUnknownFormat => T("无法识别的文件格式：需要 UGF 或卡池记录 JSON");

    
    public string CombatPage_AreaFallback => T("区域 {0}");

    
    public string KuroBbs_UnionLevel => T("联觉等级");
    public string KuroBbs_CommanderLevel => T("指挥官等级");
    public string KuroBbs_NoLevel => T("暂无等级");
    public string KuroBbs_SelectRolePgr => T("选择战双角色");
    public string KuroBbs_SelectRoleWuwa => T("选择鸣潮角色");
    public string KuroBbs_SwitchedPgr => T("已切换战双角色");
    public string KuroBbs_SwitchedWuwa => T("已切换鸣潮角色");
    public string PgrData_DailySupply => T("每日补给");
    public string KuroBbs_AutoSignDoneToast => T("每日签到完成");
    public string KuroBbs_AutoSignFailedToast => T("每日签到失败");

    
    public string RolePage_WeaponLevel => T("LV.{0} · 突破{1}");

    
    public string Common_UnknownError => T("未知错误");

}
