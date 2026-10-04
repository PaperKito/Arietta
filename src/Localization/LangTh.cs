using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangTh
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "ตัวเรียกเกม",
        ["LauncherPage_GameSetting"] = "ตั้งค่าเกม",
        ["MainPage_GameScreenshot"] = "ภาพหน้าจอ",
        ["GachaLogPage_ConveneRecords"] = "บันทึกการอัญเชิญ",
        ["Toolbox"] = "เครื่องมือ",

        
        ["ScreenshotPage_MultiSelect"] = "เลือกหลายรายการ",
        ["ScreenshotPage_OpenFolder"] = "เปิดโฟลเดอร์",
        ["ScreenshotPage_Total0Files"] = "ทั้งหมด {0} รูป",
        ["ScreenshotPage_Selected0Of1"] = "เลือกแล้ว {0} / {1}",
        ["ScreenshotPage_Empty"] = "ไม่พบโฟลเดอร์ภาพหน้าจอ",
        ["ScreenshotPage_EmptyDesc"] = "ภาพหน้าจอจะถูกบันทึกไว้ที่ <โฟลเดอร์เกม>\\Client\\Saved\\Screenshots โดยค่าเริ่มต้น โปรดระบุโฟลเดอร์เกมในการตั้งค่าก่อน",
        ["ScreenshotPage_Delete"] = "ลบ",
        ["ScreenshotPage_DeleteConfirm"] = "ย้ายภาพหน้าจอที่เลือก {0} รูปไปที่ถังรีไซเคิลหรือไม่?",
        ["ScreenshotPage_Deleted"] = "ย้ายภาพหน้าจอ {0} รูปไปที่ถังรีไซเคิลแล้ว",
        ["ScreenshotPage_DeleteFailed"] = "ลบไม่สำเร็จ: ไฟล์บางไฟล์อาจถูกโปรแกรมอื่นใช้งานอยู่",
        ["ScreenshotPage_SelectFirst"] = "โปรดเลือกภาพหน้าจอที่ต้องการลบก่อน",
        ["ScreenshotPage_OpenImage"] = "เปิดรูปภาพ",
        ["ScreenshotPage_DeletedPartial"] = "ลบแล้ว {0} / {1} รูป ที่เหลืออาจถูกใช้งานอยู่",

        
        ["KuroBbs"] = "Kuro BBS",
        ["KuroBbs_SignIn"] = "เช็คอิน",
        ["KuroBbs_Role"] = "ตัวละคร",
        ["KuroBbs_ResourceBrief"] = "สรุปทรัพยากร",

        
        ["PgrData_NoRole"] = "บัญชี Kuro BBS นี้ยังไม่ได้ตั้งค่าตัวละคร PGR โปรดตั้งค่าใน \"ของฉัน\" ของแอป Kuro BBS แล้วจึงตรวจสอบอีกครั้งที่นี่",
        ["PgrData_Locked"] = "ยังล็อกอยู่",
        ["PgrData_FullIn"] = "จะเต็มในอีก {0}",

        
        ["KuroBbs_DataTerminal"] = "เทอร์มินัลข้อมูล",
        ["KuroBbs_CombatData"] = "ข้อมูลการต่อสู้",
        ["DataTerminal_NoAccount"] = "ยังไม่ได้เข้าสู่ระบบ Kuro BBS แตะลูกศรบัญชีด้านบนเพื่อเข้าสู่ระบบก่อน",
        ["DataTerminal_NoRole"] = "บัญชี Kuro BBS นี้ยังไม่ได้ผูกตัวละคร{0} โปรดผูกในแอป Kuro BBS ก่อน",
        ["DataTerminal_LoadFailed"] = "โหลดหน้าเพจไม่สำเร็จ: {0}",

        
        ["KuroBbs_PgrRole"] = "ตัวละคร",
        ["KuroBbs_PgrResource"] = "รายงานทรัพยากร",
        ["KuroBbs_PgrCage"] = "กรงแห่งปวดทรมาน",
        ["KuroBbs_PgrWarArea"] = "โซนสงคราม",
        ["KuroBbs_PgrTransfinite"] = "การทำแผนที่ประวัติการต่อสู้",
        ["PgrRole_Fight"] = "พลังการต่อสู้ {0}",
        ["PgrResource_Total"] = "สะสม 6 เดือนล่าสุด",
        ["PgrResource_BlackCard"] = "การ์ดดำ",
        ["PgrResource_Develop"] = "ทรัพยากรวิจัยพัฒนา",
        ["PgrResource_Trade"] = "ตั๋วแลกเปลี่ยนการค้า",
        ["PgrResource_SelectPeriod"] = "เลือกช่วงเวลา",
        ["PgrChallenge_NotOpen"] = "ไม่ได้อยู่ในช่วงเปิดใช้งาน",
        ["PgrChallenge_Locked"] = "ยังไม่ปลดล็อก",
        ["PgrChallenge_NoArea"] = "ยังไม่ได้เลือกโซน",
        ["PgrChallenge_NoRecord"] = "ไม่มีบันทึกการท้าทายในรอบนี้",
        ["PgrChallenge_Level"] = "ช่วงเลเวล {0}",
        ["PgrWarArea_TotalPointLabel"] = "คะแนนรวม",
        ["PgrWarArea_TotalTimesLabel"] = "จำนวนครั้งที่ท้าทาย",
        ["PgrWarArea_Waves"] = "รอบ {0}",
        ["PgrCommon_LoadFailed"] = "ดึงข้อมูลไม่สำเร็จ",

        
        ["DailyBriefing"] = "สรุปประจำวัน",
        ["DailyBriefing_Desc"] = "ข้อมูลรายวันในเกม",
        ["DailyBriefing_Refresh"] = "รีเฟรช",
        ["DailyBriefing_Error"] = "ดึงข้อมูลสรุปประจำวันไม่สำเร็จ",
        ["DailyBriefing_NoGameAccount"] = "ไม่พบบัญชีที่เข้าสู่ระบบ โปรดเข้าสู่ระบบแล้วลองอีกครั้ง",
        ["DailyBriefing_NoRole"] = "ยังไม่ระบุตัวละครสำหรับบัญชีนี้ โปรดเปิดเกมหนึ่งครั้ง (ข้อมูลตัวละครถูกระบุออนไลน์ผ่าน SDK ทางการ)",
        ["Briefing_Liveness"] = "กิจกรรมรายวัน",
        ["Briefing_StoreEnergy"] = "ผลึกเดี่ยว",
        ["Briefing_BattlePass"] = "วิทยุ",
        ["Briefing_ActionPoint"] = "เซรั่ม",
        ["Briefing_Level"] = "เลเวล",
        ["KuroBbs_TowerOfAdversity"] = "หอคอยแห่งความยากลำบาก",
        ["KuroBbs_SongOfVoidSea"] = "เพลงแห่งทะเลหว่างเปล่า",
        ["KuroBbs_DoomsdayMatrix"] = "เมทริกซ์แห่งจุดจบ",
        ["KuroBbs_ComingSoon"] = "{0} อยู่ระหว่างพัฒนา",
        ["KuroBbs_TokenExpired"] = "การเข้าสู่ระบบ Kuro BBS หมดอายุแล้ว",
        ["KuroBbs_TokenExpiredDesc"] = "ไปที่ การตั้งค่า → บัญชี Kuro BBS เพื่อดักจับหรือกรอก token ใหม่",

        
        ["KuroAccount_UsbTitle"] = "เครื่องมือ ADB",
        ["KuroAccount_UsbShortDesc"] = "เชื่อมต่อมือถือ Android เพื่อดักจับ Token และ Did ของ Kuro BBS",
        ["KuroAccount_UsbDesc"] = "เชื่อมต่อโทรศัพท์ด้วยสาย USB และเปิด USB Debugging: เปิดแอป Kuro BBS ในโทรศัพท์และเข้าไปที่หน้า \"เทอร์มินัลข้อมูล\" หรือ \"เครื่องคำนวณการเลี้ยงดู\" แล้วกด \"เริ่มดักจับ\" เครื่องมือจะดักจับ token และ did ให้อัตโนมัติ ต้องมี adb.exe (ดาวน์โหลดได้จาก ",
        ["KuroAccount_UsbDescTail"] = " หน้าเว็บทางการ)",
        ["KuroAccount_UsbVerWarn"] = "⚠ Kuro BBS เวอร์ชันใหม่ไม่สามารถใช้วิธีดักจับนี้ได้ โปรดใช้เวอร์ชันเก่า",
        ["KuroAccount_AdbPlaceholder"] = "เส้นทาง adb.exe (เว้นว่างเพื่อใช้ PATH)",
        ["KuroAccount_ApplyAdb"] = "ใช้งาน",
        ["KuroAccount_SelectDevice"] = "เลือกอุปกรณ์",
        ["KuroAccount_RefreshDevices"] = "รีเฟรช",
        ["KuroAccount_StartCapture"] = "เริ่มดักจับ",
        ["KuroAccount_StopCapture"] = "หยุดดักจับ",
        ["KuroAccount_FieldsTitle"] = "ข้อมูลบัญชี",
        ["KuroAccount_FillHint"] = "ผลการดักจับจะกรอกให้อัตโนมัติ หรือวางเองก็ได้",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "บันทึก",
        ["KuroAccount_Clear"] = "ล้าง",
        ["KuroAccount_Log"] = "บันทึกการดักจับ",
        ["KuroAccount_AdbInvalidTitle"] = "เส้นทาง ADB ไม่ถูกต้อง",
        ["KuroAccount_AdbInvalidDesc"] = "กรอกเส้นทางแบบเต็มของ adb.exe ที่ถูกต้อง",
        ["KuroAccount_AdbApplied"] = "ใช้เส้นทาง ADB แล้ว",
        ["KuroAccount_AdbApplyFailed"] = "ใช้เส้นทาง ADB ไม่สำเร็จ",
        ["KuroAccount_NoDevice"] = "ไม่พบอุปกรณ์ Android",
        ["KuroAccount_NoDeviceDesc"] = "โปรดเชื่อมต่อโทรศัพท์และอนุญาต USB Debugging",
        ["KuroAccount_RefreshDevicesFailed"] = "รีเฟรชอุปกรณ์ไม่สำเร็จ",
        ["KuroAccount_SelectDeviceFirst"] = "โปรดเลือกอุปกรณ์ก่อน",
        ["KuroAccount_SelectDeviceFirstDesc"] = "กด \"รีเฟรช\" ก่อนแล้วจึงเลือกอุปกรณ์",
        ["KuroAccount_NoPage"] = "ไม่พบหน้าเว็บที่ดักจับได้",
        ["KuroAccount_NoPageDesc"] = "เข้าไปที่หน้าเว็บอย่าง \"เทอร์มินัลข้อมูล\" ในโทรศัพท์ก่อน แล้วจึงกด \"เริ่มดักจับ\"",
        ["KuroAccount_CaptureFailed"] = "เชื่อมต่อเพื่อดักจับไม่สำเร็จ",
        ["KuroAccount_CaptureStarted"] = "เริ่มการมอนิเตอร์แล้ว",
        ["KuroAccount_CaptureStartedDesc"] = "โปรดเข้าไปที่หน้า \"เทอร์มินัลข้อมูล\" หรือ \"เครื่องคำนวณการเลี้ยงดู\" ในโทรศัพท์",
        ["KuroAccount_Disconnected"] = "การเชื่อมต่อโทรศัพท์ขาดแล้ว",
        ["KuroAccount_DisconnectedDesc"] = "หน้า WebView อาจถูกปิดไปแล้ว กด \"เริ่มดักจับ\" ใหม่ได้",
        ["KuroAccount_NoSocket"] = "ไม่พบอินเทอร์เฟซดีบัก WebView",
        ["KuroAccount_NoSocketDesc"] = "Kuro BBS เวอร์ชันนี้อาจปิดการดีบักระยะไกลของ WebView ไว้ ดูรายละเอียดในรายงานวินิจฉัย",
        ["KuroAccount_DiagTitle"] = "รายงานวินิจฉัยการดักจับ",
        ["KuroAccount_DiagHint"] = "หากรายงานแสดง devtools socket เป็น 0 แสดงว่า Kuro BBS เวอร์ชันนี้ไม่เปิดการดีบักระยะไกลของ WebView อีกต่อไป (chrome://inspect ของ Chrome ก็มองไม่เห็น) จึงใช้การดักจับผ่าน ADB ไม่ได้ ให้ใช้การเข้าสู่ระบบด้วยหมายเลขโทรศัพท์ + รหัส SMS ใน \"Kuro BBS → บัญชี → เข้าสู่ระบบ\" เพื่อรับ Token และ Did แทน โดยไม่ต้องใช้โทรศัพท์และไม่ต้องใช้ ADB",
        ["KuroAccount_DiagCopy"] = "คัดลอกรายงาน",
        ["KuroAccount_DiagClose"] = "ปิด",
        ["KuroAccount_DiagCopied"] = "คัดลอกรายงานวินิจฉัยแล้ว",
        ["KuroAccount_Invalid"] = "ข้อมูลไม่ครบถ้วน",
        ["KuroAccount_SaveNeedTokenDid"] = "ต้องกรอก Token และ Did โดย roleId ค้นหาอัตโนมัติได้",
        ["KuroAccount_Saved"] = "บันทึกบัญชีแล้ว",
        ["KuroAccount_Cleared"] = "ล้างบัญชีแล้ว",

        
        ["RolePage_Refresh"] = "รีเฟรช",
        ["RolePage_NoAccount"] = "ยังไม่ได้ตั้งค่าบัญชี Kuro BBS",
        ["RolePage_NoAccountDesc"] = "ไปตั้งค่าที่ การตั้งค่า → บัญชี Kuro BBS ให้เสร็จแล้วรีเฟรช",
        ["RolePage_LoadFailed"] = "โหลดตัวละครไม่สำเร็จ",
        ["RolePage_Back"] = "กลับไปที่รายการ",
        ["RolePage_Phantoms"] = "เอคโค่",

        
        ["GachaLogPage_Refresh"] = "อัปเดตบันทึก",
        ["GachaLogPage_ImportUrl"] = "นำเข้าด้วยลิงก์",
        ["GachaLogPage_ImportUrlDesc"] = "วางลิงก์ของหน้ารายละเอียดการอัญเชิญ (เปิด \"รายละเอียดการอัญเชิญ\" ในเกมแล้วดึงจาก log หรือคัดลอกจากเครื่องมืออื่น) ผู้เล่นผ่านคลาวด์โฟนก็ใช้ได้",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "ลิงก์ไม่ถูกต้อง: วางลิงก์เต็มของหน้ารายละเอียดการอัญเชิญที่มี record_id",
        ["GachaLogPage_SelectPlayer"] = "เลือกบัญชี",
        ["GachaLogPage_CopyUrl"] = "คัดลอก URL ประวัติการสุ่ม",
        ["GachaLogPage_DeletePlayer"] = "ลบประวัติการสุ่ม",
        ["GachaLogPage_Delete"] = "ลบ",
        ["GachaLogPage_Avg5"] = "เฉลี่ย",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "สถิติ",
        ["GachaLogPage_Times"] = "ครั้ง",
        ["GachaLogPage_ItemStats"] = "สถิติไอเทม",
        ["GachaLogPage_UpdateSavedUrl"] = "อัปเดต (URL ที่บันทึกไว้)",
        ["GachaLogPage_UpdateAll"] = "อัปเดต (ทั้งหมด)",
        ["GachaLogPage_InputUrl"] = "ป้อน URL",
        ["GachaLogPage_ViaCloudGame"] = "ดึงผ่านเกมคลาวด์",
        ["CloudGame_Title"] = "Wuthering Waves Cloud - เข้าสู่ระบบแล้วดึงลิงก์อัญเชิญได้ในคลิกเดียว",
        ["CloudGame_Grab"] = "ดึงลิงก์อัญเชิญ",
        ["CloudGame_StatusDefault"] = "เข้าสู่ระบบด้วยบัญชี Kuro ที่นี่ กด \"เข้าสู่เกม\" แล้วเปิดหน้า \"รายละเอียดการอัญเชิญ\" สักครั้ง จากนั้นกด \"ดึงลิงก์อัญเชิญ\"",
        ["CloudGame_StatusSessionReady"] = "ตรวจพบสถานะล็อกอินของเกมคลาวด์แล้ว กด \"ดึงลิงก์อัญเชิญ\" เพื่อซิงก์ให้เสร็จ",
        ["CloudGame_StatusGrabbing"] = "กำลังอ่านข้อมูลการล็อกอิน…",
        ["CloudGame_StatusOk"] = "ดึงสำเร็จ (ผู้เล่น {0}) กำลังซิงก์บันทึกการอัญเชิญ…",
        ["CloudGame_StatusFailReason"] = "ดึงไม่สำเร็จ: {0}",
        ["CloudGame_StatusNoRuntime"] = "เตรียมตัวเบราว์เซอร์ในตัวไม่สำเร็จ: ยังไม่ได้ติดตั้ง WebView2 Runtime โปรดติดตั้งจากเว็บไซต์ Microsoft แล้วลองใหม่",
        ["GachaLogPage_ImportExport"] = "นำเข้า & ส่งออก",
        ["GachaLogPage_HidePools"] = "ซ่อนบนเนอร์",
        ["GachaLogPage_ExportAs"] = "ส่งออกประวัติการสุ่ม",
        ["GachaLogPage_ImportRecords"] = "นำเข้าบันทึก (เลือกไฟล์)",
        ["GachaLogPage_ImportOk"] = "นำเข้าสำเร็จ: ผู้เล่น {0} รวม {1} รายการ (รายการซ้ำถูกตัดออกอัตโนมัติ)",
        ["GachaLogPage_ImportFail"] = "นำเข้าไม่สำเร็จ: {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "ไฟล์ไม่มีข้อมูลบัญชีและไม่ได้เลือกบัญชีไว้ จึงระบุเจ้าของไม่ได้",
        ["GachaLogPage_OtherSettings"] = "การตั้งค่าอื่น ๆ",
        ["GachaLogPage_LinkHelp"] = "เจอ \"Authkey Timeout\" ตลอดเวลา?",
        ["GachaLogPage_LinkHelpDesc"] = "ลบโฟลเดอร์แคชแล้วรีสตาร์ทเกม",
        ["GachaLogPage_ClearSavedUrl"] = "ลบโฟลเดอร์แคช",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "ลบลิงก์อัญเชิญที่บันทึกไว้ในเครื่องหรือไม่? (บันทึกที่ดึงมาแล้วจะไม่ถูกลบ)",
        ["GachaLogPage_ClearSavedUrlDone"] = "ล้างลิงก์อัญเชิญที่บันทึกไว้แล้ว โปรดเข้าเกมแล้วเปิดหน้า \"รายละเอียดการอัญเชิญ\" ใหม่",
        ["GachaLogPage_Language"] = "ภาษา",
        ["GachaLogPage_LanguageSaved"] = "บันทึกภาษาแล้ว จะมีผลเมื่ออัปเดตบันทึกครั้งถัดไป",
        ["GachaLogPage_LanguageApplied"] = "เปลี่ยนชื่อไอเทมใน {0} รายการเป็น {1} แล้ว",
        ["GachaLogPage_NoSavedUrl"] = "ไม่มีลิงก์ที่บันทึกไว้ โปรดรัน \"อัปเดต (ทั้งหมด)\" ก่อน",
        ["GachaLogPage_LinkExpired"] = "ลิงก์อัญเชิญหมดอายุแล้ว",
        ["GachaLogPage_LinkExpiredDesc"] = "ลิงก์หมดอายุแล้ว (API ตอบ {0}) โปรดเปิด \"รายละเอียดการอัญเชิญ\" ในเกมใหม่",
        ["GachaLogPage_UpdateFailed"] = "อัปเดตบันทึกการอัญเชิญไม่สำเร็จ: {0}",
        ["GachaLogPage_FetchFailed"] = "ดึงข้อมูลการอัญเชิญไม่สำเร็จ โปรดลองอีกครั้ง",
        ["GachaLogPage_LogNotFound"] = "ไม่พบ log เกมหรือลิงก์อัญเชิญ: โปรดระบุเกม (Wuthering Waves.exe) ในการตั้งค่าและเปิดหน้า \"รายละเอียดการอัญเชิญ\" ในเกมสักครั้ง หรือเลือก \"นำเข้าด้วยลิงก์\" จากเมนู \"อัปเดตบันทึก\"",
        ["GachaLogPage_UrlNotFound"] = "ไม่พบลิงก์อัญเชิญ โปรดเปิดหน้า \"รายละเอียดการอัญเชิญ\" ในเกมสักครั้ง",
        ["GachaLogPage_UrlCopied"] = "คัดลอกลิงก์อัญเชิญไปยังคลิปบอร์ดแล้ว",
        ["GachaLogPage_PlayerDeleted"] = "ลบข้อมูลการอัญเชิญในเครื่องของ UID {0} แล้ว",
        ["GachaLogPage_DeleteConfirmText"] = "ข้อมูลการอัญเชิญในเครื่องทั้งหมดของ UID {0} จะถูกลบ และไม่สามารถกู้คืนได้",
        ["GachaLogPage_EmptyTitle"] = "ยังไม่มีบันทึกการอัญเชิญ",
        ["GachaLogPage_EmptyDesc"] = "ระบุโฟลเดอร์เกมในการตั้งค่าก่อน จากนั้นเข้าเกมเปิดหน้า \"รายละเอียดการอัญเชิญ\" สักครั้ง แล้วกลับมาที่นี่กด \"อัปเดตบันทึก\" เพื่อซิงก์ข้อมูล",

        
        ["LauncherPage_StartGame"] = "เริ่มเกม",
        ["LauncherPage_InstallGame"] = "ติดตั้งเกม",
        ["LauncherPage_ResumeInstall"] = "ติดตั้งต่อ",
        ["AccountSwitcher_SwitchAccount"] = "สลับบัญชี",
        ["AccountSwitcher_GameRunning"] = "เกมกำลังทำงาน จึงสลับบัญชีไม่ได้ชั่วคราว",
        ["AccountSwitcher_AccountInfo"] = "บัญชีเกม",
        ["AccountSwitcher_Nickname"] = "ชื่อเล่น",
        ["AccountSwitcher_RoleId"] = "UID เกม",
        ["AccountSwitcher_Save"] = "บันทึก",
        ["AccountSwitcher_Delete"] = "ลบ",
        ["AccountSwitcher_Unsaved"] = "บัญชีที่ล็อกอินอยู่ยังไม่ได้บันทึก",
        ["Notice_Activity"] = "กิจกรรม",
        ["Notice_NoticeTab"] = "ประกาศ",
        ["Notice_News"] = "ข่าวสาร",
        ["Notice_LoadFailed"] = "โหลดประกาศไม่สำเร็จ",
        ["LauncherPage_UpdateGame"] = "อัปเดตเกม",

        
        ["SettingPage_Language"] = "ภาษา",

        
        ["GameUpdate_PredownloadAvailable"] = "เวอร์ชันใหม่ {0} เปิดพรีโหลดแล้ว",
        ["GameUpdate_Predownloaded"] = "พรีโหลด {0} แล้ว เมื่อเปิดอัปเดตจะติดตั้งได้ทันที",
        ["GameUpdate_PredownloadButton"] = "พรีโหลด",
        ["GameUpdate_ConfirmPredownload"] = "พรีโหลดเวอร์ชัน {0} หรือไม่? ต้องดาวน์โหลดทั้งหมด {1} ไฟล์จะถูกเก็บไว้ในโฟลเดอร์ predownload ในโฟลเดอร์เกม เมื่อทางการเปิดอัปเดตก็ไม่ต้องดาวน์โหลดซ้ำ",
        ["GameUpdate_Applying"] = "กำลังแตกไฟล์อัปเดต…",
        ["GameUpdate_Pausing"] = "กำลังหยุดชั่วคราว…",
        ["GameUpdate_Predownloading"] = "กำลังดาวน์โหลดล่วงหน้า {0}…",

        ["GameUpdate_Done"] = "อัปเดตเป็น {0} แล้ว",
        ["GameUpdate_PredownloadDone"] = "พรีโหลด {0} เสร็จแล้ว เมื่อเปิดอัปเดตจะติดตั้งได้ทันที",
        ["GameUpdate_NoGameDir"] = "โปรดระบุเกมในการตั้งค่าก่อน",
        ["GameUpdate_Verifying"] = "กำลังตรวจสอบ {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "ตรวจสอบเสร็จแล้ว",
        ["GameUpdate_VerifyDoneNone"] = "ไม่พบไฟล์เสียหายหรือสูญหาย",
        ["GameUpdate_VerifyDoneDamage"] = "พบไฟล์เสียหาย / สูญหาย {0} ไฟล์ จากทั้งหมด {1}",
        ["GameUpdate_VerifyNoManifest"] = "ไม่สามารถดึงรายการตรวจสอบได้ โปรดตรวจเครือข่ายหรือลองใหม่ภายหลัง",
        ["GameUpdate_Repairing"] = "กำลังซ่อมแซม…",
        ["GameUpdate_RepairDone"] = "ซ่อมแล้ว {0} ไฟล์",
        ["GameUpdate_RepairNoBaseUrl"] = "เวอร์ชันปัจจุบันไม่รองรับการซ่อมแซมออนไลน์ โปรดอัปเดตเกมเป็นเวอร์ชันล่าสุดก่อนแล้วลองใหม่",

        
        ["GameLauncherPage_Gallery"] = "แกลเลอรี",
        ["Common_Copy"] = "คัดลอก",
        ["ImageViewWindow2_SaveAs"] = "บันทึกเป็น",
        ["GameLauncherPage_PlayPause"] = "เล่น/พัก",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "ลากไฟล์รูปภาพมาวางเพื่อเปลี่ยนพื้นหลัง",
        ["Background_Official"] = "พื้นหลังแอนิเมชันอย่างเป็นทางการ",
        ["Background_OfficialStatic"] = "พื้นหลังนิ่งอย่างเป็นทางการ",
        ["Background_OpenFolder"] = "เปิดโฟลเดอร์พื้นหลัง",
        ["Background_AddFailed"] = "ใช้ไฟล์พื้นหลังนี้ไม่ได้",
        ["InstallGameDialog_LocateGame"] = "ระบุตำแหน่งเกม",
        ["GameLauncherPage_GameIsRunning"] = "เกมกำลังทำงานอยู่",

        
        ["SettingPage_Download"] = "ดาวน์โหลด",

        
        ["DownloadGamePage_Downloading"] = "กำลังดาวน์โหลด",
        ["DownloadGamePage_Verifying"] = "กำลังตรวจสอบ",
        ["DownloadGamePage_Pause"] = "หยุดชั่วคราว",

        
        ["Common_Confirm"] = "ยืนยัน",
        ["Common_Cancel"] = "ยกเลิก",
        ["Common_Error"] = "ข้อผิดพลาด",
        ["Common_Ok"] = "ตกลง",
        ["Common_Select"] = "เลือก",
        ["Common_Apply"] = "ใช้งาน",
        ["Common_Exit"] = "ออก",

        
        ["SettingPage_AppSettings"] = "การตั้งค่า",
        ["SettingPage_About"] = "เกี่ยวกับ",
        ["SettingPage_General"] = "ทั่วไป",
        ["SettingPage_FileManagement"] = "จัดการไฟล์",

        
        ["FilePage_DataFolder"] = "โฟลเดอร์ข้อมูล",
        ["FilePage_DataFolderDesc"] = "ข้อมูลส่วนบุคคลของคุณถูกเก็บไว้ในโฟลเดอร์ด้านล่าง ซึ่งสำคัญมาก โปรดอย่าลบโดยพลการ",
        ["FilePage_ReselectDataFolder"] = "เลือกโฟลเดอร์ข้อมูลใหม่",
        ["FilePage_OpenDataFolder"] = "เปิดโฟลเดอร์ข้อมูล",
        ["FilePage_DeleteAllSettings"] = "ลบการตั้งค่าทั้งหมด",
        ["FilePage_ReselectFolderDone"] = "บันทึกโฟลเดอร์ข้อมูลใหม่แล้ว จะมีผลหลังรีสตาร์ทแอป โฟลเดอร์ใหม่จะว่างตอนแรก ใช้ \"ซิงก์คลาวด์\" เพื่อดึงข้อมูลเดิมกลับมาได้",
        ["FilePage_DeleteSettingsDone"] = "ลบการตั้งค่าแล้ว จะมีผลหลังรีสตาร์ทแอป",
        ["FilePage_DeleteSettingsConfirm"] = "จะลบการตั้งค่าทั้งหมด (config.ini) และกลับสู่ค่าเริ่มต้นหลังรีสตาร์ท ข้อมูลในฐานข้อมูลอย่างเวลาเล่นและบันทึกการอัญเชิญจะไม่ได้รับผลกระทบ ดำเนินการต่อหรือไม่?",
        ["FilePage_OpenLogFolder"] = "เปิดโฟลเดอร์ log",
        ["FilePage_Cache"] = "แคช",
        ["FilePage_CacheLog"] = "log",
        ["FilePage_CacheImage"] = "รูปภาพ",
        ["FilePage_CacheBackground"] = "พื้นหลังของตัวเรียกเกม",
        ["FilePage_CleanCache"] = "ล้างแคช",
        ["FilePage_CleanBackgroundCache"] = "ล้างไฟล์พื้นหลังของตัวเรียกเกม",

        
        ["FilePage_HardLink"] = "Hard Link",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "ไฟล์ที่เหมือนกันของไคลเอ็นต์ภูมิภาคอื่นในดิสก์เดียวกันจะใช้ฮาร์ดลิงก์ซ้ำ คาดดาวน์โหลดประมาณ {0} เป็นเพียงค่าประมาณ ค่าจริงขึ้นกับการตรวจสอบแต่ละไฟล์ตอนติดตั้ง",
        ["InstallGameDialog_HardLinkAfterSize"] = "ขนาดหลังฮาร์ดลิงก์: ",
        ["InstallGameDialog_EstimateOnly"] = "ค่านี้เป็นเพียงค่าประมาณการเท่านั้น",
        ["InstallGameDialog_SpaceDesc"] = "ขนาดของแพ็กเกจบีบอัด พื้นที่ที่ใช้จริงหลังแตกไฟล์จะมากกว่าค่านี้",
["FilePage_HardLinkDesc"] = "เมื่อติดตั้งหลายไคลเอ็นต์ในดิสก์เดียวกัน ไฟล์ที่เหมือนกันจะใช้ฮาร์ดลิงก์เพื่อลดพื้นที่จัดเก็บ",
        ["FilePage_Enabled"] = "เปิดอยู่",
        ["FilePage_Disabled"] = "ปิดอยู่",
        ["FilePage_SpeedLimit"] = "จำกัดความเร็ว",
        ["FilePage_SpeedLimitDesc"] = "0 หมายถึงไม่จำกัดความเร็ว",
        ["FilePage_DownloadThreads"] = "มัลติเธรด",
        ["FilePage_DownloadThreadsDesc"] = "จำนวนการเชื่อมต่อดาวน์โหลดพร้อมกัน ขนานมากเกินไปจะทำให้ HDD ช้าลงเพราะการ seek การเปลี่ยนแปลงมีผลตั้งแต่ครั้งถัดไป",

        
        ["KuroBbs_RefreshInfo"] = "รีเฟรชข้อมูล",
        ["KuroBbs_Login"] = "เข้าสู่ระบบ",
        ["KuroBbs_Settings"] = "การตั้งค่า Kuro BBS",

        
        ["Login_Title"] = "เข้าสู่ระบบ Kuro BBS",
        ["Login_ByToken"] = "เข้าสู่ระบบด้วย Token",
        ["Login_BySms"] = "เข้าสู่ระบบด้วยรหัส SMS",
        ["Login_TokenPlaceholder"] = "วาง Token การเข้าสู่ระบบของ Kuro BBS",
        ["Login_DidPlaceholder"] = "Did ที่ดักจับมาพร้อมกับ Token (เว้นว่างเพื่อสร้างอัตโนมัติ)",
        ["Login_TokenTip"] = "Token และ Did ดักจับจากแอป Kuro BBS บนมือถือ (เครื่องมือ → ดักจับผ่าน USB) และจะบันทึกเป็นคู่",
        ["Login_PhonePlaceholder"] = "หมายเลขโทรศัพท์",
        ["Login_CodePlaceholder"] = "รหัส SMS",
        ["Login_CodeLabel"] = "รหัส SMS",
        ["Login_SmsTip"] = "กด \"ส่งรหัส\" แล้วกรอกรหัส SMS ที่ได้รับ หากเข้าสู่ระบบสำเร็จจะบันทึกบัญชีให้อัตโนมัติ",
        ["Login_SendCode"] = "ส่งรหัส",
        ["Login_Confirm"] = "เข้าสู่ระบบ",
        ["Login_Cancel"] = "ยกเลิก",
        ["Login_PhoneInvalid"] = "รูปแบบหมายเลขโทรศัพท์ไม่ถูกต้อง",
        ["Login_Sending"] = "กำลังส่งรหัส SMS…",
        ["Login_CodeSent"] = "ส่งรหัสแล้ว โปรดตรวจสอบ SMS",
        ["Login_CodeSendFailed"] = "ส่งรหัสไม่สำเร็จ: {0}",
        ["Login_NeedCode"] = "กรอกรหัส SMS",
        ["Login_TokenInvalid"] = "Token ไม่ถูกต้อง: โปรดวาง Token การเข้าสู่ระบบของ Kuro BBS แบบเต็ม",
        ["Login_Working"] = "กำลังเข้าสู่ระบบ…",
        ["Login_Failed"] = "เข้าสู่ระบบไม่สำเร็จ: {0}",
        ["Login_SeekRoleFailed"] = "ค้นหาตัวละครที่ผูกไว้ไม่สำเร็จ: {0}",
        ["Login_Success"] = "เข้าสู่ระบบบัญชี Kuro BBS \"{0}\" สำเร็จ",

        
        ["ResourceBrief_Month"] = "รายเดือน",
        ["ResourceBrief_Week"] = "รายสัปดาห์",
        ["ResourceBrief_Version"] = "เวอร์ชัน",
        ["ResourceBrief_HistoryData"] = "ประวัติย้อนหลัง",
        ["ResourceBrief_TotalGain"] = "รับได้ในรอบนี้",
        ["ResourceBrief_StarComposition"] = "องค์ประกอบรายได้ Astrite",
        ["ResourceBrief_CoinComposition"] = "องค์ประกอบรายได้เชลล์เครดิต",
        ["ResourceBrief_Detail"] = "รายละเอียดการได้รับ",
        ["ResourceBrief_Empty"] = "ไม่มีข้อมูลในช่วงนี้",

        
        ["SignPage_DoSignIn"] = "เช็คอิน",
        ["SignPage_MonthlySign"] = "เช็คอินเดือนนี้",
        ["SignPage_SignedToday"] = "เช็คอินวันนี้แล้ว",
        ["SignPage_NotSignedToday"] = "ยังไม่ได้เช็คอินวันนี้",
        ["SignPage_Done"] = "เช็คอินเสร็จแล้ว",
        ["SignPage_AutoSign"] = "เช็คอินอัตโนมัติ",
        ["SignPage_SignedDaysFormat"] = "เช็คอินแล้ว {0} วันในเดือนนี้",
        ["SignPage_DayFormat"] = "วันที่ {0}",
        ["SignPage_Energy"] = "เวฟเพลต",
        ["TowerPage_TotalStarCount"] = "ดาวรวม: {0} / {1}",
        ["TowerPage_Floor"] = "ชั้นที่ {0}",
        ["TowerPage_NoChallengeData"] = "ไม่มีข้อมูลการท้าทาย",
        ["TowerPage_RefreshAfter"] = "รีเฟรชอีก {0} วัน {1} ชั่วโมง",
        ["MatrixPage_NoRecord"] = "ไม่มีบันทึกในรอบนี้",
        ["CombatPage_Locked"] = "{0} ยังไม่ปลดล็อกในรอบนี้หรือไม่มีข้อมูล",
        ["CombatPage_NoData"] = "ไม่มีข้อมูล: เซิร์ฟเวอร์ยังไม่สร้างบันทึกของรอบนี้ โปรดเปิดหน้า \"{0}\" ในแอป Kuro BBS เพื่อรีเฟรชข้อมูลแล้วลองใหม่",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "เวลาเล่นทั้งหมด",
        ["PlayTimeStatsDialog_Started0Times"] = "เปิดเกม {0} ครั้ง",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "เวลาเล่นเฉลี่ยต่อวัน",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "เล่น {0} วัน",
        ["PlayTimeStatsDialog_LongestStreak"] = "เล่นต่อเนื่องนานที่สุด",
        ["PlayTimeStatsDialog_0Days"] = "{0} วัน",
        ["PlayTimeStatsDialog_LongestSession"] = "เซสชันที่ยาวนานที่สุด",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "เล่นวันเดียวนานที่สุด",
        ["PlayTimeButton_LastStartup"] = "เล่นครั้งล่าสุด",
        ["PlayTimeStatsDialog_Last15Days"] = "15 วันล่าสุด",
        ["PlayTimeStatsDialog_Last12Weeks"] = "12 สัปดาห์ล่าสุด",
        ["PlayTimeStatsDialog_Last12Months"] = "12 เดือนล่าสุด",
        ["PlayTimeStatsDialog_Total"] = "รวม",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "สถิติเวลาเล่น",
        ["PlayTimeStatsDialog_StatsTip"] = "สถิตินับเฉพาะเวลาเล่นที่เริ่มเกมผ่านตัวเรียกเกมนี้เท่านั้น",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "ย่อไปที่ถาดระบบ",
        ["GameLauncherSettingDialog_BasicInformation"] = "ข้อมูลพื้นฐาน",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "ยืนยันการถอนการติดตั้ง",
        ["GameLauncherSettingDialog_CustomBackground"] = "พื้นหลัง",
        ["GameLauncherSettingDialog_RelocateGame"] = "ระบุตำแหน่งเกมใหม่",
        ["GameLauncherSettingDialog_Size"] = "ขนาด",
        ["GameLauncherSettingDialog_StartRepairing"] = "เริ่มซ่อมแซม",
        ["GameLauncherSettingDialog_StartupArguments"] = "อาร์กิวเมนต์การเริ่มต้น",
        ["GameLauncherSettingDialog_CommandLineArgument"] = "อาร์กิวเมนต์บรรทัดคำสั่ง",
        ["GameLauncherSettingDialog_CustomProgramTip"] = "หากเกมของเซิร์ฟเวอร์นี้ยังไม่ได้ตั้งค่าหรือไม่พบ exe การคลิก «เริ่มเกม» จะเป็นการเรียกใช้โปรแกรมนี้แทน จะไม่มีผลเมื่อตัวเกมพร้อมใช้งาน",
        ["LauncherPage_CustomStartExecutable"] = "เรียกใช้โปรแกรมที่กำหนดเอง",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "คำเตือนการถอนการติดตั้งเกม",
        ["LauncherPage_RepairGame"] = "ซ่อมแซมเกม",
        ["LauncherPage_ShowGameAnnouncement"] = "แสดงประกาศเกม",
        ["LauncherPage_UninstallGame"] = "ถอนการติดตั้งเกม",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "ไม่รองรับไดรฟ์เครือข่ายแบบแมป โปรดใช้เส้นทางแชร์เครือข่ายที่ขึ้นต้นด้วยแบ็กสแลชสองตัว",

        
        ["SettingPage_BgSizeTip"] = "คำแนะนำขนาดภาพพื้นหลัง",
        ["SettingPage_CustomBackgroundSupportVideo"] = "พื้นหลังแบบกำหนดเองรองรับวิดีโอ",

        
        ["GameSettingPage_Resolution"] = "ความละเอียด",
        ["GameSettingPage_FullScreen"] = "เต็มหน้าจอ",
        ["GameSettingPage_UsePopupWindow"] = "หน้าต่างไร้ขอบ",
        ["GameSettingPage_CustomResolution"] = "ความละเอียดแบบกำหนดเอง",
        ["GameSettingPage_CustomResolutionTip"] = "ความละเอียดแบบกำหนดเองมีผลในโหมดแสดงผล \"หน้าต่าง\"",
        ["GameSettingPage_VoiceLanguage"] = "ภาษาเสียงพากย์",
        ["GameSettingPage_VoiceLanguageDescription"] = "ตั้งค่านี้ก่อนเปิดเกมครั้งแรกจะช่วยลดการดาวน์โหลดไฟล์เสียงบางส่วนได้",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "เปิดใช้ HDR",
        ["GameSettingPage_HDRNotSupported"] = "จอปัจจุบันไม่รองรับ HDR",
        ["GameSettingPage_HDRNotEnabled"] = "Windows HDR ยังไม่ได้เปิด อาจไม่มีผลแม้เปิดใช้",
        ["GameSettingPage_GameNotInstalled"] = "ยังไม่ได้ติดตั้งเกม",
        ["GameSettingPage_GameNotInstalledDesc"] = "โปรดระบุเส้นทางเกมในหน้าตัวเรียกเกมก่อน",
        ["GameSettingPage_CloudGameNotSupport"] = "เกมคลาวด์ไม่ต้องตั้งค่าเกมในเครื่อง",
        ["GameSettingPage_SettingNotEffect"] = "การตั้งค่าที่ใช้ขณะเกมกำลังทำงานจะไม่มีผล",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "ความละเอียดต้องมากกว่า 0",
        ["GameSettingPage_LoadFailed"] = "อ่านการตั้งค่าเกมไม่สำเร็จ",
        ["GameSettingPage_ApplyFailed"] = "ใช้การตั้งค่าเกมไม่สำเร็จ",
        ["GameSettingPage_Applied"] = "ใช้แล้ว จะมีผลเมื่อเปิดเกมครั้งถัดไป",

        

        
        ["SettingPage_WindowBehavior"] = "พฤติกรรมหน้าต่าง",
        ["SettingPage_CloseWindowOption"] = "ตัวเลือกการปิดหน้าต่าง",
        ["SettingPage_ExitApp"] = "ออกจากแอปโดยสมบูรณ์",
        ["SettingPage_AfterLaunchOption"] = "หลังเปิดเกม",
        ["SettingPage_KeepWindow"] = "คงหน้าต่างไว้",
        ["SettingPage_AfterGameExitOption"] = "หลังปิดเกม",
        ["SettingPage_RestoreMainWindow"] = "แสดงหน้าต่างหลัก",
        ["SettingPage_KeepHidden"] = "ซ่อนไว้",
        ["SettingPage_CloseLauncher"] = "ปิด Launcher",
        ["SettingPage_KuroCommunitySign"] = "เช็คอินชุมชน Kuro BBS (เหรียญ Kuro)",
        ["SettingPage_SuggestSystemFeatures"] = "เพื่อประสบการณ์ที่ดีขึ้น แนะนำให้เปิดฟีเจอร์ต่อไปนี้ในการตั้งค่าระบบ",
        ["SettingPage_TransparencyEffects"] = "เอฟเฟกต์ความโปร่งใส",
        ["SettingPage_AnimationEffects"] = "เอฟเฟกต์แอนิเมชัน",

        
        ["GameSelector_Subtitle"] = "แตะไอคอนมุมซ้ายบนเพื่อเลือกหรือสลับเกม",
        ["GameSelector_AutoFind"] = "ค้นหาเกมในเครื่องอัตโนมัติ",
        ["GameSelector_SelectServer"] = "เลือกเซิร์ฟเวอร์",
        ["GameSelector_InstalledSize"] = "พื้นที่จริงที่เกมที่ติดตั้งใช้งาน ",
        ["GameSelector_Calculating"] = "กำลังคำนวณ",

        
        ["KuroBbs_AccountLevelLabel"] = "เลเวลยูเนียน ",
        ["KuroBbs_SwitchRole"] = "สลับตัวละคร",
        ["KuroBbs_ClearAccount"] = "ล้างบัญชี",

        
        ["GameBiz_Wuwa"] = "Wuthering Waves",
        ["GameBiz_WuwaCn"] = "Wuthering Waves · CN",
        ["GameBiz_WuwaGlobal"] = "Wuthering Waves · Global",
        ["GameBiz_WuwaBilibili"] = "Wuthering Waves · Bilibili",
        ["GameBiz_Pgr"] = "Punishing: Gray Raven",
        ["GameBiz_ShortCn"] = "เซิร์ฟเวอร์จีน",
        ["GameBiz_ShortGlobal"] = "โกลบอล",
        ["GameBiz_ShortBilibili"] = "Bilibili",
        ["GameBiz_ShortPgr"] = "เซิร์ฟเวอร์จีน",
        ["GameBiz_PgrGlobal"] = "Punishing: Gray Raven · Global",
        ["GameBiz_ShortGlobal"] = "โกลบอล",
        ["GameBiz_PgrKorea"] = "Punishing: Gray Raven · Korea",
        ["GameBiz_ShortKorea"] = "เซิร์ฟเวอร์เกาหลี",
        ["GameBiz_PgrJapan"] = "Punishing: Gray Raven · Japan",
        ["GameBiz_ShortJapan"] = "เซิร์ฟเวอร์ญี่ปุ่น",
        ["GameBiz_PgrTaiwan"] = "Punishing: Gray Raven · Taiwan",
        ["GameBiz_ShortTaiwan"] = "เซิร์ฟเวอร์ไต้หวัน",

        
        ["GachaPool_CharacterEvent"] = "อัญเชิญเรโซเนเตอร์ลิมิเต็ด",
        ["GachaPool_WeaponEvent"] = "อัญเชิญอาวุธลิมิเต็ด",
        ["GachaPool_CharacterStandard"] = "อัญเชิญตัวละครประจำ",
        ["GachaPool_WeaponStandard"] = "อัญเชิญอาวุธประจำ",
        ["GachaPool_Novice"] = "อัญเชิญมือใหม่",
        ["GachaPool_NoviceChoice"] = "อัญเชิญมือใหม่เลือกเอง",
        ["GachaPool_NoviceGratitude"] = "อัญเชิญมือใหม่เลือกเอง (อัญเชิญขอบคุณระบุตัวเลือก)",
        ["GachaPool_CharacterRookie"] = "อัญเชิญตัวละครมือใหม่",
        ["GachaPool_WeaponRookie"] = "อัญเชิญอาวุธมือใหม่",
        ["GachaPool_CharacterCollab"] = "อัญเชิญตัวละครคอลแล็บ",
        ["GachaPool_WeaponCollab"] = "อัญเชิญอาวุธคอลแล็บ",
        ["GachaPool_CharacterRevisit"] = "อัญเชิญตัวละครหวนคืน",
        ["GachaPool_WeaponRevisit"] = "อัญเชิญอาวุธหวนคืน",
        ["GachaPool_Unknown"] = "แบนเนอร์ {0}",

        
        ["Adb_DeviceOnline"] = "ออนไลน์",
        ["Adb_DeviceUnauthorized"] = "ยังไม่ได้รับอนุญาต",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "บันทึกข้อมูลด้วยการซิงก์ผ่านคลาวด์",
        ["Sync_ServerUrl"] = "ที่อยู่เซิร์ฟเวอร์",
        ["Sync_UserName"] = "ชื่อผู้ใช้",
        ["Sync_Password"] = "รหัสผ่านหรือรหัสอนุญาตของแอป",
        ["Sync_ServerEntry"] = "เซิร์ฟเวอร์ซิงค์",
        ["Sync_EnableOff"] = "ปิดอยู่ — เปิดเพื่อซิงค์อัตโนมัติ",
        ["Sync_EnableNeedServer"] = "โปรดตั้งค่าเซิร์ฟเวอร์ก่อน",
        ["Sync_ConnectTitle"] = "เชื่อมต่อ WebDAV",
        ["Sync_ConnectDesc"] = "กรอกข้อมูลการเชื่อมต่อจากคลาวด์ไดรฟ์หรือเซิร์ฟเวอร์ของคุณ",
        ["Sync_ConnectInvalid"] = "กรอกที่อยู่เต็มที่ขึ้นต้นด้วย http:// หรือ https://",
        ["Sync_SaveAndTest"] = "บันทึกและทดสอบ",
        ["Sync_Testing"] = "กำลังทดสอบการเชื่อมต่อ…",
        ["Sync_ConnectOk"] = "เชื่อมต่อสำเร็จ บันทึกการตั้งค่าแล้ว",
        ["Sync_ConnectFail"] = "เชื่อมต่อไม่สำเร็จ โปรดตรวจสอบที่อยู่เซิร์ฟเวอร์ บัญชี และรหัสผ่านแล้วลองอีกครั้ง",
        ["Sync_SyncNow"] = "ซิงค์เดี๋ยวนี้",
        ["Sync_RevealPassword"] = "แสดงหรือซ่อนรหัสผ่าน",
        ["Sync_Configured"] = "ตั้งค่าแล้ว",
        ["Sync_NotConfigured"] = "ยังไม่ได้ตั้งค่า",
        ["Sync_NeverSynced"] = "ยังไม่เคยซิงก์",
        ["Sync_LastSync"] = "ซิงก์ล่าสุด",
        ["Sync_Working"] = "กำลังดำเนินการ…",

        
        ["CloudGame_ErrNoLogin"] = "ไม่มีข้อมูลการเข้าสู่ระบบที่ใช้ได้",
        ["CloudGame_ErrHttp"] = "บริการเกมคลาวด์ตอบกลับ HTTP {0}",
        ["CloudGame_ErrRequest"] = "ขอบริการเกมคลาวด์ไม่สำเร็จ: {0}",
        ["CloudGame_ErrParse"] = "บริการเกมคลาวด์ตอบกลับเนื้อหาที่อ่านไม่ได้: {0}",
        ["CloudGame_ErrEmpty"] = "บริการเกมคลาวด์ตอบกลับว่างเปล่า: {0}",
        ["CloudGame_ErrCode"] = "อินเทอร์เฟซเกมคลาวด์คืนค่าข้อผิดพลาด code={0}: {1}",

        
        ["GachaLogPage_ExportEmpty"] = "บัญชีปัจจุบันไม่มีบันทึกการอัญเชิญให้ส่งออก",
        ["GachaLog_PityBuilt"] = "Pity",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "ลบการตั้งค่าเส้นทางเกมออกแล้ว",
        ["GameLauncherSettingDialog_WrongServerExe"] = "ไฟล์ exe ของเกมที่เลือกไม่ได้เป็นของเซิร์ฟเวอร์ปัจจุบัน โปรดเลือกใหม่",
        ["GameLauncherSettingDialog_ExpectExe"] = "โปรดเลือกไฟล์ exe หลักของเกม: {0}",

        
        ["KrSdk_ErrNoUserCache"] = "ไม่พบ KRSDKUserCache.json โปรดเข้าสู่ระบบในเกมสักครั้งก่อน",
        ["KrSdk_ErrEmptyContent"] = "เนื้อหาว่างเปล่า",
        ["KrSdk_ErrParseFailed"] = "แยกวิเคราะห์ KRSDKUserCache.json ไม่สำเร็จ จึงยกเลิกการเขียนเพื่อไม่ทำลายสถานะการเข้าสู่ระบบ",
        ["KrSdk_ErrNoAccountList"] = "ไม่มี account_list ใน KRSDKUserCache.json",
        ["KrSdk_ErrAccountNotInCache"] = "ไม่มีบัญชีนี้ในแคชในเครื่อง",
        ["KrSdk_ErrVerifyFailed"] = "ตรวจสอบหลังเขียนไม่ผ่าน จึงคืนค่าแคชกลับสู่สถานะก่อนแก้ไขแล้ว",

        
        ["GameUpdate_ErrNoPlan"] = "ขาดแผนการดาวน์โหลด",
        ["GameUpdate_ErrNoTarget"] = "ขาดเวอร์ชันเป้าหมาย",
        ["GameUpdate_ErrNoGamePath"] = "ยังไม่ได้ตั้งค่าเส้นทางเกม จึงอัปเดตไม่ได้",
        ["GameUpdate_ErrNoManifest"] = "ดึงรายการดาวน์โหลดไม่สำเร็จ",
        ["GameUpdate_ErrFileMissing"] = "ไฟล์อัปเดตสูญหาย: {0}",
        ["GameUpdate_ErrFileChecksum"] = "ตรวจสอบไฟล์อัปเดตไม่ผ่าน: {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "ไม่พบ LocalStorage.db ของเกม",
        ["GameSetting_ErrNoGameUserSettings"] = "ไม่พบ GameUserSettings.ini ของเกม",

        
        ["KuroBbs_ErrConnection"] = "เชื่อมต่อไม่สำเร็จ รหัสสถานะ HTTP: {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken คืน accessToken ว่างเปล่า",
        ["KuroBbs_ErrAccessToken"] = "ดึง B-At accessToken ไม่สำเร็จ (code={0}): {1}",
        ["KuroBbs_ErrSendCode"] = "ส่งรหัส SMS ไม่สำเร็จ",
        ["KuroBbs_ErrLogin"] = "เข้าสู่ระบบไม่สำเร็จ",
        ["KuroBbs_SignOk"] = "เช็คอินสำเร็จ",
        ["KuroBbs_SignAlready"] = "วันนี้เช็คอินแล้ว",
        ["KuroBbs_SignFail"] = "เช็คอินไม่สำเร็จ",

        

        
        ["Ugf_ErrNoUid"] = "ไฟล์ UGF ขาด uid ของบัญชี",
        ["Ugf_ErrUnknownFormat"] = "ไม่รู้จักรูปแบบไฟล์: ต้องเป็น UGF หรือ JSON บันทึกแบนเนอร์",

        

        
        ["CombatPage_AreaFallback"] = "พื้นที่ {0}",

        
        ["KuroBbs_UnionLevel"] = "เลเวลยูเนียน",
        ["KuroBbs_CommanderLevel"] = "เลเวลผู้บัญชาการ",
        ["KuroBbs_NoLevel"] = "ยังไม่มีเลเวล",
        ["KuroBbs_SelectRolePgr"] = "เลือกตัวละคร PGR",
        ["KuroBbs_SelectRoleWuwa"] = "เลือกตัวละคร Wuthering Waves",
        ["KuroBbs_SwitchedPgr"] = "สลับตัวละคร PGR แล้ว",
        ["KuroBbs_SwitchedWuwa"] = "สลับตัวละคร Wuthering Waves แล้ว",
        ["PgrData_DailySupply"] = "เสบียงประจำวัน",
        ["KuroBbs_AutoSignDoneToast"] = "เช็คอินรายวันเสร็จสิ้น",
        ["KuroBbs_AutoSignFailedToast"] = "เช็คอินรายวันไม่สำเร็จ",

        
        ["RolePage_WeaponLevel"] = "LV.{0} · อัปขีดจำกัด {1}",

        
        ["Common_UnknownError"] = "ข้อผิดพลาดที่ไม่รู้จัก",
    };

}
