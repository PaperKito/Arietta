using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.GachaLog;

public sealed partial class CloudGameWindow : Window
{

    private const string CloudApiPrefix = "https://cloud-game-sh.aki-game.com/";

    private readonly Func<GachaParams, Task> _onGrabbed;

    private string? _capturedCookie;
    private string? _capturedXToken;
    private string? _capturedTraceId;

    private bool _grabbing;


    public CloudGameWindow(Func<GachaParams, Task> onGrabbed)
    {
        _onGrabbed = onGrabbed;
        this.InitializeComponent();
        Title = Lang.Instance.CloudGame_Title;
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1180, 780));
        Web.Loaded += async (_, _) => await EnsureWebviewAsync();

        this.Closed += (_, _) => Web.Close();
    }


    private async Task EnsureWebviewAsync()
    {
        try
        {
            await Web.EnsureCoreWebView2Async();

            Web.CoreWebView2.WebResourceResponseReceived -= CaptureCloudApiHeaders;
            Web.CoreWebView2.WebResourceResponseReceived += CaptureCloudApiHeaders;
            Web.CoreWebView2.Navigate(CloudGameClient.CloudHomePage);
            Web.CoreWebView2.DocumentTitleChanged += (_, _) =>
                Title = $"{Lang.Instance.CloudGame_Title} - {Web.CoreWebView2.DocumentTitle}";
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<CloudGameWindow>().LogError(e, "初始化 WebView2 失败");
            SetStatus(Lang.Instance.CloudGame_StatusNoRuntime, false);
            Button_Grab.IsEnabled = false;
        }
    }


    private void CaptureCloudApiHeaders(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        try
        {
            string uri = args.Request.Uri;
            if (!uri.StartsWith(CloudApiPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            var headers = args.Request.Headers;
            foreach (var pair in headers)
            {
                switch (pair.Key.ToLowerInvariant())
                {
                    case "cookie":
                        _capturedCookie = pair.Value;
                        break;
                    case "x-token":
                        _capturedXToken = pair.Value;
                        break;
                    case "x-b3-traceid":
                        _capturedTraceId = pair.Value;
                        break;
                }
            }
            if (_capturedCookie is not null || _capturedXToken is not null)
            {
                DispatcherQueue.TryEnqueue(() =>
                    SetStatus(Lang.Instance.CloudGame_StatusSessionReady, false));
            }
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<CloudGameWindow>().LogWarning(e, "截获云游戏请求头失败");
        }
    }


    private async void Grab_Click(object sender, RoutedEventArgs e)
    {
        if (_grabbing)
        {
            return;
        }
        _grabbing = true;
        Button_Grab.IsEnabled = false;
        GrabProgress.Visibility = Visibility.Visible;
        GrabProgress.IsActive = true;
        SetStatus(Lang.Instance.CloudGame_StatusGrabbing, false);
        try
        {
            if (Web.CoreWebView2 is null)
            {
                await EnsureWebviewAsync();
            }

            (GachaParams? Params, string? Error) result;
            if (_capturedCookie is not null || _capturedXToken is not null)
            {
                result = await CloudGameClient.TryBuildParamsWithCapturedHeadersAsync(
                    _capturedCookie, _capturedXToken, _capturedTraceId);
            }
            else
            {

                var cookies = await Web.CoreWebView2.CookieManager.GetCookiesAsync("https://mc.kurogames.com/");
                var dict = cookies.ToDictionary(c => c.Name, c => c.Value);
                result = await CloudGameClient.TryBuildParamsAsync(dict);
            }

            if (result.Params is null)
            {
                SetStatus(string.Format(Lang.Instance.CloudGame_StatusFailReason, result.Error ?? "未知原因"), true);
                return;
            }

            SetStatus(string.Format(Lang.Instance.CloudGame_StatusOk, result.Params.PlayerId), false);
            Close();
            await _onGrabbed(result.Params);
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<CloudGameWindow>().LogError(ex, "获取云游戏唤取链接失败");
            SetStatus(string.Format(Lang.Instance.CloudGame_StatusFailReason, ex.Message), true);
        }
        finally
        {
            _grabbing = false;
            Button_Grab.IsEnabled = true;
            GrabProgress.Visibility = Visibility.Collapsed;
            GrabProgress.IsActive = false;
        }
    }


    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.IndianRed)
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
