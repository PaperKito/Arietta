using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Arietta.Features.KuroBbs;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Arietta.Features.ViewHost;







public sealed partial class AdbToolPage : PageBase
{
    private readonly AdbClient _adbClient = new();
    private CdpTokenSniffer? _sniffer;
    private bool _capturing;


    public AdbToolPage()
    {
        this.InitializeComponent();
        string resolved = AdbClient.ResolveAdbExecutablePath();
        TextBox_AdbPath.Text = resolved == "adb" ? "" : resolved;
    }


    protected override void OnUnloaded()
    {
        _ = StopCaptureAsync();
        base.OnUnloaded();
    }


    #region 导航

    
    private void Button_Back_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
            }
        }
        catch { }
    }

    #endregion


    #region USB 抓取

    private void AppendLog(string message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TextBox_Log.Text = $"{DateTime.Now:HH:mm:ss} {message}\n" + TextBox_Log.Text;
        });
    }


    private async void Button_ApplyAdbPath_Click(object sender, RoutedEventArgs e)
    {
        string path = TextBox_AdbPath.Text.Trim();
        if (path.Length == 0 || !File.Exists(path))
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_AdbInvalidTitle, Lang.Instance.KuroAccount_AdbInvalidDesc);
            return;
        }
        try
        {
            _adbClient.InitAdbServer(path);
            AppConfig.SetConfig("KuroBbs_AdbPath", path);
            InAppToast.MainWindow?.Success(Lang.Instance.KuroAccount_AdbApplied);
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.KuroAccount_AdbApplyFailed);
        }
        await Task.CompletedTask;
    }


    private async void Button_RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? saved = AppConfig.GetConfig("KuroBbs_AdbPath");
            if (!string.IsNullOrWhiteSpace(saved) && File.Exists(saved))
            {
                TextBox_AdbPath.Text = saved;
                _adbClient.InitAdbServer(saved);
            }
            var devices = await _adbClient.GetDevicesAsync();
            ComboBox_Devices.ItemsSource = devices.Select(d =>
                $"{d.Serial}  ({(d.IsOnline ? Localization.Lang.Instance.Adb_DeviceOnline : d.IsUnauthorized ? Localization.Lang.Instance.Adb_DeviceUnauthorized : d.State)}){(string.IsNullOrEmpty(d.Model) ? "" : "  " + d.Model)}").ToList();
            ComboBox_Devices.SelectedIndex = devices.Count == 1 ? 0 : -1;
            if (devices.Count == 0)
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_NoDevice, Lang.Instance.KuroAccount_NoDeviceDesc);
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.KuroAccount_RefreshDevicesFailed);
        }
    }


    private async void Button_Capture_Click(object sender, RoutedEventArgs e)
    {
        if (_capturing)
        {
            await StopCaptureAsync();
            return;
        }

        if (ComboBox_Devices.SelectedIndex < 0)
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_SelectDeviceFirst, Lang.Instance.KuroAccount_SelectDeviceFirstDesc);
            return;
        }
        string serial = (ComboBox_Devices.SelectedItem as string ?? "").Split(' ')[0];

        try
        {
            SetCapturing(true);
            AppendLog("正在扫描调试 Socket…");
            var sockets = await _adbClient.GetWebViewSocketsAsync(serial);
            if (sockets.Count == 0)
            {
                
                
                SetCapturing(false);
                AppendLog("没有找到任何 devtools socket（WebView 远程调试未开启）");
                string report = await _adbClient.CollectDiagnosticsAsync(serial);
                AppendLog(report);
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_NoSocket, Lang.Instance.KuroAccount_NoSocketDesc);
                await ShowDiagnosticsDialogAsync(report);
                return;
            }
            AppendLog($"找到 {sockets.Count} 个调试 socket：{string.Join("、", sockets.Select(static s => s.IsKuroBbsProcess ? $"{s.SocketName}（库街区 pid={s.ProcessId}）" : s.SocketName))}");

            (WebViewSocketInfo Socket, DevToolsTargetInfo Target)? selected = null;
            foreach (var socket in sockets)
            {
                IReadOnlyList<DevToolsTargetInfo> targets;
                try
                {
                    targets = await _adbClient.GetDevToolsTargetsAsync(serial, socket.SocketName, 9222);
                }
                catch (Exception ex)
                {
                    AppendLog($"跳过 {socket.SocketName}：{ex.Message}");
                    continue;
                }
                foreach (var target in targets.Where(static t => t.IsPageLike))
                {
                    AppendLog($"发现页面：[{socket.SocketName}] {target.Title} {target.Url}");
                }
                selected ??= targets
                    .Where(static t => t.IsPageLike)
                    .Where(static t => !string.Equals(t.Url, "about:blank", StringComparison.OrdinalIgnoreCase))
                    .Select(t => (socket, t))
                    .FirstOrDefault(t => t.t is not null);
            }

            if (selected is null)
            {
                SetCapturing(false);
                AppendLog("未找到可监控的非空白 WebView 页面");
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_NoPage, Lang.Instance.KuroAccount_NoPageDesc);
                return;
            }

            AppendLog($"选中页面：{selected.Value.Target.Title} {selected.Value.Target.Url}");
            await StartSnifferAsync(selected.Value.Target.WebSocketDebuggerUrl);
        }
        catch (Exception ex)
        {
            SetCapturing(false);
            AppendLog($"连接失败：{ex.Message}");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.KuroAccount_CaptureFailed);
        }
    }


    private async Task StartSnifferAsync(string webSocketDebuggerUrl)
    {
        await StopSnifferAsync();
        _sniffer = new CdpTokenSniffer();
        _sniffer.Log += AppendLog;
        _sniffer.DidCaptured += did => DispatcherQueue.TryEnqueue(() => TextBox_Did.Text = did);
        _sniffer.TokenCaptured += token => DispatcherQueue.TryEnqueue(() => TextBox_Token.Text = token);
        
        _sniffer.UserIdCaptured += userId => AppendLog($"捕获 userId：{userId}");
        _sniffer.Disconnected += () => DispatcherQueue.TryEnqueue(() =>
        {
            if (_capturing)
            {
                SetCapturing(false);
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_Disconnected, Lang.Instance.KuroAccount_DisconnectedDesc);
            }
        });
        await _sniffer.ConnectAsync(webSocketDebuggerUrl);
        InAppToast.MainWindow?.Information(Lang.Instance.KuroAccount_CaptureStarted, Lang.Instance.KuroAccount_CaptureStartedDesc);
    }


    private async Task StopSnifferAsync()
    {
        if (_sniffer is not null)
        {
            _sniffer.Log -= AppendLog;
            await _sniffer.DisposeAsync();
            _sniffer = null;
        }
    }


    private async Task StopCaptureAsync()
    {
        if (!_capturing && _sniffer is null) return;
        await StopSnifferAsync();
        SetCapturing(false);
        AppendLog("已停止抓取");
    }


    private void SetCapturing(bool capturing)
    {
        _capturing = capturing;
        Button_Capture.Content = capturing ? Lang.Instance.KuroAccount_StopCapture : Lang.Instance.KuroAccount_StartCapture;
    }


    
    private async Task ShowDiagnosticsDialogAsync(string report)
    {
        try
        {
            var reportBox = new TextBox
            {
                Text = report,
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.NoWrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                MinWidth = 460,
                MinHeight = 180,
                MaxHeight = 280,
            };
            
            ScrollViewer.SetHorizontalScrollBarVisibility(reportBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollBarVisibility(reportBox, ScrollBarVisibility.Auto);
            var panel = new StackPanel { Spacing = 10, MaxWidth = 460 };
            panel.Children.Add(new TextBlock
            {
                Text = Lang.Instance.KuroAccount_DiagHint,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Opacity = 0.75,
            });
            panel.Children.Add(reportBox);

            var dialog = new ContentDialog
            {
                Title = Lang.Instance.KuroAccount_DiagTitle,
                Content = panel,
                PrimaryButtonText = Lang.Instance.KuroAccount_DiagCopy,
                CloseButtonText = Lang.Instance.KuroAccount_DiagClose,
                XamlRoot = XamlRoot,
            };
            dialog.PrimaryButtonClick += (_, _) =>
            {
                var package = new DataPackage();
                package.SetText(report);
                Clipboard.SetContent(package);
                InAppToast.MainWindow?.Success(Lang.Instance.KuroAccount_DiagCopied);
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            
            AppendLog($"诊断窗口打开失败：{ex.Message}");
        }
    }

    #endregion


    #region 手动填写 / 保存

    private void Button_Save_Click(object sender, RoutedEventArgs e)
    {
        var account = ReadAccountFromInputs();
        if (string.IsNullOrWhiteSpace(account.Token) || string.IsNullOrWhiteSpace(account.Did))
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.KuroAccount_Invalid, Lang.Instance.KuroAccount_SaveNeedTokenDid);
            return;
        }
        
        SaveAccount(account);
    }


    private void SaveAccount(KuroBbsAccount account)
    {
        KuroBbsAccountStore.AddOrUpdate(account);
        KuroBbsEvents.RaiseAccountChanged();
        InAppToast.MainWindow?.Success(Lang.Instance.KuroAccount_Saved);
    }


    private void Button_Clear_Click(object sender, RoutedEventArgs e)
    {
        KuroBbsAccountStore.Clear();
        TextBox_Token.Text = TextBox_Did.Text = "";
        KuroBbsEvents.RaiseAccountChanged();
        InAppToast.MainWindow?.Success(Lang.Instance.KuroAccount_Cleared);
    }


    
    
    
    
    private KuroBbsAccount ReadAccountFromInputs()
    {
        string token = TextBox_Token.Text.Trim();
        var existing = KuroBbsAccountStore.Load();
        KuroBbsAccount account = existing is not null && existing.Token == token
            ? existing
            : new KuroBbsAccount();
        account.Token = token;
        account.Did = TextBox_Did.Text.Trim();
        return account;
    }

    #endregion

}
