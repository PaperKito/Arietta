using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Arietta.Frameworks;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;

namespace Arietta.Features.GachaLog;

public sealed partial class CloudGameWindow : WindowEx
{

    private const int TitleBarHeight = 48;

    private readonly Func<GachaParams, Task> _onGrabbed;

    private string? _capturedCookie;
    private string? _capturedXToken;
    private string? _capturedTraceId;

    private bool _grabbing;

    private bool _closed;


    public CloudGameWindow(Func<GachaParams, Task> onGrabbed)
    {
        _onGrabbed = onGrabbed;
        this.InitializeComponent();

        Title = Lang.Instance.CloudGame_WindowTitle;

        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();

        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AdaptTitleBarButtonColorToActuallTheme();
        CenterInScreen(1180, 780);
        RootGrid.Loaded += (_, _) =>
        {
            AdaptTitleBarButtonColorToActuallTheme();
            UpdateDragRectangles();
        };

        AppTitleBar.SizeChanged += (_, _) => UpdateDragRectangles();
        Button_Grab.SizeChanged += (_, _) => UpdateDragRectangles();
        AppWindow.Changed += (_, _) =>
        {
            if (!_closed)
            {
                UpdateDragRectangles();
            }
        };
        RootGrid.ActualThemeChanged += (_, _) => AdaptTitleBarButtonColorToActuallTheme();
        Web.Loaded += async (_, _) => await EnsureWebviewAsync();

        this.Closed += (_, _) =>
        {
            _closed = true;
            Web.Close();
        };
    }

    private void UpdateDragRectangles()
    {
        if (_closed)
        {
            return;
        }
        if (!AppWindowTitleBar.IsCustomizationSupported() || AppWindow.TitleBar.ExtendsContentIntoTitleBar != true)
        {
            return;
        }
        int height = (int)(TitleBarHeight * UIScale);
        if (height <= 0)
        {
            return;
        }
        var rects = new List<RectInt32>();
        double buttonLeft = double.NaN;
        if (Button_Grab.ActualWidth > 0 && AppTitleBar.ActualWidth > 0)
        {
            try
            {
                buttonLeft = Button_Grab.TransformToVisual(null)
                    .TransformPoint(new Windows.Foundation.Point(0, 0)).X;
            }
            catch
            {

            }
        }
        int split = double.IsNaN(buttonLeft)
            ? (int)(AppTitleBar.ActualWidth * UIScale)
            : (int)(buttonLeft * UIScale) - (int)(8 * UIScale);
        if (split > 0)
        {
            rects.Add(new RectInt32(0, 0, split, height));
        }
        SetDragRectangles(rects.ToArray());
    }


    private async Task EnsureWebviewAsync()
    {
        try
        {

            Arietta.Helpers.WebviewHost.UseDataFolder();
            await Web.EnsureCoreWebView2Async();

            Web.CoreWebView2.WebResourceResponseReceived -= CaptureCloudApiHeaders;
            Web.CoreWebView2.WebResourceResponseReceived += CaptureCloudApiHeaders;
            Web.CoreWebView2.Navigate(CloudGameClient.CloudHomePage);
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<CloudGameWindow>().LogError(e, "Failed to initialize WebView2");
            SetStatus(Lang.Instance.CloudGame_StatusNoRuntime, false);
            Button_Grab.IsEnabled = false;
        }
    }


    private void CaptureCloudApiHeaders(CoreWebView2 sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        try
        {
            string uri = args.Request.Uri;
            if (!uri.StartsWith(CloudGameClient.CloudApiBase, StringComparison.OrdinalIgnoreCase))
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
            AppConfig.GetLogger<CloudGameWindow>().LogWarning(e, "Failed to intercept cloud gaming request headers");
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
            AppConfig.GetLogger<CloudGameWindow>().LogError(ex, "Failed to get cloud gaming launch link");
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
