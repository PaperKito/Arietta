using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Vanara.PInvoke;
using Arietta.Localization;

namespace Arietta.Features.KuroBbs;

internal sealed partial class KuroBbsGeetestWindow : Window
{

    private const int BaseWidth = 640;
    private const int BaseHeight = 500;

    private static KuroBbsGeetestWindow? _current;

    private readonly TaskCompletionSource<string?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly KuroBbsGeetest _geetest;
    private readonly CancellationTokenSource _lifeCts = new();
    private readonly CancellationTokenRegistration _cancelReg;
    private bool _started;

    private KuroBbsGeetestWindow(CancellationToken ct)
    {
        InitializeComponent();
        Title = Lang.Instance.Login_CaptchaTitle;
        _geetest = new KuroBbsGeetest(Web);

        int width = BaseWidth;
        int height = BaseHeight;
        try
        {
            double scale = User32.GetDpiForWindow((IntPtr)AppWindow.Id.Value) / 96d;
            if (scale > 1)
            {
                width = (int)(BaseWidth * scale);
                height = (int)(BaseHeight * scale);
            }
        }
        catch { }

        AppWindow.Resize(new SizeInt32(width, height));
        try
        {
            var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary)?.WorkArea;
            if (area is { } rect)
            {
                AppWindow.Move(new PointInt32(
                    rect.X + (rect.Width - width) / 2,
                    rect.Y + (rect.Height - height) / 2));
            }
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger<KuroBbsGeetestWindow>().LogWarning(ex, "Failed to configure geetest window");
        }

        Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, this))
            {
                _current = null;
            }
            _lifeCts.Cancel();
            _result.TrySetResult(null);
        };
        _cancelReg = ct.Register(() => DispatcherQueue.TryEnqueue(() => Close()));

        Activated += (_, _) =>
        {
            if (_started)
            {
                return;
            }
            _started = true;
            _ = RunAsync();
        };
    }

    private async Task RunAsync()
    {
        string? data = await _geetest.WaitForValidateAsync(_lifeCts.Token);
        if (_result.TrySetResult(data))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _cancelReg.Dispose();
                Close();
            });
        }
    }

    public static Task<string?> ShowAsync(CancellationToken ct)
    {
        var window = new KuroBbsGeetestWindow(ct);
        _current = window;
        window.Activate();
        return window._result.Task;
    }
}
