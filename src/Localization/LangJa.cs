using System.Collections.Generic;

namespace Arietta.Localization;









internal static class LangJa
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "ランチャー",
        ["LauncherPage_GameSetting"] = "ゲーム設定",
        ["MainPage_GameScreenshot"] = "スクリーンショット",
        ["GachaLogPage_ConveneRecords"] = "喚取履歴",
        ["Toolbox"] = "ツールボックス",

        
        ["ScreenshotPage_MultiSelect"] = "複数選択",
        ["ScreenshotPage_OpenFolder"] = "フォルダを開く",
        ["ScreenshotPage_Total0Files"] = "全 {0} 枚",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1} 枚を選択中",
        ["ScreenshotPage_Empty"] = "スクリーンショットフォルダが見つかりません",
        ["ScreenshotPage_EmptyDesc"] = "スクリーンショットは既定で <ゲームフォルダ>\\Client\\Saved\\Screenshots に保存されます。先に設定でゲームフォルダを指定してください",
        ["ScreenshotPage_Delete"] = "削除",
        ["ScreenshotPage_DeleteConfirm"] = "選択した {0} 枚のスクリーンショットをごみ箱に移動しますか？",
        ["ScreenshotPage_Deleted"] = "{0} 枚のスクリーンショットをごみ箱に移動しました",
        ["ScreenshotPage_DeleteFailed"] = "削除に失敗しました：一部のファイルが他のプログラムで使用中の可能性があります",
        ["ScreenshotPage_SelectFirst"] = "先に削除するスクリーンショットを選択してください",
        ["ScreenshotPage_OpenImage"] = "画像を開く",
        ["ScreenshotPage_DeletedPartial"] = "{0} / {1} 枚を削除しました。残りは使用中の可能性があります",

        
        ["KuroBbs"] = "クロBBS",
        ["KuroBbs_SignIn"] = "チェックイン",
        ["KuroBbs_Role"] = "キャラクター",
        ["KuroBbs_ResourceBrief"] = "資源概要",

        
        ["PgrData_NoRole"] = "このクロBBSアカウントにはまだパニシングのキャラクターが設定されていません。クロBBSアプリの「マイページ」で設定してからご覧ください。",
        ["PgrData_Locked"] = "未解放",
        ["PgrData_FullIn"] = "{0}後に全回復",

        
        ["KuroBbs_DataTerminal"] = "データ端末",
        ["KuroBbs_CombatData"] = "戦闘データ",
        ["DataTerminal_NoAccount"] = "まだクロBBSにログインしていません。上のアカウント矢印をタップしてログインしてからご覧ください。",
        ["DataTerminal_NoRole"] = "このクロBBSアカウントはまだ{0}のキャラクターを紐付けていません。先にクロBBSアプリで紐付けてください。",
        ["DataTerminal_LoadFailed"] = "ページの読み込みに失敗しました：{0}",

        
        ["KuroBbs_PgrRole"] = "キャラクター",
        ["KuroBbs_PgrResource"] = "資源月報",
        ["KuroBbs_PgrCage"] = "幻痛ケージ",
        ["KuroBbs_PgrWarArea"] = "紛争戦区",
        ["KuroBbs_PgrTransfinite"] = "暦戦マッピング",
        ["PgrRole_Fight"] = "戦力 {0}",
        ["PgrResource_Total"] = "直近半年の累計",
        ["PgrResource_BlackCard"] = "ブラックカード",
        ["PgrResource_Develop"] = "開発リソース",
        ["PgrResource_Trade"] = "トレード証票",
        ["PgrResource_SelectPeriod"] = "期間を選択",
        ["PgrResource_Composition"] = "収入内訳",
        ["PgrResource_MonthlyData"] = "月間データ",
        ["PgrChallenge_NotOpen"] = "現在は解放期間外です",
        ["PgrChallenge_Locked"] = "まだ解放されていません",
        ["PgrChallenge_NoArea"] = "エリアが未選択です",
        ["PgrChallenge_NoRecord"] = "今期のチャレンジ記録はありません",
        ["PgrChallenge_Level"] = "レベル帯 {0}",
        ["PgrWarArea_TotalPointLabel"] = "合計スコア",
        ["PgrWarArea_TotalTimesLabel"] = "チャレンジ回数",
        ["PgrWarArea_Waves"] = "ウェーブ {0}",
        ["PgrCommon_LoadFailed"] = "データの取得に失敗しました",

        
        ["DailyBriefing"] = "ブリーフィング",
        ["DailyBriefing_Desc"] = "ゲーム内デイリーデータ",
        ["DailyBriefing_Refresh"] = "更新",
        ["DailyBriefing_Error"] = "ブリーフィングデータの取得に失敗しました",
        ["DailyBriefing_NoGameAccount"] = "ログインアカウントが見つかりません。ログインしてからもう一度お試しください。",
        ["DailyBriefing_NoRole"] = "このアカウントのゲームキャラクターがまだ識別されていません。一度ゲームを起動してください（キャラクター情報は公式 SDK がオンラインで識別します）。",
        ["Briefing_Liveness"] = "デイリー活躍",
        ["Briefing_StoreEnergy"] = "結晶単質",
        ["Briefing_BattlePass"] = "先約ラジオ",
        ["Briefing_ActionPoint"] = "血清",
        ["Briefing_Level"] = "レベル",
        ["KuroBbs_TowerOfAdversity"] = "逆境深塔",
        ["KuroBbs_SongOfVoidSea"] = "冥歌海虚",
        ["KuroBbs_DoomsdayMatrix"] = "終焉マトリクス",
        ["KuroBbs_ComingSoon"] = "{0}は開発中です",
        ["KuroBbs_TokenExpired"] = "クロBBSのログインの有効期限が切れました",
        ["KuroBbs_TokenExpiredDesc"] = "設定 → クロBBSアカウント で token を再取得または入力してください",

        
        ["KuroAccount_UsbTitle"] = "ADB ツール",
        ["KuroAccount_UsbShortDesc"] = "Android スマホを接続してクロBBSの Token と Did を取得します",
        ["KuroAccount_UsbDesc"] = "USB ケーブルでスマホを接続し USB デバッグを有効にしてください。スマホでクロBBSアプリを開き「データ端末」または「育成計算ツール」ページに入ってから「取得開始」をタップすると、ツールが自動的に token と did を取得します。adb.exe が必要です（",
        ["KuroAccount_UsbDescTail"] = " 公式ページからダウンロードできます）。",
        ["KuroAccount_UsbVerWarn"] = "⚠ 新バージョンのクロBBSアプリではこの取得方法は利用できません。旧バージョンをお使いください。",
        ["KuroAccount_AdbPlaceholder"] = "adb.exe のパス（空欄で PATH を使用）",
        ["KuroAccount_ApplyAdb"] = "適用",
        ["KuroAccount_SelectDevice"] = "デバイスを選択",
        ["KuroAccount_RefreshDevices"] = "更新",
        ["KuroAccount_StartCapture"] = "取得開始",
        ["KuroAccount_StopCapture"] = "取得停止",
        ["KuroAccount_FieldsTitle"] = "アカウント情報",
        ["KuroAccount_FillHint"] = "取得結果は自動的に入力されます。手動で貼り付けることもできます",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "保存",
        ["KuroAccount_Clear"] = "クリア",
        ["KuroAccount_Log"] = "取得ログ",
        ["KuroAccount_AdbInvalidTitle"] = "ADB パスが無効です",
        ["KuroAccount_AdbInvalidDesc"] = "有効な adb.exe の完全なパスを入力してください",
        ["KuroAccount_AdbApplied"] = "ADB パスを適用しました",
        ["KuroAccount_AdbApplyFailed"] = "ADB パスの適用に失敗しました",
        ["KuroAccount_NoDevice"] = "Android デバイスが見つかりません",
        ["KuroAccount_NoDeviceDesc"] = "スマホを接続し、USB デバッグの許可を有効にしてください",
        ["KuroAccount_RefreshDevicesFailed"] = "デバイスの更新に失敗しました",
        ["KuroAccount_SelectDeviceFirst"] = "先にデバイスを選択してください",
        ["KuroAccount_SelectDeviceFirstDesc"] = "先に「更新」をタップしてからデバイスを選択してください",
        ["KuroAccount_NoPage"] = "取得可能な Web ページが見つかりません",
        ["KuroAccount_NoPageDesc"] = "先にスマホで「データ端末」などの Web ページを開いてから、「取得開始」をタップしてください",
        ["KuroAccount_CaptureFailed"] = "取得用の接続に失敗しました",
        ["KuroAccount_CaptureStarted"] = "監視を開始しました",
        ["KuroAccount_CaptureStartedDesc"] = "スマホで「データ端末」または「育成計算ツール」ページを開いてください",
        ["KuroAccount_Disconnected"] = "スマホとの接続が切断されました",
        ["KuroAccount_DisconnectedDesc"] = "WebView ページが閉じた可能性があります。「取得開始」をもう一度タップできます",
        ["KuroAccount_NoSocket"] = "WebView デバッグインターフェースが見つかりません",
        ["KuroAccount_NoSocketDesc"] = "このバージョンのクロBBSは WebView リモートデバッグを無効にしている可能性があります。詳細は診断レポートをご覧ください",
        ["KuroAccount_DiagTitle"] = "取得診断レポート",
        ["KuroAccount_DiagHint"] = "レポートの devtools socket が 0 の場合、このバージョンのクロBBSアプリは WebView リモートデバッグを無効にしています（Chrome の chrome://inspect にも表示されません）。この場合 ADB での取得は不可能です。代わりに「クロBBS → アカウント → ログイン」の電話番号＋認証コードログインで Token と Did を取得できます。スマホ接続も ADB も不要です。",
        ["KuroAccount_DiagCopy"] = "レポートをコピー",
        ["KuroAccount_DiagClose"] = "閉じる",
        ["KuroAccount_DiagCopied"] = "診断レポートをコピーしました",
        ["KuroAccount_Invalid"] = "情報が不完全です",
        ["KuroAccount_SaveNeedTokenDid"] = "Token と Did は必須です。roleId は自動検索できます",
        ["KuroAccount_Saved"] = "アカウントを保存しました",
        ["KuroAccount_Cleared"] = "アカウントをクリアしました",

        
        ["RolePage_Refresh"] = "更新",
        ["RolePage_NoAccount"] = "クロBBSアカウントがまだ設定されていません",
        ["RolePage_NoAccountDesc"] = "設定 → クロBBSアカウント で設定を完了してから更新してください",
        ["RolePage_LoadFailed"] = "キャラクターの読み込みに失敗しました",
        ["RolePage_Back"] = "一覧に戻る",
        ["RolePage_Phantoms"] = "音骸",

        
        ["GachaLogPage_Refresh"] = "記録を更新",
        ["GachaLogPage_ImportUrl"] = "リンクを貼り付けてインポート",
        ["GachaLogPage_ImportUrlDesc"] = "喚取記録ページへのリンクを貼り付けてください（ゲーム内で「喚取記録」を開いた後、ログから取得するか、他のツールからコピー）。クラウドスマホのユーザーも利用できます",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "リンクが無効です：record_id を含む喚取記録ページの完全なリンクを貼り付けてください",
        ["GachaLogPage_SelectPlayer"] = "アカウントを選択",
        ["GachaLogPage_CopyUrl"] = "URLをコピー",
        ["GachaLogPage_DeletePlayer"] = "記録を削除",
        ["GachaLogPage_Delete"] = "削除",
        ["GachaLogPage_Avg5"] = "平均",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "統計",
        ["GachaLogPage_Times"] = "回",
        ["GachaLogPage_ItemStats"] = "アイテム統計",
        ["GachaLogPage_UpdateSavedUrl"] = "更新（保存済み URL）",
        ["GachaLogPage_UpdateAll"] = "更新（すべて）",
        ["GachaLogPage_InputUrl"] = "URL を入力",
        ["GachaLogPage_ViaCloudGame"] = "クラウドゲームから取得",
        ["CloudGame_Title"] = "クラウド鳴潮 - ログイン後にワンクリックで喚取リンクを取得",
        ["CloudGame_Grab"] = "喚取リンクを取得",
        ["CloudGame_StatusDefault"] = "ここでクロアカウントにログインし、「ゲームに入る」をタップして「喚取記録」画面を一度開いてから、「喚取リンクを取得」をクリックしてください",
        ["CloudGame_StatusSessionReady"] = "クラウドゲームのログイン状態を検出しました。「喚取リンクを取得」をクリックして同期を完了してください",
        ["CloudGame_StatusGrabbing"] = "ログイン情報を読み込み中…",
        ["CloudGame_StatusOk"] = "取得成功（プレイヤー {0}）。喚取履歴を同期中…",
        ["CloudGame_StatusFailReason"] = "取得失敗：{0}",
        ["CloudGame_StatusNoRuntime"] = "内蔵ブラウザの初期化に失敗しました：WebView2 ランタイムがインストールされていません。Microsoft の公式サイトからインストールしてから再試行してください",
        ["GachaLogPage_ImportExport"] = "インポート & エクスポート",
        ["GachaLogPage_HidePools"] = "バナーを非表示",
        ["GachaLogPage_ExportAs"] = "エクスポート",
        ["GachaLogPage_ImportRecords"] = "記録をインポート（ファイルを選択）",
        ["GachaLogPage_ImportOk"] = "インポート成功：プレイヤー {0}、合計 {1} 件の記録（重複記録は自動的に除外されます）",
        ["GachaLogPage_ImportFail"] = "インポートに失敗しました：{0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "ファイルにアカウント情報が含まれず、現在選択中のアカウントもないため、所有者を特定できません",
        ["GachaLogPage_OtherSettings"] = "その他の設定",
        ["GachaLogPage_LinkHelp"] = "Authkey Timeout が常に出る場合",
        ["GachaLogPage_LinkHelpDesc"] = "キャッシュフォルダを削除してからゲームを再起動してください",
        ["GachaLogPage_ClearSavedUrl"] = "キャッシュフォルダを削除",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "ローカルに保存した喚取リンクを削除しますか？（取得済みのガチャ記録は削除されません）",
        ["GachaLogPage_ClearSavedUrlDone"] = "保存済みの喚取リンクをクリアしました。ゲーム内で「喚取記録」ページを再度開いてください",
        ["GachaLogPage_Language"] = "言語",
        ["GachaLogPage_LanguageSaved"] = "言語を保存しました。次回の記録更新時に有効になります",
        ["GachaLogPage_LanguageApplied"] = "{0} 件の記録のアイテム名を {1} に変更しました",
        ["GachaLogPage_NoSavedUrl"] = "保存済みのリンクがありません。先に「更新（すべて）」を一度実行してください",
        ["GachaLogPage_LinkExpired"] = "喚取リンクの有効期限が切れています",
        ["GachaLogPage_LinkExpiredDesc"] = "リンクの有効期限が切れています（API が {0} を返しました）。ゲーム内で「喚取記録」を再度開いてください",
        ["GachaLogPage_UpdateFailed"] = "喚取履歴の更新に失敗しました：{0}",
        ["GachaLogPage_FetchFailed"] = "喚取データの取得に失敗しました。もう一度お試しください",
        ["GachaLogPage_LogNotFound"] = "ゲームログまたは喚取リンクが見つかりません：先に設定でゲーム（Wuthering Waves.exe）を指定し、ゲーム内で「喚取記録」ページを一度開くか、「記録を更新」メニューから「リンクを貼り付けてインポート」を選択してください",
        ["GachaLogPage_UrlNotFound"] = "喚取リンクが見つかりません。ゲーム内で「喚取記録」ページを一度開いてください",
        ["GachaLogPage_UrlCopied"] = "喚取リンクをクリップボードにコピーしました",
        ["GachaLogPage_PlayerDeleted"] = "UID {0} のローカル喚取記録を削除しました",
        ["GachaLogPage_DeleteConfirmText"] = "UID {0} のローカル喚取記録をすべて削除します。この操作は取り消せません。",
        ["GachaLogPage_EmptyTitle"] = "喚取履歴はまだありません",
        ["GachaLogPage_EmptyDesc"] = "先に設定でゲームフォルダを指定し、ゲーム内で「喚取記録」ページを一度開いてから、ここに戻って「記録を更新」をクリックすると同期できます。",

        
        ["LauncherPage_StartGame"] = "ゲームを起動",
        ["LauncherPage_InstallGame"] = "ゲームをインストール",
        ["LauncherPage_ResumeInstall"] = "インストールを続行",
        ["AccountSwitcher_SwitchAccount"] = "アカウントを切り替え",
        ["AccountSwitcher_GameRunning"] = "ゲーム実行中のため、アカウントを切り替えられません",
        ["AccountSwitcher_AccountInfo"] = "ゲームアカウント",
        ["AccountSwitcher_Nickname"] = "ニックネーム",
        ["AccountSwitcher_RoleId"] = "ゲーム UID",
        ["AccountSwitcher_Save"] = "保存",
        ["AccountSwitcher_Delete"] = "削除",
        ["AccountSwitcher_Unsaved"] = "現在ログイン中のアカウントは保存されていません",
        ["Notice_Activity"] = "イベント",
        ["Notice_NoticeTab"] = "お知らせ",
        ["Notice_News"] = "ニュース",
        ["Notice_LoadFailed"] = "お知らせの読み込みに失敗しました",
        ["LauncherPage_UpdateGame"] = "ゲームをアップデート",

        
        ["SettingPage_Language"] = "言語",

        
        ["GameUpdate_PredownloadAvailable"] = "新バージョン {0} の事前ダウンロードが開始されました",
        ["GameUpdate_Predownloaded"] = "{0} は事前ダウンロード済みです。アップデート開始後にそのままインストールできます",
        ["GameUpdate_PredownloadButton"] = "事前ダウンロード",
        ["GameUpdate_ConfirmPredownload"] = "バージョン {0} を事前ダウンロードしますか？合計 {1} のダウンロードが必要です。ファイルはゲームフォルダ内の predownload フォルダに保存され、公式アップデートの適用時に再ダウンロードは不要です。",
        ["GameUpdate_Applying"] = "アップデートを適用中…",
        ["GameUpdate_Predownloading"] = "{0} を事前ダウンロード中…",

        ["GameUpdate_Done"] = "{0} にアップデートしました",
        ["GameUpdate_PredownloadDone"] = "{0} の事前ダウンロードが完了しました。アップデート開始後にそのままインストールできます",
        ["GameUpdate_NoGameDir"] = "先に設定でゲームを指定してください",
        ["GameUpdate_Verifying"] = "検証中 {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "検証完了",
        ["GameUpdate_VerifyDoneNone"] = "破損または欠落しているファイルは見つかりませんでした",
        ["GameUpdate_VerifyDoneDamage"] = "破損 / 欠落ファイルが {0} 件見つかりました（合計 {1} 件）",
        ["GameUpdate_VerifyNoManifest"] = "検証マニフェストを取得できません。ネットワークを確認するか、後でもう一度お試しください",
        ["GameUpdate_Repairing"] = "修復中…",
        ["GameUpdate_RepairDone"] = "{0} 件のファイルを修復しました",
        ["GameUpdate_RepairNoBaseUrl"] = "現在のバージョンはオンライン修復に対応していません。先にゲームを最新バージョンに更新してから再試行してください",

        
        ["GameLauncherPage_Gallery"] = "ギャラリー",
        ["Common_Copy"] = "コピー",
        ["ImageViewWindow2_SaveAs"] = "名前を付けて保存",
        ["GameLauncherPage_PlayPause"] = "再生/一時停止",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "画像ファイルをドロップして背景を変更",
        ["Background_Official"] = "公式動画背景",
        ["Background_OfficialStatic"] = "公式静止背景",
        ["Background_OpenFolder"] = "背景フォルダを開く",
        ["Background_AddFailed"] = "この背景ファイルは使用できません",
        ["InstallGameDialog_LocateGame"] = "ゲームを指定",
        ["GameLauncherPage_GameIsRunning"] = "ゲームが実行中です",

        
        ["SettingPage_Download"] = "ダウンロード",

        
        ["DownloadGamePage_Downloading"] = "ダウンロード中",
        ["DownloadGamePage_Verifying"] = "検証中",
        ["DownloadGamePage_Pause"] = "一時停止",

        
        ["Common_Confirm"] = "確認",
        ["Common_Cancel"] = "キャンセル",
        ["Common_Close"] = "閉じる",
        ["Common_Save"] = "保存",
        ["Common_Error"] = "エラー",
        ["Common_Ok"] = "OK",

        
        ["SettingPage_AppSettings"] = "設定",
        ["SettingPage_About"] = "このアプリについて",
        ["SettingPage_General"] = "一般",
        ["SettingPage_FileManagement"] = "ファイル管理",

        
        ["FilePage_DataFolder"] = "データフォルダ",
        ["FilePage_DataFolderDesc"] = "個人データは以下のフォルダに保存されています。大切なデータのため、むやみに削除しないでください。",
        ["FilePage_ReselectDataFolder"] = "データフォルダを再選択",
        ["FilePage_OpenDataFolder"] = "データフォルダを開く",
        ["FilePage_DeleteAllSettings"] = "すべての設定を削除",
        ["FilePage_ReselectFolderDone"] = "新しいデータフォルダを保存しました。アプリの再起動後に有効になります。新しいフォルダは初期状態が空のため、「クラウド同期」で以前のデータを取り込めます。",
        ["FilePage_DeleteSettingsDone"] = "設定を削除しました。アプリの再起動後に有効になります。",
        ["FilePage_DeleteSettingsConfirm"] = "すべての設定（config.ini）を削除し、アプリ再起動後に既定値へ戻ります。プレイ時間や喚取記録などのデータベース内容には影響しません。続行しますか？",
        ["FilePage_OpenLogFolder"] = "ログフォルダを開く",
        ["FilePage_Cache"] = "キャッシュ",
        ["FilePage_CacheLog"] = "ログ",
        ["FilePage_CacheImage"] = "画像",
        ["FilePage_CacheBackground"] = "ランチャー背景",
        ["FilePage_CleanCache"] = "キャッシュをクリア",
        ["FilePage_CleanBackgroundCache"] = "ランチャー背景ファイルを削除",

        
        ["FilePage_HardLink"] = "ハードリンク",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "同一ディスク上の他リージョンクライアントの同一ファイルはハードリンクで再利用されます。ダウンロードは約 {0} の見込みです。これは推定値であり、実際はインストール時のファイルごとの検証によります。",
        ["GameUpdate_ResumeInstall"] = "未完了のインストール（{0}）を検出しました。前回のフォルダから再開します",
["FilePage_HardLinkDesc"] = "同一ディスクに複数のクライアントをインストールする際、同一ファイルをハードリンクしてストレージ使用量を削減します",
        ["FilePage_Enabled"] = "有効",
        ["FilePage_Disabled"] = "無効",
        ["FilePage_SpeedLimit"] = "速度制限",
        ["FilePage_SpeedLimitDesc"] = "0で制限なし",
        ["FilePage_DownloadThreads"] = "マルチスレッド",
        ["FilePage_DownloadThreadsDesc"] = "同時に張るダウンロード接続数。HDD では並列が多すぎるとシークでかえって遅くなります。変更は次回から有効。",

        
        ["KuroBbs_RefreshInfo"] = "情報を更新",
        ["KuroBbs_Login"] = "ログイン",
        ["KuroBbs_Settings"] = "クロBBS設定",

        
        ["Login_Title"] = "クロBBSにログイン",
        ["Login_ByToken"] = "Token でログイン",
        ["Login_BySms"] = "認証コードでログイン",
        ["Login_TokenPlaceholder"] = "クロBBSのログイン Token を貼り付け",
        ["Login_DidPlaceholder"] = "Token と一緒に取得した Did（空欄で自動生成）",
        ["Login_TokenTip"] = "Token と Did はスマホのクロBBSアプリから取得します（ツールボックス → USB 取得）。2 つで 1 組として保存されます",
        ["Login_PhonePlaceholder"] = "電話番号",
        ["Login_CodePlaceholder"] = "SMS 認証コード",
        ["Login_CodeLabel"] = "認証コード",
        ["Login_SmsTip"] = "「認証コードを送信」をタップした後、スマホで受信した SMS 認証コードを入力してください。ログインに成功するとアカウントは自動保存されます",
        ["Login_SendCode"] = "認証コードを送信",
        ["Login_Confirm"] = "ログイン",
        ["Login_Cancel"] = "キャンセル",
        ["Login_PhoneInvalid"] = "電話番号の形式が正しくありません",
        ["Login_Sending"] = "認証コードを送信中…",
        ["Login_CodeSent"] = "認証コードを送信しました。SMS をご確認ください",
        ["Login_CodeSendFailed"] = "認証コードの送信に失敗しました：{0}",
        ["Login_NeedCode"] = "SMS 認証コードを入力してください",
        ["Login_TokenInvalid"] = "Token が無効です：クロBBSのログイン Token を完全な形で貼り付けてください",
        ["Login_Working"] = "ログイン中…",
        ["Login_Failed"] = "ログインに失敗しました：{0}",
        ["Login_SeekRoleFailed"] = "紐付けキャラクターの検索に失敗しました：{0}",
        ["Login_Success"] = "クロBBSアカウント「{0}」でログインしました",

        
        ["ResourceBrief_Month"] = "月間",
        ["ResourceBrief_Week"] = "週間",
        ["ResourceBrief_Version"] = "バージョン",
        ["ResourceBrief_HistoryData"] = "履歴データ",
        ["ResourceBrief_TotalGain"] = "今期の獲得合計",
        ["ResourceBrief_StarComposition"] = "星声収入の内訳",
        ["ResourceBrief_CoinComposition"] = "貝幣収入の内訳",
        ["ResourceBrief_Detail"] = "獲得明細",
        ["ResourceBrief_Empty"] = "この期間のデータはありません",

        
        ["SignPage_DoSignIn"] = "チェックイン",
        ["SignPage_MonthlySign"] = "今月のチェックイン",
        ["SignPage_SignedToday"] = "本日チェックイン済み",
        ["SignPage_NotSignedToday"] = "本日未チェックイン",
        ["SignPage_Done"] = "チェックイン完了",
        ["SignPage_AutoSign"] = "自動チェックイン",
        ["SignPage_SignedDaysFormat"] = "今月 {0} 日チェックイン済み",
        ["SignPage_DayFormat"] = "{0} 日目",
        ["SignPage_Energy"] = "ウェーブプレート",
        ["TowerPage_TotalStarCount"] = "合計スター：{0} / {1}",
        ["TowerPage_Floor"] = "第{0}階層",
        ["TowerPage_NoChallengeData"] = "チャレンジデータはありません",
        ["TowerPage_RefreshAfter"] = "{0}日{1}時間後に更新",
        ["MatrixPage_Round"] = "第{0}ラウンド",
        ["MatrixPage_NoRecord"] = "今期の記録はありません",
        ["MatrixPage_Overview"] = "概要",
        ["MatrixPage_Singularity"] = "奇点拡張",
        ["SlashPage_ScoreLabel"] = "スコア：{0}",
        ["SlashPage_ChallengeFallback"] = "チャレンジ {0}",
        ["SlashPage_GroupScore"] = "総スコア：{0} / {1}",
        ["SlashPage_GroupScoreNamed"] = "{0}総スコア：{1} / {2}",
        ["SlashPage_Team1"] = "チーム1",
        ["SlashPage_Team2"] = "チーム2",
        ["SlashPage_TeamN"] = "チーム{0}",
        ["CombatPage_Locked"] = "{0} は今期未解放またはデータなし",
        ["CombatPage_NoData"] = "データなし：サーバーはまだ今期の記録を生成していません。先にクロBBSアプリで「{0}」ページを開いてデータを更新してから、再試行してください",
        ["CombatPage_StatsPeriod"] = "統計期間 {0} - {1}",
        ["CombatPage_ClearRecords"] = "戦績記録を削除",
        ["CombatPage_ClearRecordsConfirm"] = "この端末に保存された「{0}」の全期間の記録を削除します。元に戻せません。削除しますか？",
        ["CombatPage_Clear"] = "削除",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "合計プレイ時間",
        ["PlayTimeStatsDialog_Started0Times"] = "起動 {0} 回",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "1 日平均プレイ時間",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "{0} 日間プレイ",
        ["PlayTimeStatsDialog_LongestStreak"] = "最長連続プレイ",
        ["PlayTimeStatsDialog_0Days"] = "{0} 日",
        ["PlayTimeStatsDialog_LongestSession"] = "最長 1 回のプレイ",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "1 日の最長プレイ",
        ["PlayTimeButton_LastStartup"] = "前回のプレイ",
        ["PlayTimeStatsDialog_Last15Days"] = "直近 15 日",
        ["PlayTimeStatsDialog_Last12Weeks"] = "直近 12 週間",
        ["PlayTimeStatsDialog_Last12Months"] = "直近 12 か月",
        ["PlayTimeStatsDialog_Total"] = "合計",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "システムトレイに最小化",
        ["GameLauncherSettingDialog_BasicInformation"] = "基本情報",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "アンインストールの確認",
        ["GameLauncherSettingDialog_CustomBackground"] = "背景",
        ["GameLauncherSettingDialog_RelocateGame"] = "ゲームを再指定",
        ["GameLauncherSettingDialog_Size"] = "サイズ",
        ["GameLauncherSettingDialog_StartRepairing"] = "修復を開始",
        ["GameLauncherSettingDialog_StartupArguments"] = "起動引数",
        ["GameLauncherSettingDialog_CommandLineArgument"] = "コマンドライン引数",
        ["GameLauncherSettingDialog_CustomProgramTip"] = "このサーバーのゲームが未設定または exe が見つからない場合、「ゲームを起動」をクリックするとこのプログラムが起動されます。ゲーム本体が利用可能な場合は効果ありません。",
        ["LauncherPage_CustomStartExecutable"] = "カスタム実行ファイル",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "ゲームのアンインストールに関する警告",
        ["LauncherPage_RepairGame"] = "ゲームを修復",
        ["LauncherPage_ShowGameAnnouncement"] = "ゲームのお知らせを表示",
        ["LauncherPage_UninstallGame"] = "ゲームをアンインストール",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "マップ済みネットワークドライブはサポートされていません。2 つの連続するバックスラッシュで始まるネットワーク共有パスを使用してください",


        ["Common_Select"] = "選択",

        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "プレイ時間統計",
        ["PlayTimeStatsDialog_StatsTip"] = "統計には、本ランチャーから起動したゲームのプレイ時間のみが含まれます。",

        
        ["SettingPage_BgSizeTip"] = "背景画像サイズのヒント",
        ["SettingPage_CustomBackgroundSupportVideo"] = "カスタム背景は動画に対応しています",

        
        ["Common_Apply"] = "適用",
        ["GameSettingPage_Resolution"] = "解像度",
        ["GameSettingPage_FullScreen"] = "フルスクリーン",
        ["GameSettingPage_UsePopupWindow"] = "ボーダーレスウィンドウ",
        ["GameSettingPage_CustomResolution"] = "カスタム解像度",
        ["GameSettingPage_CustomResolutionTip"] = "カスタム解像度は「ウィンドウ」表示モードで有効になります",
        ["GameSettingPage_VoiceLanguage"] = "音声言語",
        ["GameSettingPage_VoiceLanguageDescription"] = "初回ゲーム起動前に設定すると、一部の音声リソースのダウンロードを回避できます",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "HDR を有効化",
        ["GameSettingPage_HDRNotSupported"] = "現在のモニターは HDR に対応していません",
        ["GameSettingPage_HDRNotEnabled"] = "Windows の HDR がまだ有効でないため、有効にしても反映されない場合があります",
        ["GameSettingPage_GameNotInstalled"] = "ゲームが未インストールです",
        ["GameSettingPage_GameNotInstalledDesc"] = "先にランチャーページでゲームパスを設定してください",
        ["GameSettingPage_CloudGameNotSupport"] = "クラウドゲームではローカルのゲーム設定は不要です",
        ["GameSettingPage_SettingNotEffect"] = "ゲーム実行中に適用した設定は反映されません",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "解像度は 0 より大きい値にしてください",
        ["GameSettingPage_LoadFailed"] = "ゲーム設定の読み取りに失敗しました",
        ["GameSettingPage_ApplyFailed"] = "ゲーム設定の適用に失敗しました",
        ["GameSettingPage_Applied"] = "適用しました。次回のゲーム起動時に有効になります",

        

        
        ["SettingPage_WindowBehavior"] = "ウィンドウの動作",
        ["SettingPage_CloseWindowOption"] = "ウィンドウを閉じた時の動作",
        ["SettingPage_ExitApp"] = "完全に終了",
        ["SettingPage_AfterLaunchOption"] = "ゲーム起動後の動作",
        ["SettingPage_KeepWindow"] = "ウィンドウを維持",
        ["SettingPage_AfterGameExitOption"] = "ゲーム終了後",
        ["SettingPage_RestoreMainWindow"] = "メインウィンドウを表示",
        ["SettingPage_KeepHidden"] = "非表示のまま",
        ["SettingPage_CloseLauncher"] = "ランチャーを終了",
        ["SettingPage_KuroCommunitySign"] = "クロBBSコミュニティチェックイン（クロコイン）",
        ["SettingPage_SuggestSystemFeatures"] = "より良い体験のために、システム設定で以下の機能を有効にすることをおすすめします。",
        ["SettingPage_TransparencyEffects"] = "透明効果",
        ["SettingPage_AnimationEffects"] = "アニメーション効果",

        

        
        ["Common_Exit"] = "終了",

        
        ["GameSelector_Subtitle"] = "左上のアイコンをクリックすると、ゲームを選択または切り替えられます",
        ["GameSelector_AutoFind"] = "端末内のゲームを自動検索",
        ["GameSelector_SelectServer"] = "サーバーを選択",
        ["GameSelector_InstalledSize"] = "インストール済みゲームの実際の使用容量 ",
        ["GameSelector_Calculating"] = "集計中",

        
        ["KuroBbs_AccountLevelLabel"] = "ユニオンレベル ",
        ["KuroBbs_SwitchRole"] = "キャラクターを切り替え",
        ["KuroBbs_ClearAccount"] = "アカウントをクリア",

        
        ["GameBiz_Wuwa"] = "鳴潮",
        ["GameBiz_WuwaCn"] = "鳴潮 · 中国サーバー",
        ["GameBiz_WuwaGlobal"] = "鳴潮 · グローバル",
        ["GameBiz_WuwaBilibili"] = "鳴潮 · bilibili",
        ["GameBiz_Pgr"] = "パニシング：グレイレイヴン",
        ["GameBiz_ShortCn"] = "中国サーバー",
        ["GameBiz_ShortGlobal"] = "グローバルサーバー",
        ["GameBiz_ShortBilibili"] = "bilibili",
        ["GameBiz_ShortPgr"] = "中国サーバー",
        ["GameBiz_PgrGlobal"] = "パニシング：グレイレイヴン · グローバル",
        ["GameBiz_ShortGlobal"] = "グローバルサーバー",
        ["GameBiz_PgrKorea"] = "パニシング：グレイレイヴン · 韓国サーバー",
        ["GameBiz_ShortKorea"] = "韓国サーバー",
        ["GameBiz_PgrJapan"] = "パニシング：グレイレイヴン · 日本サーバー",
        ["GameBiz_ShortJapan"] = "日本サーバー",
        ["GameBiz_PgrTaiwan"] = "パニシング：グレイレイヴン · 台湾サーバー",
        ["GameBiz_ShortTaiwan"] = "台湾サーバー",

        
        ["GachaPool_CharacterEvent"] = "キャラクターイベント喚取",
        ["GachaPool_WeaponEvent"] = "武器イベント喚取",
        ["GachaPool_CharacterStandard"] = "キャラクター常設喚取",
        ["GachaPool_WeaponStandard"] = "武器常設喚取",
        ["GachaPool_Novice"] = "初心者喚取",
        ["GachaPool_NoviceChoice"] = "初心者セレクト喚取",
        ["GachaPool_NoviceGratitude"] = "初心者セレクト喚取（感謝指名喚取）",
        ["GachaPool_CharacterRookie"] = "キャラクタービギナー喚取",
        ["GachaPool_WeaponRookie"] = "武器ビギナー喚取",
        ["GachaPool_CharacterCollab"] = "キャラクターコラボ喚取",
        ["GachaPool_WeaponCollab"] = "武器コラボ喚取",
        ["GachaPool_CharacterRevisit"] = "キャラクター復刻喚取",
        ["GachaPool_WeaponRevisit"] = "武器復刻喚取",
        ["GachaPool_Unknown"] = "ガチャ {0}",

        
        ["Adb_DeviceOnline"] = "オンライン",
        ["Adb_DeviceUnauthorized"] = "未認可",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "クラウド同期でデータを保存します",
        ["Sync_ServerUrl"] = "サーバーアドレス",
        ["Sync_UserName"] = "ユーザー名",
        ["Sync_Password"] = "パスワードまたはアプリ認証コード",
        ["Sync_ServerEntry"] = "同期サーバー",
        ["Sync_EnableOff"] = "オフ：オンにすると自動同期します",
        ["Sync_EnableNeedServer"] = "先にサーバーを設定してください",
        ["Sync_ConnectTitle"] = "WebDAV に接続",
        ["Sync_ConnectDesc"] = "クラウドドライブまたはサーバーの接続情報を入力してください。",
        ["Sync_ConnectInvalid"] = "http:// または https:// で始まる完全なアドレスを入力してください",
        ["Sync_SaveAndTest"] = "保存してテスト",
        ["Sync_Testing"] = "接続をテストしています…",
        ["Sync_ConnectOk"] = "接続に成功し、設定を保存しました",
        ["Sync_ConnectFail"] = "接続に失敗しました。サーバーアドレス・アカウント・パスワードを確認して再試行してください。",
        ["Sync_SyncNow"] = "今すぐ同期",
        ["Sync_RevealPassword"] = "パスワードの表示・非表示",
        ["Sync_Configured"] = "設定済み",
        ["Sync_NotConfigured"] = "未設定",
        ["Sync_NeverSynced"] = "未同期",
        ["Sync_LastSync"] = "前回の同期",
        ["Sync_Working"] = "処理中…",

        
        ["CloudGame_ErrNoLogin"] = "利用可能なログイン情報がありません",
        ["CloudGame_ErrHttp"] = "クラウドゲームサービスが HTTP {0} を返しました",
        ["CloudGame_ErrRequest"] = "クラウドゲームサービスへのリクエストに失敗しました：{0}",
        ["CloudGame_ErrParse"] = "クラウドゲームサービスが解析できない内容を返しました：{0}",
        ["CloudGame_ErrEmpty"] = "クラウドゲームサービスが空の内容を返しました：{0}",
        ["CloudGame_ErrCode"] = "クラウドゲーム API がエラーを返しました code={0}：{1}",

        
        ["GachaLogPage_ExportEmpty"] = "現在のアカウントにはエクスポートできる喚取履歴がありません",
        ["GachaLog_PityBuilt"] = "天井",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "ゲームパスの設定を削除しました",
        ["GameLauncherSettingDialog_WrongServerExe"] = "選択されたゲーム exe は現在のサーバーに属していないため、選び直してください",
        ["GameLauncherSettingDialog_ExpectExe"] = "ゲームの実行ファイルを選択してください：{0}",

        
        ["KrSdk_ErrNoUserCache"] = "KRSDKUserCache.json が見つかりません。先にゲーム内で一度ログインしてください。",
        ["KrSdk_ErrEmptyContent"] = "内容が空です",
        ["KrSdk_ErrParseFailed"] = "KRSDKUserCache.json の解析に失敗したため、ログイン状態を壊さないよう書き込みを中止しました。",
        ["KrSdk_ErrNoAccountList"] = "KRSDKUserCache.json に account_list がありません。",
        ["KrSdk_ErrAccountNotInCache"] = "このアカウントはローカルキャッシュにありません。",
        ["KrSdk_ErrVerifyFailed"] = "書き込み後の検証に失敗したため、変更前のキャッシュに戻しました。",

        
        ["GameUpdate_ErrNoPlan"] = "ダウンロードプランがありません",
        ["GameUpdate_ErrNoTarget"] = "ターゲットバージョンがありません",
        ["GameUpdate_ErrNoGamePath"] = "ゲームパスが未設定のため、アップデートできません",
        ["GameUpdate_ErrNoManifest"] = "ダウンロードマニフェストの取得に失敗しました",
        ["GameUpdate_ErrFileMissing"] = "アップデートファイルが見つかりません：{0}",
        ["GameUpdate_ErrFileChecksum"] = "アップデートファイルの検証に失敗しました：{0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "ゲームの LocalStorage.db が見つかりません",
        ["GameSetting_ErrNoGameUserSettings"] = "ゲームの GameUserSettings.ini が見つかりません",

        
        ["KuroBbs_ErrConnection"] = "接続に失敗しました。ステータスコード：{0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken が空の accessToken を返しました",
        ["KuroBbs_ErrAccessToken"] = "B-At accessToken の取得に失敗しました（code={0}）：{1}",
        ["KuroBbs_ErrSendCode"] = "認証コードの送信に失敗しました",
        ["KuroBbs_ErrLogin"] = "ログインに失敗しました",
        ["KuroBbs_SignOk"] = "チェックインに成功しました",
        ["KuroBbs_SignAlready"] = "本日はチェックイン済みです",
        ["KuroBbs_SignFail"] = "チェックインに失敗しました",

        

        
        ["Ugf_ErrNoUid"] = "UGF ファイルにアカウントの uid がありません",
        ["Ugf_ErrUnknownFormat"] = "認識できないファイル形式です：UGFまたはガチャ記録 JSON が必要です",

        

        
        ["CombatPage_AreaFallback"] = "エリア {0}",

        
        ["KuroBbs_UnionLevel"] = "ユニオンレベル",
        ["KuroBbs_CommanderLevel"] = "コマンダーレベル",
        ["KuroBbs_NoLevel"] = "レベルなし",
        ["KuroBbs_SelectRolePgr"] = "パニシングのキャラクターを選択",
        ["KuroBbs_SelectRoleWuwa"] = "鳴潮のキャラクターを選択",
        ["KuroBbs_SwitchedPgr"] = "パニシングのキャラクターを切り替えました",
        ["KuroBbs_SwitchedWuwa"] = "鳴潮のキャラクターを切り替えました",
        ["PgrData_DailySupply"] = "デイリー補給",
        ["KuroBbs_AutoSignDoneToast"] = "デイリーチェックイン完了",
        ["KuroBbs_AutoSignFailedToast"] = "デイリーチェックインに失敗しました",

        
        ["RolePage_WeaponLevel"] = "LV.{0} · 突破{1}",

        
        ["Common_UnknownError"] = "不明なエラー",
    };

}
