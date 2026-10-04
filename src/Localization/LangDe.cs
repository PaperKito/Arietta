using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangDe
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "Launcher",
        ["LauncherPage_GameSetting"] = "Spieleinstellungen",
        ["MainPage_GameScreenshot"] = "Screenshots",
        ["GachaLogPage_ConveneRecords"] = "Einberufungsverlauf",
        ["Toolbox"] = "Werkzeuge",

        
        ["ScreenshotPage_MultiSelect"] = "Mehrfachauswahl",
        ["ScreenshotPage_OpenFolder"] = "Ordner öffnen",
        ["ScreenshotPage_Total0Files"] = "{0} Elemente",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1} ausgewählt",
        ["ScreenshotPage_Empty"] = "Kein Screenshot-Ordner gefunden",
        ["ScreenshotPage_EmptyDesc"] = "Screenshots werden standardmäßig in <Spielordner>\\Client\\Saved\\Screenshots gespeichert. Lege zuerst in den Einstellungen den Spielordner fest.",
        ["ScreenshotPage_Delete"] = "Löschen",
        ["ScreenshotPage_DeleteConfirm"] = "Die {0} ausgewählten Screenshots in den Papierkorb verschieben?",
        ["ScreenshotPage_Deleted"] = "{0} Screenshots in den Papierkorb verschoben",
        ["ScreenshotPage_DeleteFailed"] = "Löschen fehlgeschlagen: Einige Dateien werden möglicherweise von einem anderen Programm verwendet",
        ["ScreenshotPage_SelectFirst"] = "Wähle zuerst die zu löschenden Screenshots aus",
        ["ScreenshotPage_OpenImage"] = "Bild öffnen",
        ["ScreenshotPage_DeletedPartial"] = "{0} / {1} gelöscht; der Rest ist möglicherweise in Verwendung",

        
        ["KuroBbs"] = "Kuro BBS",
        ["KuroBbs_SignIn"] = "Check-in",
        ["KuroBbs_Role"] = "Charaktere",
        ["KuroBbs_ResourceBrief"] = "Ressourcenübersicht",

        
        ["PgrData_NoRole"] = "Dieses Kuro-BBS-Konto hat noch keinen PGR-Charakter festgelegt. Lege einen in der Kuro-BBS-App unter „Ich“ fest und sieh dann hier noch einmal nach.",
        ["PgrData_Locked"] = "Gesperrt",
        ["PgrData_FullIn"] = "Voll in {0}",

        
        ["KuroBbs_DataTerminal"] = "Datenterminal",
        ["KuroBbs_CombatData"] = "Kampfdaten",
        ["DataTerminal_NoAccount"] = "Noch nicht bei Kuro BBS angemeldet. Tippe oben auf den Konto-Pfeil, um dich anzumelden.",
        ["DataTerminal_NoRole"] = "Dieses Kuro-BBS-Konto hat noch keinen {0}-Charakter verknüpft. Verknüpfe zuerst einen in der Kuro-BBS-App.",
        ["DataTerminal_LoadFailed"] = "Seite konnte nicht geladen werden: {0}",

        
        ["KuroBbs_PgrRole"] = "Charaktere",
        ["KuroBbs_PgrResource"] = "Ressourcenbericht",
        ["KuroBbs_PgrCage"] = "Schmerzenskäfig",
        ["KuroBbs_PgrWarArea"] = "Kriegszone",
        ["KuroBbs_PgrTransfinite"] = "Transfinite Kartierung",
        ["PgrRole_Fight"] = "Kampfkraft {0}",
        ["PgrResource_Total"] = "Letzte 6 Monate",
        ["PgrResource_BlackCard"] = "Schwarzkarten",
        ["PgrResource_Develop"] = "F&E-Ressourcen",
        ["PgrResource_Trade"] = "Handelsbelege",
        ["PgrResource_SelectPeriod"] = "Zeitraum wählen",
        ["PgrChallenge_NotOpen"] = "Derzeit keine offene Phase",
        ["PgrChallenge_Locked"] = "Noch nicht freigeschaltet",
        ["PgrChallenge_NoArea"] = "Kein Bereich ausgewählt",
        ["PgrChallenge_NoRecord"] = "Keine Herausforderungsdaten in dieser Periode",
        ["PgrChallenge_Level"] = "Stufenbereich {0}",
        ["PgrWarArea_TotalPointLabel"] = "Gesamtpunkte",
        ["PgrWarArea_TotalTimesLabel"] = "Versuche",
        ["PgrWarArea_Waves"] = "Welle {0}",
        ["PgrCommon_LoadFailed"] = "Daten konnten nicht abgerufen werden",

        
        ["DailyBriefing"] = "Briefing",
        ["DailyBriefing_Desc"] = "Tägliche Spieldaten",
        ["DailyBriefing_Refresh"] = "Aktualisieren",
        ["DailyBriefing_Error"] = "Briefing-Daten konnten nicht abgerufen werden",
        ["DailyBriefing_NoGameAccount"] = "Kein angemeldetes Konto gefunden. Bitte anmelden und erneut versuchen.",
        ["DailyBriefing_NoRole"] = "Für dieses Konto wurde noch kein Spielcharakter erkannt. Starte das Spiel einmal (Characterdaten werden online über das offizielle SDK ermittelt).",
        ["Briefing_Liveness"] = "Tägliche Aktivität",
        ["Briefing_StoreEnergy"] = "Gespeicherte Wellenplatten",
        ["Briefing_BattlePass"] = "Pionier-Podcast",
        ["Briefing_ActionPoint"] = "Serum",
        ["Briefing_Level"] = "Stufe",
        ["KuroBbs_TowerOfAdversity"] = "Turm der Widrigkeit",
        ["KuroBbs_SongOfVoidSea"] = "Gesang des Leeren Meeres",
        ["KuroBbs_DoomsdayMatrix"] = "Endzeit-Matrix",
        ["KuroBbs_ComingSoon"] = "{0} in Entwicklung",
        ["KuroBbs_TokenExpired"] = "Kuro-BBS-Anmeldung abgelaufen",
        ["KuroBbs_TokenExpiredDesc"] = "Gehe zu Einstellungen → Kuro-BBS-Konto, um den Token neu zu erfassen oder einzugeben",

        
        ["KuroAccount_UsbTitle"] = "ADB-Tool",
        ["KuroAccount_UsbShortDesc"] = "Android-Telefon verbinden, um Kuro-BBS-Token und Did zu erfassen",
        ["KuroAccount_UsbDesc"] = "Verbinde das Telefon per USB-Kabel und aktiviere USB-Debugging: Öffne auf dem Telefon die Kuro-BBS-App und rufe die Seite „Datenterminal“ oder „Entwicklungsrechner“ auf, tippe dann auf „Erfassung starten“ – das Tool erfasst Token und did automatisch. adb.exe erforderlich (herunterladbar von der ",
        ["KuroAccount_UsbDescTail"] = " offiziellen Seite).",
        ["KuroAccount_UsbVerWarn"] = "⚠ Die neue Version der Kuro-BBS-App unterstützt diese Erfassungsmethode nicht mehr; bitte eine ältere Version verwenden.",
        ["KuroAccount_AdbPlaceholder"] = "adb.exe-Pfad (leer lassen für PATH)",
        ["KuroAccount_ApplyAdb"] = "Anwenden",
        ["KuroAccount_SelectDevice"] = "Gerät auswählen",
        ["KuroAccount_RefreshDevices"] = "Aktualisieren",
        ["KuroAccount_StartCapture"] = "Erfassung starten",
        ["KuroAccount_StopCapture"] = "Erfassung stoppen",
        ["KuroAccount_FieldsTitle"] = "Kontoinfos",
        ["KuroAccount_FillHint"] = "Erfassungsergebnisse werden automatisch eingetragen; du kannst sie auch selbst einfügen",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "Speichern",
        ["KuroAccount_Clear"] = "Leeren",
        ["KuroAccount_Log"] = "Erfassungsprotokoll",
        ["KuroAccount_AdbInvalidTitle"] = "Ungültiger ADB-Pfad",
        ["KuroAccount_AdbInvalidDesc"] = "Gib einen vollständigen, gültigen Pfad zu adb.exe ein",
        ["KuroAccount_AdbApplied"] = "ADB-Pfad angewendet",
        ["KuroAccount_AdbApplyFailed"] = "ADB-Pfad konnte nicht angewendet werden",
        ["KuroAccount_NoDevice"] = "Kein Android-Gerät gefunden",
        ["KuroAccount_NoDeviceDesc"] = "Verbinde das Telefon und erlaube das USB-Debugging",
        ["KuroAccount_RefreshDevicesFailed"] = "Geräte konnten nicht aktualisiert werden",
        ["KuroAccount_SelectDeviceFirst"] = "Wähle zuerst ein Gerät",
        ["KuroAccount_SelectDeviceFirstDesc"] = "Tippe zuerst auf „Aktualisieren“ und wähle dann ein Gerät",
        ["KuroAccount_NoPage"] = "Keine erfassbare Webseite gefunden",
        ["KuroAccount_NoPageDesc"] = "Öffne zuerst auf dem Telefon eine Webseite wie „Datenterminal“ und tippe dann auf „Erfassung starten“",
        ["KuroAccount_CaptureFailed"] = "Erfassungsverbindung fehlgeschlagen",
        ["KuroAccount_CaptureStarted"] = "Überwachung gestartet",
        ["KuroAccount_CaptureStartedDesc"] = "Öffne auf dem Telefon die Seite „Datenterminal“ oder „Entwicklungsrechner“",
        ["KuroAccount_Disconnected"] = "Telefonverbindung getrennt",
        ["KuroAccount_DisconnectedDesc"] = "Die WebView-Seite wurde möglicherweise geschlossen. Tippe erneut auf „Erfassung starten“.",
        ["KuroAccount_NoSocket"] = "Keine WebView-Debugging-Schnittstelle gefunden",
        ["KuroAccount_NoSocketDesc"] = "Diese Kuro-BBS-Version hat das WebView-Remote-Debugging möglicherweise deaktiviert. Details im Diagnosebericht.",
        ["KuroAccount_DiagTitle"] = "Diagnosebericht zur Erfassung",
        ["KuroAccount_DiagHint"] = "Zeigt der Bericht 0 Devtools-Sockets, aktiviert diese Version der Kuro-BBS-App das WebView-Remote-Debugging nicht mehr (auch chrome://inspect in Chrome sieht sie nicht) — die ADB-Erfassung führt also nicht zum Ziel. Nutze stattdessen die Anmeldung mit Telefonnummer + SMS-Code unter „Kuro BBS → Konto → Anmelden“, um Token und Did zu erhalten — ohne Handy-Erfassung, ohne ADB.",
        ["KuroAccount_DiagCopy"] = "Bericht kopieren",
        ["KuroAccount_DiagClose"] = "Schließen",
        ["KuroAccount_DiagCopied"] = "Diagnosebericht kopiert",
        ["KuroAccount_Invalid"] = "Unvollständige Angaben",
        ["KuroAccount_SaveNeedTokenDid"] = "Token und Did sind Pflicht; roleId kann automatisch ermittelt werden",
        ["KuroAccount_Saved"] = "Konto gespeichert",
        ["KuroAccount_Cleared"] = "Konto geleert",

        
        ["RolePage_Refresh"] = "Aktualisieren",
        ["RolePage_NoAccount"] = "Kuro-BBS-Konto noch nicht konfiguriert",
        ["RolePage_NoAccountDesc"] = "Schließe die Einrichtung unter Einstellungen → Kuro-BBS-Konto ab und aktualisiere dann",
        ["RolePage_LoadFailed"] = "Charakter konnte nicht geladen werden",
        ["RolePage_Back"] = "Zurück zur Liste",
        ["RolePage_Phantoms"] = "Echos",

        
        ["GachaLogPage_Refresh"] = "Einberufungen aktualisieren",
        ["GachaLogPage_ImportUrl"] = "Über Link importieren",
        ["GachaLogPage_ImportUrlDesc"] = "Füge den Link der Einberufungsdetailseite ein (öffne „Einberufungsnachweise“ im Spiel und hole ihn aus dem Log, oder kopiere ihn aus anderen Tools). Funktioniert auch mit Cloud-Handys.",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "Ungültiger Link: füge den vollständigen Link der Einberufungsdetailseite mit record_id ein",
        ["GachaLogPage_SelectPlayer"] = "Konto auswählen",
        ["GachaLogPage_CopyUrl"] = "URL kopieren",
        ["GachaLogPage_DeletePlayer"] = "Lösche Aufzeichnungen",
        ["GachaLogPage_Delete"] = "Löschen",
        ["GachaLogPage_Avg5"] = "Ø",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "Statistik",
        ["GachaLogPage_Times"] = "Züge",
        ["GachaLogPage_ItemStats"] = "Gegenstandsstatistik",
        ["GachaLogPage_UpdateSavedUrl"] = "Aktualisieren (gespeicherte URL)",
        ["GachaLogPage_UpdateAll"] = "Aktualisieren (alle)",
        ["GachaLogPage_InputUrl"] = "URL eingeben",
        ["GachaLogPage_ViaCloudGame"] = "Über Cloud-Spiel abrufen",
        ["CloudGame_Title"] = "Cloud Wuthering Waves - Anmelden und Einberufungslink mit einem Klick holen",
        ["CloudGame_Grab"] = "Einberufungslink holen",
        ["CloudGame_StatusDefault"] = "Melde dich hier mit deinem Kuro-Konto an, tippe auf „Spiel betreten“ und öffne einmal die Seite „Einberufungsnachweise“, dann tippe auf „Einberufungslink holen“",
        ["CloudGame_StatusSessionReady"] = "Cloud-Spiel-Anmeldung erkannt. Tippe auf „Einberufungslink holen“, um die Synchronisierung abzuschließen.",
        ["CloudGame_StatusGrabbing"] = "Anmeldeinformationen werden gelesen…",
        ["CloudGame_StatusOk"] = "Erfolgreich (Spieler {0}). Einberufungsverlauf wird synchronisiert…",
        ["CloudGame_StatusFailReason"] = "Fehlgeschlagen: {0}",
        ["CloudGame_StatusNoRuntime"] = "Initialisierung des integrierten Browsers fehlgeschlagen: Die WebView2-Runtime ist nicht installiert. Installiere sie von der Microsoft-Website und versuche es erneut",
        ["GachaLogPage_ImportExport"] = "Import & Export",
        ["GachaLogPage_HidePools"] = "Banner ausblenden",
        ["GachaLogPage_ExportAs"] = "Exportieren als",
        ["GachaLogPage_ImportRecords"] = "Einberufungen importieren (Datei wählen)",
        ["GachaLogPage_ImportOk"] = "Import erfolgreich: Spieler {0}, {1} Einträge (Duplikate automatisch entfernt)",
        ["GachaLogPage_ImportFail"] = "Import fehlgeschlagen: {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "Die Datei enthält keine Kontoinfos und kein Konto ist ausgewählt — der Besitzer lässt sich nicht bestimmen",
        ["GachaLogPage_OtherSettings"] = "Weitere Einstellungen",
        ["GachaLogPage_LinkHelp"] = "Immer „Authkey Timeout“?",
        ["GachaLogPage_LinkHelpDesc"] = "Lösche den Cache-Ordner und starte das Spiel neu",
        ["GachaLogPage_ClearSavedUrl"] = "Cache-Ordner löschen",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "Den lokal gespeicherten Einberufungslink löschen? (Bereits abgerufene Einberufungen werden nicht gelöscht.)",
        ["GachaLogPage_ClearSavedUrlDone"] = "Gespeicherter Einberufungslink gelöscht. Öffne die Seite „Einberufungsnachweise“ im Spiel erneut.",
        ["GachaLogPage_Language"] = "Sprache",
        ["GachaLogPage_LanguageSaved"] = "Sprache gespeichert. Sie wird bei der nächsten Aktualisierung wirksam.",
        ["GachaLogPage_LanguageApplied"] = "{0} Einträge auf {1} umbenannt",
        ["GachaLogPage_NoSavedUrl"] = "Kein gespeicherter Link vorhanden. Führe zuerst „Aktualisieren (alle)“ aus.",
        ["GachaLogPage_LinkExpired"] = "Einberufungslink abgelaufen",
        ["GachaLogPage_LinkExpiredDesc"] = "Der Link ist abgelaufen (API antwortete mit {0}). Öffne „Einberufungsnachweise“ im Spiel erneut.",
        ["GachaLogPage_UpdateFailed"] = "Einberufungsverlauf konnte nicht aktualisiert werden: {0}",
        ["GachaLogPage_FetchFailed"] = "Einberufungsdaten konnten nicht abgerufen werden. Bitte erneut versuchen.",
        ["GachaLogPage_LogNotFound"] = "Spiel-Log oder Einberufungslink nicht gefunden: lege das Spiel (Wuthering Waves.exe) in den Einstellungen fest und öffne einmal „Einberufungsnachweise“ im Spiel, oder wähle im Menü „Einberufungen aktualisieren“ die Option „Über Link importieren“",
        ["GachaLogPage_UrlNotFound"] = "Einberufungslink nicht gefunden. Öffne einmal die Seite „Einberufungsnachweise“ im Spiel.",
        ["GachaLogPage_UrlCopied"] = "Einberufungslink in die Zwischenablage kopiert",
        ["GachaLogPage_PlayerDeleted"] = "Die lokalen Einberufungsdatensätze für UID {0} wurden gelöscht",
        ["GachaLogPage_DeleteConfirmText"] = "Alle lokalen Einberufungsdatensätze für UID {0} werden gelöscht. Dies kann nicht rückgängig gemacht werden.",
        ["GachaLogPage_EmptyTitle"] = "Noch keine Einberufungen",
        ["GachaLogPage_EmptyDesc"] = "Lege zuerst den Spielordner in den Einstellungen fest, öffne im Spiel einmal die Seite „Einberufungsnachweise“, kehre dann hierher zurück und tippe auf „Einberufungen aktualisieren“.",

        
        ["LauncherPage_StartGame"] = "Spiel starten",
        ["LauncherPage_InstallGame"] = "Spiel installieren",
        ["LauncherPage_ResumeInstall"] = "Installation fortsetzen",
        ["AccountSwitcher_SwitchAccount"] = "Konto wechseln",
        ["AccountSwitcher_GameRunning"] = "Spiel läuft – Kontowechsel derzeit nicht möglich",
        ["AccountSwitcher_AccountInfo"] = "Spielkonto",
        ["AccountSwitcher_Nickname"] = "Spitzname",
        ["AccountSwitcher_RoleId"] = "Spiel-UID",
        ["AccountSwitcher_Save"] = "Speichern",
        ["AccountSwitcher_Delete"] = "Löschen",
        ["AccountSwitcher_Unsaved"] = "Das aktuell angemeldete Konto ist nicht gespeichert",
        ["Notice_Activity"] = "Ereignisse",
        ["Notice_NoticeTab"] = "Ankündigungen",
        ["Notice_News"] = "Neuigkeiten",
        ["Notice_LoadFailed"] = "Ankündigungen konnten nicht geladen werden",
        ["LauncherPage_UpdateGame"] = "Spiel aktualisieren",

        
        ["SettingPage_Language"] = "Sprache",

        
        ["GameUpdate_PredownloadAvailable"] = "Vorab-Download der Version {0} verfügbar",
        ["GameUpdate_Predownloaded"] = "{0} vorab geladen; kann nach Freigabe des Updates direkt installiert werden",
        ["GameUpdate_PredownloadButton"] = "Vorab laden",
        ["GameUpdate_ConfirmPredownload"] = "Version {0} vorab laden? Gesamtgröße {1}. Die Dateien werden im Ordner predownload des Spielordners zwischengespeichert; beim Anwenden des offiziellen Updates ist kein erneuter Download nötig.",
        ["GameUpdate_Applying"] = "Update wird entpackt…",
        ["GameUpdate_Pausing"] = "Pausieren…",
        ["GameUpdate_Predownloading"] = "Vorab-Download {0}…",

        ["GameUpdate_Done"] = "Auf {0} aktualisiert",
        ["GameUpdate_PredownloadDone"] = "Vorab-Download von {0} abgeschlossen; kann nach Freigabe des Updates direkt installiert werden",
        ["GameUpdate_NoGameDir"] = "Lege zuerst das Spiel in den Einstellungen fest",
        ["GameUpdate_Verifying"] = "Überprüfung {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "Überprüfung abgeschlossen",
        ["GameUpdate_VerifyDoneNone"] = "Keine beschädigten oder fehlenden Dateien gefunden",
        ["GameUpdate_VerifyDoneDamage"] = "{0} beschädigte / fehlende Dateien von {1} gefunden",
        ["GameUpdate_VerifyNoManifest"] = "Überprüfungsmanifest konnte nicht abgerufen werden. Prüfe das Netzwerk oder versuche es später erneut.",
        ["GameUpdate_Repairing"] = "Wird repariert…",
        ["GameUpdate_RepairDone"] = "{0} Dateien repariert",
        ["GameUpdate_RepairNoBaseUrl"] = "Die aktuelle Version unterstützt die Online-Reparatur nicht. Aktualisiere das Spiel zuerst auf die neueste Version.",

        
        ["GameLauncherPage_Gallery"] = "Galerie",
        ["Common_Copy"] = "Kopieren",
        ["ImageViewWindow2_SaveAs"] = "Speichern unter",
        ["GameLauncherPage_PlayPause"] = "Wiedergabe/Pause",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "Bilddateien ablegen, um den Hintergrund zu wechseln",
        ["Background_Official"] = "Offizieller animierter Hintergrund",
        ["Background_OfficialStatic"] = "Offizieller statischer Hintergrund",
        ["Background_OpenFolder"] = "Hintergrundordner öffnen",
        ["Background_AddFailed"] = "Diese Hintergrunddatei kann nicht verwendet werden",
        ["InstallGameDialog_LocateGame"] = "Spiel suchen",
        ["GameLauncherPage_GameIsRunning"] = "Das Spiel läuft bereits",

        
        ["SettingPage_Download"] = "Herunterladen",

        
        ["DownloadGamePage_Downloading"] = "Wird geladen",
        ["DownloadGamePage_Verifying"] = "Wird überprüft",
        ["DownloadGamePage_Pause"] = "Pause",

        
        ["Common_Confirm"] = "Bestätigen",
        ["Common_Cancel"] = "Abbrechen",
        ["Common_Error"] = "Fehler",
        ["Common_Ok"] = "OK",
        ["Common_Select"] = "Auswählen",
        ["Common_Apply"] = "Anwenden",
        ["Common_Exit"] = "Beenden",

        
        ["SettingPage_AppSettings"] = "Einstellungen",
        ["SettingPage_About"] = "Über",
        ["SettingPage_General"] = "Allgemein",
        ["SettingPage_FileManagement"] = "Dateiverwaltung",

        
        ["FilePage_DataFolder"] = "Datenordner",
        ["FilePage_DataFolderDesc"] = "Deine persönlichen Daten werden im folgenden Ordner gespeichert. Sie sind wichtig — bitte nicht leichtfertig löschen.",
        ["FilePage_ReselectDataFolder"] = "Datenordner neu auswählen",
        ["FilePage_OpenDataFolder"] = "Datenordner öffnen",
        ["FilePage_DeleteAllSettings"] = "Alle Einstellungen löschen",
        ["FilePage_ReselectFolderDone"] = "Neuer Datenordner gespeichert. Er wird nach dem Neustart der App wirksam. Der neue Ordner ist anfangs leer; hole deine alten Daten über die „Cloud-Synchronisierung“ zurück.",
        ["FilePage_DeleteSettingsDone"] = "Einstellungen gelöscht. Sie werden nach dem Neustart der App wirksam.",
        ["FilePage_DeleteSettingsConfirm"] = "Alle Einstellungen (config.ini) werden gelöscht und nach dem Neustart auf die Standardwerte zurückgesetzt. Datenbankinhalte wie Spielzeit und Einberufungen sind nicht betroffen. Fortfahren?",
        ["FilePage_OpenLogFolder"] = "Protokollordner öffnen",
        ["FilePage_Cache"] = "Cache",
        ["FilePage_CacheLog"] = "Protokolle",
        ["FilePage_CacheImage"] = "Bilder",
        ["FilePage_CacheBackground"] = "Launcher-Hintergrund",
        ["FilePage_CleanCache"] = "Cache leeren",
        ["FilePage_CleanBackgroundCache"] = "Launcher-Hintergrunddateien leeren",

        
        ["FilePage_HardLink"] = "Hardlinks",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "Identische Dateien anderer Region-Clients auf derselben Festplatte werden per Hardlink wiederverwendet. Geschätzter Download: ca. {0}. Nur eine Schätzung, maßgeblich ist die dateiweise Prüfung bei der Installation.",
        ["InstallGameDialog_SpaceDesc"] = "Größe der komprimierten Pakete; der tatsächliche Speicherplatzbedarf nach dem Entpacken ist größer.",
["FilePage_HardLinkDesc"] = "Bei mehreren Clients auf demselben Laufwerk werden identische Dateien gehardlinkt, um Speicherplatz zu sparen",
        ["FilePage_Enabled"] = "Aktiviert",
        ["FilePage_Disabled"] = "Deaktiviert",
        ["FilePage_SpeedLimit"] = "Geschwindigkeitsbegrenzung",
        ["FilePage_SpeedLimitDesc"] = "0 bedeutet unbegrenzt",
        ["FilePage_DownloadThreads"] = "Multithreading",
        ["FilePage_DownloadThreadsDesc"] = "Anzahl gleichzeitiger Download-Verbindungen. Zu viel Parallelität bremst auf HDDs durch Suchvorgänge. Änderungen greifen beim nächsten Download.",

        
        ["KuroBbs_RefreshInfo"] = "Infos aktualisieren",
        ["KuroBbs_Login"] = "Anmelden",
        ["KuroBbs_Settings"] = "Kuro-BBS-Einstellungen",

        
        ["Login_Title"] = "Bei Kuro BBS anmelden",
        ["Login_ByToken"] = "Token-Anmeldung",
        ["Login_BySms"] = "SMS-Code-Anmeldung",
        ["Login_TokenPlaceholder"] = "Füge den Kuro-BBS-Login-Token ein",
        ["Login_DidPlaceholder"] = "Did, zusammen mit dem Token erfasst (leer lassen für automatische Erzeugung)",
        ["Login_TokenTip"] = "Token und Did werden aus der Kuro-BBS-Mobile-App erfasst (Werkzeuge → USB-Erfassung) und als Paar gespeichert",
        ["Login_PhonePlaceholder"] = "Telefonnummer",
        ["Login_CodePlaceholder"] = "SMS-Code",
        ["Login_CodeLabel"] = "SMS-Code",
        ["Login_SmsTip"] = "Tippe auf „Code senden“ und gib dann den per SMS erhaltenen Code ein. Nach erfolgreicher Anmeldung wird das Konto automatisch gespeichert.",
        ["Login_SendCode"] = "Code senden",
        ["Login_Confirm"] = "Anmelden",
        ["Login_Cancel"] = "Abbrechen",
        ["Login_PhoneInvalid"] = "Ungültiges Telefonnummer-Format",
        ["Login_Sending"] = "SMS-Code wird gesendet…",
        ["Login_CodeSent"] = "Code gesendet. Prüfe deine SMS.",
        ["Login_CodeSendFailed"] = "Code konnte nicht gesendet werden: {0}",
        ["Login_NeedCode"] = "Gib den SMS-Code ein",
        ["Login_TokenInvalid"] = "Token ungültig: füge den vollständigen Kuro-BBS-Login-Token ein",
        ["Login_Working"] = "Anmeldung läuft…",
        ["Login_Failed"] = "Anmeldung fehlgeschlagen: {0}",
        ["Login_SeekRoleFailed"] = "Verknüpfter Charakter konnte nicht gesucht werden: {0}",
        ["Login_Success"] = "Kuro-BBS-Konto „{0}“ angemeldet",

        
        ["ResourceBrief_Month"] = "Monatlich",
        ["ResourceBrief_Week"] = "Wöchentlich",
        ["ResourceBrief_Version"] = "Version",
        ["ResourceBrief_HistoryData"] = "Verlauf",
        ["ResourceBrief_TotalGain"] = "In dieser Periode erhalten",
        ["ResourceBrief_StarComposition"] = "Aufschlüsselung der Astrite-Einnahmen",
        ["ResourceBrief_CoinComposition"] = "Aufschlüsselung der Muschel-Credits-Einnahmen",
        ["ResourceBrief_Detail"] = "Details",
        ["ResourceBrief_Empty"] = "Keine Daten für diese Periode",

        
        ["SignPage_DoSignIn"] = "Einchecken",
        ["SignPage_MonthlySign"] = "Diesen Monat",
        ["SignPage_SignedToday"] = "Heute eingecheckt",
        ["SignPage_NotSignedToday"] = "Heute noch nicht eingecheckt",
        ["SignPage_Done"] = "Check-in abgeschlossen",
        ["SignPage_AutoSign"] = "Auto-Check-in",
        ["SignPage_SignedDaysFormat"] = "{0} Tage diesen Monat eingecheckt",
        ["SignPage_DayFormat"] = "Tag {0}",
        ["SignPage_Energy"] = "Wellenplatten",
        ["TowerPage_TotalStarCount"] = "Sterne gesamt: {0} / {1}",
        ["TowerPage_Floor"] = "Etage {0}",
        ["TowerPage_NoChallengeData"] = "Keine Herausforderungsdaten",
        ["TowerPage_RefreshAfter"] = "Aktualisierung in {0} T {1} Std",
        ["MatrixPage_NoRecord"] = "Keine Einträge in dieser Periode",
        ["CombatPage_Locked"] = "{0} ist in dieser Periode gesperrt oder hat keine Daten",
        ["CombatPage_NoData"] = "Keine Daten: Der Server hat die Einträge dieser Periode noch nicht erzeugt. Öffne die Seite „{0}“ in der Kuro-BBS-App, um die Daten zu aktualisieren, und versuche es erneut.",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "Gesamtspielzeit",
        ["PlayTimeStatsDialog_Started0Times"] = "{0}-mal gestartet",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "Durchschnittliche tägliche Spielzeit",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "{0} Tage gespielt",
        ["PlayTimeStatsDialog_LongestStreak"] = "Längste Serie",
        ["PlayTimeStatsDialog_0Days"] = "{0} Tage",
        ["PlayTimeStatsDialog_LongestSession"] = "Längste Sitzung",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "Längster Einzelspieltag",
        ["PlayTimeButton_LastStartup"] = "Letzte Sitzung",
        ["PlayTimeStatsDialog_Last15Days"] = "Letzte 15 Tage",
        ["PlayTimeStatsDialog_Last12Weeks"] = "Letzte 12 Wochen",
        ["PlayTimeStatsDialog_Last12Months"] = "Letzte 12 Monate",
        ["PlayTimeStatsDialog_Total"] = "Gesamt",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "Spielzeitstatistik",
        ["PlayTimeStatsDialog_StatsTip"] = "Die Statistik zählt nur Spielzeiten von Sitzungen, die über diesen Launcher gestartet wurden.",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "In den Infobereich minimieren",
        ["GameLauncherSettingDialog_BasicInformation"] = "Grundlegende Informationen",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "Deinstallation bestätigen",
        ["GameLauncherSettingDialog_CustomBackground"] = "Hintergrund",
        ["GameLauncherSettingDialog_RelocateGame"] = "Spiel neu suchen",
        ["GameLauncherSettingDialog_Size"] = "Größe",
        ["GameLauncherSettingDialog_StartRepairing"] = "Reparatur starten",
        ["GameLauncherSettingDialog_StartupArguments"] = "Startargumente",
        ["GameLauncherSettingDialog_CommandLineArgument"] = "Befehlszeilenargumente",
        ["GameLauncherSettingDialog_CustomProgramTip"] = "Ist das Spiel dieses Servers nicht konfiguriert oder fehlt seine exe, startet ein Klick auf „Spiel starten“ stattdessen dieses Programm; ist das Spiel selbst verfügbar, hat es keine Auswirkung.",
        ["LauncherPage_CustomStartExecutable"] = "Eigene Programmdatei",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "Deinstallationswarnung",
        ["LauncherPage_RepairGame"] = "Spiel reparieren",
        ["LauncherPage_ShowGameAnnouncement"] = "Spielankündigungen anzeigen",
        ["LauncherPage_UninstallGame"] = "Spiel deinstallieren",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "Zugeordnete Netzlaufwerke werden nicht unterstützt. Verwende einen Netzwerkfreigabepfad, der mit einem doppelten Backslash beginnt.",

        
        ["SettingPage_BgSizeTip"] = "Hinweis zur Hintergrundgröße",
        ["SettingPage_CustomBackgroundSupportVideo"] = "Eigener Hintergrund unterstützt Video",

        
        ["GameSettingPage_Resolution"] = "Auflösung",
        ["GameSettingPage_FullScreen"] = "Vollbild",
        ["GameSettingPage_UsePopupWindow"] = "Randloses Fenster",
        ["GameSettingPage_CustomResolution"] = "Eigene Auflösung",
        ["GameSettingPage_CustomResolutionTip"] = "Die eigene Auflösung gilt im Anzeigemodus „Fenster“",
        ["GameSettingPage_VoiceLanguage"] = "Sprachausgabe",
        ["GameSettingPage_VoiceLanguageDescription"] = "Lege dies vor dem ersten Spielstart fest, um das Laden einiger Sprachdateien zu vermeiden",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "HDR aktivieren",
        ["GameSettingPage_HDRNotSupported"] = "Der aktuelle Bildschirm unterstützt kein HDR",
        ["GameSettingPage_HDRNotEnabled"] = "Windows-HDR ist noch nicht aktiviert; die Einstellung ist eventuell wirkungslos",
        ["GameSettingPage_GameNotInstalled"] = "Spiel nicht installiert",
        ["GameSettingPage_GameNotInstalledDesc"] = "Lege zuerst auf der Launcher-Seite den Spielpfad fest",
        ["GameSettingPage_CloudGameNotSupport"] = "Das Cloud-Spiel benötigt keine lokalen Spieleinstellungen",
        ["GameSettingPage_SettingNotEffect"] = "Einstellungen, die bei laufendem Spiel angewendet werden, sind wirkungslos",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "Die Auflösung muss größer als 0 sein",
        ["GameSettingPage_LoadFailed"] = "Spieleinstellungen konnten nicht gelesen werden",
        ["GameSettingPage_ApplyFailed"] = "Spieleinstellungen konnten nicht angewendet werden",
        ["GameSettingPage_Applied"] = "Angewendet. Wirksam beim nächsten Spielstart.",

        

        
        ["SettingPage_WindowBehavior"] = "Fensterverhalten",
        ["SettingPage_CloseWindowOption"] = "Beim Schließen des Fensters",
        ["SettingPage_ExitApp"] = "App beenden",
        ["SettingPage_AfterLaunchOption"] = "Nach dem Spielstart",
        ["SettingPage_KeepWindow"] = "Fenster beibehalten",
        ["SettingPage_AfterGameExitOption"] = "Nach Spielende",
        ["SettingPage_RestoreMainWindow"] = "Hauptfenster anzeigen",
        ["SettingPage_KeepHidden"] = "Ausgeblendet lassen",
        ["SettingPage_CloseLauncher"] = "Launcher schließen",
        ["SettingPage_KuroCommunitySign"] = "Kuro-BBS-Community-Check-in (Kuro-Münzen)",
        ["SettingPage_SuggestSystemFeatures"] = "Für ein besseres Erlebnis aktiviere Folgendes in den Systemeinstellungen.",
        ["SettingPage_TransparencyEffects"] = "Transparenzeffekte",
        ["SettingPage_AnimationEffects"] = "Animationseffekte",

        
        ["GameSelector_Subtitle"] = "Tippe oben links auf das Symbol, um ein Spiel zu wählen oder zu wechseln",
        ["GameSelector_AutoFind"] = "Installierte Spiele automatisch finden",
        ["GameSelector_SelectServer"] = "Server auswählen",
        ["GameSelector_InstalledSize"] = "Tatsächlich belegter Speicherplatz der installierten Spiele ",
        ["GameSelector_Calculating"] = "Wird berechnet",

        
        ["KuroBbs_AccountLevelLabel"] = "Union-Stufe ",
        ["KuroBbs_SwitchRole"] = "Charakter wechseln",
        ["KuroBbs_ClearAccount"] = "Konto leeren",

        
        ["GameBiz_Wuwa"] = "Wuthering Waves",
        ["GameBiz_WuwaCn"] = "Wuthering Waves · CN",
        ["GameBiz_WuwaGlobal"] = "Wuthering Waves · Global",
        ["GameBiz_WuwaBilibili"] = "Wuthering Waves · Bilibili",
        ["GameBiz_Pgr"] = "Punishing: Gray Raven",
        ["GameBiz_ShortCn"] = "China-Server",
        ["GameBiz_ShortGlobal"] = "Global-Server",
        ["GameBiz_ShortBilibili"] = "Bilibili",
        ["GameBiz_ShortPgr"] = "China-Server",
        ["GameBiz_PgrGlobal"] = "Punishing: Gray Raven · Global",
        ["GameBiz_ShortGlobal"] = "Global-Server",
        ["GameBiz_PgrKorea"] = "Punishing: Gray Raven · Korea",
        ["GameBiz_ShortKorea"] = "Korea Server",
        ["GameBiz_PgrJapan"] = "Punishing: Gray Raven · Japan",
        ["GameBiz_ShortJapan"] = "Japan Server",
        ["GameBiz_PgrTaiwan"] = "Punishing: Gray Raven · Taiwan",
        ["GameBiz_ShortTaiwan"] = "Taiwan Server",

        
        ["GachaPool_CharacterEvent"] = "Hervorgehobenes Resonator-Zusammenrufen",
        ["GachaPool_WeaponEvent"] = "Hervorgehobenes Waffen-Zusammenrufen",
        ["GachaPool_CharacterStandard"] = "Standard-Charakter-Einberufung",
        ["GachaPool_WeaponStandard"] = "Standard-Waffen-Einberufung",
        ["GachaPool_Novice"] = "Neulings-Einberufung",
        ["GachaPool_NoviceChoice"] = "Neulings-Wahleinberufung",
        ["GachaPool_NoviceGratitude"] = "Neulings-Wahleinberufung (Dank-Einberufung)",
        ["GachaPool_CharacterRookie"] = "Charakter-Neulings-Einberufung",
        ["GachaPool_WeaponRookie"] = "Waffen-Neulings-Einberufung",
        ["GachaPool_CharacterCollab"] = "Charakter-Collab-Einberufung",
        ["GachaPool_WeaponCollab"] = "Waffen-Collab-Einberufung",
        ["GachaPool_CharacterRevisit"] = "Charakter-Wiederkehr-Einberufung",
        ["GachaPool_WeaponRevisit"] = "Waffen-Wiederkehr-Einberufung",
        ["GachaPool_Unknown"] = "Banner {0}",

        
        ["Adb_DeviceOnline"] = "online",
        ["Adb_DeviceUnauthorized"] = "nicht autorisiert",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "Sichere deine Daten durch Cloud-Synchronisierung",
        ["Sync_ServerUrl"] = "Serveradresse",
        ["Sync_UserName"] = "Benutzername",
        ["Sync_Password"] = "Passwort oder App-Autorisierungscode",
        ["Sync_ServerEntry"] = "Synchronisierungsserver",
        ["Sync_EnableOff"] = "Deaktiviert – zum automatischen Synchronisieren aktivieren",
        ["Sync_EnableNeedServer"] = "Bitte zuerst einen Server konfigurieren",
        ["Sync_ConnectTitle"] = "WebDAV verbinden",
        ["Sync_ConnectDesc"] = "Gib die Verbindungsinformationen deiner Cloud oder deines Servers ein.",
        ["Sync_ConnectInvalid"] = "Bitte eine vollständige http://- oder https://-Adresse eingeben",
        ["Sync_SaveAndTest"] = "Speichern und testen",
        ["Sync_Testing"] = "Verbindung wird getestet…",
        ["Sync_ConnectOk"] = "Verbunden. Einstellungen gespeichert.",
        ["Sync_ConnectFail"] = "Verbindung fehlgeschlagen. Prüfe Serveradresse, Konto und Passwort und versuche es erneut.",
        ["Sync_SyncNow"] = "Jetzt synchronisieren",
        ["Sync_RevealPassword"] = "Passwort anzeigen oder verbergen",
        ["Sync_Configured"] = "Konfiguriert",
        ["Sync_NotConfigured"] = "Nicht konfiguriert",
        ["Sync_NeverSynced"] = "Noch nie synchronisiert",
        ["Sync_LastSync"] = "Letzte Synchronisierung",
        ["Sync_Working"] = "Wird verarbeitet…",

        
        ["CloudGame_ErrNoLogin"] = "Keine verwendbaren Anmeldeinformationen",
        ["CloudGame_ErrHttp"] = "Der Cloud-Spieldienst antwortete mit HTTP {0}",
        ["CloudGame_ErrRequest"] = "Anfrage an den Cloud-Spieldienst fehlgeschlagen: {0}",
        ["CloudGame_ErrParse"] = "Der Cloud-Spieldienst lieferte nicht auswertbare Inhalte: {0}",
        ["CloudGame_ErrEmpty"] = "Der Cloud-Spieldienst lieferte leere Inhalte: {0}",
        ["CloudGame_ErrCode"] = "Die Cloud-Spiel-API meldete Fehler code={0}: {1}",

        
        ["GachaLogPage_ExportEmpty"] = "Das aktuelle Konto hat keine exportierbaren Einberufungen",
        ["GachaLog_PityBuilt"] = "Pity",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "Spielpfad-Konfiguration entfernt",
        ["GameLauncherSettingDialog_WrongServerExe"] = "Die ausgewählte Spiel-EXE gehört nicht zum aktuellen Server, bitte erneut wählen",
        ["GameLauncherSettingDialog_ExpectExe"] = "Bitte die Hauptprogramm-EXE des Spiels auswählen: {0}",

        
        ["KrSdk_ErrNoUserCache"] = "KRSDKUserCache.json nicht gefunden. Melde dich zuerst einmal im Spiel an.",
        ["KrSdk_ErrEmptyContent"] = "Inhalt ist leer",
        ["KrSdk_ErrParseFailed"] = "KRSDKUserCache.json konnte nicht ausgewertet werden; Schreiben abgebrochen, um den Anmeldezustand nicht zu beschädigen.",
        ["KrSdk_ErrNoAccountList"] = "Keine account_list in KRSDKUserCache.json.",
        ["KrSdk_ErrAccountNotInCache"] = "Dieses Konto ist nicht im lokalen Cache.",
        ["KrSdk_ErrVerifyFailed"] = "Verifikation nach dem Schreiben fehlgeschlagen; der Cache wurde auf den vorherigen Zustand zurückgesetzt.",

        
        ["GameUpdate_ErrNoPlan"] = "Downloadplan fehlt",
        ["GameUpdate_ErrNoTarget"] = "Zielversion fehlt",
        ["GameUpdate_ErrNoGamePath"] = "Spielpfad nicht konfiguriert; Aktualisierung nicht möglich",
        ["GameUpdate_ErrNoManifest"] = "Download-Manifest konnte nicht abgerufen werden",
        ["GameUpdate_ErrFileMissing"] = "Update-Datei fehlt: {0}",
        ["GameUpdate_ErrFileChecksum"] = "Prüfsumme der Update-Datei fehlgeschlagen: {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "LocalStorage.db des Spiels nicht gefunden",
        ["GameSetting_ErrNoGameUserSettings"] = "GameUserSettings.ini des Spiels nicht gefunden",

        
        ["KuroBbs_ErrConnection"] = "Verbindung fehlgeschlagen, HTTP-Status: {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken lieferte einen leeren accessToken",
        ["KuroBbs_ErrAccessToken"] = "B-At accessToken konnte nicht abgerufen werden (code={0}): {1}",
        ["KuroBbs_ErrSendCode"] = "SMS-Code konnte nicht gesendet werden",
        ["KuroBbs_ErrLogin"] = "Anmeldung fehlgeschlagen",
        ["KuroBbs_SignOk"] = "Eingecheckt",
        ["KuroBbs_SignAlready"] = "Heute bereits eingecheckt",
        ["KuroBbs_SignFail"] = "Check-in fehlgeschlagen",

        

        
        ["Ugf_ErrNoUid"] = "Der UGF-Datei fehlt die Konto-UID",
        ["Ugf_ErrUnknownFormat"] = "Unbekanntes Dateiformat: UGF oder Banner-Verlaufs-JSON erwartet",

        

        
        ["CombatPage_AreaFallback"] = "Bereich {0}",

        
        ["KuroBbs_UnionLevel"] = "Union-Stufe",
        ["KuroBbs_CommanderLevel"] = "Kommandantenstufe",
        ["KuroBbs_NoLevel"] = "Noch keine Stufe",
        ["KuroBbs_SelectRolePgr"] = "PGR-Charakter auswählen",
        ["KuroBbs_SelectRoleWuwa"] = "Wuthering-Waves-Charakter auswählen",
        ["KuroBbs_SwitchedPgr"] = "PGR-Charakter gewechselt",
        ["KuroBbs_SwitchedWuwa"] = "Wuthering-Waves-Charakter gewechselt",
        ["PgrData_DailySupply"] = "Tägliche Versorgung",
        ["KuroBbs_AutoSignDoneToast"] = "Täglicher Check-in abgeschlossen",
        ["KuroBbs_AutoSignFailedToast"] = "Täglicher Check-in fehlgeschlagen",

        
        ["RolePage_WeaponLevel"] = "St.{0} · Aufstieg {1}",

        
        ["Common_UnknownError"] = "Unbekannter Fehler",
    };

}
