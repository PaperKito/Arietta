using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Arietta.Frameworks;
using System;
using System.IO;
using Windows.Graphics;

namespace Arietta.Features.ViewHost;

public sealed partial class MainWindow : WindowEx
{

    public static new MainWindow Current { get; private set; }


    public MainWindow()
    {
        Current = this;
        MainWindowId = AppWindow.Id;
        this.InitializeComponent();
        InitializeMainWindow();
    }


    private void InitializeMainWindow()
    {
        
        Title = $"Arietta ({File.GetLastWriteTime(typeof(MainWindow).Assembly.Location):MM-dd HH:mm})";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        CenterInScreen(1200, 676);
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));
        
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        
        AppWindow.Changed += (s, e) =>
        {
            bool visible = !(s.Presenter is OverlappedPresenter p && p.State == OverlappedPresenterState.Minimized)
                           && Vanara.PInvoke.User32.IsWindowVisible(WindowHandle);
            Arietta.Features.DynamicBackground.DynamicBackgroundHost.Current?.SetWindowVisible(visible);
        };
        
        AppWindow.Closing += (s, e) =>
        {
            if (AppConfig.GetConfig("CloseWindowAction") != "Exit")
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                App.Current.ExitApp();
            }
        };
    }


    protected override nint WindowSubclassProc(Vanara.PInvoke.HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == 0x0112) 
        {
            if (wParam == 0xF030)
            {
                
                return IntPtr.Zero;
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }

}
