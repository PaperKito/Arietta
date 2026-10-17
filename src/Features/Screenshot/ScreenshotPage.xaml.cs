using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Arietta.Features.ViewHost;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.Screenshot;

public sealed partial class ScreenshotPage : PageBase
{

    private readonly ILogger _logger = AppConfig.GetLogger<ScreenshotPage>();

    private int _totalCount;


    public ScreenshotPage()
    {
        this.InitializeComponent();
        UpdateCountText();
    }


    protected override async void OnLoaded()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            var folders = ScreenshotService.GetFolders(MainView.CurrentGameBiz);
            _logger.LogInformation("截图目录候选：{Folders}", string.Join(" | ", folders));

            var files = await Task.Run(() => ScreenshotService.EnumerateImages(folders));
            var groups = files
                .GroupBy(x => x.Time.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new ScreenshotGroup(g.Key.ToString("yyyy-MM-dd"), g.Select(x => new ScreenshotItem(x))))
                .ToList();

            _totalCount = files.Count;
            var source = new CollectionViewSource
            {
                IsSourceGrouped = true,
                Source = groups,
            };
            GridView_Images.ItemsSource = source.View;

            bool empty = groups.Count == 0;
            GridView_Images.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
            Panel_Empty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
            if (empty)
            {
                _logger.LogWarning("没有枚举到任何截图，候选目录：{Folders}", string.Join(" | ", folders));
            }
            UpdateCountText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载截图列表失败");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.MainPage_GameScreenshot);
        }
    }

    private void GridView_Images_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (ToggleButton_MultiSelect.IsChecked == true)
            {
                return;
            }
            if (e.ClickedItem is ScreenshotItem item)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FilePath,
                    UseShellExecute = true,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用默认看图工具打开截图失败");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.ScreenshotPage_OpenImage);
        }
    }


    private void Button_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? folder = ScreenshotService.GetFolders(MainView.CurrentGameBiz).FirstOrDefault();
            if (folder is null)
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.ScreenshotPage_Empty, Lang.Instance.ScreenshotPage_EmptyDesc);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开截图文件夹失败");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.ScreenshotPage_OpenFolder);
        }
    }


    private void ToggleButton_MultiSelect_Changed(object sender, RoutedEventArgs e)
    {
        try
        {
            bool multiSelect = ToggleButton_MultiSelect.IsChecked == true;
            if (!multiSelect)
            {
                GridView_Images.SelectedItems.Clear();
            }
            GridView_Images.SelectionMode = multiSelect ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
            Button_DeleteSelected.Visibility = multiSelect ? Visibility.Visible : Visibility.Collapsed;
            Button_DeleteSelected.IsEnabled = GridView_Images.SelectedItems.Count > 0;
            UpdateCountText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换多选模式失败");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.ScreenshotPage_MultiSelect);
        }
    }


    private void GridView_Images_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCountText();
        try
        {
            Button_DeleteSelected.IsEnabled = GridView_Images.SelectedItems.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新删除按钮状态失败");
        }
    }

    private async void Button_DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        var files = new List<string>();
        try
        {
            files = GridView_Images.SelectedItems.OfType<ScreenshotItem>().Select(x => x.FilePath).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取选中截图失败");
        }
        if (files.Count == 0)
        {
            InAppToast.MainWindow?.Warning(Lang.Instance.ScreenshotPage_SelectFirst);
            return;
        }

        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.Instance.ScreenshotPage_Delete,
                Content = string.Format(Lang.Instance.ScreenshotPage_DeleteConfirm, files.Count),
                PrimaryButtonText = Lang.Instance.ScreenshotPage_Delete,
                CloseButtonText = Lang.Instance.Common_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            bool ok = await Task.Run(() => RecycleBin.DeleteToRecycleBin(files));
            int remaining = files.Count(File.Exists);
            _logger.LogInformation("批量删除截图：ok={Ok}, remaining={Remaining}/{Total}", ok, remaining, files.Count);

            if (ok && remaining == 0)
            {
                InAppToast.MainWindow?.Success(string.Format(Lang.Instance.ScreenshotPage_Deleted, files.Count), null, 2500);
            }
            else if (remaining < files.Count)
            {
                InAppToast.MainWindow?.Warning(string.Format(Lang.Instance.ScreenshotPage_DeletedPartial, files.Count - remaining, files.Count));
            }
            else
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.ScreenshotPage_DeleteFailed);
            }
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除截图失败");
            InAppToast.MainWindow?.Error(ex, Lang.Instance.ScreenshotPage_Delete);
        }
    }


    private void UpdateCountText()
    {
        try
        {
            TextBlock_SelectCount.Text = ToggleButton_MultiSelect.IsChecked == true
                ? string.Format(Lang.Instance.ScreenshotPage_Selected0Of1, GridView_Images.SelectedItems.Count, _totalCount)
                : string.Format(Lang.Instance.ScreenshotPage_Total0Files, _totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新张数文本失败");
        }
    }

}
