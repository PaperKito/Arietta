using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Features.GameLauncher;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.GachaLog;

public sealed partial class GachaLogPage : PageBase
{

    public GachaLogViewModel ViewModel { get; } = new();


    public GachaLogPage()
    {
        this.InitializeComponent();
        
        _restoringGachaLanguage = true;
        ComboBox_GachaLanguage.SelectedIndex = Math.Max(0, Array.FindIndex(
            AnnouncementLanguageSettings.Options, o => o.Code == GachaLanguageSettings.Selected));
        _restoringGachaLanguage = false;
        InitializeGachaPools();
        
        ViewModel.CloudGameFetchRequested = OpenCloudGameWindow;
    }


    
    private bool _restoringGachaPools;


    
    
    
    
    private void InitializeGachaPools()
    {
        var items = GachaPools.DisplayOrder.Select(id => new GachaPoolItem(id, GachaPools.GetName(id))).ToList();
        ListView_GachaPools.ItemsSource = items;
        var visible = GachaPools.GetVisiblePools();
        _restoringGachaPools = true;
        try
        {
            if (visible is not null)
            {
                foreach (var item in items.Where(x => visible.Contains(x.PoolId)))
                {
                    ListView_GachaPools.SelectedItems.Add(item);
                }
            }
            if (ListView_GachaPools.SelectedItems.Count == 0)
            {
                foreach (var item in items)
                {
                    ListView_GachaPools.SelectedItems.Add(item);
                }
            }
        }
        finally
        {
            _restoringGachaPools = false;
        }
    }


    private void ListView_GachaPools_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringGachaPools)
        {
            return;
        }
        
        GachaPools.SetVisiblePools(ListView_GachaPools.SelectedItems.Cast<GachaPoolItem>().Select(x => x.PoolId));
        ViewModel.ApplyVisiblePools();
    }


    
    public Visibility EmptyVisibility(bool hasData, bool isLoading) => hasData || isLoading ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ContentVisibility(bool hasData) => hasData ? Visibility.Visible : Visibility.Collapsed;


    
    
    
    
    
    private void ToggleItemStatsPane_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPaneOpen = !ViewModel.IsPaneOpen;
    }


    
    
    private bool _restoringGachaLanguage;


    
    private void ComboBox_GachaLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringGachaLanguage)
        {
            return;
        }
        int index = ComboBox_GachaLanguage.SelectedIndex;
        if (index < 0 || index >= AnnouncementLanguageSettings.Options.Length)
        {
            return;
        }
        ViewModel.ApplyGachaLanguageCommand.Execute(AnnouncementLanguageSettings.Options[index].Code);
    }


    protected override void OnLoaded()
    {
        base.OnLoaded();
        try
        {
            ViewModel.Initialize();
        }
        catch (Exception e)
        {
            InAppToast.MainWindow?.Error(e);
        }
    }


    private void DeletePlayer_Click(object sender, RoutedEventArgs e)
    {
        _ = DeletePlayerAsync();
    }


    private async Task DeletePlayerAsync()
    {
        if (ViewModel.SelectedPlayer is not { } playerId)
        {
            return;
        }
        var dialog = new ContentDialog
        {
            Title = Lang.Instance.GachaLogPage_DeletePlayer,
            Content = string.Format(Lang.Instance.GachaLogPage_DeleteConfirmText, playerId),
            PrimaryButtonText = Lang.Instance.GachaLogPage_Delete,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.DeletePlayerCommand.Execute(null);
        }
    }


    private void ImportUrl_Click(object sender, RoutedEventArgs e)
    {
        _ = ImportUrlAsync();
    }


    
    private void ImportRecords_Click(object sender, RoutedEventArgs e)
    {
        _ = ImportRecordsAsync();
    }


    
    private void ExportUgf_Click(object sender, RoutedEventArgs e)
    {
        _ = ExportRecordsAsync("json");
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        _ = ExportRecordsAsync("xlsx");
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        _ = ExportRecordsAsync("csv");
    }


    
    private async Task ImportRecordsAsync()
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".csv");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            var file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }
            string ext = file.FileType.ToLowerInvariant();
            if (ext == ".json")
            {
                string json = await Windows.Storage.FileIO.ReadTextAsync(file);
                await ViewModel.ImportRecordsAsync(json);
            }
            else
            {
                byte[] data = await System.IO.File.ReadAllBytesAsync(file.Path);
                await ViewModel.ImportTableRecordsAsync(file.Name, data);
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GachaLogPage_ImportExport);
        }
    }


    
    private async Task ExportRecordsAsync(string format)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = $"GachaLog_{ViewModel.SelectedPlayer}_{DateTime.Now:yyyyMMdd_HHmm}";
            
            picker.FileTypeChoices.Add(format switch
            {
                "xlsx" => "Excel",
                "csv" => "CSV",
                _ => "JSON",
            }, [format == "json" ? ".json" : "." + format]);
            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }
            if (format is "xlsx" or "csv")
            {
                var pool = ViewModel.GetExportPool(ViewModel.SelectedPlayer);
                if (pool is null || pool.Count == 0)
                {
                    InAppToast.MainWindow?.Warning(Lang.Instance.GachaLogPage_ImportExport, Lang.Instance.GachaLogPage_ExportEmpty);
                    return;
                }
                if (format == "xlsx")
                {
                    byte[] data = await Task.Run(() => GachaTableImporter.ExportXlsx(ViewModel.SelectedPlayer ?? "", pool));
                    await Windows.Storage.FileIO.WriteBytesAsync(file, data);
                }
                else
                {
                    string csv = await Task.Run(() => GachaTableImporter.ExportCsv(ViewModel.SelectedPlayer ?? "", pool));
                    await Windows.Storage.FileIO.WriteTextAsync(file, csv);
                }
            }
            else
            {
                string? json = ViewModel.BuildExportJson(ViewModel.SelectedPlayer);
                if (json is null)
                {
                    InAppToast.MainWindow?.Warning(Lang.Instance.GachaLogPage_ImportExport, Lang.Instance.GachaLogPage_ExportEmpty);
                    return;
                }
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GachaLogPage_ImportExport);
        }
    }


    
    
    
    
    private void CloudGame_Click(object sender, RoutedEventArgs e)
    {
        OpenCloudGameWindow();
    }


    
    private void OpenCloudGameWindow()
    {
        var window = new CloudGameWindow(async parameters => await ViewModel.ImportFromParamsAsync(parameters));
        window.Activate();
    }


    
    private void ClearSavedUrl_Click(object sender, RoutedEventArgs e)
    {
        _ = ClearSavedUrlAsync();
    }


    private async Task ClearSavedUrlAsync()
    {
        if (ViewModel.SelectedPlayer is null)
        {
            return;
        }
        var dialog = new ContentDialog
        {
            Title = Lang.Instance.GachaLogPage_ClearSavedUrl,
            Content = Lang.Instance.GachaLogPage_ClearSavedUrlConfirm,
            PrimaryButtonText = Lang.Instance.Common_Confirm,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.ClearSavedUrlCommand.Execute(null);
        }
    }


    
    private async Task ImportUrlAsync()
    {
        var input = new TextBox
        {
            PlaceholderText = Lang.Instance.GachaLogPage_ImportUrlPlaceholder,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = false,
        };
        var dialog = new ContentDialog
        {
            Title = Lang.Instance.GachaLogPage_ImportUrl,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = Lang.Instance.GachaLogPage_ImportUrlDesc,
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.Colors.Gray),
                        TextWrapping = TextWrapping.Wrap,
                    },
                    input,
                },
            },
            PrimaryButtonText = Lang.Instance.Common_Confirm,
            CloseButtonText = Lang.Instance.Common_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && input.Text is { Length: > 0 } url)
        {
            await ViewModel.ImportFromUrlAsync(url);
        }
    }

}



public sealed record GachaPoolItem(string PoolId, string Name);
