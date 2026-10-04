using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangKo
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "런처",
        ["LauncherPage_GameSetting"] = "게임 설정",
        ["MainPage_GameScreenshot"] = "스크린샷",
        ["GachaLogPage_ConveneRecords"] = "소환 기록",
        ["Toolbox"] = "도구",

        
        ["ScreenshotPage_MultiSelect"] = "다중 선택",
        ["ScreenshotPage_OpenFolder"] = "폴더 열기",
        ["ScreenshotPage_Total0Files"] = "총 {0}장",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1}장 선택됨",
        ["ScreenshotPage_Empty"] = "스크린샷 폴더를 찾을 수 없습니다",
        ["ScreenshotPage_EmptyDesc"] = "스크린샷은 기본적으로 <게임 폴더>\\Client\\Saved\\Screenshots에 저장됩니다. 먼저 설정에서 게임 폴더를 지정하세요",
        ["ScreenshotPage_Delete"] = "삭제",
        ["ScreenshotPage_DeleteConfirm"] = "선택한 {0}장의 스크린샷을 휴지통으로 이동할까요?",
        ["ScreenshotPage_Deleted"] = "{0}장의 스크린샷을 휴지통으로 이동했습니다",
        ["ScreenshotPage_DeleteFailed"] = "삭제 실패: 일부 파일이 다른 프로그램에서 사용 중일 수 있습니다",
        ["ScreenshotPage_SelectFirst"] = "먼저 삭제할 스크린샷을 선택하세요",
        ["ScreenshotPage_OpenImage"] = "이미지 열기",
        ["ScreenshotPage_DeletedPartial"] = "{0} / {1}장을 삭제했습니다. 나머지는 사용 중일 수 있습니다",

        
        ["KuroBbs"] = "쿠로 BBS",
        ["KuroBbs_SignIn"] = "출석 체크",
        ["KuroBbs_Role"] = "캐릭터",
        ["KuroBbs_ResourceBrief"] = "자원 요약",

        
        ["PgrData_NoRole"] = "이 쿠로 BBS 계정에는 아직 퍼니싱 캐릭터가 설정되어 있지 않습니다. 쿠로 BBS 앱의 '마이'에서 설정한 후 다시 확인하세요.",
        ["PgrData_Locked"] = "잠김",
        ["PgrData_FullIn"] = "{0} 후 전부 회복",

        
        ["KuroBbs_DataTerminal"] = "데이터 터미널",
        ["KuroBbs_CombatData"] = "전투 데이터",
        ["DataTerminal_NoAccount"] = "아직 쿠로 BBS에 로그인하지 않았습니다. 위의 계정 화살표를 눌러 로그인한 후 확인하세요.",
        ["DataTerminal_NoRole"] = "이 쿠로 BBS 계정에는 아직 {0} 캐릭터가 연동되어 있지 않습니다. 먼저 쿠로 BBS 앱에서 연동해 주세요.",
        ["DataTerminal_LoadFailed"] = "페이지를 불러오지 못했습니다: {0}",

        
        ["KuroBbs_PgrRole"] = "캐릭터",
        ["KuroBbs_PgrResource"] = "자원 리포트",
        ["KuroBbs_PgrCage"] = "통곡의 감옥",
        ["KuroBbs_PgrWarArea"] = "교전 지역",
        ["KuroBbs_PgrTransfinite"] = "역대 전투 매핑",
        ["PgrRole_Fight"] = "전투력 {0}",
        ["PgrResource_Total"] = "최근 6개월 누적",
        ["PgrResource_BlackCard"] = "블랙 카드",
        ["PgrResource_Develop"] = "개발 자원",
        ["PgrResource_Trade"] = "교역 증표",
        ["PgrResource_SelectPeriod"] = "기간 선택",
        ["PgrChallenge_NotOpen"] = "현재는 개방 기간이 아닙니다",
        ["PgrChallenge_Locked"] = "아직 잠금 해제되지 않았습니다",
        ["PgrChallenge_NoArea"] = "구역이 선택되지 않았습니다",
        ["PgrChallenge_NoRecord"] = "이번 기간의 도전 기록이 없습니다",
        ["PgrChallenge_Level"] = "레벨 구간 {0}",
        ["PgrWarArea_TotalPointLabel"] = "총점",
        ["PgrWarArea_TotalTimesLabel"] = "도전 횟수",
        ["PgrWarArea_Waves"] = "웨이브 {0}",
        ["PgrCommon_LoadFailed"] = "데이터를 가져오지 못했습니다",

        
        ["DailyBriefing"] = "브리핑",
        ["DailyBriefing_Desc"] = "게임 내 일일 데이터",
        ["DailyBriefing_Refresh"] = "새로 고침",
        ["DailyBriefing_Error"] = "브리핑 데이터를 가져오지 못했습니다",
        ["DailyBriefing_NoGameAccount"] = "로그인 계정을 찾을 수 없습니다. 로그인 후 다시 시도해 주세요.",
        ["DailyBriefing_NoRole"] = "이 계정의 게임 캐릭터가 아직 식별되지 않았습니다. 게임을 한 번 실행해 주세요(캐릭터 정보는 공식 SDK가 온라인으로 식별합니다).",
        ["Briefing_Liveness"] = "일일 활성도",
        ["Briefing_StoreEnergy"] = "결정 단질",
        ["Briefing_BattlePass"] = "선약 라디오",
        ["Briefing_ActionPoint"] = "혈청",
        ["Briefing_Level"] = "레벨",
        ["KuroBbs_TowerOfAdversity"] = "역경의 탑",
        ["KuroBbs_SongOfVoidSea"] = "공허바다의 노래",
        ["KuroBbs_DoomsdayMatrix"] = "종언 매트릭스",
        ["KuroBbs_ComingSoon"] = "{0} 기능 개발 중",
        ["KuroBbs_TokenExpired"] = "쿠로 BBS 로그인이 만료되었습니다",
        ["KuroBbs_TokenExpiredDesc"] = "설정 → 쿠로 BBS 계정에서 토큰을 다시 캡처하거나 입력하세요",

        
        ["KuroAccount_UsbTitle"] = "ADB 도구",
        ["KuroAccount_UsbShortDesc"] = "안드로이드 폰을 연결해 쿠로 BBS 토큰과 Did를 캡처",
        ["KuroAccount_UsbDesc"] = "USB 케이블로 휴대폰을 연결하고 USB 디버깅을 켜세요. 휴대폰에서 쿠로 BBS 앱을 열고 '데이터 터미널' 또는 '육성 계산기' 페이지에 진입한 뒤 '캡처 시작'을 누르면 도구가 토큰과 did를 자동으로 캡처합니다. adb.exe가 필요합니다(",
        ["KuroAccount_UsbDescTail"] = " 공식 페이지에서 다운로드).",
        ["KuroAccount_UsbVerWarn"] = "⚠ 새 버전의 쿠로 BBS 앱에서는 이 캡처 방식을 사용할 수 없습니다. 이전 버전을 사용해 주세요.",
        ["KuroAccount_AdbPlaceholder"] = "adb.exe 경로 (비우면 PATH 사용)",
        ["KuroAccount_ApplyAdb"] = "적용",
        ["KuroAccount_SelectDevice"] = "기기 선택",
        ["KuroAccount_RefreshDevices"] = "새로 고침",
        ["KuroAccount_StartCapture"] = "캡처 시작",
        ["KuroAccount_StopCapture"] = "캡처 중지",
        ["KuroAccount_FieldsTitle"] = "계정 정보",
        ["KuroAccount_FillHint"] = "캡처 결과가 자동으로 입력되며, 직접 붙여넣을 수도 있습니다",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "저장",
        ["KuroAccount_Clear"] = "지우기",
        ["KuroAccount_Log"] = "캡처 로그",
        ["KuroAccount_AdbInvalidTitle"] = "ADB 경로가 잘못되었습니다",
        ["KuroAccount_AdbInvalidDesc"] = "유효한 adb.exe 전체 경로를 입력하세요",
        ["KuroAccount_AdbApplied"] = "ADB 경로가 적용되었습니다",
        ["KuroAccount_AdbApplyFailed"] = "ADB 경로 적용에 실패했습니다",
        ["KuroAccount_NoDevice"] = "안드로이드 기기를 찾을 수 없습니다",
        ["KuroAccount_NoDeviceDesc"] = "휴대폰을 연결하고 USB 디버깅 권한을 허용하세요",
        ["KuroAccount_RefreshDevicesFailed"] = "기기를 새로 고치지 못했습니다",
        ["KuroAccount_SelectDeviceFirst"] = "먼저 기기를 선택하세요",
        ["KuroAccount_SelectDeviceFirstDesc"] = "먼저 '새로 고침'을 누른 뒤 기기를 선택하세요",
        ["KuroAccount_NoPage"] = "캡처 가능한 웹 페이지를 찾을 수 없습니다",
        ["KuroAccount_NoPageDesc"] = "휴대폰에서 '데이터 터미널' 같은 웹 페이지에 진입한 뒤 '캡처 시작'을 누르세요",
        ["KuroAccount_CaptureFailed"] = "캡처 연결에 실패했습니다",
        ["KuroAccount_CaptureStarted"] = "모니터링이 시작되었습니다",
        ["KuroAccount_CaptureStartedDesc"] = "휴대폰에서 '데이터 터미널' 또는 '육성 계산기' 페이지에 진입하세요",
        ["KuroAccount_Disconnected"] = "휴대폰 연결이 끊겼습니다",
        ["KuroAccount_DisconnectedDesc"] = "WebView 페이지가 닫혔을 수 있습니다. '캡처 시작'을 다시 누르세요.",
        ["KuroAccount_NoSocket"] = "WebView 디버깅 인터페이스를 찾을 수 없습니다",
        ["KuroAccount_NoSocketDesc"] = "이 버전의 쿠로 BBS는 WebView 원격 디버깅이 꺼져 있을 수 있습니다. 자세한 내용은 진단 보고서를 참조하세요.",
        ["KuroAccount_DiagTitle"] = "캡처 진단 보고서",
        ["KuroAccount_DiagHint"] = "보고서에서 devtools 소켓이 0이면, 이 버전의 쿠로 BBS 앱은 WebView 원격 디버깅을 더 이상 사용하지 않는다는 뜻입니다(Chrome의 chrome://inspect에서도 보이지 않습니다). 이 경우 ADB 캡처는 불가능합니다. 대신 '쿠로 BBS → 계정 → 로그인'의 휴대폰 번호 + 인증번호 로그인을 사용하면 토큰과 Did를 얻을 수 있으며, 휴대폰 캡처나 ADB가 필요 없습니다.",
        ["KuroAccount_DiagCopy"] = "보고서 복사",
        ["KuroAccount_DiagClose"] = "닫기",
        ["KuroAccount_DiagCopied"] = "진단 보고서가 복사되었습니다",
        ["KuroAccount_Invalid"] = "정보가 불완전합니다",
        ["KuroAccount_SaveNeedTokenDid"] = "토큰과 Did는 필수이며, roleId는 자동 조회할 수 있습니다",
        ["KuroAccount_Saved"] = "계정이 저장되었습니다",
        ["KuroAccount_Cleared"] = "계정이 삭제되었습니다",

        
        ["RolePage_Refresh"] = "새로 고침",
        ["RolePage_NoAccount"] = "쿠로 BBS 계정이 아직 설정되지 않았습니다",
        ["RolePage_NoAccountDesc"] = "설정 → 쿠로 BBS 계정에서 설정을 마친 뒤 새로 고치세요",
        ["RolePage_LoadFailed"] = "캐릭터를 불러오지 못했습니다",
        ["RolePage_Back"] = "목록으로 돌아가기",
        ["RolePage_Phantoms"] = "에코",

        
        ["GachaLogPage_Refresh"] = "기록 업데이트",
        ["GachaLogPage_ImportUrl"] = "링크 붙여넣어 가져오기",
        ["GachaLogPage_ImportUrlDesc"] = "소환 상세 페이지 링크를 붙여넣으세요(게임 내 '소환 상세'를 연 뒤 로그에서 얻거나, 다른 도구에서 복사). 클라우드폰 유저도 사용할 수 있습니다.",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "잘못된 링크: record_id가 포함된 소환 상세 페이지 전체 링크를 붙여넣으세요",
        ["GachaLogPage_SelectPlayer"] = "계정 선택",
        ["GachaLogPage_CopyUrl"] = "URL 복사",
        ["GachaLogPage_DeletePlayer"] = "기록 삭제",
        ["GachaLogPage_Delete"] = "삭제",
        ["GachaLogPage_Avg5"] = "평균",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "통계",
        ["GachaLogPage_Times"] = "회",
        ["GachaLogPage_ItemStats"] = "아이템 통계",
        ["GachaLogPage_UpdateSavedUrl"] = "업데이트 (저장된 URL)",
        ["GachaLogPage_UpdateAll"] = "업데이트 (전체)",
        ["GachaLogPage_InputUrl"] = "URL 입력",
        ["GachaLogPage_ViaCloudGame"] = "클라우드 게임으로 가져오기",
        ["CloudGame_Title"] = "클라우드 명조 - 로그인 후 한 번에 소환 링크 가져오기",
        ["CloudGame_Grab"] = "소환 링크 가져오기",
        ["CloudGame_StatusDefault"] = "여기서 쿠로 계정으로 로그인하고 '게임 입장'을 누른 뒤 '소환 상세' 화면을 한 번 열고, '소환 링크 가져오기'를 누르세요",
        ["CloudGame_StatusSessionReady"] = "클라우드 게임 로그인 상태가 인식되었습니다. '소환 링크 가져오기'를 눌러 동기화를 완료하세요",
        ["CloudGame_StatusGrabbing"] = "로그인 정보를 읽는 중…",
        ["CloudGame_StatusOk"] = "가져오기 성공 (플레이어 {0}). 소환 기록을 동기화하는 중…",
        ["CloudGame_StatusFailReason"] = "가져오기 실패: {0}",
        ["CloudGame_StatusNoRuntime"] = "내장 브라우저 초기화 실패: WebView2 런타임이 설치되어 있지 않습니다. 마이크로소프트 공식 사이트에서 설치한 뒤 다시 시도하세요",
        ["GachaLogPage_ImportExport"] = "가져오기 & 내보내기",
        ["GachaLogPage_HidePools"] = "배너 숨기기",
        ["GachaLogPage_ExportAs"] = "내보내기",
        ["GachaLogPage_ImportRecords"] = "기록 가져오기 (파일 선택)",
        ["GachaLogPage_ImportOk"] = "가져오기 성공: 플레이어 {0}, 총 {1}건의 기록 (중복 기록은 자동으로 제거됨)",
        ["GachaLogPage_ImportFail"] = "가져오기 실패: {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "파일에 계정 정보가 없고 선택된 계정도 없어 소유자를 판별할 수 없습니다",
        ["GachaLogPage_OtherSettings"] = "기타 설정",
        ["GachaLogPage_LinkHelp"] = "Authkey Timeout이 계속 발생하나요?",
        ["GachaLogPage_LinkHelpDesc"] = "캐시 폴더를 삭제한 뒤 게임을 재시작하세요",
        ["GachaLogPage_ClearSavedUrl"] = "캐시 폴더 삭제",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "로컬에 저장된 소환 링크를 삭제할까요? (이미 받아온 기록은 삭제되지 않습니다)",
        ["GachaLogPage_ClearSavedUrlDone"] = "저장된 소환 링크가 삭제되었습니다. 게임에 들어가 '소환 상세' 페이지를 다시 열어주세요",
        ["GachaLogPage_Language"] = "언어",
        ["GachaLogPage_LanguageSaved"] = "언어가 저장되었습니다. 다음 기록 업데이트 때 적용됩니다",
        ["GachaLogPage_LanguageApplied"] = "{0}건의 기록 아이템 이름을 {1}(으)로 변경했습니다",
        ["GachaLogPage_NoSavedUrl"] = "저장된 링크가 없습니다. 먼저 '업데이트 (전체)'를 실행하세요",
        ["GachaLogPage_LinkExpired"] = "소환 링크가 만료되었습니다",
        ["GachaLogPage_LinkExpiredDesc"] = "링크가 만료되었습니다 (API 응답 {0}). 게임 내에서 '소환 상세'를 다시 열어주세요",
        ["GachaLogPage_UpdateFailed"] = "소환 기록 업데이트 실패: {0}",
        ["GachaLogPage_FetchFailed"] = "소환 데이터를 가져오지 못했습니다. 다시 시도해 주세요",
        ["GachaLogPage_LogNotFound"] = "게임 로그 또는 소환 링크를 찾을 수 없습니다: 설정에서 게임(Wuthering Waves.exe)을 지정하고 게임 내에서 '소환 상세' 페이지를 한 번 열거나, '기록 업데이트' 메뉴에서 '링크 붙여넣어 가져오기'를 선택하세요",
        ["GachaLogPage_UrlNotFound"] = "소환 링크를 찾을 수 없습니다. 게임 내에서 '소환 상세' 페이지를 한 번 열어주세요",
        ["GachaLogPage_UrlCopied"] = "소환 링크가 클립보드에 복사되었습니다",
        ["GachaLogPage_PlayerDeleted"] = "UID {0}의 로컬 소환 기록이 삭제되었습니다",
        ["GachaLogPage_DeleteConfirmText"] = "UID {0}의 모든 로컬 소환 기록을 삭제합니다. 이 작업은 되돌릴 수 없습니다.",
        ["GachaLogPage_EmptyTitle"] = "아직 소환 기록이 없습니다",
        ["GachaLogPage_EmptyDesc"] = "먼저 설정에서 게임 폴더를 지정하고, 게임에 들어가 '소환 상세' 페이지를 한 번 연 뒤, 여기로 돌아와 '기록 업데이트'를 누르면 동기화됩니다.",

        
        ["LauncherPage_StartGame"] = "게임 시작",
        ["LauncherPage_InstallGame"] = "게임 설치",
        ["LauncherPage_ResumeInstall"] = "설치 계속",
        ["AccountSwitcher_SwitchAccount"] = "계정 전환",
        ["AccountSwitcher_GameRunning"] = "게임 실행 중에는 계정을 전환할 수 없습니다",
        ["AccountSwitcher_AccountInfo"] = "게임 계정",
        ["AccountSwitcher_Nickname"] = "닉네임",
        ["AccountSwitcher_RoleId"] = "게임 UID",
        ["AccountSwitcher_Save"] = "저장",
        ["AccountSwitcher_Delete"] = "삭제",
        ["AccountSwitcher_Unsaved"] = "현재 로그인된 계정이 저장되지 않았습니다",
        ["Notice_Activity"] = "이벤트",
        ["Notice_NoticeTab"] = "공지",
        ["Notice_News"] = "뉴스",
        ["Notice_LoadFailed"] = "공지를 불러오지 못했습니다",
        ["LauncherPage_UpdateGame"] = "게임 업데이트",

        
        ["SettingPage_Language"] = "언어",

        
        ["GameUpdate_PredownloadAvailable"] = "새 버전 {0} 사전 다운로드 시작",
        ["GameUpdate_Predownloaded"] = "{0} 사전 다운로드 완료. 업데이트가 열리면 바로 설치할 수 있습니다",
        ["GameUpdate_PredownloadButton"] = "사전 다운로드",
        ["GameUpdate_ConfirmPredownload"] = "버전 {0}을(를) 사전 다운로드할까요? 총 다운로드 {1}. 파일은 게임 폴더 안의 predownload 폴더에 저장되며, 공식 업데이트를 적용할 때 다시 다운로드할 필요가 없습니다.",
        ["GameUpdate_Applying"] = "업데이트 압축 해제 중…",
        ["GameUpdate_Pausing"] = "일시 중지 중…",
        ["GameUpdate_Predownloading"] = "{0} 사전 다운로드 중…",

        ["GameUpdate_Done"] = "{0}(으)로 업데이트했습니다",
        ["GameUpdate_PredownloadDone"] = "{0} 사전 다운로드 완료. 업데이트가 열리면 바로 설치할 수 있습니다",
        ["GameUpdate_NoGameDir"] = "먼저 설정에서 게임을 지정하세요",
        ["GameUpdate_Verifying"] = "검증 중 {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "검증 완료",
        ["GameUpdate_VerifyDoneNone"] = "손상되거나 누락된 파일이 없습니다",
        ["GameUpdate_VerifyDoneDamage"] = "손상 / 누락 파일 {0}개 발견 (총 {1})",
        ["GameUpdate_VerifyNoManifest"] = "검증 목록을 가져올 수 없습니다. 네트워크를 확인하거나 나중에 다시 시도하세요",
        ["GameUpdate_Repairing"] = "복구 중…",
        ["GameUpdate_RepairDone"] = "파일 {0}개를 복구했습니다",
        ["GameUpdate_RepairNoBaseUrl"] = "현재 버전은 온라인 복구를 지원하지 않습니다. 먼저 게임을 최신 버전으로 업데이트한 뒤 다시 시도하세요",

        
        ["GameLauncherPage_Gallery"] = "갤러리",
        ["Common_Copy"] = "복사",
        ["ImageViewWindow2_SaveAs"] = "다른 이름으로 저장",
        ["GameLauncherPage_PlayPause"] = "재생/일시정지",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "이미지 파일을 끌어다 놓아 배경 변경",
        ["Background_Official"] = "공식 애니메이션 배경",
        ["Background_OfficialStatic"] = "공식 정적 배경",
        ["Background_OpenFolder"] = "배경 폴더 열기",
        ["Background_AddFailed"] = "이 배경 파일은 사용할 수 없습니다",
        ["InstallGameDialog_LocateGame"] = "게임 찾기",
        ["GameLauncherPage_GameIsRunning"] = "게임이 실행 중입니다",

        
        ["SettingPage_Download"] = "다운로드",

        
        ["DownloadGamePage_Downloading"] = "다운로드 중",
        ["DownloadGamePage_Verifying"] = "검증 중",
        ["DownloadGamePage_Pause"] = "일시정지",

        
        ["Common_Confirm"] = "확인",
        ["Common_Cancel"] = "취소",
        ["Common_Error"] = "오류",
        ["Common_Ok"] = "확인",
        ["Common_Select"] = "선택",
        ["Common_Apply"] = "적용",
        ["Common_Exit"] = "종료",

        
        ["SettingPage_AppSettings"] = "설정",
        ["SettingPage_About"] = "정보",
        ["SettingPage_General"] = "일반",
        ["SettingPage_FileManagement"] = "파일 관리",

        
        ["FilePage_DataFolder"] = "데이터 폴더",
        ["FilePage_DataFolderDesc"] = "개인 데이터는 아래 폴더에 저장됩니다. 중요한 데이터이므로 함부로 삭제하지 마세요.",
        ["FilePage_ReselectDataFolder"] = "데이터 폴더 다시 선택",
        ["FilePage_OpenDataFolder"] = "데이터 폴더 열기",
        ["FilePage_DeleteAllSettings"] = "모든 설정 삭제",
        ["FilePage_ReselectFolderDone"] = "새 데이터 폴더가 저장되었습니다. 앱 재시작 후 적용됩니다. 새 폴더는 처음에 비어 있으며, '클라우드 동기화'로 기존 데이터를 가져올 수 있습니다.",
        ["FilePage_DeleteSettingsDone"] = "설정이 삭제되었습니다. 앱 재시작 후 적용됩니다.",
        ["FilePage_DeleteSettingsConfirm"] = "모든 설정(config.ini)이 삭제되고 재시작 후 기본값으로 돌아갑니다. 플레이 시간과 소환 기록 같은 데이터베이스 내용은 영향을 받지 않습니다. 계속할까요?",
        ["FilePage_OpenLogFolder"] = "로그 폴더 열기",
        ["FilePage_Cache"] = "캐시",
        ["FilePage_CacheLog"] = "로그",
        ["FilePage_CacheImage"] = "이미지",
        ["FilePage_CacheBackground"] = "런처 배경",
        ["FilePage_CleanCache"] = "캐시 정리",
        ["FilePage_CleanBackgroundCache"] = "런처 배경 파일 정리",

        
        ["FilePage_HardLink"] = "하드 링크",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "같은 디스크의 다른 리전 클라이언트와 동일한 파일은 하드링크로 재사용됩니다. 예상 다운로드는 약 {0}이며, 실제는 설치 시 파일별 검증 기준입니다.",
        ["InstallGameDialog_SpaceDesc"] = "압축 패키지 크기이며, 압축 해제 후 실제 사용 공간은 이 값보다 커집니다.",
["FilePage_HardLinkDesc"] = "같은 디스크에 여러 클라이언트를 설치할 때 동일 파일을 하드링크하여 저장 공간 사용을 줄입니다",
        ["FilePage_Enabled"] = "켜짐",
        ["FilePage_Disabled"] = "꺼짐",
        ["FilePage_SpeedLimit"] = "속도 제한",
        ["FilePage_SpeedLimitDesc"] = "0이면 제한 없음",
        ["FilePage_DownloadThreads"] = "멀티스레드",
        ["FilePage_DownloadThreadsDesc"] = "동시 다운로드 연결 수. HDD에서는 병렬이 과하면 탐색 때문에 오히려 느려집니다. 변경은 다음부터 적용됩니다.",

        
        ["KuroBbs_RefreshInfo"] = "정보 새로 고침",
        ["KuroBbs_Login"] = "로그인",
        ["KuroBbs_Settings"] = "쿠로 BBS 설정",

        
        ["Login_Title"] = "쿠로 BBS 로그인",
        ["Login_ByToken"] = "토큰 로그인",
        ["Login_BySms"] = "인증번호 로그인",
        ["Login_TokenPlaceholder"] = "쿠로 BBS 로그인 토큰을 붙여넣으세요",
        ["Login_DidPlaceholder"] = "토큰과 함께 캡처한 Did (비우면 자동 생성)",
        ["Login_TokenTip"] = "토큰과 Did는 모바일 쿠로 BBS 앱에서 캡처하며(도구 → USB 캡처), 한 쌍으로 저장됩니다",
        ["Login_PhonePlaceholder"] = "휴대폰 번호",
        ["Login_CodePlaceholder"] = "SMS 인증번호",
        ["Login_CodeLabel"] = "인증번호",
        ["Login_SmsTip"] = "'인증번호 보내기'를 누른 뒤 휴대폰으로 받은 SMS 인증번호를 입력하세요. 로그인에 성공하면 계정이 자동 저장됩니다.",
        ["Login_SendCode"] = "인증번호 보내기",
        ["Login_Confirm"] = "로그인",
        ["Login_Cancel"] = "취소",
        ["Login_PhoneInvalid"] = "휴대폰 번호 형식이 올바르지 않습니다",
        ["Login_Sending"] = "인증번호 전송 중…",
        ["Login_CodeSent"] = "인증번호를 보냈습니다. 문자를 확인하세요",
        ["Login_CodeSendFailed"] = "인증번호 전송 실패: {0}",
        ["Login_NeedCode"] = "SMS 인증번호를 입력하세요",
        ["Login_TokenInvalid"] = "토큰이 잘못되었습니다: 쿠로 BBS 로그인 토큰 전체를 붙여넣으세요",
        ["Login_Working"] = "로그인 중…",
        ["Login_Failed"] = "로그인 실패: {0}",
        ["Login_SeekRoleFailed"] = "연동 캐릭터 조회 실패: {0}",
        ["Login_Success"] = "쿠로 BBS 계정 '{0}' 로그인 성공",

        
        ["ResourceBrief_Month"] = "월간",
        ["ResourceBrief_Week"] = "주간",
        ["ResourceBrief_Version"] = "버전",
        ["ResourceBrief_HistoryData"] = "히스토리",
        ["ResourceBrief_TotalGain"] = "이번 기간 획득",
        ["ResourceBrief_StarComposition"] = "아스트라이트 수입 구성",
        ["ResourceBrief_CoinComposition"] = "셸 크레딧 수입 구성",
        ["ResourceBrief_Detail"] = "상세 내역",
        ["ResourceBrief_Empty"] = "이번 기간 데이터가 없습니다",

        
        ["SignPage_DoSignIn"] = "출석 체크",
        ["SignPage_MonthlySign"] = "이번 달 출석",
        ["SignPage_SignedToday"] = "오늘 출석 완료",
        ["SignPage_NotSignedToday"] = "오늘 아직 출석하지 않았습니다",
        ["SignPage_Done"] = "출석 완료",
        ["SignPage_AutoSign"] = "자동 출석",
        ["SignPage_SignedDaysFormat"] = "이번 달 {0}일 출석",
        ["SignPage_DayFormat"] = "{0}일차",
        ["SignPage_Energy"] = "파장판",
        ["TowerPage_TotalStarCount"] = "총 별: {0} / {1}",
        ["TowerPage_Floor"] = "{0}층",
        ["TowerPage_NoChallengeData"] = "도전 데이터가 없습니다",
        ["TowerPage_RefreshAfter"] = "{0}일 {1}시간 후 갱신",
        ["MatrixPage_NoRecord"] = "이번 기간 기록 없음",
        ["CombatPage_Locked"] = "{0}은(는) 이번 기간에 잠겨 있거나 데이터가 없습니다",
        ["CombatPage_NoData"] = "데이터 없음: 서버가 아직 이번 기간 기록을 생성하지 않았습니다. 쿠로 BBS 앱에서 '{0}' 페이지를 열어 데이터를 새로 고친 뒤 다시 시도하세요",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "총 플레이 시간",
        ["PlayTimeStatsDialog_Started0Times"] = "{0}회 실행",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "일평균 플레이 시간",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "{0}일 플레이",
        ["PlayTimeStatsDialog_LongestStreak"] = "최장 연속 플레이",
        ["PlayTimeStatsDialog_0Days"] = "{0}일",
        ["PlayTimeStatsDialog_LongestSession"] = "최장 단일 플레이",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "하루 최장 플레이",
        ["PlayTimeButton_LastStartup"] = "마지막 플레이",
        ["PlayTimeStatsDialog_Last15Days"] = "최근 15일",
        ["PlayTimeStatsDialog_Last12Weeks"] = "최근 12주",
        ["PlayTimeStatsDialog_Last12Months"] = "최근 12개월",
        ["PlayTimeStatsDialog_Total"] = "합계",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "플레이 시간 통계",
        ["PlayTimeStatsDialog_StatsTip"] = "통계는 이 런처로 게임을 시작한 시간만 포함합니다.",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "시스템 트레이로 최소화",
        ["GameLauncherSettingDialog_BasicInformation"] = "기본 정보",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "제거 확인",
        ["GameLauncherSettingDialog_CustomBackground"] = "배경",
        ["GameLauncherSettingDialog_RelocateGame"] = "게임 다시 찾기",
        ["GameLauncherSettingDialog_Size"] = "크기",
        ["GameLauncherSettingDialog_StartRepairing"] = "복구 시작",
        ["GameLauncherSettingDialog_StartupArguments"] = "시작 인수",
        ["GameLauncherSettingDialog_CommandLineArgument"] = "명령줄 인수",
        ["GameLauncherSettingDialog_CustomProgramTip"] = "이 서버의 게임이 구성되지 않았거나 exe가 없을 경우 '게임 시작'을 클릭하면 이 프로그램이 대신 실행됩니다. 게임 본체를 사용할 수 있는 경우에는 적용되지 않습니다.",
        ["LauncherPage_CustomStartExecutable"] = "사용자 지정 실행 파일",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "게임 제거 경고",
        ["LauncherPage_RepairGame"] = "게임 복구",
        ["LauncherPage_ShowGameAnnouncement"] = "게임 공지 표시",
        ["LauncherPage_UninstallGame"] = "게임 제거",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "매핑된 네트워크 드라이브는 지원되지 않습니다. 이중 백슬래시로 시작하는 네트워크 공유 경로를 사용하세요.",

        
        ["SettingPage_BgSizeTip"] = "배경 이미지 크기 안내",
        ["SettingPage_CustomBackgroundSupportVideo"] = "사용자 지정 배경은 동영상 지원",

        
        ["GameSettingPage_Resolution"] = "해상도",
        ["GameSettingPage_FullScreen"] = "전체 화면",
        ["GameSettingPage_UsePopupWindow"] = "테두리 없는 창",
        ["GameSettingPage_CustomResolution"] = "사용자 지정 해상도",
        ["GameSettingPage_CustomResolutionTip"] = "사용자 지정 해상도는 '창 모드'에서 적용됩니다",
        ["GameSettingPage_VoiceLanguage"] = "음성 언어",
        ["GameSettingPage_VoiceLanguageDescription"] = "게임을 처음 실행하기 전에 설정하면 일부 음성 리소스 다운로드를 피할 수 있습니다",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "HDR 사용",
        ["GameSettingPage_HDRNotSupported"] = "현재 디스플레이는 HDR을 지원하지 않습니다",
        ["GameSettingPage_HDRNotEnabled"] = "Windows HDR이 아직 켜지지 않았습니다. 켜도 적용되지 않을 수 있습니다",
        ["GameSettingPage_GameNotInstalled"] = "게임이 설치되지 않았습니다",
        ["GameSettingPage_GameNotInstalledDesc"] = "먼저 런처 페이지에서 게임 경로를 설정하세요",
        ["GameSettingPage_CloudGameNotSupport"] = "클라우드 게임은 로컬 게임 설정이 필요 없습니다",
        ["GameSettingPage_SettingNotEffect"] = "게임 실행 중 적용한 설정은 효과가 없습니다",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "해상도는 0보다 커야 합니다",
        ["GameSettingPage_LoadFailed"] = "게임 설정을 읽지 못했습니다",
        ["GameSettingPage_ApplyFailed"] = "게임 설정 적용에 실패했습니다",
        ["GameSettingPage_Applied"] = "적용되었습니다. 다음 게임 시작 시 효과가 있습니다.",

        

        
        ["SettingPage_WindowBehavior"] = "창 동작",
        ["SettingPage_CloseWindowOption"] = "창 닫기 옵션",
        ["SettingPage_ExitApp"] = "완전 종료",
        ["SettingPage_AfterLaunchOption"] = "게임 실행 후",
        ["SettingPage_KeepWindow"] = "창 유지",
        ["SettingPage_AfterGameExitOption"] = "게임 종료 후",
        ["SettingPage_RestoreMainWindow"] = "메인 창 표시",
        ["SettingPage_KeepHidden"] = "숨김 유지",
        ["SettingPage_CloseLauncher"] = "런처 종료",
        ["SettingPage_KuroCommunitySign"] = "쿠로 BBS 커뮤니티 출석 체크 (쿠로 코인)",
        ["SettingPage_SuggestSystemFeatures"] = "더 나은 경험을 위해 시스템 설정에서 아래 기능을 켜는 것을 권장합니다.",
        ["SettingPage_TransparencyEffects"] = "투명 효과",
        ["SettingPage_AnimationEffects"] = "애니메이션 효과",

        
        ["GameSelector_Subtitle"] = "왼쪽 위 아이콘을 눌러 게임을 선택하거나 전환하세요",
        ["GameSelector_AutoFind"] = "설치된 게임 자동 찾기",
        ["GameSelector_SelectServer"] = "서버 선택",
        ["GameSelector_InstalledSize"] = "설치된 게임의 실제 디스크 사용량 ",
        ["GameSelector_Calculating"] = "계산 중",

        
        ["KuroBbs_AccountLevelLabel"] = "공감 레벨 ",
        ["KuroBbs_SwitchRole"] = "캐릭터 전환",
        ["KuroBbs_ClearAccount"] = "계정 지우기",

        
        ["GameBiz_Wuwa"] = "명조",
        ["GameBiz_WuwaCn"] = "명조 · 중국 서버",
        ["GameBiz_WuwaGlobal"] = "명조 · 글로벌 서버",
        ["GameBiz_WuwaBilibili"] = "명조 · 빌리빌리",
        ["GameBiz_Pgr"] = "퍼니싱: 그레이 레이븐",
        ["GameBiz_ShortCn"] = "중국 서버",
        ["GameBiz_ShortGlobal"] = "글로벌",
        ["GameBiz_ShortBilibili"] = "빌리빌리",
        ["GameBiz_ShortPgr"] = "중국 서버",
        ["GameBiz_PgrGlobal"] = "퍼니싱: 그레이 레이븐 · 글로벌",
        ["GameBiz_ShortGlobal"] = "글로벌",
        ["GameBiz_PgrKorea"] = "퍼니싱: 그레이 레이븐 · 한국 서버",
        ["GameBiz_ShortKorea"] = "한국 서버",
        ["GameBiz_PgrJapan"] = "퍼니싱: 그레이 레이븐 · 일본 서버",
        ["GameBiz_ShortJapan"] = "일본 서버",
        ["GameBiz_PgrTaiwan"] = "퍼니싱: 그레이 레이븐 · 대만 서버",
        ["GameBiz_ShortTaiwan"] = "대만 서버",

        
        ["GachaPool_CharacterEvent"] = "캐릭터 이벤트 소환",
        ["GachaPool_WeaponEvent"] = "무기 이벤트 소환",
        ["GachaPool_CharacterStandard"] = "캐릭터 상시 소환",
        ["GachaPool_WeaponStandard"] = "무기 상시 소환",
        ["GachaPool_Novice"] = "초심자 소환",
        ["GachaPool_NoviceChoice"] = "초심자 선택 소환",
        ["GachaPool_NoviceGratitude"] = "초심자 선택 소환 (감사 지정 소환)",
        ["GachaPool_CharacterRookie"] = "캐릭터 루키 소환",
        ["GachaPool_WeaponRookie"] = "무기 루키 소환",
        ["GachaPool_CharacterCollab"] = "캐릭터 콜라보 소환",
        ["GachaPool_WeaponCollab"] = "무기 콜라보 소환",
        ["GachaPool_CharacterRevisit"] = "캐릭터 회귀 소환",
        ["GachaPool_WeaponRevisit"] = "무기 회귀 소환",
        ["GachaPool_Unknown"] = "배너 {0}",

        
        ["Adb_DeviceOnline"] = "온라인",
        ["Adb_DeviceUnauthorized"] = "미승인",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "클라우드 동기화로 데이터를 저장합니다",
        ["Sync_ServerUrl"] = "서버 주소",
        ["Sync_UserName"] = "사용자 이름",
        ["Sync_Password"] = "비밀번호 또는 앱 인증 코드",
        ["Sync_ServerEntry"] = "동기화 서버",
        ["Sync_EnableOff"] = "꺼짐 — 켜면 자동으로 동기화됩니다",
        ["Sync_EnableNeedServer"] = "먼저 서버를 설정하세요",
        ["Sync_ConnectTitle"] = "WebDAV 연결",
        ["Sync_ConnectDesc"] = "클라우드 드라이브 또는 서버의 연결 정보를 입력하세요.",
        ["Sync_ConnectInvalid"] = "http:// 또는 https:// 로 시작하는 전체 주소를 입력하세요",
        ["Sync_SaveAndTest"] = "저장 후 테스트",
        ["Sync_Testing"] = "연결 테스트 중…",
        ["Sync_ConnectOk"] = "연결에 성공했으며 설정이 저장되었습니다",
        ["Sync_ConnectFail"] = "연결에 실패했습니다. 서버 주소, 계정, 비밀번호를 확인 후 다시 시도하세요.",
        ["Sync_SyncNow"] = "지금 동기화",
        ["Sync_RevealPassword"] = "비밀번호 표시/숨기기",
        ["Sync_Configured"] = "설정됨",
        ["Sync_NotConfigured"] = "설정되지 않음",
        ["Sync_NeverSynced"] = "아직 동기화하지 않음",
        ["Sync_LastSync"] = "마지막 동기화",
        ["Sync_Working"] = "처리 중…",

        
        ["CloudGame_ErrNoLogin"] = "사용 가능한 로그인 정보가 없습니다",
        ["CloudGame_ErrHttp"] = "클라우드 게임 서비스가 HTTP {0}을(를) 반환했습니다",
        ["CloudGame_ErrRequest"] = "클라우드 게임 서비스 요청 실패: {0}",
        ["CloudGame_ErrParse"] = "클라우드 게임 서비스가 파싱할 수 없는 내용을 반환했습니다: {0}",
        ["CloudGame_ErrEmpty"] = "클라우드 게임 서비스가 빈 내용을 반환했습니다: {0}",
        ["CloudGame_ErrCode"] = "클라우드 게임 API가 오류 code={0}을(를) 반환했습니다: {1}",

        
        ["GachaLogPage_ExportEmpty"] = "현재 계정에 내보낼 소환 기록이 없습니다",
        ["GachaLog_PityBuilt"] = "플티",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "게임 경로 설정이 제거되었습니다",
        ["GameLauncherSettingDialog_WrongServerExe"] = "선택한 게임 exe는 현재 서버에 속하지 않습니다. 다시 선택해 주세요",
        ["GameLauncherSettingDialog_ExpectExe"] = "게임 실행 파일을 선택해 주세요: {0}",

        
        ["KrSdk_ErrNoUserCache"] = "KRSDKUserCache.json을 찾을 수 없습니다. 먼저 게임 내에서 한 번 로그인하세요.",
        ["KrSdk_ErrEmptyContent"] = "내용이 비어 있습니다",
        ["KrSdk_ErrParseFailed"] = "KRSDKUserCache.json 파싱에 실패했습니다. 로그인 상태 손상을 피하기 위해 쓰기를 중단했습니다.",
        ["KrSdk_ErrNoAccountList"] = "KRSDKUserCache.json에 account_list가 없습니다.",
        ["KrSdk_ErrAccountNotInCache"] = "로컬 캐시에 이 계정이 없습니다.",
        ["KrSdk_ErrVerifyFailed"] = "쓰기 후 검증에 실패하여 변경 전 캐시로 복원했습니다.",

        
        ["GameUpdate_ErrNoPlan"] = "다운로드 계획이 없습니다",
        ["GameUpdate_ErrNoTarget"] = "대상 버전이 없습니다",
        ["GameUpdate_ErrNoGamePath"] = "게임 경로가 설정되지 않아 업데이트할 수 없습니다",
        ["GameUpdate_ErrNoManifest"] = "다운로드 목록을 가져오지 못했습니다",
        ["GameUpdate_ErrFileMissing"] = "업데이트 파일 누락: {0}",
        ["GameUpdate_ErrFileChecksum"] = "업데이트 파일 검증 실패: {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "게임의 LocalStorage.db를 찾을 수 없습니다",
        ["GameSetting_ErrNoGameUserSettings"] = "게임의 GameUserSettings.ini를 찾을 수 없습니다",

        
        ["KuroBbs_ErrConnection"] = "연결 실패, HTTP 상태: {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken이 빈 accessToken을 반환했습니다",
        ["KuroBbs_ErrAccessToken"] = "B-At accessToken을 가져오지 못했습니다 (code={0}): {1}",
        ["KuroBbs_ErrSendCode"] = "인증번호 전송에 실패했습니다",
        ["KuroBbs_ErrLogin"] = "로그인 실패",
        ["KuroBbs_SignOk"] = "출석 완료",
        ["KuroBbs_SignAlready"] = "오늘 이미 출석했습니다",
        ["KuroBbs_SignFail"] = "출석 체크에 실패했습니다",

        

        
        ["Ugf_ErrNoUid"] = "UGF 파일에 계정 uid가 없습니다",
        ["Ugf_ErrUnknownFormat"] = "인식할 수 없는 파일 형식: UGF 또는 배너 기록 JSON이 필요합니다",

        

        
        ["CombatPage_AreaFallback"] = "구역 {0}",

        
        ["KuroBbs_UnionLevel"] = "공감 레벨",
        ["KuroBbs_CommanderLevel"] = "지휘관 레벨",
        ["KuroBbs_NoLevel"] = "아직 레벨 없음",
        ["KuroBbs_SelectRolePgr"] = "퍼니싱 캐릭터 선택",
        ["KuroBbs_SelectRoleWuwa"] = "명조 캐릭터 선택",
        ["KuroBbs_SwitchedPgr"] = "퍼니싱 캐릭터가 전환되었습니다",
        ["KuroBbs_SwitchedWuwa"] = "명조 캐릭터가 전환되었습니다",
        ["PgrData_DailySupply"] = "일일 보급",
        ["KuroBbs_AutoSignDoneToast"] = "일일 출석 완료",
        ["KuroBbs_AutoSignFailedToast"] = "일일 출석 실패",

        
        ["RolePage_WeaponLevel"] = "LV.{0} · 돌파 {1}",

        
        ["Common_UnknownError"] = "알 수 없는 오류",
    };

}
