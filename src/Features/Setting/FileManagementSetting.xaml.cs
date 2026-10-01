using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;
using Arietta.Features.Database;
using Arietta.Localization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinRT.Interop;

namespace Arietta.Features.Setting;





public sealed partial class FileManagementSetting : PageBase
{

    public FileManagementSetting()
    {
        this.InitializeComponent();
    }


    protected override void OnLoaded()
    {
        base.OnLoaded();
        RefreshDataFolder();
        RefreshCacheSize();
    }


    #region 数据文件夹

    private void RefreshDataFolder()
    {
        TextBlock_DataPath.Text = DatabaseService.DatabaseFolder;
    }


    private async void Button_ReselectDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            var window = (App.Current as App)?.MainWindow;
            if (window is null)
            {
                return;
            }
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }
            AppConfig.SetConfig(DatabaseService.ConfigKey_UserDataFolder, folder.Path);
            await ShowInfoAsync(Lang.Instance.FilePage_ReselectFolderDone);
        }
        catch (Exception ex)
        {
            await ShowInfoAsync($"{Lang.Instance.Common_Error}：{ex.Message}");
        }
    }


    private void Button_OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        OpenInExplorer(DatabaseService.DatabaseFolder);
    }


    private async void Button_DeleteSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Lang.Instance.FilePage_DeleteAllSettings,
            Content = Lang.Instance.FilePage_DeleteSettingsConfirm,
            PrimaryButtonText = Lang.Instance.Common_Confirm,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }
        try
        {
            if (File.Exists(AppConfig.ConfigFile))
            {
                File.Delete(AppConfig.ConfigFile);
            }
            await ShowInfoAsync(Lang.Instance.FilePage_DeleteSettingsDone);
        }
        catch (Exception ex)
        {
            await ShowInfoAsync($"{Lang.Instance.Common_Error}：{ex.Message}");
        }
    }

    #endregion


    #region 日志

    private void Button_OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppConfig.LogFolder);
        OpenInExplorer(AppConfig.LogFolder);
    }

    #endregion


    #region 缓存

    
    private static string GachaIconFolder => Path.Combine(AppConfig.LocalFolder, "GachaIcons");

    private void RefreshCacheSize()
    {
        TextBlock_CacheLogSize.Text = FormatSize(GetDirectorySize(AppConfig.LogFolder));
        TextBlock_CacheImageSize.Text = FormatSize(GetDirectorySize(GachaIconFolder));
        TextBlock_CacheBackgroundSize.Text = FormatSize(
            GetDirectorySize(AppConfig.BackgroundFolder) + GetDirectorySize(AppConfig.DynamicBackgroundFolder));
    }


    private async void Button_CleanCache_Click(object sender, RoutedEventArgs e)
    {
        
        bool cleanBg = CheckBox_CleanBackground.IsChecked == true;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Lang.Instance.FilePage_CleanCache,
            Content = cleanBg
                ? "将清理日志与图片，并删除全部启动器背景文件（含官方动态背景视频），确定继续？"
                : "将清理日志与图片，确定继续？",
            PrimaryButtonText = Lang.Instance.Common_Confirm,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }
        await Task.Run(() =>
        {
            CleanFolder(AppConfig.LogFolder);
            CleanFolder(GachaIconFolder);
            if (cleanBg)
            {
                CleanFolder(AppConfig.BackgroundFolder);
                CleanFolder(AppConfig.DynamicBackgroundFolder);
            }
        });
        RefreshCacheSize();
    }


    private static void CleanFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion


    #region 通用

    private static void OpenInExplorer(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
        catch { }
    }


    private async Task ShowInfoAsync(string message)
    {
        await new ContentDialog
        {
            XamlRoot = XamlRoot,
            Content = message,
            CloseButtonText = Lang.Instance.Common_Confirm,
            DefaultButton = ContentDialogButton.Close,
        }.ShowAsync();
    }


    private static long GetDirectorySize(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }
            return new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }


    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            >= 1L << 30 => $"{bytes / 1073741824.0:F2} GB",
            >= 1L << 20 => $"{bytes / 1048576.0:F2} MB",
            >= 1L << 10 => $"{bytes / 1024.0:F2} KB",
            _ => $"{bytes} B",
        };
    }

    #endregion

}
