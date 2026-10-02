using System.Collections.Generic;

namespace Arietta.Localization;








internal static class LangPtBr
{

    public static readonly Dictionary<string, string> Map = new()
    {
        
        ["MainPage_Launcer"] = "Launcher",
        ["LauncherPage_GameSetting"] = "Configurações do jogo",
        ["MainPage_GameScreenshot"] = "Capturas de tela",
        ["GachaLogPage_ConveneRecords"] = "Registros de convocação",
        ["Toolbox"] = "Ferramentas",

        
        ["ScreenshotPage_MultiSelect"] = "Seleção múltipla",
        ["ScreenshotPage_OpenFolder"] = "Abrir pasta",
        ["ScreenshotPage_Total0Files"] = "{0} itens",
        ["ScreenshotPage_Selected0Of1"] = "{0} / {1} selecionados",
        ["ScreenshotPage_Empty"] = "Pasta de capturas não encontrada",
        ["ScreenshotPage_EmptyDesc"] = "As capturas são salvas por padrão em <Pasta do jogo>\\Client\\Saved\\Screenshots. Defina antes a pasta do jogo nas configurações.",
        ["ScreenshotPage_Delete"] = "Excluir",
        ["ScreenshotPage_DeleteConfirm"] = "Mover as {0} capturas selecionadas para a Lixeira?",
        ["ScreenshotPage_Deleted"] = "{0} capturas movidas para a Lixeira",
        ["ScreenshotPage_DeleteFailed"] = "Falha ao excluir: alguns arquivos podem estar em uso por outro programa",
        ["ScreenshotPage_SelectFirst"] = "Selecione antes as capturas que deseja excluir",
        ["ScreenshotPage_OpenImage"] = "Abrir imagem",
        ["ScreenshotPage_DeletedPartial"] = "{0} / {1} excluídas; o restante pode estar em uso",

        
        ["KuroBbs"] = "Kuro BBS",
        ["KuroBbs_SignIn"] = "Check-in",
        ["KuroBbs_Role"] = "Personagens",
        ["KuroBbs_ResourceBrief"] = "Resumo de recursos",

        
        ["PgrData_NoRole"] = "Esta conta do Kuro BBS ainda não definiu um personagem de PGR. Defina um em \"Eu\" no app do Kuro BBS e depois verifique aqui de novo.",
        ["PgrData_Locked"] = "Bloqueado",
        ["PgrData_FullIn"] = "Cheio em {0}",

        
        ["KuroBbs_DataTerminal"] = "Terminal de Dados",
        ["KuroBbs_CombatData"] = "Dados de combate",
        ["DataTerminal_NoAccount"] = "Você ainda não entrou no Kuro BBS. Toque na seta de conta acima para entrar.",
        ["DataTerminal_NoRole"] = "Esta conta do Kuro BBS ainda não vinculou um personagem de {0}. Vincule um antes no app do Kuro BBS.",
        ["DataTerminal_LoadFailed"] = "Falha ao carregar a página: {0}",

        
        ["KuroBbs_PgrRole"] = "Personagens",
        ["KuroBbs_PgrResource"] = "Relatório de recursos",
        ["KuroBbs_PgrCage"] = "Jaula da Dor",
        ["KuroBbs_PgrWarArea"] = "Zona de Guerra",
        ["KuroBbs_PgrTransfinite"] = "Mapeamento Transfinito",
        ["PgrRole_Fight"] = "Poder de combate {0}",
        ["PgrResource_Total"] = "Últimos 6 meses",
        ["PgrResource_BlackCard"] = "Cartões Pretos",
        ["PgrResource_Develop"] = "Recursos de P&D",
        ["PgrResource_Trade"] = "Vales de comércio",
        ["PgrResource_SelectPeriod"] = "Selecionar período",
        ["PgrChallenge_NotOpen"] = "Fora do período de abertura",
        ["PgrChallenge_Locked"] = "Ainda não desbloqueado",
        ["PgrChallenge_NoArea"] = "Nenhuma zona selecionada",
        ["PgrChallenge_NoRecord"] = "Nenhum registro de desafio neste período",
        ["PgrChallenge_Level"] = "Faixa de nível {0}",
        ["PgrWarArea_TotalPointLabel"] = "Pontuação total",
        ["PgrWarArea_TotalTimesLabel"] = "Tentativas",
        ["PgrWarArea_Waves"] = "Ondas {0}",
        ["PgrCommon_LoadFailed"] = "Falha ao obter os dados",

        
        ["DailyBriefing"] = "Briefing",
        ["DailyBriefing_Desc"] = "Dados diários do jogo",
        ["DailyBriefing_Refresh"] = "Atualizar",
        ["DailyBriefing_Error"] = "Falha ao obter os dados do briefing",
        ["DailyBriefing_NoGameAccount"] = "Nenhuma conta conectada encontrada. Faça login e tente novamente.",
        ["DailyBriefing_NoRole"] = "Nenhum personagem foi identificado para esta conta ainda. Inicie o jogo uma vez (as informações do personagem são obtidas online pelo SDK oficial).",
        ["Briefing_Liveness"] = "Atividade diária",
        ["Briefing_StoreEnergy"] = "Placas de onda armazenadas",
        ["Briefing_BattlePass"] = "Podcast pioneiro",
        ["Briefing_ActionPoint"] = "Soro",
        ["Briefing_Level"] = "Nível",
        ["KuroBbs_TowerOfAdversity"] = "Torre da Adversidade",
        ["KuroBbs_SongOfVoidSea"] = "Canção do Mar Vazio",
        ["KuroBbs_DoomsdayMatrix"] = "Matriz do Apocalipse",
        ["KuroBbs_ComingSoon"] = "{0} em desenvolvimento",
        ["KuroBbs_TokenExpired"] = "Login do Kuro BBS expirou",
        ["KuroBbs_TokenExpiredDesc"] = "Vá para Configurações → Conta do Kuro BBS para capturar ou digitar o token novamente",

        
        ["KuroAccount_UsbTitle"] = "Ferramenta ADB",
        ["KuroAccount_UsbShortDesc"] = "Conecte um celular Android para capturar o Token e o Did do Kuro BBS",
        ["KuroAccount_UsbDesc"] = "Conecte o celular pelo cabo USB e ative a depuração USB: abra o app do Kuro BBS no celular e entre na página \"Terminal de Dados\" ou \"Calculadora de Crescimento\", depois toque em \"Iniciar captura\" e a ferramenta capturará o token e o did automaticamente. É necessário o adb.exe (baixe da ",
        ["KuroAccount_UsbDescTail"] = " página oficial).",
        ["KuroAccount_UsbVerWarn"] = "⚠ A nova versão do app do Kuro BBS não funciona mais com este método de captura; use uma versão anterior.",
        ["KuroAccount_AdbPlaceholder"] = "Caminho do adb.exe (deixe vazio para usar o PATH)",
        ["KuroAccount_ApplyAdb"] = "Aplicar",
        ["KuroAccount_SelectDevice"] = "Selecionar dispositivo",
        ["KuroAccount_RefreshDevices"] = "Atualizar",
        ["KuroAccount_StartCapture"] = "Iniciar captura",
        ["KuroAccount_StopCapture"] = "Parar captura",
        ["KuroAccount_FieldsTitle"] = "Informações da conta",
        ["KuroAccount_FillHint"] = "Os resultados da captura são preenchidos automaticamente; você também pode colá-los manualmente",
        ["KuroAccount_Token"] = "Token",
        ["KuroAccount_Did"] = "Did",
        ["KuroAccount_Save"] = "Salvar",
        ["KuroAccount_Clear"] = "Limpar",
        ["KuroAccount_Log"] = "Log de captura",
        ["KuroAccount_AdbInvalidTitle"] = "Caminho do ADB inválido",
        ["KuroAccount_AdbInvalidDesc"] = "Digite um caminho completo válido para o adb.exe",
        ["KuroAccount_AdbApplied"] = "Caminho do ADB aplicado",
        ["KuroAccount_AdbApplyFailed"] = "Falha ao aplicar o caminho do ADB",
        ["KuroAccount_NoDevice"] = "Nenhum dispositivo Android encontrado",
        ["KuroAccount_NoDeviceDesc"] = "Conecte o celular e permita a depuração USB",
        ["KuroAccount_RefreshDevicesFailed"] = "Falha ao atualizar os dispositivos",
        ["KuroAccount_SelectDeviceFirst"] = "Selecione um dispositivo antes",
        ["KuroAccount_SelectDeviceFirstDesc"] = "Toque em \"Atualizar\" antes e depois selecione um dispositivo",
        ["KuroAccount_NoPage"] = "Nenhuma página web capturável encontrada",
        ["KuroAccount_NoPageDesc"] = "Entre antes em uma página web como \"Terminal de Dados\" no celular e depois toque em \"Iniciar captura\"",
        ["KuroAccount_CaptureFailed"] = "Falha na conexão de captura",
        ["KuroAccount_CaptureStarted"] = "Monitoramento iniciado",
        ["KuroAccount_CaptureStartedDesc"] = "Abra a página \"Terminal de Dados\" ou \"Calculadora de Crescimento\" no celular",
        ["KuroAccount_Disconnected"] = "O celular foi desconectado",
        ["KuroAccount_DisconnectedDesc"] = "A página WebView pode ter sido fechada. Toque em \"Iniciar captura\" de novo.",
        ["KuroAccount_NoSocket"] = "Interface de depuração do WebView não encontrada",
        ["KuroAccount_NoSocketDesc"] = "Esta versão do Kuro BBS pode ter desativado a depuração remota do WebView. Veja o relatório de diagnóstico para detalhes.",
        ["KuroAccount_DiagTitle"] = "Relatório de diagnóstico da captura",
        ["KuroAccount_DiagHint"] = "Se o relatório mostrar 0 sockets de devtools, esta versão do app do Kuro BBS não ativa mais a depuração remota do WebView (o chrome://inspect do Chrome também não a vê), então a captura via ADB não é possível. Use em vez disso o login com número de telefone + código SMS em \"Kuro BBS → Conta → Entrar\" para obter o Token e o Did, sem captura pelo celular e sem ADB.",
        ["KuroAccount_DiagCopy"] = "Copiar relatório",
        ["KuroAccount_DiagClose"] = "Fechar",
        ["KuroAccount_DiagCopied"] = "Relatório de diagnóstico copiado",
        ["KuroAccount_Invalid"] = "Informações incompletas",
        ["KuroAccount_SaveNeedTokenDid"] = "Token e Did são obrigatórios; o roleId pode ser buscado automaticamente",
        ["KuroAccount_Saved"] = "Conta salva",
        ["KuroAccount_Cleared"] = "Conta limpa",

        
        ["RolePage_Refresh"] = "Atualizar",
        ["RolePage_NoAccount"] = "Conta do Kuro BBS ainda não configurada",
        ["RolePage_NoAccountDesc"] = "Conclua a configuração em Configurações → Conta do Kuro BBS e depois atualize",
        ["RolePage_LoadFailed"] = "Falha ao carregar o personagem",
        ["RolePage_Back"] = "Voltar à lista",
        ["RolePage_Phantoms"] = "Ecos",

        
        ["GachaLogPage_Refresh"] = "Atualizar registros",
        ["GachaLogPage_ImportUrl"] = "Importar com link",
        ["GachaLogPage_ImportUrlDesc"] = "Cole o link da página de detalhes da convocação (abra \"Detalhes da Convocação\" no jogo e pegue-o pelo log, ou copie de outras ferramentas). Jogadores de celular na nuvem também podem usar.",
        ["GachaLogPage_ImportUrlPlaceholder"] = "https://aki-gm-resources.aki-game.com/aki/gacha/index.html#/record?...",
        ["GachaLogPage_UrlInvalid"] = "Link inválido: cole o link completo da página de detalhes da convocação contendo record_id",
        ["GachaLogPage_SelectPlayer"] = "Selecionar conta",
        ["GachaLogPage_CopyUrl"] = "Copiar URL",
        ["GachaLogPage_DeletePlayer"] = "Excluir registros",
        ["GachaLogPage_Delete"] = "Excluir",
        ["GachaLogPage_Avg5"] = "Média",
        ["GachaLogPage_Up"] = "/ UP",
        ["GachaLogPage_Stat"] = "Estatísticas",
        ["GachaLogPage_Times"] = "puxadas",
        ["GachaLogPage_ItemStats"] = "Estatísticas de itens",
        ["GachaLogPage_UpdateSavedUrl"] = "Atualizar (URL salva)",
        ["GachaLogPage_UpdateAll"] = "Atualizar (tudo)",
        ["GachaLogPage_InputUrl"] = "Inserir URL",
        ["GachaLogPage_ViaCloudGame"] = "Obter pelo jogo na nuvem",
        ["CloudGame_Title"] = "Wuthering Waves Cloud - Entre e pegue o link de convocação em um clique",
        ["CloudGame_Grab"] = "Obter link de convocação",
        ["CloudGame_StatusDefault"] = "Entre aqui com sua conta Kuro, toque em \"Entrar no jogo\" e abra uma vez a página \"Detalhes da Convocação\"; depois toque em \"Obter link de convocação\"",
        ["CloudGame_StatusSessionReady"] = "Sessão do jogo na nuvem detectada. Toque em \"Obter link de convocação\" para concluir a sincronização.",
        ["CloudGame_StatusGrabbing"] = "Lendo as informações de login…",
        ["CloudGame_StatusOk"] = "Conseguido (jogador {0}). Sincronizando os registros de convocação…",
        ["CloudGame_StatusFailReason"] = "Falha: {0}",
        ["CloudGame_StatusNoRuntime"] = "Falha ao inicializar o navegador integrado: o runtime WebView2 não está instalado. Instale-o pelo site da Microsoft e tente de novo",
        ["GachaLogPage_ImportExport"] = "Importar & exportar",
        ["GachaLogPage_HidePools"] = "Ocultar banners",
        ["GachaLogPage_ExportAs"] = "Exportar como",
        ["GachaLogPage_ImportRecords"] = "Importar registros (escolher arquivo)",
        ["GachaLogPage_ImportOk"] = "Importação bem-sucedida: jogador {0}, {1} registros (duplicatas removidas automaticamente)",
        ["GachaLogPage_ImportFail"] = "Falha na importação: {0}",
        ["GachaLogPage_ImportFailNoPlayer"] = "O arquivo não contém informações de conta e nenhuma conta está selecionada, então não é possível determinar o dono",
        ["GachaLogPage_OtherSettings"] = "Outras configurações",
        ["GachaLogPage_LinkHelp"] = "Sempre aparece \"Authkey Timeout\"?",
        ["GachaLogPage_LinkHelpDesc"] = "Exclua a pasta de cache e reinicie o jogo",
        ["GachaLogPage_ClearSavedUrl"] = "Excluir pasta de cache",
        ["GachaLogPage_ClearSavedUrlConfirm"] = "Excluir o link de convocação salvo localmente? (Os registros já obtidos não serão excluídos.)",
        ["GachaLogPage_ClearSavedUrlDone"] = "Link de convocação salvo foi limpo. Abra a página \"Detalhes da Convocação\" no jogo de novo.",
        ["GachaLogPage_Language"] = "Idioma",
        ["GachaLogPage_LanguageSaved"] = "Idioma salvo. Ele terá efeito na próxima atualização de registros.",
        ["GachaLogPage_LanguageApplied"] = "{0} registros renomeados para {1}",
        ["GachaLogPage_NoSavedUrl"] = "Nenhum link salvo. Execute \"Atualizar (tudo)\" antes.",
        ["GachaLogPage_LinkExpired"] = "O link de convocação expirou",
        ["GachaLogPage_LinkExpiredDesc"] = "O link expirou (a API respondeu {0}). Abra \"Detalhes da Convocação\" no jogo de novo.",
        ["GachaLogPage_UpdateFailed"] = "Falha ao atualizar os registros de convocação: {0}",
        ["GachaLogPage_FetchFailed"] = "Falha ao obter os dados de convocação. Tente de novo.",
        ["GachaLogPage_LogNotFound"] = "Log do jogo ou link de convocação não encontrado: defina o jogo (Wuthering Waves.exe) nas configurações e abra uma vez \"Detalhes da Convocação\" no jogo, ou escolha \"Importar com link\" no menu \"Atualizar registros\"",
        ["GachaLogPage_UrlNotFound"] = "Link de convocação não encontrado. Abra uma vez a página \"Detalhes da Convocação\" no jogo.",
        ["GachaLogPage_UrlCopied"] = "Link de convocação copiado para a área de transferência",
        ["GachaLogPage_PlayerDeleted"] = "Os registros de convocação locais da UID {0} foram excluídos",
        ["GachaLogPage_DeleteConfirmText"] = "Todos os registros de convocação locais da UID {0} serão excluídos. Esta ação não pode ser desfeita.",
        ["GachaLogPage_EmptyTitle"] = "Ainda não há registros de convocação",
        ["GachaLogPage_EmptyDesc"] = "Defina antes a pasta do jogo nas configurações, abra uma vez a página \"Detalhes da Convocação\" no jogo, volte aqui e toque em \"Atualizar registros\" para sincronizar.",

        
        ["LauncherPage_StartGame"] = "Iniciar jogo",
        ["LauncherPage_InstallGame"] = "Instalar jogo",
        ["AccountSwitcher_SwitchAccount"] = "Trocar de conta",
        ["AccountSwitcher_GameRunning"] = "O jogo está em execução; não é possível trocar de conta no momento",
        ["AccountSwitcher_AccountInfo"] = "Conta do jogo",
        ["AccountSwitcher_Nickname"] = "Apelido",
        ["AccountSwitcher_RoleId"] = "UID do jogo",
        ["AccountSwitcher_Save"] = "Salvar",
        ["AccountSwitcher_Delete"] = "Excluir",
        ["AccountSwitcher_Unsaved"] = "A conta com sessão aberta não está salva",
        ["Notice_Activity"] = "Eventos",
        ["Notice_NoticeTab"] = "Avisos",
        ["Notice_News"] = "Notícias",
        ["Notice_LoadFailed"] = "Falha ao carregar os avisos",
        ["LauncherPage_UpdateGame"] = "Atualizar jogo",

        
        ["SettingPage_Language"] = "Idioma",

        
        ["GameUpdate_PredownloadAvailable"] = "Pré-download da versão {0} disponível",
        ["GameUpdate_Predownloaded"] = "{0} já está em pré-download; poderá ser instalado diretamente quando a atualização abrir",
        ["GameUpdate_PredownloadButton"] = "Pré-baixar",
        ["GameUpdate_ConfirmPredownload"] = "Pré-baixar a versão {0}? Download total: {1}. Os arquivos ficam na pasta predownload dentro da pasta do jogo; ao aplicar a atualização oficial não será necessário baixar de novo.",
        ["GameUpdate_Applying"] = "Aplicando a atualização…",
        ["GameUpdate_Predownloading"] = "Pré-baixando {0}…",

        ["GameUpdate_Done"] = "Atualizado para {0}",
        ["GameUpdate_PredownloadDone"] = "Pré-download de {0} concluído; poderá ser instalado diretamente quando a atualização abrir",
        ["GameUpdate_NoGameDir"] = "Defina antes o jogo nas configurações",
        ["GameUpdate_Verifying"] = "Verificando {0} / {1}",
        ["GameUpdate_VerifyReportTitle"] = "Verificação concluída",
        ["GameUpdate_VerifyDoneNone"] = "Nenhum arquivo corrompido ou ausente encontrado",
        ["GameUpdate_VerifyDoneDamage"] = "{0} arquivos corrompidos / ausentes de {1} encontrados",
        ["GameUpdate_VerifyNoManifest"] = "Não foi possível obter o manifesto de verificação. Verifique a rede ou tente mais tarde.",
        ["GameUpdate_Repairing"] = "Reparando…",
        ["GameUpdate_RepairDone"] = "{0} arquivos reparados",
        ["GameUpdate_RepairNoBaseUrl"] = "A versão atual não suporta reparo online. Atualize antes o jogo para a versão mais recente.",

        
        ["GameLauncherPage_Gallery"] = "Galeria",
        ["Common_Copy"] = "Copiar",
        ["ImageViewWindow2_SaveAs"] = "Salvar como",
        ["GameLauncherPage_PlayPause"] = "Reproduzir/Pausar",
        ["GameLauncherSettingDialog_DragAndDropImageOrVideoFiles"] = "Arraste arquivos de imagem para trocar o plano de fundo",
        ["Background_Official"] = "Plano de fundo animado oficial",
        ["Background_OfficialStatic"] = "Fundo estático oficial",
        ["Background_OpenFolder"] = "Abrir pasta de fundos",
        ["Background_AddFailed"] = "Este arquivo de fundo não pode ser usado",
        ["InstallGameDialog_LocateGame"] = "Localizar jogo",
        ["GameLauncherPage_GameIsRunning"] = "O jogo está em execução",

        
        ["SettingPage_Download"] = "Baixar",

        
        ["DownloadGamePage_Downloading"] = "Baixando",
        ["DownloadGamePage_Verifying"] = "Verificando",
        ["DownloadGamePage_Pause"] = "Pausar",

        
        ["Common_Confirm"] = "Confirmar",
        ["Common_Cancel"] = "Cancelar",
        ["Common_Error"] = "Erro",
        ["Common_Ok"] = "OK",
        ["Common_Select"] = "Selecionar",
        ["Common_Apply"] = "Aplicar",
        ["Common_Exit"] = "Sair",

        
        ["SettingPage_AppSettings"] = "Configurações",
        ["SettingPage_About"] = "Sobre",
        ["SettingPage_General"] = "Geral",
        ["SettingPage_FileManagement"] = "Gerenciamento de arquivos",

        
        ["FilePage_DataFolder"] = "Pasta de dados",
        ["FilePage_DataFolderDesc"] = "Seus dados pessoais ficam na pasta abaixo. Eles são importantes — não os exclua por impulso.",
        ["FilePage_ReselectDataFolder"] = "Reescolher pasta de dados",
        ["FilePage_OpenDataFolder"] = "Abrir pasta de dados",
        ["FilePage_DeleteAllSettings"] = "Excluir todas as configurações",
        ["FilePage_ReselectFolderDone"] = "Nova pasta de dados salva. Terá efeito após reiniciar o aplicativo. A pasta nova começa vazia; use a \"Sincronização na nuvem\" para trazer de volta os dados antigos.",
        ["FilePage_DeleteSettingsDone"] = "Configurações excluídas. Terão efeito após reiniciar o aplicativo.",
        ["FilePage_DeleteSettingsConfirm"] = "Todas as configurações (config.ini) serão excluídas e os padrões restaurados após a reinicialização. Dados do banco, como tempo de jogo e registros de convocação, não são afetados. Continuar?",
        ["FilePage_OpenLogFolder"] = "Abrir pasta de logs",
        ["FilePage_Cache"] = "Cache",
        ["FilePage_CacheLog"] = "Logs",
        ["FilePage_CacheImage"] = "Imagens",
        ["FilePage_CacheBackground"] = "Fundo do launcher",
        ["FilePage_CleanCache"] = "Limpar cache",
        ["FilePage_CleanBackgroundCache"] = "Limpar arquivos de fundo do launcher",

        
        ["FilePage_HardLink"] = "Hardlinks",
        ["InstallGameDialog_HardLinkEstimateDesc"] = "Arquivos idênticos de outros clientes de região no mesmo disco serão reutilizados via links físicos. Download estimado: cerca de {0}. Apenas uma estimativa; vale a verificação arquivo por arquivo na instalação.",
        ["GameUpdate_ResumeInstall"] = "Instalação inacabada de {0} detectada, continuando do diretório anterior",
["FilePage_HardLinkDesc"] = "Ao instalar vários clientes no mesmo disco, arquivos idênticos são vinculados fisicamente para reduzir o uso de armazenamento",
        ["FilePage_Enabled"] = "Ativado",
        ["FilePage_Disabled"] = "Desativado",
        ["FilePage_SpeedLimit"] = "Limite de velocidade",
        ["FilePage_SpeedLimitDesc"] = "0 significa sem limite",
        ["FilePage_DownloadThreads"] = "Multithreading",
        ["FilePage_DownloadThreadsDesc"] = "Número de conexões de download simultâneas (1-16). Valores maiores aproveitam melhor a banda, ao custo de uma carga um pouco maior de CPU, memória e disco. As alterações valem para o próximo download.",

        
        ["KuroBbs_RefreshInfo"] = "Atualizar informações",
        ["KuroBbs_Login"] = "Entrar",
        ["KuroBbs_Settings"] = "Configurações do Kuro BBS",

        
        ["Login_Title"] = "Entrar no Kuro BBS",
        ["Login_ByToken"] = "Entrar com Token",
        ["Login_BySms"] = "Entrar com código SMS",
        ["Login_TokenPlaceholder"] = "Cole o token de login do Kuro BBS",
        ["Login_DidPlaceholder"] = "Did capturado junto com o Token (deixe vazio para gerar automaticamente)",
        ["Login_TokenTip"] = "O Token e o Did são capturados no app móvel do Kuro BBS (Ferramentas → Captura USB) e salvos em par",
        ["Login_PhonePlaceholder"] = "Número de telefone",
        ["Login_CodePlaceholder"] = "Código SMS",
        ["Login_CodeLabel"] = "Código SMS",
        ["Login_SmsTip"] = "Toque em \"Enviar código\" e depois digite o código recebido por SMS. A conta é salva automaticamente após o login bem-sucedido.",
        ["Login_SendCode"] = "Enviar código",
        ["Login_Confirm"] = "Entrar",
        ["Login_Cancel"] = "Cancelar",
        ["Login_PhoneInvalid"] = "Formato de número de telefone inválido",
        ["Login_Sending"] = "Enviando o código SMS…",
        ["Login_CodeSent"] = "Código enviado. Verifique suas mensagens.",
        ["Login_CodeSendFailed"] = "Falha ao enviar o código: {0}",
        ["Login_NeedCode"] = "Digite o código SMS",
        ["Login_TokenInvalid"] = "Token inválido: cole o token de login completo do Kuro BBS",
        ["Login_Working"] = "Entrando…",
        ["Login_Failed"] = "Falha no login: {0}",
        ["Login_SeekRoleFailed"] = "Falha ao buscar o personagem vinculado: {0}",
        ["Login_Success"] = "Conta do Kuro BBS \"{0}\" conectada",

        
        ["ResourceBrief_Month"] = "Mensal",
        ["ResourceBrief_Week"] = "Semanal",
        ["ResourceBrief_Version"] = "Versão",
        ["ResourceBrief_HistoryData"] = "Histórico",
        ["ResourceBrief_TotalGain"] = "Ganho neste período",
        ["ResourceBrief_StarComposition"] = "Composição da receita de Astrita",
        ["ResourceBrief_CoinComposition"] = "Composição da receita de Créditos de Concha",
        ["ResourceBrief_Detail"] = "Detalhes",
        ["ResourceBrief_Empty"] = "Sem dados neste período",

        
        ["SignPage_DoSignIn"] = "Fazer check-in",
        ["SignPage_MonthlySign"] = "Este mês",
        ["SignPage_SignedToday"] = "Check-in feito hoje",
        ["SignPage_NotSignedToday"] = "Ainda sem check-in hoje",
        ["SignPage_Done"] = "Check-in concluído",
        ["SignPage_AutoSign"] = "Check-in automático",
        ["SignPage_SignedDaysFormat"] = "{0} dias com check-in neste mês",
        ["SignPage_DayFormat"] = "Dia {0}",
        ["SignPage_Energy"] = "Placas de Onda",
        ["TowerPage_TotalStarCount"] = "Total de estrelas: {0} / {1}",
        ["TowerPage_Floor"] = "Andar {0}",
        ["TowerPage_NoChallengeData"] = "Nenhum dado de desafio",
        ["TowerPage_RefreshAfter"] = "Atualiza em {0} d {1} h",
        ["MatrixPage_NoRecord"] = "Nenhum registro neste período",
        ["CombatPage_Locked"] = "{0} está bloqueado neste período ou não tem dados",
        ["CombatPage_NoData"] = "Sem dados: o servidor ainda não gerou os registros deste período. Abra a página \"{0}\" no app do Kuro BBS para atualizar os dados e tente de novo.",

        

        
        ["PlayTimeStatsDialog_TotalPlaytime"] = "Tempo de jogo total",
        ["PlayTimeStatsDialog_Started0Times"] = "Iniciado {0} vezes",
        ["PlayTimeStatsDialog_AverageDailyPlaytime"] = "Média diária de jogo",
        ["PlayTimeStatsDialog_PlayedFor0Days"] = "Jogou por {0} dias",
        ["PlayTimeStatsDialog_LongestStreak"] = "Sequência mais longa",
        ["PlayTimeStatsDialog_0Days"] = "{0} dias",
        ["PlayTimeStatsDialog_LongestSession"] = "Sessão mais longa",
        ["PlayTimeStatsDialog_LongestDailyPlaytime"] = "Dia mais longo de jogo",
        ["PlayTimeButton_LastStartup"] = "Última sessão",
        ["PlayTimeStatsDialog_Last15Days"] = "Últimos 15 dias",
        ["PlayTimeStatsDialog_Last12Weeks"] = "Últimas 12 semanas",
        ["PlayTimeStatsDialog_Last12Months"] = "Últimos 12 meses",
        ["PlayTimeStatsDialog_Total"] = "Total",
        ["PlayTimeStatsDialog_PlaytimeStatistics"] = "Estatísticas de tempo de jogo",
        ["PlayTimeStatsDialog_StatsTip"] = "As estatísticas contam apenas o tempo de jogo das sessões iniciadas por este launcher.",

        
        ["ExperienceSettingPage_MinimizeToSystemTray"] = "Minimizar para a bandeja do sistema",
        ["GameLauncherSettingDialog_BasicInformation"] = "Informações básicas",
        ["GameLauncherSettingDialog_ConfirmUninstall"] = "Confirmar desinstalação",
        ["GameLauncherSettingDialog_CustomBackground"] = "Plano de Fundo",
        ["GameLauncherSettingDialog_RelocateGame"] = "Realocar o jogo",
        ["GameLauncherSettingDialog_Size"] = "Tamanho",
        ["GameLauncherSettingDialog_StartRepairing"] = "Iniciar reparo",
        ["GameLauncherSettingDialog_UninstallGameWarning"] = "Aviso de desinstalação do jogo",
        ["LauncherPage_RepairGame"] = "Reparar jogo",
        ["LauncherPage_ShowGameAnnouncement"] = "Mostrar avisos do jogo",
        ["LauncherPage_UninstallGame"] = "Desinstalar jogo",
        ["InstallGameDialog_MappedNetworkDrivesAreNotSupportedPleaseUseANetworkSharePathStartingWithDoubleBackslashes"] = "Unidades de rede mapeadas não são suportadas. Use um caminho de compartilhamento de rede começando com barra invertida dupla.",

        
        ["SettingPage_BgSizeTip"] = "Dica sobre o tamanho do fundo",
        ["SettingPage_CustomBackgroundSupportVideo"] = "O fundo personalizado aceita vídeo",

        
        ["GameSettingPage_Resolution"] = "Resolução",
        ["GameSettingPage_FullScreen"] = "Tela cheia",
        ["GameSettingPage_UsePopupWindow"] = "Janela sem bordas",
        ["GameSettingPage_CustomResolution"] = "Resolução personalizada",
        ["GameSettingPage_CustomResolutionTip"] = "A resolução personalizada vale no modo de exibição \"Janela\"",
        ["GameSettingPage_VoiceLanguage"] = "Idioma da voz",
        ["GameSettingPage_VoiceLanguageDescription"] = "Defina isso antes de iniciar o jogo pela primeira vez para evitar baixar alguns arquivos de voz",
        ["GameSettingPage_HDR"] = "HDR",
        ["GameSettingPage_EnableHDR"] = "Ativar HDR",
        ["GameSettingPage_HDRNotSupported"] = "A tela atual não suporta HDR",
        ["GameSettingPage_HDRNotEnabled"] = "O HDR do Windows ainda não está ativado; pode não ter efeito",
        ["GameSettingPage_GameNotInstalled"] = "Jogo não instalado",
        ["GameSettingPage_GameNotInstalledDesc"] = "Defina antes o caminho do jogo na página do launcher",
        ["GameSettingPage_CloudGameNotSupport"] = "O jogo na nuvem não precisa de configurações locais do jogo",
        ["GameSettingPage_SettingNotEffect"] = "Configurações aplicadas com o jogo aberto não terão efeito",
        ["GameSettingPage_ResolutionMustBeGreaterThan0"] = "A resolução deve ser maior que 0",
        ["GameSettingPage_LoadFailed"] = "Falha ao ler as configurações do jogo",
        ["GameSettingPage_ApplyFailed"] = "Falha ao aplicar as configurações do jogo",
        ["GameSettingPage_Applied"] = "Aplicado. Terá efeito na próxima vez que o jogo iniciar.",

        

        
        ["SettingPage_WindowBehavior"] = "Comportamento da janela",
        ["SettingPage_CloseWindowOption"] = "Ao fechar a janela",
        ["SettingPage_ExitApp"] = "Sair do aplicativo",
        ["SettingPage_AfterLaunchOption"] = "Após iniciar o jogo",
        ["SettingPage_KeepWindow"] = "Manter a janela",
        ["SettingPage_AfterGameExitOption"] = "Após sair do jogo",
        ["SettingPage_RestoreMainWindow"] = "Mostrar janela principal",
        ["SettingPage_KeepHidden"] = "Manter oculta",
        ["SettingPage_CloseLauncher"] = "Fechar launcher",
        ["SettingPage_KuroCommunitySign"] = "Check-in da comunidade do Kuro BBS (moedas Kuro)",
        ["SettingPage_SuggestSystemFeatures"] = "Para uma melhor experiência, ative o seguinte nas configurações do sistema.",
        ["SettingPage_TransparencyEffects"] = "Efeitos de transparência",
        ["SettingPage_AnimationEffects"] = "Efeitos de animação",

        
        ["GameSelector_Subtitle"] = "Toque no ícone no canto superior esquerdo para escolher ou trocar de jogo",
        ["GameSelector_AutoFind"] = "Detectar jogos instalados automaticamente",
        ["GameSelector_SelectServer"] = "Selecionar servidor",
        ["GameSelector_InstalledSize"] = "Espaço em disco realmente usado pelos jogos instalados ",
        ["GameSelector_Calculating"] = "Calculando",

        
        ["KuroBbs_AccountLevelLabel"] = "Nível de União ",
        ["KuroBbs_SwitchRole"] = "Trocar de personagem",
        ["KuroBbs_ClearAccount"] = "Limpar conta",

        
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
        ["GameBiz_ShortKorea"] = "Servidor Coreia",
        ["GameBiz_PgrJapan"] = "Punishing: Gray Raven · Japan",
        ["GameBiz_ShortJapan"] = "Servidor Japão",
        ["GameBiz_PgrTaiwan"] = "Punishing: Gray Raven · Taiwan",
        ["GameBiz_ShortTaiwan"] = "Servidor Taiwan",

        
        ["GachaPool_CharacterEvent"] = "Convocação de Evento de Personagem",
        ["GachaPool_WeaponEvent"] = "Convocação de Evento de Arma",
        ["GachaPool_CharacterStandard"] = "Convocação Padrão de Personagem",
        ["GachaPool_WeaponStandard"] = "Convocação Padrão de Arma",
        ["GachaPool_Novice"] = "Convocação de Novato",
        ["GachaPool_NoviceChoice"] = "Convocação de Escolha de Novato",
        ["GachaPool_NoviceGratitude"] = "Convocação de Escolha de Novato (Convocação de Gratidão)",
        ["GachaPool_CharacterRookie"] = "Convocação de Recruta de Personagem",
        ["GachaPool_WeaponRookie"] = "Convocação de Recruta de Arma",
        ["GachaPool_CharacterCollab"] = "Convocação de Colab de Personagem",
        ["GachaPool_WeaponCollab"] = "Convocação de Colab de Arma",
        ["GachaPool_CharacterRevisit"] = "Convocação de Revisita de Personagem",
        ["GachaPool_WeaponRevisit"] = "Convocação de Revisita de Arma",
        ["GachaPool_Unknown"] = "Banner {0}",

        
        ["Adb_DeviceOnline"] = "online",
        ["Adb_DeviceUnauthorized"] = "não autorizado",

        
        ["Sync_Title"] = "WebDAV",
        ["Sync_Description"] = "Salve seus dados sincronizando com a nuvem",
        ["Sync_ServerUrl"] = "Endereço do servidor",
        ["Sync_UserName"] = "Nome de usuário",
        ["Sync_Password"] = "Senha ou código de autorização do app",
        ["Sync_ServerEntry"] = "Servidor de sincronização",
        ["Sync_EnableOff"] = "Desativada — ative para sincronizar automaticamente",
        ["Sync_EnableNeedServer"] = "Configure um servidor primeiro",
        ["Sync_ConnectTitle"] = "Conectar WebDAV",
        ["Sync_ConnectDesc"] = "Informe os dados de conexão do seu armazenamento em nuvem ou servidor.",
        ["Sync_ConnectInvalid"] = "Informe um endereço completo http:// ou https://",
        ["Sync_SaveAndTest"] = "Salvar e testar",
        ["Sync_Testing"] = "Testando conexão…",
        ["Sync_ConnectOk"] = "Conectado. Configurações salvas.",
        ["Sync_ConnectFail"] = "Falha na conexão. Verifique o endereço do servidor, a conta e a senha, e tente novamente.",
        ["Sync_SyncNow"] = "Sincronizar agora",
        ["Sync_RevealPassword"] = "Mostrar ou ocultar a senha",
        ["Sync_Configured"] = "Configurado",
        ["Sync_NotConfigured"] = "Não configurado",
        ["Sync_NeverSynced"] = "Nunca sincronizado",
        ["Sync_LastSync"] = "Última sincronização",
        ["Sync_Working"] = "Processando…",

        
        ["CloudGame_ErrNoLogin"] = "Nenhuma informação de login utilizável",
        ["CloudGame_ErrHttp"] = "O serviço do jogo na nuvem respondeu HTTP {0}",
        ["CloudGame_ErrRequest"] = "Falha na requisição ao serviço do jogo na nuvem: {0}",
        ["CloudGame_ErrParse"] = "O serviço do jogo na nuvem devolveu conteúdo ilegível: {0}",
        ["CloudGame_ErrEmpty"] = "O serviço do jogo na nuvem devolveu conteúdo vazio: {0}",
        ["CloudGame_ErrCode"] = "A API do jogo na nuvem retornou o erro code={0}: {1}",

        
        ["GachaLogPage_ExportEmpty"] = "A conta atual não tem registros de convocação para exportar",
        ["GachaLog_PityBuilt"] = "Pity",

        
        ["GameLauncherSettingDialog_PathRemoved"] = "Configuração do caminho do jogo removida",
        ["GameLauncherSettingDialog_WrongServerExe"] = "O exe do jogo selecionado não pertence ao servidor atual, escolha novamente",
        ["GameLauncherSettingDialog_ExpectExe"] = "Selecione o executável principal do jogo: {0}",

        
        ["KrSdk_ErrNoUserCache"] = "KRSDKUserCache.json não encontrado. Entre uma vez no jogo antes.",
        ["KrSdk_ErrEmptyContent"] = "Conteúdo vazio",
        ["KrSdk_ErrParseFailed"] = "Falha ao interpretar o KRSDKUserCache.json; gravação abortada para não corromper o estado de login.",
        ["KrSdk_ErrNoAccountList"] = "Sem account_list no KRSDKUserCache.json.",
        ["KrSdk_ErrAccountNotInCache"] = "Esta conta não está no cache local.",
        ["KrSdk_ErrVerifyFailed"] = "A verificação após a gravação falhou; o cache foi restaurado ao estado anterior.",

        
        ["GameUpdate_ErrNoPlan"] = "Falta o plano de download",
        ["GameUpdate_ErrNoTarget"] = "Falta a versão de destino",
        ["GameUpdate_ErrNoGamePath"] = "Caminho do jogo não configurado; não é possível atualizar",
        ["GameUpdate_ErrNoManifest"] = "Falha ao obter o manifesto de download",
        ["GameUpdate_ErrFileMissing"] = "Arquivo de atualização ausente: {0}",
        ["GameUpdate_ErrFileChecksum"] = "Falha na verificação do arquivo de atualização: {0}",

        
        ["GameSetting_ErrNoLocalStorage"] = "O LocalStorage.db do jogo não foi encontrado",
        ["GameSetting_ErrNoGameUserSettings"] = "O GameUserSettings.ini do jogo não foi encontrado",

        
        ["KuroBbs_ErrConnection"] = "Falha de conexão, status HTTP: {0}",
        ["KuroBbs_ErrAccessTokenEmpty"] = "requestToken devolveu um accessToken vazio",
        ["KuroBbs_ErrAccessToken"] = "Falha ao obter o B-At accessToken (code={0}): {1}",
        ["KuroBbs_ErrSendCode"] = "Falha ao enviar o código SMS",
        ["KuroBbs_ErrLogin"] = "Falha no login",
        ["KuroBbs_SignOk"] = "Check-in concluído",
        ["KuroBbs_SignAlready"] = "Já fez check-in hoje",
        ["KuroBbs_SignFail"] = "Falha no check-in",

        

        
        ["Ugf_ErrNoUid"] = "O arquivo UGF está sem o uid da conta",
        ["Ugf_ErrUnknownFormat"] = "Formato de arquivo não reconhecido: esperado UGF ou JSON de registros de banner",

        

        
        ["CombatPage_AreaFallback"] = "Zona {0}",

        
        ["KuroBbs_UnionLevel"] = "Nível de União",
        ["KuroBbs_CommanderLevel"] = "Nível de Comandante",
        ["KuroBbs_NoLevel"] = "Ainda sem nível",
        ["KuroBbs_SelectRolePgr"] = "Selecionar personagem de PGR",
        ["KuroBbs_SelectRoleWuwa"] = "Selecionar personagem de Wuthering Waves",
        ["KuroBbs_SwitchedPgr"] = "Personagem de PGR trocado",
        ["KuroBbs_SwitchedWuwa"] = "Personagem de Wuthering Waves trocado",
        ["PgrData_DailySupply"] = "Suprimento diário",
        ["KuroBbs_AutoSignDoneToast"] = "Check-in diário concluído",
        ["KuroBbs_AutoSignFailedToast"] = "Falha no check-in diário",

        
        ["RolePage_WeaponLevel"] = "Nv.{0} · Ascensão {1}",

        
        ["Common_UnknownError"] = "Erro desconhecido",
    };

}
