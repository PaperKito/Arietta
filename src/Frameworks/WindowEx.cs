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




    #region Message Loop



    private readonly ComCtl32.SUBCLASSPROC windowSubclassProc;



    protected virtual unsafe IntPtr WindowSubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }




    #endregion




    #region Window Method



    public virtual void Show()
    {
        AppWindow.Show(true);
        AppWindow.MoveInZOrderAtTop();
        User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(WindowHandle);
    }



    public virtual void Hide()
    {
        AppWindow.Hide();
    }



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



    #endregion




    #region Theme



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
        if (element.ActualTheme is ElementTheme.Dark)
        {
            element.RequestedTheme = ElementTheme.Light;
            element.RequestedTheme = ElementTheme.Dark;
        }
        else if (element.ActualTheme is ElementTheme.Light)
        {
            element.RequestedTheme = ElementTheme.Dark;
            element.RequestedTheme = ElementTheme.Light;
        }
    }



    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true)]
    public static partial bool ShouldAppsUseDarkMode();


    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
    protected static partial bool ShouldSystemUseDarkMode();



    #endregion


}

