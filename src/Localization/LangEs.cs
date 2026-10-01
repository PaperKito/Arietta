using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangEs
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "Lanzador",
        ["LauncherPage_GameSetting"] = "Ajustes del juego",
        ["MainPage_GameScreenshot"] = "Capturas de pantalla",
        ["GachaLogPage_ConveneRecords"] = "Historial de invocación",
        ["Toolbox"] = "Herramientas",

        
        ["ScreenshotPage_MultiSelect"] = "Selección múltiple",
        ["ScreenshotPage_OpenFolder"] = "Abrir carpeta",
        ["ScreenshotPage_Total0Files"] = "{0} elementos",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1} seleccionados",
        ["ScreenshotPage_Empty"] = "No se encontró la carpeta de capturas",
        ["ScreenshotPage_EmptyDesc"] = "Las capturas se guardan por defecto en <Carpeta del juego>\\Client\\Saved\\Screenshots. Indica antes la carpeta del juego en los ajustes.",
        ["ScreenshotPage_Delete"] = "Eliminar",
        ["ScreenshotPage_DeleteConfirm"] = "¿Mover las {0} capturas seleccionadas a la papelera de reciclaje?",
        ["ScreenshotPage_Deleted"] = "{0} capturas movidas a la papelera de reciclaje",
        ["ScreenshotPage_DeleteFailed"] = "Error al eliminar: algunos archivos pueden estar en uso por otro programa",
        ["ScreenshotPage_SelectFirst"] = "Selecciona antes las capturas que quieras eliminar",
        ["ScreenshotPage_OpenImage"] = "Abrir imagen",
        ["ScreenshotPage_DeletedPartial"] = "Se eliminaron {0} / {1}; el resto puede estar en uso",

        
        ["KuroBbs"] = "Kuro BBS",
        ["KuroBbs_SignIn"] = "Registro diario",
        ["KuroBbs_Role"] = "Personajes",
        ["KuroBbs_ResourceBrief"] = "Resumen de recursos",

        
        ["PgrData_NoRole"] = "Esta cuenta de Kuro BBS todavía no ha definido un personaje de PGR. Defínelo en «Mi perfil» de la app de Kuro BBS y vuelve a consultarlo aquí.",
        ["PgrData_Locked"] = "Bloqueado",
        ["PgrData_FullIn"] = "Se llena en {0}",

        
        ["KuroBbs_DataTerminal"] = "Terminal de datos",
        ["KuroBbs_CombatData"] = "Datos de combate",
        ["DataTerminal_NoAccount"] = "Todavía no has iniciado sesión en Kuro BBS. Toca la flecha de cuenta de arriba para iniciar sesión.",
        ["DataTerminal_NoRole"] = "Esta cuenta de Kuro BBS todavía no ha vinculado un personaje de {0}. Vincula uno antes en la app de Kuro BBS.",
        ["DataTerminal_LoadFailed"] = "No se pudo cargar la página: {0}",

        
        ["KuroBbs_PgrRole"] = "Personajes",
        ["KuroBbs_PgrResource"] = "Informe de recursos",
        ["KuroBbs_PgrCage"] = "Jaula del Dolor",
        ["KuroBbs_PgrWarArea"] = "Zona de guerra",
        ["KuroBbs_PgrTransfinite"] = "Cartografía transfinita",
        ["PgrRole_Fight"] = "Poder de combate {0}",
        ["PgrResource_Total"] = "Últimos 6 meses",
        ["PgrResource_BlackCard"] = "Tarjetas negras",
        ["PgrResource_Develop"] = "Recursos de I+D",
        ["PgrResource_Trade"] = "Bonos de comercio",
        ["PgrResource_SelectPeriod"] = "Seleccionar periodo",
        ["PgrChallenge_NotOpen"] = "No está en periodo de apertura",
        ["PgrChallenge_Locked"] = "Aún sin desbloquear",
        ["PgrChallenge_NoArea"] = "Ninguna zona seleccionada",
        ["PgrChallenge_NoRecord"] = "Sin registros de desafío en este periodo",
        ["PgrChallenge_Level"] = "Rango de nivel {0}",
        ["PgrWarArea_TotalPointLabel"] = "Puntuación total",
        ["PgrWarArea_TotalTimesLabel"] = "Intentos",
        ["PgrWarArea_Waves"] = "Oleadas {0}",
        ["PgrCommon_LoadFailed"] = "No se pudieron obtener los datos",

        
        ["DailyBriefing"] = "Informe diario",
        ["DailyBriefing_Desc"] = "Datos diarios del juego",
        ["DailyBriefing_Refresh"] = "Actualizar",
        ["DailyBriefing_Error"] = "No se pudieron obtener los datos del informe diario",
        ["DailyBriefing_NoGameAccount"] = "No se encontró ninguna cuenta iniciada. Inicie sesión e inténtelo de nuevo.",
        ["DailyBriefing_NoRole"] = "Todavía no se ha identificado ningún personaje para esta cuenta. Inicia el juego una vez (los datos del personaje se obtienen en línea mediante el SDK oficial).",
        ["Briefing_Liveness"] = "Actividad diaria",
        ["Briefing_StoreEnergy"] = "Placas de onda almacenadas",
        ["Briefing_BattlePass"] = "Podcast pionero",
        ["Briefing_ActionPoint"] = "Suero",
        ["Briefing_Level"] = "Nivel",
        ["KuroBbs_TowerOfAdversity"] = "Torre de la adversidad",
        ["KuroBbs_SongOfVoidSea"] = "Canción del mar del vacío",
        ["KuroBbs_DoomsdayMatrix"] = "Matriz del apocalipsis",
        ["KuroBbs_ComingSoon"] = "{0} en desarrollo",
        ["KuroBbs_TokenExpired"] = "La sesión de Kuro BBS ha caducado",
        ["KuroBbs_TokenExpiredDesc"] = "Ve a Ajustes → Cuenta de Kuro BBS para volver a capturar o introducir el token",

        
        ["KuroAccount_UsbTitle"] = "Herramienta ADB",
        ["KuroAccount_UsbShortDesc"] = "Conecta un teléfono Android para capturar el Token y el Did de Kuro BBS",
        ["KuroAccount_UsbDesc"] = "Conecta el teléfono con un cable USB y activa la depuración USB: abre la app de Kuro BBS en el teléfono y entra en la página «Terminal de datos» o «Calculadora de desarrollo», luego pulsa «Iniciar captura» y la herramienta capturará automáticamente el token y el did. Se necesita adb.exe (descárgalo desde la ",
        ["KuroAccount_UsbDescTail"] = " página oficial).",
        ["KuroAccount_UsbVerWarn"] = "⚠ La nueva versión de la app de Kuro BBS ya no funciona con este método de captura; usa una versión anterior.",
        ["KuroAccount_AdbPlaceholder"] = "Ruta de adb.exe (déjalo vacío para usar el PATH)",
        ["KuroAccount_ApplyAdb"] = "Aplicar",
        ["KuroAccount_SelectDevice"] = "Seleccionar dispositivo",
        ["KuroAccount_RefreshDevices"] = "Actualizar",
        ["KuroAccount_StartCapture"] = "Iniciar captura",
        ["KuroAccount_StopCapture"] = "Detener captura",
        ["KuroAccount_FieldsTitle"] = "Información de la cuenta",
        ["KuroAccount_FillHint"] = "Los resultados de la captura se rellenan automáticamente; también puedes pegarlos manualmente",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "Guardar",
        ["KuroAccount_Clear"] = "Borrar",
        ["KuroAccount_Log"] = "Registro de captura",
        ["KuroAccount_AdbInvalidTitle"] = "Ruta de ADB no válida",
        ["KuroAccount_AdbInvalidDesc"] = "Introduce una ruta completa válida de adb.exe",
        ["KuroAccount_AdbApplied"] = "Ruta de ADB aplicada",
        ["KuroAccount_AdbApplyFailed"] = "No se pudo aplicar la ruta de ADB",
        ["KuroAccount_NoDevice"] = "No se encontró ningún dispositivo Android",
        ["KuroAccount_NoDeviceDesc"] = "Conecta el teléfono y permite la depuración USB",
        ["KuroAccount_RefreshDevicesFailed"] = "No se pudieron actualizar los dispositivos",
        ["KuroAccount_SelectDeviceFirst"] = "Selecciona antes un dispositivo",
        ["KuroAccount_SelectDeviceFirstDesc"] = "Pulsa primero «Actualizar» y luego selecciona un dispositivo",
        ["KuroAccount_NoPage"] = "No se encontró ninguna página web capturable",
        ["KuroAccount_NoPageDesc"] = "Entra primero en una página web como «Terminal de datos» en el teléfono y pulsa después «Iniciar captura»",
        ["KuroAccount_CaptureFailed"] = "Error en la conexión de captura",
        ["KuroAccount_CaptureStarted"] = "Supervisión iniciada",
        ["KuroAccount_CaptureStartedDesc"] = "Abre en el teléfono la página «Terminal de datos» o «Calculadora de desarrollo»",
        ["KuroAccount_Disconnected"] = "El teléfono se ha desconectado",
        ["KuroAccount_DisconnectedDesc"] = "Es posible que la página WebView se haya cerrado. Vuelve a pulsar «Iniciar captura».",
        ["KuroAccount_NoSocket"] = "No se encontró la interfaz de depuración de WebView",
        ["KuroAccount_NoSocketDesc"] = "Esta versión de Kuro BBS puede tener desactivada la depuración remota de WebView. Consulta el informe de diagnóstico para más detalles.",
        ["KuroAccount_DiagTitle"] = "Informe de diagnóstico de captura",
        ["KuroAccount_DiagHint"] = "Si el informe muestra 0 sockets de devtools, esta versión de la app de Kuro BBS ya no activa la depuración remota de WebView (tampoco la verá chrome://inspect de Chrome), por lo que la captura por ADB no es posible. En su lugar, usa el inicio de sesión con número de teléfono + código SMS en «Kuro BBS → Cuenta → Iniciar sesión» para obtener el Token y el Did, sin captura por teléfono ni ADB.",
        ["KuroAccount_DiagCopy"] = "Copiar informe",
        ["KuroAccount_DiagClose"] = "Cerrar",
        ["KuroAccount_DiagCopied"] = "Informe de diagnóstico copiado",
        ["KuroAccount_Invalid"] = "Información incompleta",
        ["KuroAccount_SaveNeedTokenDid"] = "El Token y el Did son obligatorios; el roleId se puede consultar automáticamente",
        ["KuroAccount_Saved"] = "Cuenta guardada",
        ["KuroAccount_Cleared"] = "Cuenta borrada",

        
        ["RolePage_Refresh"] = "Actualizar",
        ["RolePage_NoAccount"] = "Cuenta de Kuro BBS todavía sin configurar",
        ["RolePage_NoAccountDesc"] = "Termina la configuración en Ajustes → Cuenta de Kuro BBS y actualiza después",
        ["RolePage_LoadFailed"] = "No se pudo cargar el personaje",
        ["RolePage_Back"] = "Volver a la lista",
        ["RolePage_Phantoms"] = "Ecos",

        
        ["GachaLogPage_Refresh"] = "Actualizar registros",
        ["GachaLogPage_ImportUrl"] = "Importar con enlace",
        ["GachaLogPage_ImportUrlDesc"] = "Pega el enlace de la página de detalles de invocación (abre «Detalles de invocación» en el juego y consíguelo del registro, o cópialo de otras herramientas). También sirve para jugadores de teléfono en la nube.",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "Enlace no válido: pega el enlace completo de la página de detalles de invocación que contenga record_id",
        ["GachaLogPage_SelectPlayer"] = "Seleccionar cuenta",
        ["GachaLogPage_CopyUrl"] = "Copiar URL",
        ["GachaLogPage_DeletePlayer"] = "Eliminar registros",
        ["GachaLogPage_Delete"] = "Eliminar",
        ["GachaLogPage_Avg5"] = "Media",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "Estadísticas",
        ["GachaLogPage_Times"] = "tiradas",
        ["GachaLogPage_ItemStats"] = "Estadísticas de objetos",
        ["GachaLogPage_UpdateSavedUrl"] = "Actualizar (URL guardada)",
        ["GachaLogPage_UpdateAll"] = "Actualizar (todo)",
        ["GachaLogPage_InputUrl"] = "Introducir URL",
        ["GachaLogPage_ViaCloudGame"] = "Obtener mediante el juego en la nube",
        ["CloudGame_Title"] = "Wuthering Waves Cloud - Inicia sesión y consigue el enlace de invocación en un clic",
        ["CloudGame_Grab"] = "Obtener enlace de invocación",
        ["CloudGame_StatusDefault"] = "Inicia sesión aquí con tu cuenta de Kuro, pulsa «Entrar al juego» y abre una vez la página «Detalles de invocación»; después pulsa «Obtener enlace de invocación»",
        ["CloudGame_StatusSessionReady"] = "Sesión del juego en la nube detectada. Pulsa «Obtener enlace de invocación» para terminar la sincronización.",
        ["CloudGame_StatusGrabbing"] = "Leyendo la información de sesión…",
        ["CloudGame_StatusOk"] = "Conseguido (jugador {0}). Sincronizando el historial de invocación…",
        ["CloudGame_StatusFailReason"] = "Error: {0}",
        ["CloudGame_StatusNoRuntime"] = "No se pudo inicializar el navegador integrado: el runtime de WebView2 no está instalado. Instálalo desde la web de Microsoft e inténtalo de nuevo",
        ["GachaLogPage_ImportExport"] = "Importar y exportar",
        ["GachaLogPage_HidePools"] = "Ocultar banners",
        ["GachaLogPage_ExportAs"] = "Exportar como",
        ["GachaLogPage_ImportRecords"] = "Importar registros (elegir archivo)",
        ["GachaLogPage_ImportOk"] = "Importación correcta: jugador {0}, {1} registros (los duplicados se eliminan automáticamente)",
        ["GachaLogPage_ImportFail"] = "Error al importar: {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "El archivo no contiene información de cuenta y no hay ninguna cuenta seleccionada, así que no se puede determinar a quién pertenece",
        ["GachaLogPage_OtherSettings"] = "Otros ajustes",
        ["GachaLogPage_LinkHelp"] = "¿Aparece siempre «Authkey Timeout»?",
        ["GachaLogPage_LinkHelpDesc"] = "Elimina la carpeta de caché y reinicia el juego",
        ["GachaLogPage_ClearSavedUrl"] = "Eliminar carpeta de caché",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "¿Eliminar el enlace de invocación guardado localmente? (Los registros ya obtenidos no se eliminarán.)",
        ["GachaLogPage_ClearSavedUrlDone"] = "Enlace de invocación guardado borrado. Vuelve a abrir la página «Detalles de invocación» en el juego.",
        ["GachaLogPage_Language"] = "Idioma",
        ["GachaLogPage_LanguageSaved"] = "Idioma guardado. Se aplicará en la próxima actualización de registros.",
        ["GachaLogPage_LanguageApplied"] = "Se renombraron {0} registros a {1}",
        ["GachaLogPage_NoSavedUrl"] = "No hay enlace guardado. Ejecuta antes «Actualizar (todo)».",
        ["GachaLogPage_LinkExpired"] = "El enlace de invocación ha caducado",
        ["GachaLogPage_LinkExpiredDesc"] = "El enlace ha caducado (la API devolvió {0}). Vuelve a abrir «Detalles de invocación» en el juego.",
        ["GachaLogPage_UpdateFailed"] = "Error al actualizar el historial de invocación: {0}",
        ["GachaLogPage_FetchFailed"] = "No se pudieron obtener los datos de invocación. Inténtalo de nuevo.",
        ["GachaLogPage_LogNotFound"] = "No se encontró el registro del juego ni el enlace de invocación: indica el juego (Wuthering Waves.exe) en los ajustes y abre una vez «Detalles de invocación» en el juego, o elige «Importar con enlace» en el menú «Actualizar registros»",
        ["GachaLogPage_UrlNotFound"] = "No se encontró el enlace de invocación. Abre una vez la página «Detalles de invocación» en el juego.",
        ["GachaLogPage_UrlCopied"] = "Enlace de invocación copiado al portapapeles",
        ["GachaLogPage_PlayerDeleted"] = "Se han eliminado los registros de invocación locales de la UID {0}",
        ["GachaLogPage_DeleteConfirmText"] = "Se eliminarán todos los registros de invocación locales de la UID {0}. Esta acción no se puede deshacer.",
        ["GachaLogPage_EmptyTitle"] = "Aún no hay registros de invocación",
        ["GachaLogPage_EmptyDesc"] = "Indica antes la carpeta del juego en los ajustes, abre una vez la página «Detalles de invocación» en el juego, vuelve aquí y pulsa «Actualizar registros» para sincronizar.",

        
        ["LauncherPage_StartGame"] = "Iniciar juego",
        ["LauncherPage_InstallGame"] = "Instalar juego",
        ["AccountSwitcher_SwitchAccount"] = "Cambiar de cuenta",
        ["AccountSwitcher_GameRunning"] = "El juego está en ejecución; no se puede cambiar de cuenta por ahora",
        ["AccountSwitcher_AccountInfo"] = "Cuenta del juego",
        ["AccountSwitcher_Nickname"] = "Apodo",
        ["AccountSwitcher_RoleId"] = "UID del juego",
        ["AccountSwitcher_Save"] = "Guardar",
        ["AccountSwitcher_Delete"] = "Eliminar",
        ["AccountSwitcher_Unsaved"] = "La cuenta con sesión iniciada no está guardada",
        ["Notice_Activity"] = "Eventos",
        ["Notice_NoticeTab"] = "Avisos",
        ["Notice_News"] = "Noticias",
        ["Notice_LoadFailed"] = "No se pudieron cargar los avisos",
        ["LauncherPage_UpdateGame"] = "Actualizar juego",

        
        ["SettingPage_Language"] = "Idioma",

        
        ["GameUpdate_PredownloadAvailable"] = "Predescarga de la versión {0} disponible",
        ["GameUpdate_Predownloaded"] = "{0} predescargado; se podrá instalar directamente cuando se abra la actualización",
        ["GameUpdate_PredownloadButton"] = "Predescargar",
        ["GameUpdate_ConfirmPredownload"] = "¿Predescargar la versión {0}? Descarga total: {1}. Los archivos se guardan en la carpeta predownload de la carpeta del juego; al aplicar la actualización oficial no hará falta volver a descargarlos.",
        ["GameUpdate_Applying"] = "Aplicando la actualización…",
        ["GameUpdate_Predownloading"] = "Predescargando {0}…",

        ["GameUpdate_Done"] = "Actualizado a {0}",
        ["GameUpdate_PredownloadDone"] = "Predescarga de {0} completada; se podrá instalar directamente cuando se abra la actualización",
        ["GameUpdate_NoGameDir"] = "Indica antes el juego en los ajustes",
        ["GameUpdate_Verifying"] = "Verificando {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "Verificación completada",
        ["GameUpdate_VerifyDoneNone"] = "No se encontraron archivos dañados o ausentes",
        ["GameUpdate_VerifyDoneDamage"] = "Se encontraron {0} archivos dañados / ausentes de {1}",
        ["GameUpdate_VerifyNoManifest"] = "No se pudo obtener el manifiesto de verificación. Comprueba la red o inténtalo más tarde.",
        ["GameUpdate_Repairing"] = "Reparando…",
        ["GameUpdate_RepairDone"] = "Se repararon {0} archivos",
        ["GameUpdate_RepairNoBaseUrl"] = "La versión actual no admite la reparación en línea. Actualiza antes el juego a la última versión.",

        
        ["GameLauncherPage_Gallery"] = "Galería",
        ["Common_Copy"] = "Copiar",
        ["ImageViewWindow2_SaveAs"] = "Guardar como",
        ["GameLauncherPage_PlayPause"] = "Reproducir/Pausar",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "Arrastra archivos de imagen para cambiar el fondo",
        ["Background_Official"] = "Fondo animado oficial",
        ["Background_OfficialStatic"] = "Fondo estático oficial",
        ["Background_OpenFolder"] = "Abrir carpeta de fondos",
        ["Background_AddFailed"] = "No se puede usar este archivo de fondo",
        ["InstallGameDialog_LocateGame"] = "Localizar juego",
        ["GameLauncherPage_GameIsRunning"] = "El juego está en ejecución",

        
        ["SettingPage_Download"] = "Descargar",

        
        ["DownloadGamePage_Downloading"] = "Descargando",
        ["DownloadGamePage_Verifying"] = "Verificando",
        ["DownloadGamePage_Pause"] = "Pausar",

        
        ["Common_Confirm"] = "Confirmar",
        ["Common_Cancel"] = "Cancelar",
        ["Common_Error"] = "Error",
        ["Common_Ok"] = "Aceptar",
        ["Common_Select"] = "Seleccionar",
        ["Common_Apply"] = "Aplicar",
        ["Common_Exit"] = "Salir",

        
        ["SettingPage_AppSettings"] = "Ajustes",
        ["SettingPage_About"] = "Acerca de",
        ["SettingPage_General"] = "General",
        ["SettingPage_FileManagement"] = "Gestión de archivos",

        
        ["FilePage_DataFolder"] = "Carpeta de datos",
        ["FilePage_DataFolderDesc"] = "Tus datos personales se guardan en la carpeta de abajo. Son importantes: no los borres a la ligera.",
        ["FilePage_ReselectDataFolder"] = "Volver a elegir la carpeta de datos",
        ["FilePage_OpenDataFolder"] = "Abrir carpeta de datos",
        ["FilePage_DeleteAllSettings"] = "Eliminar todos los ajustes",
        ["FilePage_ReselectFolderDone"] = "Nueva carpeta de datos guardada. Se aplicará al reiniciar la aplicación. La carpeta nueva empieza vacía; usa la «Sincronización en la nube» para recuperar tus datos antiguos.",
        ["FilePage_DeleteSettingsDone"] = "Ajustes eliminados. Se aplicará al reiniciar la aplicación.",
        ["FilePage_DeleteSettingsConfirm"] = "Se eliminarán todos los ajustes (config.ini) y se restaurarán los valores predeterminados tras reiniciar. Los datos de la base de datos, como el tiempo de juego y las invocaciones, no se verán afectados. ¿Continuar?",
        ["FilePage_OpenLogFolder"] = "Abrir carpeta de registros",
        ["FilePage_Cache"] = "Caché",
        ["FilePage_CacheLog"] = "Registros",
        ["FilePage_CacheImage"] = "Imágenes",
        ["FilePage_CacheBackground"] = "Fondo del lanzador",
        ["FilePage_CleanCache"] = "Borrar caché",
        ["FilePage_CleanBackgroundCache"] = "Borrar archivos de fondo del lanzador",

        
        ["FilePage_HardLink"] = "Vínculos físicos",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "Los archivos idénticos de otros clientes de región en el mismo disco se reutilizarán mediante vínculos físicos. Descarga estimada: unos {0}. Solo es una estimación; rige la verificación por archivo durante la instalación.",
        ["GameUpdate_ResumeInstall"] = "Se detectó una instalación sin terminar de {0}; se continúa desde el directorio anterior",
["FilePage_HardLinkDesc"] = "Al instalar varios clientes en el mismo disco, los archivos idénticos se vinculan físicamente para reducir el almacenamiento",
        ["FilePage_Enabled"] = "Activado",
        ["FilePage_Disabled"] = "Desactivado",
        ["FilePage_SpeedLimit"] = "Límite de velocidad",
        ["FilePage_SpeedLimitDesc"] = "0 significa sin límite",

        
        ["KuroBbs_RefreshInfo"] = "Actualizar información",
        ["KuroBbs_Login"] = "Iniciar sesión",
        ["KuroBbs_Settings"] = "Ajustes de Kuro BBS",

        
        ["Login_Title"] = "Iniciar sesión en Kuro BBS",
        ["Login_ByToken"] = "Iniciar sesión con Token",
        ["Login_BySms"] = "Iniciar sesión con código SMS",
        ["Login_TokenPlaceholder"] = "Pega el token de inicio de sesión de Kuro BBS",
        ["Login_DidPlaceholder"] = "Did capturado junto con el Token (déjalo vacío para generarlo automáticamente)",
        ["Login_TokenTip"] = "El Token y el Did se capturan desde la app móvil de Kuro BBS (Herramientas → Captura USB) y se guardan en pareja",
        ["Login_PhonePlaceholder"] = "Número de teléfono",
        ["Login_CodePlaceholder"] = "Código SMS",
        ["Login_CodeLabel"] = "Código SMS",
        ["Login_SmsTip"] = "Pulsa «Enviar código» e introduce después el código recibido por SMS. La cuenta se guarda automáticamente tras iniciar sesión con éxito.",
        ["Login_SendCode"] = "Enviar código",
        ["Login_Confirm"] = "Iniciar sesión",
        ["Login_Cancel"] = "Cancelar",
        ["Login_PhoneInvalid"] = "Formato de número de teléfono no válido",
        ["Login_Sending"] = "Enviando el código SMS…",
        ["Login_CodeSent"] = "Código enviado. Revisa tus mensajes.",
        ["Login_CodeSendFailed"] = "Error al enviar el código: {0}",
        ["Login_NeedCode"] = "Introduce el código SMS",
        ["Login_TokenInvalid"] = "Token no válido: pega el token de inicio de sesión completo de Kuro BBS",
        ["Login_Working"] = "Iniciando sesión…",
        ["Login_Failed"] = "Error al iniciar sesión: {0}",
        ["Login_SeekRoleFailed"] = "Error al buscar el personaje vinculado: {0}",
        ["Login_Success"] = "Sesión iniciada con la cuenta de Kuro BBS «{0}»",

        
        ["ResourceBrief_Month"] = "Mensual",
        ["ResourceBrief_Week"] = "Semanal",
        ["ResourceBrief_Version"] = "Versión",
        ["ResourceBrief_HistoryData"] = "Historial",
        ["ResourceBrief_TotalGain"] = "Obtenido en este periodo",
        ["ResourceBrief_StarComposition"] = "Composición de ingresos de Astrita",
        ["ResourceBrief_CoinComposition"] = "Composición de ingresos de créditos de concha",
        ["ResourceBrief_Detail"] = "Detalles",
        ["ResourceBrief_Empty"] = "Sin datos en este periodo",

        
        ["SignPage_DoSignIn"] = "Registrar",
        ["SignPage_MonthlySign"] = "Este mes",
        ["SignPage_SignedToday"] = "Registrado hoy",
        ["SignPage_NotSignedToday"] = "Aún no registrado hoy",
        ["SignPage_Done"] = "Registro completado",
        ["SignPage_AutoSign"] = "Registro automático",
        ["SignPage_SignedDaysFormat"] = "{0} días registrados este mes",
        ["SignPage_DayFormat"] = "Día {0}",
        ["SignPage_Energy"] = "Placas de onda",
        ["TowerPage_TotalStarCount"] = "Estrellas totales: {0} / {1}",
        ["TowerPage_Floor"] = "Planta {0}",
        ["TowerPage_NoChallengeData"] = "Sin datos de desafío",
        ["TowerPage_RefreshAfter"] = "Se actualiza en {0} d {1} h",
        ["MatrixPage_NoRecord"] = "Sin registros en este periodo",
        ["CombatPage_Locked"] = "{0} está bloqueado en este periodo o no tiene datos",
        ["CombatPage_NoData"] = "Sin datos: el servidor todavía no ha generado los registros de este periodo. Abre la página «{0}» en la app de Kuro BBS para actualizar los datos e inténtalo de nuevo.",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "Tiempo de juego total",
        ["PlayTimeStatsDialog_Started0Times"] = "Iniciado {0} veces",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "Tiempo de juego diario medio",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "Jugado durante {0} días",
        ["PlayTimeStatsDialog_LongestStreak"] = "Racha más larga",
        ["PlayTimeStatsDialog_0Days"] = "{0} días",
        ["PlayTimeStatsDialog_LongestSession"] = "Sesión más larga",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "Día con más juego",
        ["PlayTimeButton_LastStartup"] = "Última sesión",
        ["PlayTimeStatsDialog_Last15Days"] = "Últimos 15 días",
        ["PlayTimeStatsDialog_Last12Weeks"] = "Últimas 12 semanas",
        ["PlayTimeStatsDialog_Last12Months"] = "Últimos 12 meses",
        ["PlayTimeStatsDialog_Total"] = "Total",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "Estadísticas de tiempo de juego",
        ["PlayTimeStatsDialog_StatsTip"] = "Las estadísticas solo cuentan el tiempo de juego de las partidas iniciadas desde este lanzador.",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "Minimizar a la bandeja del sistema",
        ["GameLauncherSettingDialog_BasicInformation"] = "Información básica",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "Confirmar desinstalación",
        ["GameLauncherSettingDialog_CustomBackground"] = "Fondo personalizado",
        ["GameLauncherSettingDialog_RelocateGame"] = "Reubicar el juego",
        ["GameLauncherSettingDialog_Size"] = "Tamaño",
        ["GameLauncherSettingDialog_StartRepairing"] = "Iniciar reparación",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "Advertencia de desinstalación del juego",
        ["LauncherPage_RepairGame"] = "Reparar juego",
        ["LauncherPage_ShowGameAnnouncement"] = "Mostrar avisos del juego",
        ["LauncherPage_UninstallGame"] = "Desinstalar juego",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "No se admiten unidades de red asignadas. Usa una ruta de recurso compartido de red que empiece por doble barra invertida.",

        
        ["SettingPage_BgSizeTip"] = "Consejo sobre el tamaño del fondo",
        ["SettingPage_CustomBackgroundSupportVideo"] = "El fondo personalizado admite vídeo",

        
        ["GameSettingPage_Resolution"] = "Resolución",
        ["GameSettingPage_FullScreen"] = "Pantalla completa",
        ["GameSettingPage_UsePopupWindow"] = "Ventana sin bordes",
        ["GameSettingPage_CustomResolution"] = "Resolución personalizada",
        ["GameSettingPage_CustomResolutionTip"] = "La resolución personalizada se aplica en el modo «Ventana»",
        ["GameSettingPage_VoiceLanguage"] = "Idioma de voz",
        ["GameSettingPage_VoiceLanguageDescription"] = "Configúralo antes de iniciar el juego por primera vez para evitar descargar algunos archivos de voz",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "Activar HDR",
        ["GameSettingPage_HDRNotSupported"] = "La pantalla actual no admite HDR",
        ["GameSettingPage_HDRNotEnabled"] = "El HDR de Windows aún no está activado; podría no tener efecto",
        ["GameSettingPage_GameNotInstalled"] = "Juego no instalado",
        ["GameSettingPage_GameNotInstalledDesc"] = "Indica antes la ruta del juego en la página del lanzador",
        ["GameSettingPage_CloudGameNotSupport"] = "El juego en la nube no necesita ajustes locales del juego",
        ["GameSettingPage_SettingNotEffect"] = "Los ajustes aplicados con el juego en ejecución no tendrán efecto",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "La resolución debe ser mayor que 0",
        ["GameSettingPage_LoadFailed"] = "No se pudieron leer los ajustes del juego",
        ["GameSettingPage_ApplyFailed"] = "No se pudieron aplicar los ajustes del juego",
        ["GameSettingPage_Applied"] = "Aplicado. Tendrá efecto la próxima vez que se inicie el juego.",

        

        
        ["SettingPage_WindowBehavior"] = "Comportamiento de la ventana",
        ["SettingPage_CloseWindowOption"] = "Al cerrar la ventana",
        ["SettingPage_ExitApp"] = "Salir de la aplicación",
        ["SettingPage_AfterLaunchOption"] = "Tras iniciar el juego",
        ["SettingPage_KeepWindow"] = "Mantener la ventana",
        ["SettingPage_AfterGameExitOption"] = "Al cerrar el juego",
        ["SettingPage_RestoreMainWindow"] = "Mostrar ventana principal",
        ["SettingPage_KeepHidden"] = "Mantener oculta",
        ["SettingPage_CloseLauncher"] = "Cerrar launcher",
        ["SettingPage_KuroCommunitySign"] = "Registro diario de la comunidad de Kuro BBS (monedas Kuro)",
        ["SettingPage_SuggestSystemFeatures"] = "Para una mejor experiencia, activa las siguientes funciones en los ajustes del sistema.",
        ["SettingPage_TransparencyEffects"] = "Efectos de transparencia",
        ["SettingPage_AnimationEffects"] = "Efectos de animación",

        
        ["GameSelector_Subtitle"] = "Toca el icono de la esquina superior izquierda para elegir o cambiar de juego",
        ["GameSelector_AutoFind"] = "Buscar automáticamente los juegos instalados",
        ["GameSelector_SelectServer"] = "Seleccionar servidor",
        ["GameSelector_InstalledSize"] = "Espacio en disco que ocupan realmente los juegos instalados ",
        ["GameSelector_Calculating"] = "Calculando",

        
        ["KuroBbs_AccountLevelLabel"] = "Nivel de unión ",
        ["KuroBbs_SwitchRole"] = "Cambiar de personaje",
        ["KuroBbs_ClearAccount"] = "Borrar cuenta",

        
        ["GameBiz_Wuwa"] = "Wuthering Waves",
        ["GameBiz_WuwaCn"] = "Wuthering Waves · CN",
        ["GameBiz_WuwaGlobal"] = "Wuthering Waves · Global",
        ["GameBiz_WuwaBilibili"] = "Wuthering Waves · Bilibili",
        ["GameBiz_Pgr"] = "Punishing: Gray Raven",
        ["GameBiz_ShortCn"] = "Servidor China",
        ["GameBiz_ShortGlobal"] = "Servidor Global",
        ["GameBiz_ShortBilibili"] = "Bilibili",
        ["GameBiz_ShortPgr"] = "Servidor China",
        ["GameBiz_PgrGlobal"] = "Punishing: Gray Raven · Global",
        ["GameBiz_ShortGlobal"] = "Servidor Global",
        ["GameBiz_PgrKorea"] = "Punishing: Gray Raven · Korea",
        ["GameBiz_ShortKorea"] = "Servidor Corea",
        ["GameBiz_PgrJapan"] = "Punishing: Gray Raven · Japan",
        ["GameBiz_ShortJapan"] = "Servidor Japón",
        ["GameBiz_PgrTaiwan"] = "Punishing: Gray Raven · Taiwan",
        ["GameBiz_ShortTaiwan"] = "Servidor Taiwán",

        
        ["GachaPool_CharacterEvent"] = "Invocación de evento de personaje",
        ["GachaPool_WeaponEvent"] = "Invocación de evento de arma",
        ["GachaPool_CharacterStandard"] = "Invocación estándar de personaje",
        ["GachaPool_WeaponStandard"] = "Invocación estándar de arma",
        ["GachaPool_Novice"] = "Invocación de novato",
        ["GachaPool_NoviceChoice"] = "Invocación de elección de novato",
        ["GachaPool_NoviceGratitude"] = "Invocación de elección de novato (invocación de gratitud)",
        ["GachaPool_CharacterRookie"] = "Invocación de recluta de personaje",
        ["GachaPool_WeaponRookie"] = "Invocación de recluta de arma",
        ["GachaPool_CharacterCollab"] = "Invocación de colaboración de personaje",
        ["GachaPool_WeaponCollab"] = "Invocación de colaboración de arma",
        ["GachaPool_CharacterRevisit"] = "Invocación de reedición de personaje",
        ["GachaPool_WeaponRevisit"] = "Invocación de reedición de arma",
        ["GachaPool_Unknown"] = "Banner {0}",

        
        ["Adb_DeviceOnline"] = "en línea",
        ["Adb_DeviceUnauthorized"] = "sin autorizar",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "Guarda tus datos sincronizándolos con la nube",
        ["Sync_ServerUrl"] = "Dirección del servidor",
        ["Sync_UserName"] = "Nombre de usuario",
        ["Sync_Password"] = "Contraseña o código de autorización de la app",
        ["Sync_ServerEntry"] = "Servidor de sincronización",
        ["Sync_EnableOff"] = "Desactivada: actívala para sincronizar automáticamente",
        ["Sync_EnableNeedServer"] = "Configura primero el servidor",
        ["Sync_ConnectTitle"] = "Conectar WebDAV",
        ["Sync_ConnectDesc"] = "Introduce los datos de conexión de tu nube o servidor.",
        ["Sync_ConnectInvalid"] = "Introduce una dirección completa http:// o https://",
        ["Sync_SaveAndTest"] = "Guardar y probar",
        ["Sync_Testing"] = "Probando conexión…",
        ["Sync_ConnectOk"] = "Conectado. Ajustes guardados.",
        ["Sync_ConnectFail"] = "Error de conexión. Comprueba la dirección del servidor, la cuenta y la contraseña, e inténtalo de nuevo.",
        ["Sync_SyncNow"] = "Sincronizar ahora",
        ["Sync_RevealPassword"] = "Mostrar u ocultar la contraseña",
        ["Sync_Configured"] = "Configurado",
        ["Sync_NotConfigured"] = "Sin configurar",
        ["Sync_NeverSynced"] = "Nunca sincronizado",
        ["Sync_LastSync"] = "Última sincronización",
        ["Sync_Working"] = "Procesando…",

        
        ["CloudGame_ErrNoLogin"] = "No hay información de sesión utilizable",
        ["CloudGame_ErrHttp"] = "El servicio del juego en la nube devolvió HTTP {0}",
        ["CloudGame_ErrRequest"] = "Error en la petición al servicio del juego en la nube: {0}",
        ["CloudGame_ErrParse"] = "El servicio del juego en la nube devolvió contenido no interpretable: {0}",
        ["CloudGame_ErrEmpty"] = "El servicio del juego en la nube devolvió contenido vacío: {0}",
        ["CloudGame_ErrCode"] = "La API del juego en la nube devolvió el error code={0}: {1}",

        
        ["GachaLogPage_ExportEmpty"] = "La cuenta actual no tiene registros de invocación que exportar",
        ["GachaLog_PityBuilt"] = "Pity",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "Configuración de la ruta del juego eliminada",
        ["GameLauncherSettingDialog_WrongServerExe"] = "El exe del juego seleccionado no pertenece al servidor actual, elige de nuevo",
        ["GameLauncherSettingDialog_ExpectExe"] = "Selecciona el ejecutable principal del juego: {0}",

        
        ["KrSdk_ErrNoUserCache"] = "No se encontró KRSDKUserCache.json. Inicia sesión una vez en el juego primero.",
        ["KrSdk_ErrEmptyContent"] = "El contenido está vacío",
        ["KrSdk_ErrParseFailed"] = "No se pudo interpretar KRSDKUserCache.json; escritura cancelada para no dañar el estado de la sesión.",
        ["KrSdk_ErrNoAccountList"] = "No hay account_list en KRSDKUserCache.json.",
        ["KrSdk_ErrAccountNotInCache"] = "Esta cuenta no está en la caché local.",
        ["KrSdk_ErrVerifyFailed"] = "La verificación tras la escritura falló; la caché se restauró a su estado anterior.",

        
        ["GameUpdate_ErrNoPlan"] = "Falta el plan de descarga",
        ["GameUpdate_ErrNoTarget"] = "Falta la versión de destino",
        ["GameUpdate_ErrNoGamePath"] = "Ruta del juego sin configurar; no se puede actualizar",
        ["GameUpdate_ErrNoManifest"] = "No se pudo obtener el manifiesto de descarga",
        ["GameUpdate_ErrFileMissing"] = "Falta un archivo de actualización: {0}",
        ["GameUpdate_ErrFileChecksum"] = "Error en la suma de comprobación del archivo de actualización: {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "No se encontró el LocalStorage.db del juego",
        ["GameSetting_ErrNoGameUserSettings"] = "No se encontró el GameUserSettings.ini del juego",

        
        ["KuroBbs_ErrConnection"] = "Error de conexión, estado HTTP: {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken devolvió un accessToken vacío",
        ["KuroBbs_ErrAccessToken"] = "No se pudo obtener el B-At accessToken (code={0}): {1}",
        ["KuroBbs_ErrSendCode"] = "Error al enviar el código SMS",
        ["KuroBbs_ErrLogin"] = "Error al iniciar sesión",
        ["KuroBbs_SignOk"] = "Registro completado",
        ["KuroBbs_SignAlready"] = "Ya registrado hoy",
        ["KuroBbs_SignFail"] = "Error en el registro diario",

        

        
        ["Ugf_ErrNoUid"] = "Al archivo UGF le falta el uid de la cuenta",
        ["Ugf_ErrUnknownFormat"] = "Formato de archivo no reconocido: se esperaba UGF o un JSON de registros de banner",

        

        
        ["CombatPage_AreaFallback"] = "Zona {0}",

        
        ["KuroBbs_UnionLevel"] = "Nivel de unión",
        ["KuroBbs_CommanderLevel"] = "Nivel de comandante",
        ["KuroBbs_NoLevel"] = "Aún sin nivel",
        ["KuroBbs_SelectRolePgr"] = "Seleccionar personaje de PGR",
        ["KuroBbs_SelectRoleWuwa"] = "Seleccionar personaje de Wuthering Waves",
        ["KuroBbs_SwitchedPgr"] = "Personaje de PGR cambiado",
        ["KuroBbs_SwitchedWuwa"] = "Personaje de Wuthering Waves cambiado",
        ["PgrData_DailySupply"] = "Suministro diario",
        ["KuroBbs_AutoSignDoneToast"] = "Check-in diario completado",
        ["KuroBbs_AutoSignFailedToast"] = "Error en el check-in diario",

        
        ["RolePage_WeaponLevel"] = "Nv.{0} · Ascensión {1}",

        
        ["Common_UnknownError"] = "Error desconocido",
    };

}
