using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Arietta.Frameworks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Graphics;
using Windows.System;

namespace Arietta.Features.Setting;

public sealed partial class UpdateWindow : WindowEx
{
    private static readonly ILogger _logger = AppConfig.GetLogger<UpdateWindow>();

    private const string NotesHost = "notes.arietta.local";

    private const string HideSkipLinkScript =
        "(() => {" +
        "  if (!document.getElementById('arietta-hide-skip')) {" +
        "    const st = document.createElement('style');" +
        "    st.id = 'arietta-hide-skip';" +
        "    st.textContent = 'a.js-skip-to-content{display:none !important;pointer-events:none !important;outline:none !important;}';" +
        "    (document.head || document.documentElement).appendChild(st);" +
        "  }" +
        "  const a = document.activeElement;" +
        "  if (a && a.classList && a.classList.contains('js-skip-to-content') && a.blur) a.blur();" +
        "})()";

    public const string ShowNotesConfigKey = "ShowUpdateContentAfterUpdateRestart";

    private SelfUpdateInfo _info;

    private readonly bool _whatsNew;

    private readonly string? _fromVersion;

    private bool _isDownloading;

    private int _focusCount;

    private readonly System.Collections.Generic.List<string> _focusTrace = new();

    private int _activateCount;

    private bool _webReady;

    public string CheckingText => Localization.Lang.Instance.UpdateWindow_Checking;

    private string ReleasesListUrl
    {
        get
        {
            int cut = _info.ReleaseUrl.IndexOf("/releases/tag/", StringComparison.OrdinalIgnoreCase);
            return cut > 0 ? _info.ReleaseUrl[..cut] + "/releases" : _info.ReleaseUrl;
        }
    }

    public string RemindLaterText { get; } = Localization.Lang.Instance.UpdateWindow_RemindLater;

    public UpdateWindow()
        : this(whatsNew: false, fromVersion: null)
    {
    }


    private UpdateWindow(bool whatsNew, string? fromVersion)
    {
        _whatsNew = whatsNew;
        _fromVersion = fromVersion;
        InitializeComponent();
        Title = "Arietta · Update";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));

        Panel_Checking.Visibility = Visibility.Visible;
        Panel_Left.Visibility = Visibility.Collapsed;
        Panel_Options.Visibility = Visibility.Collapsed;
        Panel_Actions.Visibility = Visibility.Collapsed;

        Closed += (_, _) =>
        {
            _closed = true;
            _notesGuard?.Stop();
            _dialogCts?.Cancel();
        };
        ApplyWindowPlacement();

        StartFocusStormProbe();
    }

    private void StartFocusStormProbe()
    {
        Activated += (_, _) => _activateCount++;
        RootGrid.GotFocus += (_, e) =>
        {
            _focusCount++;
            if (_focusTrace.Count < 12)
            {
                _focusTrace.Add(e.OriginalSource is FrameworkElement fe
                    ? (string.IsNullOrEmpty(fe.Name) ? fe.GetType().Name : fe.Name)
                    : "?");
            }
        };

        Microsoft.UI.Dispatching.DispatcherQueueTimer probe = DispatcherQueue.CreateTimer();
        probe.Interval = TimeSpan.FromSeconds(10);
        probe.IsRepeating = false;
        probe.Tick += (_, _) =>
        {
            probe.Stop();
            if (_focusCount > 30)
            {
                _logger.LogWarning(
                    "SelfUpdate: focus storm detected ({Count} GotFocus / {Activate} Activated in 10s): {Trace}",
                    _focusCount, _activateCount, string.Join(" -> ", _focusTrace));
            }
        };
        probe.Start();
    }


    public UpdateWindow(SelfUpdateInfo info)
        : this(whatsNew: false, fromVersion: null)
    {
        ApplyInfo(info);
    }

    public void ApplyInfo(SelfUpdateInfo info)
    {
        _info = info;
        IsWaiting = false;
        Panel_Checking.Visibility = Visibility.Collapsed;
        Panel_Left.Visibility = Visibility.Visible;
        if (!_whatsNew)
        {
            Panel_Options.Visibility = Visibility.Visible;
            Panel_Actions.Visibility = Visibility.Visible;
        }

        var lang = Localization.Lang.Instance;
        TextBlock_Title.Text = _whatsNew
            ? lang.UpdateWindow_WhatsNew
            : lang.SettingPage_UpdateAvailable;
        TextBlock_OldVersion.Text = _fromVersion ?? AppConfig.AppVersion;
        TextBlock_NewVersion.Text = info.Version;
        TextBlock_ArchLabel.Text = lang.UpdateWindow_Arch;

        TextBlock_Arch.Text = $"{AppConfig.AppArchitecture} · {(SelfUpdateService.IsPortableLayout ? "Portable" : "Dev")}";
        TextBlock_ChannelLabel.Text = lang.UpdateWindow_Channel;

        TextBlock_Channel.Text = lang.UpdateWindow_ChannelStable;
        TextBlock_BuildLabel.Text = lang.UpdateWindow_BuildTime;
        RefreshBuildTime();
        TextBlock_LinksLabel.Text = lang.SettingPage_About_Links;
        HyperlinkButton_Release.Content = lang.UpdateWindow_ReleasePage;
        Button_UpdateNow.Content = lang.UpdateWindow_UpdateNow;
        MenuFlyoutItem_Ignore.Text = lang.UpdateDialog_IgnoreVersion;
        CheckBox_ShowNotes.Content = lang.UpdateWindow_ShowNotes;
        CheckBox_ShowNotes.IsChecked = !string.Equals(
            AppConfig.GetConfig(ShowNotesConfigKey), "off", StringComparison.OrdinalIgnoreCase);

        RenderNotes(info.Notes);

        MaybeStartContent();
    }

    private void RefreshBuildTime()
    {
        string? time = _info?.ReleaseTime;

        TextBlock_Build.Text = string.IsNullOrWhiteSpace(time) ? "—" : time.Replace('-', '/');
    }

    private void MaybeStartContent()
    {
        if (!_uiReady || _contentStarted || _info is null)
        {
            return;
        }
        _contentStarted = true;
        _ = StartContentAsync();
    }

    private bool _uiReady;

    private bool _contentStarted;

    public bool IsWaiting { get; private set; } = true;

    private async System.Threading.Tasks.Task StartContentAsync()
    {
        if (!await EnsureNotesWebAsync() || _closed)
        {
            if (!_closed)
            {
                ShowNativeNotes();
            }
            return;
        }

        StartNotesGuard();

        if (_whatsNew)
        {
            _buildingNotes = true;
            _ = LoadNotesHtmlAsync();
            return;
        }

        _buildingNotes = true;
        _ = LoadNotesHtmlAsync();

        _ = EnrichAsync();
    }

    private void ApplyWindowPlacement()
    {
        DisplayArea display = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest);
        double scale = UIScale;

        if (_whatsNew)
        {
            int h = (int)(display.WorkArea.Height * 0.95);
            int w = (int)(h / 4.0 * 3.0);
            if (w > display.WorkArea.Width * 0.95)
            {
                w = (int)(display.WorkArea.Width * 0.95);
                h = (int)(w * 4.0 / 3.0);
            }
            CenterInScreen((int)Math.Round(w / scale), (int)Math.Round(h / scale));
            return;
        }

        int pw = (int)(1000 * scale), ph = (int)(750 * scale);
        if (ph > display.WorkArea.Height * 0.9 || pw > display.WorkArea.Width * 0.9)
        {
            ph = (int)(display.WorkArea.Height * 0.9);
            pw = (int)(ph * 4.0 / 3.0);
            if (pw > display.WorkArea.Width)
            {
                pw = (int)(display.WorkArea.Width * 0.9);
                ph = (int)(pw * 3.0 / 4.0);
            }
        }
        CenterInScreen((int)Math.Round(pw / scale), (int)Math.Round(ph / scale));
    }

    public UpdateWindow(SelfUpdateInfo info, bool whatsNew, string? fromVersion = null)
        : this(whatsNew, fromVersion)
    {

        Panel_Left.Visibility = Visibility.Collapsed;
        Grid.SetColumn(Panel_Notes, 0);
        Grid.SetColumnSpan(Panel_Notes, 2);
        Panel_Notes.Margin = new Thickness(0, 32, 0, 0);
        Title = Localization.Lang.Instance.UpdateWindow_WhatsNew;
        ApplyInfo(info);
        Panel_Left.Visibility = Visibility.Collapsed;
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

            ProgressBar_Download.IsIndeterminate = _info.AssetSize <= 0;
            string newAppDir = await SelfUpdateService.DownloadAndStageAsync(
                _info, new Progress<SelfUpdateProgress>(p =>
                {
                    if (_info.AssetSize > 0)
                    {
                        ProgressBar_Download.Value = p.Percent;
                        TextBlock_Percent.Text = $"{p.Percent:F0}%";
                        TextBlock_Bytes.Text = FormatSize(p.Received) + " / " + FormatSize(_info.AssetSize);
                    }
                    else
                    {

                        TextBlock_Percent.Text = "";
                        TextBlock_Bytes.Text = FormatSize(p.Received);
                    }
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

            (string title, string message) = SelfUpdateService.DescribeFailure(ex);
            TextBlock_ProgressMessage.Text = $"{title}：{message}";
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

    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        AppConfig.SetConfig(ShowNotesConfigKey, CheckBox_ShowNotes.IsChecked == true ? "on" : "off");
    }

    private void RenderNotes(string? notes)
    {
        RenderImages(notes);
        Rich_Notes.Blocks.Clear();
        IReadOnlyList<(string Text, bool Heading)> blocks = Arietta.Helpers.NotesText.ToBlocks(notes);
        if (blocks.Count == 0)
        {
            return;
        }
        foreach ((string text, bool heading) in blocks)
        {
            AddBlock(text, heading ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                heading ? 15 : 14, heading ? 12 : 0, heading ? 2 : 7);
        }
    }

    private void RenderImages(string? raw)
    {
        Images_Notes.Items.Clear();
        foreach (string url in Arietta.Helpers.NotesText.ExtractImages(raw))
        {
            try
            {
                Images_Notes.Items.Add(new Image
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(url)),

                    MaxWidth = Math.Max(320, (Scroll_Notes.ActualWidth > 0 ? Scroll_Notes.ActualWidth : 690) - 48),
                    Margin = new Thickness(0, 0, 0, 12),
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "SelfUpdate: skip note image {Url}", url);
            }
        }
    }


    private void AddBlock(string text, Windows.UI.Text.FontWeight weight, double size, double top, double bottom)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, top, 0, bottom) };

        foreach ((string segment, bool bold) in Arietta.Helpers.NotesText.SplitBold(text))
        {
            paragraph.Inlines.Add(new Run
            {
                Text = segment,
                FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : weight,
                FontSize = size,
            });
        }
        Rich_Notes.Blocks.Add(paragraph);
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        _uiReady = true;
        MaybeStartContent();
    }

    private System.Threading.Tasks.Task? _webInitTask;

    private async System.Threading.Tasks.Task<bool> EnsureNotesWebAsync()
    {
        if (_webReady)
        {
            return true;
        }
        _webInitTask ??= InitializeNotesWebAsync();
        try
        {
            await _webInitTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: WebView2 unavailable, using native notes");
        }
        return _webReady;
    }

    private async System.Threading.Tasks.Task WaitForNotesWebLoadedAsync()
    {
        for (int i = 0; i < 60; i++)
        {
            if (NotesWeb.XamlRoot is not null && NotesWeb.ActualWidth > 0)
            {
                return;
            }
            await System.Threading.Tasks.Task.Delay(50);
        }
        _logger.LogWarning(
            "SelfUpdate: webview still not laid out after 3s (loaded={Loaded}, width={Width})",
            NotesWeb.IsLoaded, NotesWeb.ActualWidth);
    }


    private async System.Threading.Tasks.Task InitializeNotesWebAsync()
    {
        await WaitForNotesWebLoadedAsync();

        Arietta.Helpers.WebviewHost.UseDataFolder();

        NotesWeb.CoreWebView2Initialized += (_, e) =>
        {
            if (e.Exception is not null)
            {
                _logger.LogError(e.Exception, "SelfUpdate: CoreWebView2 initialization failed");
            }
        };
        await NotesWeb.EnsureCoreWebView2Async();
        if (NotesWeb.CoreWebView2 is null)
        {

            await System.Threading.Tasks.Task.Delay(100);
            await NotesWeb.EnsureCoreWebView2Async();
        }
        if (NotesWeb.CoreWebView2 is null)
        {

            throw new InvalidOperationException(
                "CoreWebView2 is still null after EnsureCoreWebView2Async");
        }

        NotesWeb.UseSystemFocusVisuals = false;
        NotesWeb.CoreWebView2.Profile.PreferredColorScheme = IsDarkTheme
            ? Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark
            : Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light;

        NotesWeb.CoreWebView2.Settings.IsStatusBarEnabled = false;

        string notesDir = Path.Combine(AppConfig.LocalFolder, "webview");
        Directory.CreateDirectory(notesDir);
        NotesWeb.CoreWebView2.SetVirtualHostNameToFolderMapping(
            NotesHost, notesDir, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.DenyCors);

        NotesWeb.CoreWebView2.NavigationStarting += (w, args) =>
        {
            _navigating = true;
            _domReady = false;
            _logger.LogInformation("SelfUpdate: webview nav start {Uri}", args.Uri);
        };

        int webProcessFailures = 0;
        NotesWeb.CoreWebView2.ProcessFailed += (w, args) =>
        {
            if (++webProcessFailures == 1)
            {
                _logger.LogWarning("SelfUpdate: webview process failed {Kind} {Reason}",
                    args.ProcessFailedKind, args.Reason);
            }
        };

        NotesWeb.CoreWebView2.NewWindowRequested += (w, args) =>
        {
            args.Handled = true;
            _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
        };
        NotesWeb.CoreWebView2.DOMContentLoaded += async (w, _) =>
        {
            _domReady = true;
            if (_closed)
            {
                return;
            }
            try
            {
                await NotesWeb.CoreWebView2.ExecuteScriptAsync(HideSkipLinkScript);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SelfUpdate: skip-link css injection failed");
            }
        };
        NotesWeb.CoreWebView2.NavigationCompleted += async (w, args) =>
        {

            if (args.IsSuccess
                || args.WebErrorStatus != Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.ConnectionAborted)
            {
                _navigating = false;
            }
            if (_closed)
            {
                return;
            }
            if (!args.IsSuccess)
            {

                if (args.WebErrorStatus == Microsoft.Web.WebView2.Core.CoreWebView2WebErrorStatus.ConnectionAborted)
                {
                    _logger.LogInformation("SelfUpdate: navigation superseded, keep waiting");
                    return;
                }
                _logger.LogWarning("SelfUpdate: release page failed ({Status})", args.WebErrorStatus);
                ShowNativeNotes();
                return;
            }
            _logger.LogInformation("SelfUpdate: webview nav done {Uri}", NotesWeb.CoreWebView2.Source);
            _webNotesOk = true;
            _notesGuard?.Stop();
            NotesWeb.Visibility = Visibility.Visible;
            Scroll_Notes.Visibility = Visibility.Collapsed;
            try
            {

                await NotesWeb.CoreWebView2.ExecuteScriptAsync(HideSkipLinkScript);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SelfUpdate: failed to tidy release page");
            }

            try
            {
                NotesWeb.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SelfUpdate: focus webview failed");
            }
        };
        _webReady = true;
    }

    private void StartNotesGuard()
    {
        if (_notesGuard is null)
        {
            Microsoft.UI.Dispatching.DispatcherQueueTimer guard = DispatcherQueue.CreateTimer();
            guard.Interval = TimeSpan.FromSeconds(15);
            guard.IsRepeating = false;
            guard.Tick += (_, _) =>
            {
                guard.Stop();
                if (_closed)
                {
                    return;
                }

                if (!_webNotesOk && !_buildingNotes && !_domReady)
                {

                    if (_navigating && !_guardExtended)
                    {
                        _guardExtended = true;
                        guard.Start();
                        return;
                    }

                    _logger.LogWarning("SelfUpdate: notes page not rendered in 15s, falling back to native notes");
                    ShowNativeNotes();
                }
            };
            _notesGuard = guard;
        }
        _notesGuard.Stop();
        _notesGuard.Start();
    }

    private bool _webNotesOk;

    private bool _navigating;

    private bool _domReady;

    private bool _guardExtended;

    private bool _buildingNotes;

    private bool _closed;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _notesGuard;

    private bool _localHtmlNavigated;

    private void StartWebPage()
    {
        if (_info is null)
        {
            return;
        }
        NotesWeb.Source = new Uri(_info.ReleaseUrl);
    }

    private void ShowNativeNotes()
    {
        if (_closed)
        {
            return;
        }
        _webNotesOk = false;

        if (_info is null)
        {
            return;
        }
        if (Rich_Notes.Blocks.Count == 0)
        {
            RenderNotes(_info.Notes);
        }
        if (Rich_Notes.Blocks.Count == 0)
        {

            RenderNotes(Localization.Lang.Instance.UpdateWindow_NotesUnavailable);
            if (!_whatsNew)
            {

                _ = LoadDeferredNotesAsync();
            }
        }
        NotesWeb.Visibility = Visibility.Collapsed;
        Scroll_Notes.Visibility = Visibility.Visible;
    }

    private async System.Threading.Tasks.Task LoadNotesHtmlAsync()
    {

        if (!_notesBuildLock.Wait(0))
        {
            _logger.LogInformation("SelfUpdate: notes build already running, skip duplicate");
            return;
        }
        try
        {

            string cachePath = NotesCachePath();
            string? cachedHtml = null;
            if (File.Exists(cachePath))
            {
                string cached = await File.ReadAllTextAsync(cachePath, DialogToken);
                if (_closed)
                {
                    return;
                }
                if (cached is { Length: > 64 })
                {
                    await NavigateNotesAsync(cached);
                    cachedHtml = cached;
                    _logger.LogInformation("SelfUpdate: notes cache hit, shown instantly ({Path})", cachePath);
                }
            }

            List<(string Tag, string Date, string? Body, bool IsMarkdown)> items = [];
            if (_whatsNew)
            {

                IReadOnlyList<SelfUpdateService.ReleaseEntry> releases =
                    await SelfUpdateService.FetchNotesRangeAsync(null, _info.Version, DialogToken);
                _logger.LogInformation("SelfUpdate: whats-new history {Count} release(s) up to {To}",
                    releases.Count, _info.Version);

                foreach (SelfUpdateService.ReleaseEntry r in releases.Reverse())
                {
                    items.Add((r.Tag, r.Date, r.Body, false));
                }
            }
            else
            {
                string? body = _info.Notes;
                if (string.IsNullOrWhiteSpace(body))
                {

                    _logger.LogInformation("SelfUpdate: notes not available yet, waiting for enrich");
                    return;
                }
                string tag = _info.Version.StartsWith('v') ? _info.Version : "v" + _info.Version;
                items.Add((tag, string.Empty, body, _info.NotesIsMarkdown));
            }

            if (items.Count == 0)
            {
                _logger.LogWarning("SelfUpdate: no release notes to render (whatsNew={WhatsNew})", _whatsNew);
                if (_whatsNew)
                {
                    ShowNativeNotes();
                }

                return;
            }

            bool[] isMarkdown = new bool[items.Count];
            string[] rawBodies = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                rawBodies[i] = items[i].Body ?? "";
                isMarkdown[i] = items[i].IsMarkdown;
                if (rawBodies[i].Length > 0 && isMarkdown[i])
                {
                    items[i] = (items[i].Tag, items[i].Date,
                        Arietta.Helpers.ReleaseNotesHtml.PlainTextBody(rawBodies[i]), true);
                }
            }

            string?[] renderedBodies = new string?[items.Count];
            async System.Threading.Tasks.Task RenderOneAsync(int index)
            {
                if (rawBodies[index].Length == 0 || !isMarkdown[index])
                {
                    return;
                }
                renderedBodies[index] = await SelfUpdateService.TryRenderMarkdownAsync(rawBodies[index], DialogToken);
            }
            if (items.Count > 0)
            {
                System.Threading.Tasks.Task all = System.Threading.Tasks.Task.WhenAll(
                    Enumerable.Range(0, items.Count).Select(RenderOneAsync));

                System.Threading.Tasks.Task finished = await System.Threading.Tasks.Task.WhenAny(
                    all, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(10)));
                if (!ReferenceEquals(finished, all))
                {
                    _ = all.ContinueWith(static t => _ = t.Exception, System.Threading.Tasks.TaskScheduler.Default);
                    _logger.LogWarning("SelfUpdate: markdown render exceeded 10s, falling back to plain text");
                }
                for (int i = 0; i < items.Count; i++)
                {
                    if (renderedBodies[i] is { Length: > 0 } renderedHtml)
                    {
                        items[i] = (items[i].Tag, items[i].Date, renderedHtml, false);
                    }
                }
            }
            if (_closed)
            {
                return;
            }

            string? heroUrl = null;
            string? heroLocal = null;
            if (items.Count > 0)
            {
                IReadOnlyList<string> images = Arietta.Helpers.NotesText.ExtractImages(items[0].Body);
                if (images.Count > 0)
                {

                    heroUrl = Arietta.Helpers.ReleaseNotesHtml.NormalizeImageUrl(images[0]);
                    heroLocal = CachedImageRelativePath(heroUrl);
                }
            }

            string html = Arietta.Helpers.ReleaseNotesHtml.Build(
                items.Select(static x => (x.Tag, x.Date, x.Body ?? "")).ToList(), IsDarkTheme,
                CachedImageRelativePath,
                deferHeroImage: heroUrl is not null && heroLocal is null);
            if (cachedHtml is not null && string.Equals(html, cachedHtml, StringComparison.Ordinal))
            {

                _logger.LogInformation("SelfUpdate: notes unchanged, skip re-navigation");
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                await File.WriteAllTextAsync(cachePath, html, DialogToken);
                if (_closed)
                {
                    return;
                }
                await NavigateNotesAsync(html);
            }

            if (heroUrl is not null && heroLocal is null)
            {
                _ = WarmHeroCacheAsync(heroUrl);
            }

            const int coldImageLimit = 6;
            List<string> coldImages = [];
            foreach (var item in items)
            {
                foreach (string image in Arietta.Helpers.NotesText.ExtractImages(item.Body ?? ""))
                {
                    if (coldImages.Count >= coldImageLimit)
                    {
                        break;
                    }
                    string normalized = Arietta.Helpers.ReleaseNotesHtml.NormalizeImageUrl(image);
                    if (!string.Equals(normalized, heroUrl, StringComparison.OrdinalIgnoreCase)
                        && CachedImageRelativePath(normalized) is null
                        && !coldImages.Contains(normalized))
                    {
                        coldImages.Add(normalized);
                    }
                }
            }
            if (coldImages.Count > 0)
            {
                _ = WarmColdImagesAsync(coldImages);
            }

            _localHtmlNavigated = true;
            _logger.LogInformation("SelfUpdate: notes page built, {Count} release(s)", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SelfUpdate: failed to build notes page");

            if (!_webNotesOk && !_localHtmlNavigated)
            {
                ShowNativeNotes();
            }
        }
        finally
        {
            _buildingNotes = false;
            _notesBuildLock.Release();
        }
    }

    private readonly System.Threading.SemaphoreSlim _notesBuildLock = new(1, 1);

    private async System.Threading.Tasks.Task ShrinkHeroAsync(string file)
    {
        string tmpOut = file + ".shrink";
        try
        {
            if (!file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (new FileInfo(file).Length < 400 * 1024)
            {
                return;
            }
            const int MaxEdge = 1600;
            Microsoft.Graphics.Canvas.CanvasDevice device = Microsoft.Graphics.Canvas.CanvasDevice.GetSharedDevice();
            using (Microsoft.Graphics.Canvas.CanvasBitmap bitmap =
                   await Microsoft.Graphics.Canvas.CanvasBitmap.LoadAsync(device, file))
            {
                int srcW = (int)bitmap.SizeInPixels.Width;
                int srcH = (int)bitmap.SizeInPixels.Height;
                if (Math.Max(srcW, srcH) <= MaxEdge)
                {
                    return;
                }
                double scale = (double)MaxEdge / Math.Max(srcW, srcH);
                int dstW = Math.Max(1, (int)Math.Round(srcW * scale));
                int dstH = Math.Max(1, (int)Math.Round(srcH * scale));
                using var target = new Microsoft.Graphics.Canvas.CanvasRenderTarget(device, dstW, dstH, 96);
                using (Microsoft.Graphics.Canvas.CanvasDrawingSession ds = target.CreateDrawingSession())
                {
                    ds.DrawImage(bitmap,
                        new Windows.Foundation.Rect(0, 0, dstW, dstH),
                        new Windows.Foundation.Rect(0, 0, srcW, srcH),
                        1f,
                        Microsoft.Graphics.Canvas.CanvasImageInterpolation.HighQualityCubic);
                }
                await target.SaveAsync(tmpOut, Microsoft.Graphics.Canvas.CanvasBitmapFileFormat.Jpeg, 0.85f);
            }
            File.Move(tmpOut, file, true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SelfUpdate: shrink hero skipped");
        }
        finally
        {
            try
            {
                if (File.Exists(tmpOut))
                {
                    File.Delete(tmpOut);
                }
            }
            catch
            {

            }
        }
    }

    private static string HeroCacheBasePath(string url)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url));
        return Path.Combine(AppConfig.LocalFolder, "webview", "cache",
            "hero_" + Convert.ToHexString(hash)[..16].ToLowerInvariant());
    }


    private static readonly string[] HeroImageExtensions = [".jpg", ".png", ".webp", ".gif", ".avif"];

    private static string? CachedImageRelativePath(string url)
    {
        string basePath = HeroCacheBasePath(url);
        foreach (string ext in HeroImageExtensions)
        {
            try
            {
                var file = new FileInfo(basePath + ext);
                if (file.Exists && file.Length > 1024)
                {

                    return $"https://{NotesHost}/cache/{file.Name}";
                }
            }
            catch
            {

            }
        }
        return null;
    }

    private static readonly TimeSpan HeroDownloadCap = TimeSpan.FromSeconds(180);

    private async System.Threading.Tasks.Task WarmHeroCacheAsync(string url)
    {
        try
        {
            string file = await DownloadHeroAsync(url);

            if (!_closed && _domReady)
            {
                string localUrl = $"https://{NotesHost}/cache/{Path.GetFileName(file)}";
                string js = "(function(){var i=document.querySelector('.hero img');" +
                            "if(i){i.style.display='block';i.src=" + System.Text.Json.JsonSerializer.Serialize(localUrl) + ";}})()";
                await NotesWeb.CoreWebView2.ExecuteScriptAsync(js);
                _logger.LogInformation("SelfUpdate: hero image swapped to local copy");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SelfUpdate: hero image cache failed");

            if (!_closed && _domReady)
            {
                try
                {
                    string js = "(function(){var i=document.querySelector('.hero img');" +
                                "if(i){i.src=" + System.Text.Json.JsonSerializer.Serialize(url) + ";}})()";
                    await NotesWeb.CoreWebView2.ExecuteScriptAsync(js);
                    _logger.LogInformation("SelfUpdate: hero fetch failed, handed back to webview");
                }
                catch (Exception inner)
                {
                    _logger.LogDebug(inner, "SelfUpdate: hand hero back failed");
                }
            }
        }
    }

    private async System.Threading.Tasks.Task WarmColdImagesAsync(IReadOnlyList<string> urls)
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(8), DialogToken);
            foreach (string url in urls)
            {
                if (_closed)
                {
                    return;
                }
                try
                {
                    await DownloadHeroAsync(url);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "SelfUpdate: note image cache failed");
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    private async System.Threading.Tasks.Task<string> DownloadHeroAsync(string url)
    {
        string? tmp = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(DialogToken);
            cts.CancelAfter(HeroDownloadCap);
            using HttpResponseMessage response = await Arietta.Helpers.SharedHttp.SelfUpdate
                .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 40L * 1024 * 1024)
            {
                throw new InvalidOperationException("hero image too large");
            }
            string ext = response.Content.Headers.ContentType?.MediaType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                "image/avif" => ".avif",
                _ => ".jpg",
            };
            string file = HeroCacheBasePath(url) + ext;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            tmp = file + ".tmp";
            await using (FileStream fs = File.Create(tmp))
            {
                await response.Content.CopyToAsync(fs, cts.Token);
            }
            File.Move(tmp, file, true);
            tmp = null;
            long rawSize = new FileInfo(file).Length;

            await ShrinkHeroAsync(file);
            _logger.LogInformation("SelfUpdate: image cached ({Raw} KB → {Size} KB)",
                rawSize / 1024, new FileInfo(file).Length / 1024);
            return file;
        }
        finally
        {
            if (tmp is not null)
            {
                try
                {
                    File.Delete(tmp);
                }
                catch
                {

                }
            }
        }
    }

    private string NotesCachePath()
    {

        string key = _whatsNew
            ? $"v5_whatsnew_{_info.Version}"
            : $"v5_tag_{_info.Version}";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            key = key.Replace(c, '_');
        }
        return Path.Combine(AppConfig.LocalFolder, "webview", "cache",
            $"{key}_{(IsDarkTheme ? "dark" : "light")}.html");
    }

    private async System.Threading.Tasks.Task NavigateNotesAsync(string html)
    {
        string notesDir = Path.Combine(AppConfig.LocalFolder, "webview");
        Directory.CreateDirectory(notesDir);
        await File.WriteAllTextAsync(Path.Combine(notesDir, "notes.html"), html, DialogToken);
        if (_closed)
        {
            return;
        }

        NotesWeb.Source = new Uri($"https://{NotesHost}/notes.html?v={DateTime.UtcNow.Ticks}");
    }

    private async System.Threading.Tasks.Task EnrichAsync()
    {
        try
        {
            SelfUpdateInfo enriched = await SelfUpdateService.TryEnrichAsync(_info, DialogToken);
            if (_closed)
            {
                return;
            }
            _info = enriched;

            RefreshBuildTime();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SelfUpdate: enrich failed");
        }
        if (_closed || _whatsNew || _localHtmlNavigated)
        {
            return;
        }

        if (_info.Notes is not { Length: > 0 })
        {
            try
            {
                string tag = _info.Version.StartsWith('v') ? _info.Version : "v" + _info.Version;
                string? notes = await SelfUpdateService.TryFetchNotesAsync(tag, DialogToken);
                if (_closed)
                {
                    return;
                }
                if (notes is { Length: > 0 })
                {

                    _info = _info with { Notes = notes, NotesIsMarkdown = false };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "SelfUpdate: atom notes fallback failed");
            }
        }
        if (_closed)
        {
            return;
        }
        if (_info.Notes is { Length: > 0 })
        {

            _buildingNotes = true;
            _ = LoadNotesHtmlAsync();
        }
        else
        {

            StartWebPage();
        }
    }

    private async System.Threading.Tasks.Task LoadDeferredNotesAsync()
    {
        if (_info is null)
        {
            return;
        }
        try
        {
            string tag = _info.Version.StartsWith('v') ? _info.Version : "v" + _info.Version;
            string? notes = await SelfUpdateService.TryFetchNotesAsync(tag, DialogToken);
            if (_closed)
            {
                return;
            }
            if (notes is { Length: > 0 } && !_webNotesOk)
            {
                RenderNotes(notes);
                NotesWeb.Visibility = Visibility.Collapsed;
                Scroll_Notes.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SelfUpdate: deferred notes fetch failed");
        }
    }

    private bool IsDarkTheme => (Content as FrameworkElement)?.ActualTheme is not ElementTheme.Light;

    private CancellationToken DialogToken => (_dialogCts ??= new CancellationTokenSource()).Token;

    private CancellationTokenSource? _dialogCts;
}
