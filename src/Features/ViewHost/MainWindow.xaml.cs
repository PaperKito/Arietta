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
        
        
#if DEBUG
        Title = $"Arietta ({File.GetLastWriteTime(typeof(MainWindow).Assembly.Location):MM-dd HH:mm})";
#else
        Title = "Arietta";
#endif
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


    public override void Hide()
    {
        
        Arietta.Features.DynamicBackground.DynamicBackgroundHost.Current?.SetWindowVisible(false);
        base.Hide();
        
        
        try { SetProcessWorkingSetSize(-1, -1, -1); } catch {  }
    }

    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(nint hProcess, nint min, nint max);

    public override void Show()
    {
        base.Show();
        Arietta.Features.DynamicBackground.DynamicBackgroundHost.Current?.SetWindowVisible(true);
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
        else if (uMsg == 0x0005) 
        {
            var host = Arietta.Features.DynamicBackground.DynamicBackgroundHost.Current;
            if (wParam == 1)
            {
                host?.SetWindowVisible(false);
            }
            else if (wParam == 0)
            {
                host?.SetWindowVisible(true);
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }

}
