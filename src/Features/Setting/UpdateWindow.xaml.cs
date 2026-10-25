using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arietta.Frameworks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.System;

namespace Arietta.Features.Setting;

public sealed partial class UpdateWindow : WindowEx
{
    private static readonly ILogger _logger = AppConfig.GetLogger<UpdateWindow>();

    private readonly SelfUpdateInfo _info;

    private bool _isDownloading;

    public string RemindLaterText { get; } = Localization.Lang.Instance.UpdateWindow_RemindLater;

    public UpdateWindow(SelfUpdateInfo info)
    {
        _info = info;
        InitializeComponent();
        Title = "Arietta · Update";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));

        var lang = Localization.Lang.Instance;
        TextBlock_Title.Text = lang.SettingPage_UpdateAvailable;
        TextBlock_OldVersion.Text = AppConfig.AppVersion;
        TextBlock_NewVersion.Text = info.Version;
        TextBlock_SizeLabel.Text = lang.UpdateWindow_PackageSize;
        TextBlock_Size.Text = FormatSize(info.AssetSize);
        TextBlock_ChannelLabel.Text = lang.UpdateWindow_Channel;
        TextBlock_LinksLabel.Text = lang.SettingPage_About_Links;
        HyperlinkButton_Release.Content = lang.UpdateWindow_ReleasePage;
        HyperlinkButton_History.Content = lang.UpdateWindow_UpdateHistory;
        Button_UpdateNow.Content = lang.UpdateWindow_UpdateNow;
        MenuFlyoutItem_Ignore.Text = lang.UpdateDialog_IgnoreVersion;

        Closed += (_, _) => _dialogCts?.Cancel();

        DisplayArea display = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest);
        double scale = UIScale;
        int w = (int)(1000 * scale), h = (int)(750 * scale);
        if (h > display.WorkArea.Height * 0.9 || w > display.WorkArea.Width * 0.9)
        {
            h = (int)(display.WorkArea.Height * 0.9);
            w = (int)(h * 4.0 / 3.0);
            if (w > display.WorkArea.Width)
            {
                w = (int)(display.WorkArea.Width * 0.9);
                h = (int)(w * 3.0 / 4.0);
            }
        }
        CenterInScreen((int)Math.Round(w / scale), (int)Math.Round(h / scale));
    }


    private void OnUpdateNowClick(object sender, RoutedEventArgs e)
    {
        if (_isDownloading)
        {
            return;
        }
        if (!SelfUpdateService.IsPortableLayout)
        {
            ShowProgress();
            TextBlock_ProgressMessage.Text = Localization.Lang.Instance.SettingPage_UpdateDevLayout;
            return;
        }
        _isDownloading = true;
        Button_UpdateNow.IsEnabled = false;
        ShowProgress();
        TextBlock_ProgressMessage.Text = "";
        _ = RunDownloadAsync();
    }


    private async System.Threading.Tasks.Task RunDownloadAsync()
    {
        try
        {
            string newAppDir = await SelfUpdateService.DownloadAndStageAsync(
                _info, new Progress<double>(p =>
                {
                    ProgressBar_Download.Value = p;
                    TextBlock_Percent.Text = $"{p:F0}%";
                    TextBlock_Bytes.Text = FormatSize((long)(_info.AssetSize * p / 100.0)) + " / " + FormatSize(_info.AssetSize);
                }), DialogToken);
            TextBlock_ProgressMessage.Text = Localization.Lang.Instance.SettingPage_UpdateReady;

            SelfUpdateService.InstallNow(newAppDir);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: download/install from update window failed");
            TextBlock_ProgressMessage.Text = $"{Localization.Lang.Instance.SettingPage_UpdateFailed}: {ex.Message}";
            _isDownloading = false;
            Button_UpdateNow.IsEnabled = true;
        }
    }


    private void ShowProgress()
    {
        Panel_Progress.Visibility = Visibility.Visible;
    }


    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1L << 30 => $"{bytes / (double)(1L << 30):F2} GB",
            >= 1L << 20 => $"{bytes / (double)(1L << 20):F1} MB",
            >= 1L << 10 => $"{bytes / (double)(1L << 10):F1} KB",
            _ => $"{bytes} B",
        };
    }


    private void OnRemindLaterClick(SplitButton sender, SplitButtonClickEventArgs args)
    {
        Close();
    }

    private void SplitButton_RemindLater_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SplitButton_RemindLater.Opacity = 1;
    }


    private void SplitButton_RemindLater_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SplitButton_RemindLater.Opacity = 0;
    }


    private void OnIgnoreVersionClick(object sender, RoutedEventArgs e)
    {

        SelfUpdateService.IgnoreVersion(_info.Version);
        Close();
    }


    private void HyperlinkButton_Release_Click(object sender, RoutedEventArgs e)
    {
        _ = Launcher.LaunchUriAsync(new Uri(_info.ReleaseUrl));
    }


    private bool _historyLoading;

    private async void HyperlinkButton_History_Click(object sender, RoutedEventArgs e)
    {
        if (_historyLoading)
        {
            return;
        }
        _historyLoading = true;
        HyperlinkButton_History.IsEnabled = false;

        NotesWeb.NavigateToString(BuildSimpleHtml(Localization.Lang.Instance.UpdateWindow_LoadingHistory));
        try
        {
            var releases = await SelfUpdateService.FetchRecentReleasesAsync();
            NotesWeb.NavigateToString(BuildHistoryHtml(releases));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: failed to load release history");
            NotesWeb.NavigateToString(BuildSimpleHtml(
                $"{Localization.Lang.Instance.SettingPage_UpdateFailed}: {ex.Message}"));
        }
        finally
        {
            _historyLoading = false;
            HyperlinkButton_History.IsEnabled = true;
        }
    }

    private string BuildHistoryHtml(IReadOnlyList<SelfUpdateService.ReleaseEntry> releases)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(
            releases.Select(r => new { v = r.Tag.TrimStart('v', 'V'), d = r.Date, b = r.Body ?? "" }),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
        ).Replace("</", "<\\/");

        const string extraStyle = """
  h2 { margin: 0.8em 0 0.2em; }
  h2 .date { font-size: 12px; font-weight: 400; opacity: .6; margin-left: 10px; }
  h3 { margin: 0.6em 0 0.3em; }
  hr { border: none; border-top: 1px solid #8886; margin: 14px 0; }
  .err { opacity: .8; padding-top: 40px; }
""";
        const string script = """
const releases = __BODY_JSON__;
const c = document.getElementById('c');
function esc(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }
releases.forEach((r, i) => {
  const h = document.createElement('h2');
  h.innerHTML = 'v' + esc(r.v) + (r.d ? ' <span class="date">' + esc(r.d) + '</span>' : '');
  c.appendChild(h);
  const box = document.createElement('div');
  try { box.innerHTML = marked.parse(r.b || ''); } catch (e) { box.textContent = r.b || ''; }
  c.appendChild(box);
  if (i < releases.length - 1) { c.appendChild(document.createElement('hr')); }
});
""";
        return WrapHtml(json, extraStyle, script);
    }

    private static string WrapHtml(string bodyJson, string extraStyle, string script)
    {
        return $$"""
<!doctype html><html><head><meta charset="utf-8">
<style>
  html { background: transparent; }
  body { font-family: 'Segoe UI', system-ui, sans-serif; font-size: 14px; line-height: 1.55;
         margin: 10px 24px; background: transparent; }
  @media (prefers-color-scheme: light) {
    body { color: #1b1b1b; } a { color: #0067c0; } pre { background: #0000000d; }
  }
  @media (prefers-color-scheme: dark) {
    body { color: #f1f1f1; } a { color: #7fbaff; } pre { background: #ffffff14; }
  }
  a { color: #7fbaff; }
  img { max-width: 100%; border-radius: 6px; }
  pre { padding: 8px; border-radius: 6px; overflow: auto; }
  code { font-family: Consolas, monospace; }
{{extraStyle}}  ul, ol { padding-left: 22px; }
  blockquote { border-left: 3px solid #8888; margin: 6px 0; padding-left: 10px; opacity: .85; }
</style>
<script src="https://cdn.jsdelivr.net/npm/marked@12.0.2/marked.min.js"></script>
</head><body><div id="c"></div>
<script>
{{script.TrimEnd('\n')}}
</script></body></html>
""".Replace("__BODY_JSON__", bodyJson);
    }

    private string BuildSimpleHtml(string message)
    {
        return """
<!doctype html><html><head><meta charset="utf-8">
<style>html{background:transparent}body{font-family:'Segoe UI',system-ui,sans-serif;font-size:14px;
margin:24px;opacity:.85}</style>
</head><body><div class="err">__MSG__</div></body></html>
""".Replace("__MSG__", System.Net.WebUtility.HtmlEncode(message));
    }

    private CancellationToken DialogToken => (_dialogCts ??= new CancellationTokenSource()).Token;

    private CancellationTokenSource? _dialogCts;

    private async void NotesWeb_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Arietta.Helpers.WebviewHost.UseDataFolder();
            await NotesWeb.EnsureCoreWebView2Async();
            NotesWeb.CoreWebView2.NavigationStarting += (w, args) =>
            {

                if (args.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                args.Cancel = true;
                _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
            };
            NotesWeb.CoreWebView2.NewWindowRequested += (w, args) =>
            {
                args.Handled = true;
                _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
            };
            NotesWeb.NavigateToString(BuildNotesHtml());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: failed to render release notes in WebView2");
            NotesWeb.Visibility = Visibility.Collapsed;
            TextBlock_NotesFallback.Text = _info.Notes ?? "";
            Scroll_NotesFallback.Visibility = Visibility.Visible;
        }
    }

    private string BuildNotesHtml()
    {

        string json = System.Text.Json.JsonSerializer.Serialize(_info.Notes ?? "").Replace("</", "<\\/");
        const string extraStyle = """
  h1, h2, h3 { margin: 0.6em 0 0.3em; }
""";
        const string script = """
const raw = __BODY_JSON__;
const c = document.getElementById('c');
try { c.innerHTML = marked.parse(raw); } catch (e) { c.textContent = raw; }
""";
        return WrapHtml(json, extraStyle, script);
    }
}
