using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI;

namespace Arietta.Frameworks;

public abstract partial class WindowEx : Window
{


    public IntPtr WindowHandle { get; private init; }


    public double UIScale => User32.GetDpiForWindow(WindowHandle) / 96d;


    public static Microsoft.UI.WindowId MainWindowId { get; protected set; }



    public WindowEx()
    {
        WindowHandle = (IntPtr)AppWindow.Id.Value;
        windowSubclassProc = new(WindowSubclassProc);
        ComCtl32.SetWindowSubclass(WindowHandle, windowSubclassProc, 1001, IntPtr.Zero);
    }

    private readonly ComCtl32.SUBCLASSPROC windowSubclassProc;



    protected virtual unsafe IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public virtual void Show()
    {

        Activate();
        AppWindow.MoveInZOrderAtTop();
    }



    public virtual void Hide()
    {
        AppWindow.Hide();
    }

    public void BringToFront()
    {
        const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_SHOWWINDOW = 0x0040;
        var topMost = new IntPtr(-1);
        var noTopMost = new IntPtr(-2);
        _ = SetWindowPos(WindowHandle, topMost, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        _ = SetWindowPos(WindowHandle, noTopMost, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        _ = User32.SetForegroundWindow(WindowHandle);
    }

    public void RestoreAndBringToFront()
    {

        bool needsRestore = !IsWindowVisible(WindowHandle) || IsIconic(WindowHandle);
        if (needsRestore)
        {
            _ = ShowWindow(WindowHandle, SW_RESTORE);
        }

        EnsureForeground();
        if (User32.GetForegroundWindow() != WindowHandle)
        {
            FlashTaskbarButton();
        }
    }

    public void FlashTaskbarButton()
    {
        FLASHWINFO info = new()
        {
            cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
            hwnd = WindowHandle,
            dwFlags = FLASHW_TRAY | FLASHW_TIMER,
            uCount = 6,
            dwTimeout = 0,
        };
        _ = FlashWindowEx(ref info);
    }


    private const int SW_RESTORE = 9;
    private const uint FLASHW_TRAY = 0x00000002;
    private const uint FLASHW_TIMER = 0x00000004;

    [StructLayout(LayoutKind.Sequential)]
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }

    public void EnsureForeground()
    {
        if (User32.GetForegroundWindow() == WindowHandle)
        {
            return;
        }
        _ = User32.SetForegroundWindow(WindowHandle);
        if (User32.GetForegroundWindow() == WindowHandle)
        {
            return;
        }
        BringToFront();
    }


    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);


    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);


    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);


    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);



    public virtual void CenterInScreen(int? width = null, int? height = null)
    {
        width = width <= 0 ? null : width;
        height = height <= 0 ? null : height;
        DisplayArea display = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest);
        double scale = UIScale;
        int w = (int)((width * scale) ?? AppWindow.Size.Width);
        int h = (int)((height * scale) ?? AppWindow.Size.Height);
        int x = display.WorkArea.X + (display.WorkArea.Width - w) / 2;
        int y = display.WorkArea.Y + (display.WorkArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }

    public void SetDragRectangles(params RectInt32[] value)
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            AppWindow.TitleBar.SetDragRectangles(value);
        }
    }


    public void AdaptTitleBarButtonColorToActuallTheme()
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            if (Content is FrameworkElement element)
            {
                switch (element.ActualTheme)
                {
                    case ElementTheme.Default:
                        break;
                    case ElementTheme.Light:
                        titleBar.ButtonForegroundColor = Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    case ElementTheme.Dark:
                        titleBar.ButtonForegroundColor = Colors.White;
                        titleBar.ButtonHoverForegroundColor = Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    default:
                        break;
                }
            }
        }
    }



    public virtual void ChangeAccentColor(Color? backColor = null, Color? foreColor = null)
    {
        FoldTheme(Content as FrameworkElement);

        if (Content is FrameworkElement root && root.XamlRoot is { } xamlRoot)
        {
            try
            {
                foreach (Microsoft.UI.Xaml.Controls.Primitives.Popup popup
                         in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
                {
                    FoldTheme(popup.Child as FrameworkElement);
                }
            }
            catch
            {

            }
        }
    }

    private static void FoldTheme(FrameworkElement? element)
    {
        if (element is null)
        {
            return;
        }
        element.RequestedTheme = element.ActualTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            _ => ElementTheme.Default,
        };
        element.RequestedTheme = ElementTheme.Default;
    }



    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
    public static partial bool ShouldAppsUseDarkMode();


    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
    protected static partial bool ShouldSystemUseDarkMode();

}

