using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangFr
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "Lanceur",
        ["LauncherPage_GameSetting"] = "Paramètres du jeu",
        ["MainPage_GameScreenshot"] = "Captures d'écran",
        ["GachaLogPage_ConveneRecords"] = "Registres de convocation",
        ["Toolbox"] = "Boîte à outils",

        
        ["ScreenshotPage_MultiSelect"] = "Sélection multiple",
        ["ScreenshotPage_OpenFolder"] = "Ouvrir le dossier",
        ["ScreenshotPage_Total0Files"] = "{0} éléments",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1} sélectionnés",
        ["ScreenshotPage_Empty"] = "Dossier de captures introuvable",
        ["ScreenshotPage_EmptyDesc"] = "Les captures sont enregistrées par défaut dans <Dossier du jeu>\\Client\\Saved\\Screenshots. Indiquez d'abord le dossier du jeu dans les paramètres.",
        ["ScreenshotPage_Delete"] = "Supprimer",
        ["ScreenshotPage_DeleteConfirm"] = "Déplacer les {0} captures sélectionnées vers la corbeille ?",
        ["ScreenshotPage_Deleted"] = "{0} captures déplacées vers la corbeille",
        ["ScreenshotPage_DeleteFailed"] = "Échec de la suppression : certains fichiers sont peut-être utilisés par un autre programme",
        ["ScreenshotPage_SelectFirst"] = "Sélectionnez d'abord les captures à supprimer",
        ["ScreenshotPage_OpenImage"] = "Ouvrir l'image",
        ["ScreenshotPage_DeletedPartial"] = "{0} / {1} supprimées ; le reste est peut-être en cours d'utilisation",

        
        ["KuroBbs"] = "Kuro BBS",
        ["KuroBbs_SignIn"] = "Présence",
        ["KuroBbs_Role"] = "Personnages",
        ["KuroBbs_ResourceBrief"] = "Résumé des ressources",

        
        ["PgrData_NoRole"] = "Ce compte Kuro BBS n'a pas encore défini de personnage PGR. Définissez-en un dans « Mon profil » de l'application Kuro BBS, puis revenez ici.",
        ["PgrData_Locked"] = "Verrouillé",
        ["PgrData_FullIn"] = "Recharge complète dans {0}",

        
        ["KuroBbs_DataTerminal"] = "Terminal de données",
        ["KuroBbs_CombatData"] = "Données de combat",
        ["DataTerminal_NoAccount"] = "Vous n'êtes pas encore connecté à Kuro BBS. Appuyez sur la flèche de compte ci-dessus pour vous connecter.",
        ["DataTerminal_NoRole"] = "Ce compte Kuro BBS n'a pas encore lié de personnage {0}. Liez-en un d'abord dans l'application Kuro BBS.",
        ["DataTerminal_LoadFailed"] = "Échec du chargement de la page : {0}",

        
        ["KuroBbs_PgrRole"] = "Personnages",
        ["KuroBbs_PgrResource"] = "Rapport des ressources",
        ["KuroBbs_PgrCage"] = "Cage de la Douleur",
        ["KuroBbs_PgrWarArea"] = "Zone de guerre",
        ["KuroBbs_PgrTransfinite"] = "Cartographie transfinie",
        ["PgrRole_Fight"] = "Puissance de combat {0}",
        ["PgrResource_Total"] = "6 derniers mois",
        ["PgrResource_BlackCard"] = "Cartes noires",
        ["PgrResource_Develop"] = "Ressources de R&D",
        ["PgrResource_Trade"] = "Bons d'échange",
        ["PgrResource_SelectPeriod"] = "Choisir la période",
        ["PgrChallenge_NotOpen"] = "Hors période d'ouverture",
        ["PgrChallenge_Locked"] = "Pas encore déverrouillé",
        ["PgrChallenge_NoArea"] = "Aucune zone sélectionnée",
        ["PgrChallenge_NoRecord"] = "Aucun relevé de défi pour cette période",
        ["PgrChallenge_Level"] = "Plage de niveau {0}",
        ["PgrWarArea_TotalPointLabel"] = "Score total",
        ["PgrWarArea_TotalTimesLabel"] = "Tentatives",
        ["PgrWarArea_Waves"] = "Vagues {0}",
        ["PgrCommon_LoadFailed"] = "Échec de la récupération des données",

        
        ["DailyBriefing"] = "Briefing",
        ["DailyBriefing_Desc"] = "Données quotidiennes du jeu",
        ["DailyBriefing_Refresh"] = "Actualiser",
        ["DailyBriefing_Error"] = "Échec de la récupération des données du briefing",
        ["DailyBriefing_NoGameAccount"] = "Aucun compte connecté trouvé. Veuillez vous connecter puis réessayer.",
        ["DailyBriefing_NoRole"] = "Aucun personnage n'a encore été identifié pour ce compte. Lance le jeu une fois (les infos du personnage sont résolues en ligne via le SDK officiel).",
        ["Briefing_Liveness"] = "Activité quotidienne",
        ["Briefing_StoreEnergy"] = "Plaques d'onde stockées",
        ["Briefing_BattlePass"] = "Podcast des pionniers",
        ["Briefing_ActionPoint"] = "Sérum",
        ["Briefing_Level"] = "Niveau",
        ["KuroBbs_TowerOfAdversity"] = "Tour de l'Adversité",
        ["KuroBbs_SongOfVoidSea"] = "Chant de la mer du Vide",
        ["KuroBbs_DoomsdayMatrix"] = "Matrice de l'Apocalypse",
        ["KuroBbs_ComingSoon"] = "{0} en cours de développement",
        ["KuroBbs_TokenExpired"] = "Connexion Kuro BBS expirée",
        ["KuroBbs_TokenExpiredDesc"] = "Allez dans Paramètres → Compte Kuro BBS pour recapturer ou ressaisir le token",

        
        ["KuroAccount_UsbTitle"] = "Outil ADB",
        ["KuroAccount_UsbShortDesc"] = "Connecter un téléphone Android pour capturer le Token et le Did de Kuro BBS",
        ["KuroAccount_UsbDesc"] = "Connectez le téléphone par câble USB et activez le débogage USB : ouvrez l'application Kuro BBS sur le téléphone et accédez à la page « Terminal de données » ou « Calculateur de développement », puis appuyez sur « Démarrer la capture » ; l'outil capture automatiquement le token et le did. Nécessite adb.exe (téléchargeable depuis la ",
        ["KuroAccount_UsbDescTail"] = " page officielle).",
        ["KuroAccount_UsbVerWarn"] = "⚠ La nouvelle version de l'application Kuro BBS ne prend plus en charge cette méthode de capture ; veuillez utiliser une version antérieure.",
        ["KuroAccount_AdbPlaceholder"] = "Chemin d'adb.exe (laisser vide pour utiliser le PATH)",
        ["KuroAccount_ApplyAdb"] = "Appliquer",
        ["KuroAccount_SelectDevice"] = "Sélectionner l'appareil",
        ["KuroAccount_RefreshDevices"] = "Actualiser",
        ["KuroAccount_StartCapture"] = "Démarrer la capture",
        ["KuroAccount_StopCapture"] = "Arrêter la capture",
        ["KuroAccount_FieldsTitle"] = "Infos du compte",
        ["KuroAccount_FillHint"] = "Les résultats de capture se remplissent automatiquement ; vous pouvez aussi les coller manuellement",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "Enregistrer",
        ["KuroAccount_Clear"] = "Effacer",
        ["KuroAccount_Log"] = "Journal de capture",
        ["KuroAccount_AdbInvalidTitle"] = "Chemin ADB invalide",
        ["KuroAccount_AdbInvalidDesc"] = "Saisissez un chemin complet valide vers adb.exe",
        ["KuroAccount_AdbApplied"] = "Chemin ADB appliqué",
        ["KuroAccount_AdbApplyFailed"] = "Échec de l'application du chemin ADB",
        ["KuroAccount_NoDevice"] = "Aucun appareil Android détecté",
        ["KuroAccount_NoDeviceDesc"] = "Connectez le téléphone et autorisez le débogage USB",
        ["KuroAccount_RefreshDevicesFailed"] = "Échec de l'actualisation des appareils",
        ["KuroAccount_SelectDeviceFirst"] = "Sélectionnez d'abord un appareil",
        ["KuroAccount_SelectDeviceFirstDesc"] = "Appuyez d'abord sur « Actualiser », puis sélectionnez un appareil",
        ["KuroAccount_NoPage"] = "Aucune page web capturable trouvée",
        ["KuroAccount_NoPageDesc"] = "Ouvrez d'abord une page web telle que « Terminal de données » sur le téléphone, puis appuyez sur « Démarrer la capture »",
        ["KuroAccount_CaptureFailed"] = "Échec de la connexion de capture",
        ["KuroAccount_CaptureStarted"] = "Surveillance démarrée",
        ["KuroAccount_CaptureStartedDesc"] = "Ouvrez la page « Terminal de données » ou « Calculateur de développement » sur le téléphone",
        ["KuroAccount_Disconnected"] = "Téléphone déconnecté",
        ["KuroAccount_DisconnectedDesc"] = "La page WebView a peut-être été fermée. Appuyez à nouveau sur « Démarrer la capture ».",
        ["KuroAccount_NoSocket"] = "Interface de débogage WebView introuvable",
        ["KuroAccount_NoSocketDesc"] = "Cette version de Kuro BBS a peut-être désactivé le débogage à distance WebView. Consultez le rapport de diagnostic pour plus de détails.",
        ["KuroAccount_DiagTitle"] = "Rapport de diagnostic de capture",
        ["KuroAccount_DiagHint"] = "Si le rapport indique 0 socket devtools, cette version de l'application Kuro BBS n'active plus le débogage à distance WebView (invisible non plus depuis chrome://inspect de Chrome) ; la capture via ADB est donc impossible. Utilisez alors la connexion par numéro de téléphone + code SMS dans « Kuro BBS → Compte → Connexion » pour obtenir le Token et le Did, sans téléphone ni ADB.",
        ["KuroAccount_DiagCopy"] = "Copier le rapport",
        ["KuroAccount_DiagClose"] = "Fermer",
        ["KuroAccount_DiagCopied"] = "Rapport de diagnostic copié",
        ["KuroAccount_Invalid"] = "Informations incomplètes",
        ["KuroAccount_SaveNeedTokenDid"] = "Le Token et le Did sont obligatoires ; le roleId peut être récupéré automatiquement",
        ["KuroAccount_Saved"] = "Compte enregistré",
        ["KuroAccount_Cleared"] = "Compte effacé",

        
        ["RolePage_Refresh"] = "Actualiser",
        ["RolePage_NoAccount"] = "Compte Kuro BBS pas encore configuré",
        ["RolePage_NoAccountDesc"] = "Terminez la configuration dans Paramètres → Compte Kuro BBS, puis actualisez",
        ["RolePage_LoadFailed"] = "Échec du chargement du personnage",
        ["RolePage_Back"] = "Retour à la liste",
        ["RolePage_Phantoms"] = "Échos",

        
        ["GachaLogPage_Refresh"] = "Mettre à jour les relevés",
        ["GachaLogPage_ImportUrl"] = "Importer via un lien",
        ["GachaLogPage_ImportUrlDesc"] = "Collez le lien de la page de détails de convocation (ouvrez « Détails de convocation » en jeu puis récupérez-le depuis le journal, ou copiez-le depuis d'autres outils). Fonctionne aussi avec les téléphones cloud.",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "Lien invalide : collez le lien complet de la page de détails de convocation contenant record_id",
        ["GachaLogPage_SelectPlayer"] = "Sélectionner un compte",
        ["GachaLogPage_CopyUrl"] = "Copier l'URL",
        ["GachaLogPage_DeletePlayer"] = "Supprimer les enregistrements",
        ["GachaLogPage_Delete"] = "Supprimer",
        ["GachaLogPage_Avg5"] = "Moy.",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "Stats",
        ["GachaLogPage_Times"] = "invocations",
        ["GachaLogPage_ItemStats"] = "Stats des objets",
        ["GachaLogPage_UpdateSavedUrl"] = "Mettre à jour (URL enregistrée)",
        ["GachaLogPage_UpdateAll"] = "Mettre à jour (tout)",
        ["GachaLogPage_InputUrl"] = "Saisir l'URL",
        ["GachaLogPage_ViaCloudGame"] = "Obtenir via le jeu cloud",
        ["CloudGame_Title"] = "Wuthering Waves Cloud - Connectez-vous et récupérez le lien de convocation en un clic",
        ["CloudGame_Grab"] = "Obtenir le lien de convocation",
        ["CloudGame_StatusDefault"] = "Connectez-vous ici avec votre compte Kuro, appuyez sur « Entrer dans le jeu » et ouvrez une fois la page « Détails de convocation », puis appuyez sur « Obtenir le lien de convocation »",
        ["CloudGame_StatusSessionReady"] = "Connexion au jeu cloud détectée. Appuyez sur « Obtenir le lien de convocation » pour terminer la synchronisation.",
        ["CloudGame_StatusGrabbing"] = "Lecture des informations de connexion…",
        ["CloudGame_StatusOk"] = "Récupération réussie (joueur {0}). Synchronisation des registres de convocation…",
        ["CloudGame_StatusFailReason"] = "Échec : {0}",
        ["CloudGame_StatusNoRuntime"] = "Échec de l'initialisation du navigateur intégré : le runtime WebView2 n'est pas installé. Installez-le depuis le site de Microsoft puis réessayez",
        ["GachaLogPage_ImportExport"] = "Importer & exporter",
        ["GachaLogPage_HidePools"] = "Masquer les bannières",
        ["GachaLogPage_ExportAs"] = "Exporter en tant que",
        ["GachaLogPage_ImportRecords"] = "Importer des relevés (choisir un fichier)",
        ["GachaLogPage_ImportOk"] = "Import réussi : joueur {0}, {1} relevés (doublons supprimés automatiquement)",
        ["GachaLogPage_ImportFail"] = "Échec de l'import : {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "Le fichier ne contient aucune info de compte et aucun compte n'est sélectionné : impossible de déterminer son propriétaire",
        ["GachaLogPage_OtherSettings"] = "Autres paramètres",
        ["GachaLogPage_LinkHelp"] = "Toujours « Authkey Timeout » ?",
        ["GachaLogPage_LinkHelpDesc"] = "Supprimez le dossier de cache puis relancez le jeu",
        ["GachaLogPage_ClearSavedUrl"] = "Supprimer le dossier de cache",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "Supprimer le lien de convocation enregistré localement ? (Les relevés déjà récupérés ne seront pas supprimés.)",
        ["GachaLogPage_ClearSavedUrlDone"] = "Lien de convocation enregistré effacé. Rouvrez la page « Détails de convocation » en jeu.",
        ["GachaLogPage_Language"] = "Langue",
        ["GachaLogPage_LanguageSaved"] = "Langue enregistrée. Elle prendra effet lors de la prochaine mise à jour des relevés.",
        ["GachaLogPage_LanguageApplied"] = "{0} relevés renommés en {1}",
        ["GachaLogPage_NoSavedUrl"] = "Aucun lien enregistré. Exécutez d'abord « Mettre à jour (tout) ».",
        ["GachaLogPage_LinkExpired"] = "Lien de convocation expiré",
        ["GachaLogPage_LinkExpiredDesc"] = "Le lien a expiré (l'API a renvoyé {0}). Rouvrez « Détails de convocation » en jeu.",
        ["GachaLogPage_UpdateFailed"] = "Échec de la mise à jour des registres de convocation : {0}",
        ["GachaLogPage_FetchFailed"] = "Échec de la récupération des données de convocation. Réessayez.",
        ["GachaLogPage_LogNotFound"] = "Journal du jeu ou lien de convocation introuvable : indiquez le jeu (Wuthering Waves.exe) dans les paramètres et ouvrez une fois « Détails de convocation » en jeu, ou choisissez « Importer via un lien » dans le menu « Mettre à jour les relevés »",
        ["GachaLogPage_UrlNotFound"] = "Lien de convocation introuvable. Ouvrez une fois la page « Détails de convocation » en jeu.",
        ["GachaLogPage_UrlCopied"] = "Lien de convocation copié dans le presse-papiers",
        ["GachaLogPage_PlayerDeleted"] = "Les enregistrements de convocation locaux de l'UID {0} ont été supprimés",
        ["GachaLogPage_DeleteConfirmText"] = "Tous les enregistrements de convocation locaux de l'UID {0} seront supprimés. Cette action est irréversible.",
        ["GachaLogPage_EmptyTitle"] = "Aucun relevé de convocation",
        ["GachaLogPage_EmptyDesc"] = "Indiquez le dossier du jeu dans les paramètres, ouvrez une fois la page « Détails de convocation » en jeu, puis revenez ici et appuyez sur « Mettre à jour les relevés » pour synchroniser.",

        
        ["LauncherPage_StartGame"] = "Lancer le jeu",
        ["LauncherPage_InstallGame"] = "Installer le jeu",
        ["AccountSwitcher_SwitchAccount"] = "Changer de compte",
        ["AccountSwitcher_GameRunning"] = "Le jeu est en cours d'exécution ; changement de compte indisponible pour le moment",
        ["AccountSwitcher_AccountInfo"] = "Compte de jeu",
        ["AccountSwitcher_Nickname"] = "Pseudonyme",
        ["AccountSwitcher_RoleId"] = "UID du jeu",
        ["AccountSwitcher_Save"] = "Enregistrer",
        ["AccountSwitcher_Delete"] = "Supprimer",
        ["AccountSwitcher_Unsaved"] = "Le compte actuellement connecté n'est pas enregistré",
        ["Notice_Activity"] = "Événements",
        ["Notice_NoticeTab"] = "Annonces",
        ["Notice_News"] = "Actualités",
        ["Notice_LoadFailed"] = "Échec du chargement des annonces",
        ["LauncherPage_UpdateGame"] = "Mettre à jour le jeu",

        
        ["SettingPage_Language"] = "Langue",

        
        ["GameUpdate_PredownloadAvailable"] = "Pré-téléchargement de la version {0} disponible",
        ["GameUpdate_Predownloaded"] = "{0} pré-téléchargé ; il pourra être installé directement dès l'ouverture de la mise à jour",
        ["GameUpdate_PredownloadButton"] = "Pré-télécharger",
        ["GameUpdate_ConfirmPredownload"] = "Pré-télécharger la version {0} ? Téléchargement total : {1}. Les fichiers sont stockés dans le dossier predownload du dossier du jeu ; lors de l'application de la mise à jour officielle, aucun nouveau téléchargement n'est nécessaire.",
        ["GameUpdate_Applying"] = "Application de la mise à jour…",
        ["GameUpdate_Predownloading"] = "Pré-téléchargement de {0}…",

        ["GameUpdate_Done"] = "Mis à jour vers {0}",
        ["GameUpdate_PredownloadDone"] = "Pré-téléchargement de {0} terminé ; il pourra être installé directement dès l'ouverture de la mise à jour",
        ["GameUpdate_NoGameDir"] = "Indiquez d'abord le jeu dans les paramètres",
        ["GameUpdate_Verifying"] = "Vérification {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "Vérification terminée",
        ["GameUpdate_VerifyDoneNone"] = "Aucun fichier corrompu ou manquant détecté",
        ["GameUpdate_VerifyDoneDamage"] = "{0} fichiers corrompus / manquants détectés sur {1}",
        ["GameUpdate_VerifyNoManifest"] = "Impossible d'obtenir le manifeste de vérification. Vérifiez le réseau ou réessayez plus tard.",
        ["GameUpdate_Repairing"] = "Réparation…",
        ["GameUpdate_RepairDone"] = "{0} fichiers réparés",
        ["GameUpdate_RepairNoBaseUrl"] = "La version actuelle ne prend pas en charge la réparation en ligne. Mettez d'abord le jeu à jour vers la dernière version.",

        
        ["GameLauncherPage_Gallery"] = "Galerie",
        ["Common_Copy"] = "Copier",
        ["ImageViewWindow2_SaveAs"] = "Enregistrer sous",
        ["GameLauncherPage_PlayPause"] = "Lecture/Pause",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "Déposez des fichiers image pour changer le fond",
        ["Background_Official"] = "Fond animé officiel",
        ["Background_OfficialStatic"] = "Fond statique officiel",
        ["Background_OpenFolder"] = "Ouvrir le dossier des fonds",
        ["Background_AddFailed"] = "Ce fichier de fond ne peut pas être utilisé",
        ["InstallGameDialog_LocateGame"] = "Localiser le jeu",
        ["GameLauncherPage_GameIsRunning"] = "Le jeu est en cours d'exécution",

        
        ["SettingPage_Download"] = "Télécharger",

        
        ["DownloadGamePage_Downloading"] = "Téléchargement",
        ["DownloadGamePage_Verifying"] = "Vérification",
        ["DownloadGamePage_Pause"] = "Pause",

        
        ["Common_Confirm"] = "Confirmer",
        ["Common_Cancel"] = "Annuler",
        ["Common_Error"] = "Erreur",
        ["Common_Ok"] = "OK",
        ["Common_Select"] = "Sélectionner",
        ["Common_Apply"] = "Appliquer",
        ["Common_Exit"] = "Quitter",

        
        ["SettingPage_AppSettings"] = "Paramètres",
        ["SettingPage_About"] = "À propos",
        ["SettingPage_General"] = "Général",
        ["SettingPage_FileManagement"] = "Gestion des fichiers",

        
        ["FilePage_DataFolder"] = "Dossier de données",
        ["FilePage_DataFolderDesc"] = "Vos données personnelles sont stockées dans le dossier ci-dessous. Elles sont importantes — ne les supprimez pas à la légère.",
        ["FilePage_ReselectDataFolder"] = "Resélectionner le dossier de données",
        ["FilePage_OpenDataFolder"] = "Ouvrir le dossier de données",
        ["FilePage_DeleteAllSettings"] = "Supprimer tous les paramètres",
        ["FilePage_ReselectFolderDone"] = "Nouveau dossier de données enregistré. Il prendra effet après redémarrage de l'application. Le nouveau dossier est vide au départ ; utilisez la « Synchronisation cloud » pour récupérer vos anciennes données.",
        ["FilePage_DeleteSettingsDone"] = "Paramètres supprimés. Ils prendront effet après redémarrage de l'application.",
        ["FilePage_DeleteSettingsConfirm"] = "Tous les paramètres (config.ini) seront supprimés et les valeurs par défaut rétablies après redémarrage. Les données de la base (temps de jeu, registres de convocation…) ne sont pas affectées. Continuer ?",
        ["FilePage_OpenLogFolder"] = "Ouvrir le dossier des journaux",
        ["FilePage_Cache"] = "Cache",
        ["FilePage_CacheLog"] = "Journaux",
        ["FilePage_CacheImage"] = "Images",
        ["FilePage_CacheBackground"] = "Fond du lanceur",
        ["FilePage_CleanCache"] = "Nettoyer le cache",
        ["FilePage_CleanBackgroundCache"] = "Nettoyer les fichiers de fond du lanceur",

        
        ["FilePage_HardLink"] = "Liens matériels",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "Les fichiers identiques d’autres clients de région sur le même disque seront réutilisés par liens physiques. Téléchargement estimé : environ {0}. Valeur indicative ; la vérification fichier par fichier lors de l’installation fait foi.",
        ["GameUpdate_ResumeInstall"] = "Installation inachevée de {0} détectée, reprise depuis le répertoire précédent",
["FilePage_HardLinkDesc"] = "Lorsque plusieurs clients sont installés sur le même disque, les fichiers identiques sont liés physiquement pour réduire l’espace utilisé",
        ["FilePage_Enabled"] = "Activé",
        ["FilePage_Disabled"] = "Désactivé",
        ["FilePage_SpeedLimit"] = "Limitation de vitesse",
        ["FilePage_SpeedLimitDesc"] = "0 signifie aucune limite",
        ["FilePage_DownloadThreads"] = "Multithreading",
        ["FilePage_DownloadThreadsDesc"] = "Nombre de connexions de téléchargement simultanées (1-16). Une valeur élevée exploite mieux la bande passante, au prix d'une charge CPU, mémoire et disque légèrement accrue. Les changements prennent effet au prochain téléchargement.",

        
        ["KuroBbs_RefreshInfo"] = "Actualiser les infos",
        ["KuroBbs_Login"] = "Se connecter",
        ["KuroBbs_Settings"] = "Paramètres de Kuro BBS",

        
        ["Login_Title"] = "Connexion à Kuro BBS",
        ["Login_ByToken"] = "Connexion par Token",
        ["Login_BySms"] = "Connexion par code SMS",
        ["Login_TokenPlaceholder"] = "Collez le token de connexion Kuro BBS",
        ["Login_DidPlaceholder"] = "Did capturé avec le Token (laisser vide pour générer automatiquement)",
        ["Login_TokenTip"] = "Le Token et le Did sont capturés depuis l'application mobile Kuro BBS (Boîte à outils → Capture USB) et sauvegardés par paire",
        ["Login_PhonePlaceholder"] = "Numéro de téléphone",
        ["Login_CodePlaceholder"] = "Code SMS",
        ["Login_CodeLabel"] = "Code SMS",
        ["Login_SmsTip"] = "Appuyez sur « Envoyer le code », puis saisissez le code reçu par SMS. Le compte est enregistré automatiquement après une connexion réussie.",
        ["Login_SendCode"] = "Envoyer le code",
        ["Login_Confirm"] = "Se connecter",
        ["Login_Cancel"] = "Annuler",
        ["Login_PhoneInvalid"] = "Format de numéro de téléphone invalide",
        ["Login_Sending"] = "Envoi du code SMS…",
        ["Login_CodeSent"] = "Code envoyé. Consultez vos SMS.",
        ["Login_CodeSendFailed"] = "Échec de l'envoi du code : {0}",
        ["Login_NeedCode"] = "Saisissez le code SMS",
        ["Login_TokenInvalid"] = "Token invalide : collez le token de connexion Kuro BBS complet",
        ["Login_Working"] = "Connexion…",
        ["Login_Failed"] = "Échec de la connexion : {0}",
        ["Login_SeekRoleFailed"] = "Échec de la recherche du personnage lié : {0}",
        ["Login_Success"] = "Compte Kuro BBS « {0} » connecté",

        
        ["ResourceBrief_Month"] = "Mensuel",
        ["ResourceBrief_Week"] = "Hebdomadaire",
        ["ResourceBrief_Version"] = "Version",
        ["ResourceBrief_HistoryData"] = "Historique",
        ["ResourceBrief_TotalGain"] = "Gagné cette période",
        ["ResourceBrief_StarComposition"] = "Répartition des revenus d'Astrite",
        ["ResourceBrief_CoinComposition"] = "Répartition des revenus de crédits coquillage",
        ["ResourceBrief_Detail"] = "Détails",
        ["ResourceBrief_Empty"] = "Aucune donnée pour cette période",

        
        ["SignPage_DoSignIn"] = "Pointer",
        ["SignPage_MonthlySign"] = "Ce mois-ci",
        ["SignPage_SignedToday"] = "Présence enregistrée aujourd'hui",
        ["SignPage_NotSignedToday"] = "Pas encore de présence aujourd'hui",
        ["SignPage_Done"] = "Présence enregistrée",
        ["SignPage_AutoSign"] = "Présence automatique",
        ["SignPage_SignedDaysFormat"] = "{0} jours de présence ce mois-ci",
        ["SignPage_DayFormat"] = "Jour {0}",
        ["SignPage_Energy"] = "Ondes cristallisées",
        ["TowerPage_TotalStarCount"] = "Total d'étoiles : {0} / {1}",
        ["TowerPage_Floor"] = "Étage {0}",
        ["TowerPage_NoChallengeData"] = "Aucune donnée de défi",
        ["TowerPage_RefreshAfter"] = "Actualisation dans {0} j {1} h",
        ["MatrixPage_NoRecord"] = "Aucun relevé pour cette période",
        ["CombatPage_Locked"] = "{0} est verrouillé cette période ou n'a pas de données",
        ["CombatPage_NoData"] = "Aucune donnée : le serveur n'a pas encore généré les relevés de cette période. Ouvrez la page « {0} » dans l'application Kuro BBS pour actualiser les données, puis réessayez.",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "Temps de jeu total",
        ["PlayTimeStatsDialog_Started0Times"] = "Lancé {0} fois",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "Temps de jeu quotidien moyen",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "Joué pendant {0} jours",
        ["PlayTimeStatsDialog_LongestStreak"] = "Plus longue série",
        ["PlayTimeStatsDialog_0Days"] = "{0} jours",
        ["PlayTimeStatsDialog_LongestSession"] = "Plus longue session",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "Journée de jeu la plus longue",
        ["PlayTimeButton_LastStartup"] = "Dernière session",
        ["PlayTimeStatsDialog_Last15Days"] = "15 derniers jours",
        ["PlayTimeStatsDialog_Last12Weeks"] = "12 dernières semaines",
        ["PlayTimeStatsDialog_Last12Months"] = "12 derniers mois",
        ["PlayTimeStatsDialog_Total"] = "Total",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "Statistiques de temps de jeu",
        ["PlayTimeStatsDialog_StatsTip"] = "Les statistiques ne comptent que les sessions lancées depuis ce lanceur.",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "Réduire dans la zone de notification",
        ["GameLauncherSettingDialog_BasicInformation"] = "Informations de base",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "Confirmer la désinstallation",
        ["GameLauncherSettingDialog_CustomBackground"] = "Fond",
        ["GameLauncherSettingDialog_RelocateGame"] = "Relocaliser le jeu",
        ["GameLauncherSettingDialog_Size"] = "Taille",
        ["GameLauncherSettingDialog_StartRepairing"] = "Démarrer la réparation",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "Avertissement de désinstallation",
        ["LauncherPage_RepairGame"] = "Réparer le jeu",
        ["LauncherPage_ShowGameAnnouncement"] = "Afficher les annonces du jeu",
        ["LauncherPage_UninstallGame"] = "Désinstaller le jeu",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "Les lecteurs réseau mappés ne sont pas pris en charge. Utilisez un chemin de partage réseau commençant par une double barre oblique inverse.",

        
        ["SettingPage_BgSizeTip"] = "Conseil sur la taille du fond",
        ["SettingPage_CustomBackgroundSupportVideo"] = "Le fond personnalisé prend en charge la vidéo",

        
        ["GameSettingPage_Resolution"] = "Résolution",
        ["GameSettingPage_FullScreen"] = "Plein écran",
        ["GameSettingPage_UsePopupWindow"] = "Fenêtre sans bordure",
        ["GameSettingPage_CustomResolution"] = "Résolution personnalisée",
        ["GameSettingPage_CustomResolutionTip"] = "La résolution personnalisée s'applique en mode « Fenêtré »",
        ["GameSettingPage_VoiceLanguage"] = "Langue vocale",
        ["GameSettingPage_VoiceLanguageDescription"] = "Définissez ceci avant le premier lancement du jeu pour éviter de télécharger certains fichiers vocaux",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "Activer le HDR",
        ["GameSettingPage_HDRNotSupported"] = "L'écran actuel ne prend pas en charge le HDR",
        ["GameSettingPage_HDRNotEnabled"] = "Le HDR Windows n'est pas encore activé ; cela pourrait ne pas prendre effet",
        ["GameSettingPage_GameNotInstalled"] = "Jeu non installé",
        ["GameSettingPage_GameNotInstalledDesc"] = "Définissez d'abord le chemin du jeu sur la page du lanceur",
        ["GameSettingPage_CloudGameNotSupport"] = "Le jeu cloud n'a pas besoin de paramètres de jeu locaux",
        ["GameSettingPage_SettingNotEffect"] = "Les paramètres appliqués pendant que le jeu tourne ne prendront pas effet",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "La résolution doit être supérieure à 0",
        ["GameSettingPage_LoadFailed"] = "Échec de la lecture des paramètres du jeu",
        ["GameSettingPage_ApplyFailed"] = "Échec de l'application des paramètres du jeu",
        ["GameSettingPage_Applied"] = "Appliqué. Prendra effet au prochain lancement du jeu.",

        

        
        ["SettingPage_WindowBehavior"] = "Comportement de la fenêtre",
        ["SettingPage_CloseWindowOption"] = "À la fermeture de la fenêtre",
        ["SettingPage_ExitApp"] = "Quitter l'application",
        ["SettingPage_AfterLaunchOption"] = "Après le lancement du jeu",
        ["SettingPage_KeepWindow"] = "Garder la fenêtre",
        ["SettingPage_AfterGameExitOption"] = "Après la fermeture du jeu",
        ["SettingPage_RestoreMainWindow"] = "Afficher la fenêtre principale",
        ["SettingPage_KeepHidden"] = "Garder masqué",
        ["SettingPage_CloseLauncher"] = "Fermer le launcher",
        ["SettingPage_KuroCommunitySign"] = "Présence communautaire Kuro BBS (pièces Kuro)",
        ["SettingPage_SuggestSystemFeatures"] = "Pour une meilleure expérience, activez les fonctions suivantes dans les paramètres système.",
        ["SettingPage_TransparencyEffects"] = "Effets de transparence",
        ["SettingPage_AnimationEffects"] = "Effets d'animation",

        
        ["GameSelector_Subtitle"] = "Appuyez sur l'icône en haut à gauche pour choisir ou changer de jeu",
        ["GameSelector_AutoFind"] = "Détecter les jeux installés",
        ["GameSelector_SelectServer"] = "Sélectionner le serveur",
        ["GameSelector_InstalledSize"] = "Espace disque réellement occupé par les jeux installés ",
        ["GameSelector_Calculating"] = "Calcul en cours",

        
        ["KuroBbs_AccountLevelLabel"] = "Niveau d'union ",
        ["KuroBbs_SwitchRole"] = "Changer de personnage",
        ["KuroBbs_ClearAccount"] = "Effacer le compte",

        
        ["GameBiz_Wuwa"] = "Wuthering Waves",
        ["GameBiz_WuwaCn"] = "Wuthering Waves · CN",
        ["GameBiz_WuwaGlobal"] = "Wuthering Waves · Global",
        ["GameBiz_WuwaBilibili"] = "Wuthering Waves · Bilibili",
        ["GameBiz_Pgr"] = "Punishing: Gray Raven",
        ["GameBiz_ShortCn"] = "Serveur Chine",
        ["GameBiz_ShortGlobal"] = "Serveur Global",
        ["GameBiz_ShortBilibili"] = "Bilibili",
        ["GameBiz_ShortPgr"] = "Serveur Chine",
        ["GameBiz_PgrGlobal"] = "Punishing: Gray Raven · Global",
        ["GameBiz_ShortGlobal"] = "Serveur Global",
        ["GameBiz_PgrKorea"] = "Punishing: Gray Raven · Korea",
        ["GameBiz_ShortKorea"] = "Serveur Corée",
        ["GameBiz_PgrJapan"] = "Punishing: Gray Raven · Japan",
        ["GameBiz_ShortJapan"] = "Serveur Japon",
        ["GameBiz_PgrTaiwan"] = "Punishing: Gray Raven · Taiwan",
        ["GameBiz_ShortTaiwan"] = "Serveur Taïwan",

        
        ["GachaPool_CharacterEvent"] = "Convocation d'événement de personnage",
        ["GachaPool_WeaponEvent"] = "Convocation d'événement d'arme",
        ["GachaPool_CharacterStandard"] = "Convocation standard de personnage",
        ["GachaPool_WeaponStandard"] = "Convocation standard d'arme",
        ["GachaPool_Novice"] = "Convocation de novice",
        ["GachaPool_NoviceChoice"] = "Convocation à choix de novice",
        ["GachaPool_NoviceGratitude"] = "Convocation à choix de novice (convocation de gratitude)",
        ["GachaPool_CharacterRookie"] = "Convocation de recrue de personnage",
        ["GachaPool_WeaponRookie"] = "Convocation de recrue d'arme",
        ["GachaPool_CharacterCollab"] = "Convocation de collaboration de personnage",
        ["GachaPool_WeaponCollab"] = "Convocation de collaboration d'arme",
        ["GachaPool_CharacterRevisit"] = "Convocation commémorative de personnage",
        ["GachaPool_WeaponRevisit"] = "Convocation commémorative d'arme",
        ["GachaPool_Unknown"] = "Bannière {0}",

        
        ["Adb_DeviceOnline"] = "en ligne",
        ["Adb_DeviceUnauthorized"] = "non autorisé",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "Sauvegardez vos données en les synchronisant avec le cloud",
        ["Sync_ServerUrl"] = "Adresse du serveur",
        ["Sync_UserName"] = "Nom d'utilisateur",
        ["Sync_Password"] = "Mot de passe ou code d'autorisation de l'appli",
        ["Sync_ServerEntry"] = "Serveur de synchronisation",
        ["Sync_EnableOff"] = "Désactivé — activez-le pour synchroniser automatiquement",
        ["Sync_EnableNeedServer"] = "Configurez d'abord le serveur",
        ["Sync_ConnectTitle"] = "Connecter WebDAV",
        ["Sync_ConnectDesc"] = "Saisissez les informations de connexion de votre cloud ou serveur.",
        ["Sync_ConnectInvalid"] = "Saisissez une adresse complète http:// ou https://",
        ["Sync_SaveAndTest"] = "Enregistrer et tester",
        ["Sync_Testing"] = "Test de la connexion…",
        ["Sync_ConnectOk"] = "Connecté. Paramètres enregistrés.",
        ["Sync_ConnectFail"] = "Échec de la connexion. Vérifiez l'adresse du serveur, le compte et le mot de passe, puis réessayez.",
        ["Sync_SyncNow"] = "Synchroniser maintenant",
        ["Sync_RevealPassword"] = "Afficher ou masquer le mot de passe",
        ["Sync_Configured"] = "Configuré",
        ["Sync_NotConfigured"] = "Non configuré",
        ["Sync_NeverSynced"] = "Jamais synchronisé",
        ["Sync_LastSync"] = "Dernière synchronisation",
        ["Sync_Working"] = "Traitement…",

        
        ["CloudGame_ErrNoLogin"] = "Aucune information de connexion utilisable",
        ["CloudGame_ErrHttp"] = "Le service de jeu cloud a renvoyé HTTP {0}",
        ["CloudGame_ErrRequest"] = "Échec de la requête au service de jeu cloud : {0}",
        ["CloudGame_ErrParse"] = "Le service de jeu cloud a renvoyé un contenu illisible : {0}",
        ["CloudGame_ErrEmpty"] = "Le service de jeu cloud a renvoyé un contenu vide : {0}",
        ["CloudGame_ErrCode"] = "L'API du jeu cloud a renvoyé l'erreur code={0} : {1}",

        
        ["GachaLogPage_ExportEmpty"] = "Le compte actuel n'a aucun relevé de convocation à exporter",
        ["GachaLog_PityBuilt"] = "Pitié",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "Configuration du chemin du jeu supprimée",
        ["GameLauncherSettingDialog_WrongServerExe"] = "L'exe du jeu sélectionné n'appartient pas au serveur actuel, veuillez choisir à nouveau",
        ["GameLauncherSettingDialog_ExpectExe"] = "Veuillez sélectionner l'exécutable principal du jeu : {0}",

        
        ["KrSdk_ErrNoUserCache"] = "KRSDKUserCache.json introuvable. Connectez-vous une fois dans le jeu d'abord.",
        ["KrSdk_ErrEmptyContent"] = "Contenu vide",
        ["KrSdk_ErrParseFailed"] = "Échec de l'analyse de KRSDKUserCache.json ; écriture abandonnée pour ne pas corrompre l'état de connexion.",
        ["KrSdk_ErrNoAccountList"] = "Aucun account_list dans KRSDKUserCache.json.",
        ["KrSdk_ErrAccountNotInCache"] = "Ce compte n'est pas dans le cache local.",
        ["KrSdk_ErrVerifyFailed"] = "La vérification après écriture a échoué ; le cache a été restauré dans son état précédent.",

        
        ["GameUpdate_ErrNoPlan"] = "Plan de téléchargement manquant",
        ["GameUpdate_ErrNoTarget"] = "Version cible manquante",
        ["GameUpdate_ErrNoGamePath"] = "Chemin du jeu non configuré ; mise à jour impossible",
        ["GameUpdate_ErrNoManifest"] = "Échec de la récupération du manifeste de téléchargement",
        ["GameUpdate_ErrFileMissing"] = "Fichier de mise à jour manquant : {0}",
        ["GameUpdate_ErrFileChecksum"] = "Échec de la somme de contrôle du fichier de mise à jour : {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "LocalStorage.db du jeu introuvable",
        ["GameSetting_ErrNoGameUserSettings"] = "GameUserSettings.ini du jeu introuvable",

        
        ["KuroBbs_ErrConnection"] = "Échec de la connexion, statut HTTP : {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken a renvoyé un accessToken vide",
        ["KuroBbs_ErrAccessToken"] = "Échec de l'obtention du B-At accessToken (code={0}) : {1}",
        ["KuroBbs_ErrSendCode"] = "Échec de l'envoi du code SMS",
        ["KuroBbs_ErrLogin"] = "Échec de la connexion",
        ["KuroBbs_SignOk"] = "Présence enregistrée",
        ["KuroBbs_SignAlready"] = "Présence déjà enregistrée aujourd'hui",
        ["KuroBbs_SignFail"] = "Échec de la présence",

        

        
        ["Ugf_ErrNoUid"] = "Le fichier UGF n'a pas l'uid du compte",
        ["Ugf_ErrUnknownFormat"] = "Format de fichier non reconnu : UGF ou JSON de relevés de bannière attendu",

        

        
        ["CombatPage_AreaFallback"] = "Zone {0}",

        
        ["KuroBbs_UnionLevel"] = "Niveau d'union",
        ["KuroBbs_CommanderLevel"] = "Niveau de commandant",
        ["KuroBbs_NoLevel"] = "Pas encore de niveau",
        ["KuroBbs_SelectRolePgr"] = "Sélectionner un personnage PGR",
        ["KuroBbs_SelectRoleWuwa"] = "Sélectionner un personnage Wuthering Waves",
        ["KuroBbs_SwitchedPgr"] = "Personnage PGR changé",
        ["KuroBbs_SwitchedWuwa"] = "Personnage Wuthering Waves changé",
        ["PgrData_DailySupply"] = "Ravitaillement quotidien",
        ["KuroBbs_AutoSignDoneToast"] = "Check-in quotidien terminé",
        ["KuroBbs_AutoSignFailedToast"] = "Échec du check-in quotidien",

        
        ["RolePage_WeaponLevel"] = "NV.{0} · Ascension {1}",

        
        ["Common_UnknownError"] = "Erreur inconnue",
    };

}
