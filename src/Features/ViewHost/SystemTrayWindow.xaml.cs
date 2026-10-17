using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Arietta.Frameworks;
using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Foundation;

namespace Arietta.Features.ViewHost;

public sealed partial class SystemTrayWindow : WindowEx
{

    public SystemTrayWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        SetTrayIcon();
    }




    private unsafe void InitializeWindow()
    {
        SystemBackdrop = new DesktopAcrylicBackdrop();

        AppWindow.IsShownInSwitchers = false;
        AppWindow.Closing += (s, e) => e.Cancel = true;
        this.Activated += SystemTrayWindow_Activated;
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        var flag = User32.GetWindowLongPtr(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
        flag &= ~(nint)User32.WindowStyles.WS_CAPTION;
        flag &= ~(nint)User32.WindowStyles.WS_BORDER;
        User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, flag);
        int corner = DWMWCP_ROUND;
        DwmSetWindowAttribute(WindowHandle, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));

        Show();
        Hide();
    }

    private void SetTrayIcon()
    {
        try
        {
            nint hIcon = User32.LoadIcon(HINSTANCE.NULL, (ResourceId)IDI_APPLICATION).DangerousGetHandle();
            if (hIcon != 0)
            {
                trayIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);
            }
        }
        catch { }
    }




    private void SystemTrayWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is WindowActivationState.Deactivated)
        {
            Hide();
        }
    }




    [RelayCommand]
    public override void Show()
    {
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        RootGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        SIZE windowSize = new()
        {
            Width = (int)(RootGrid.DesiredSize.Width * UIScale),
            Height = (int)(RootGrid.DesiredSize.Height * UIScale)
        };
        User32.GetCursorPos(out POINT point);
        User32.CalculatePopupWindowPosition(point, windowSize, User32.TrackPopupMenuFlags.TPM_RIGHTALIGN | User32.TrackPopupMenuFlags.TPM_BOTTOMALIGN | User32.TrackPopupMenuFlags.TPM_WORKAREA, null, out RECT windowPos);
        User32.MoveWindow(WindowHandle, windowPos.X, windowPos.Y, windowPos.Width, windowPos.Height, true);
        base.Show();
    }




    [RelayCommand]
    public override void Hide()
    {
        base.Hide();
    }




    [RelayCommand]
    public void ShowMainWindow()
    {
        App.Current.EnsureMainWindow();
    }




    [RelayCommand]
    private void Exit()
    {
        App.Current.ExitApp();
    }

    public void DisposeTrayIcon()
    {
        trayIcon?.Dispose();
    }




    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        DisposeTrayIcon();
    }




    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    private const int DWMWCP_ROUND = 2;

    private const int IDI_APPLICATION = 32512;


    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int value, int size);


}
