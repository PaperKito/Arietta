using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using Arietta.Features.Database;
using Arietta.Features.GameLauncher;
using Arietta.Helpers;
using Arietta.Localization;
using Microsoft.Extensions.Logging;
using Windows.Foundation;
using Windows.UI;

namespace Arietta.Features.GachaLog;




public partial class GachaLogViewModel : ObservableObject
{
    private readonly GachaApiClient _apiClient = new();

    
    private const string LastPlayerKey = "gacha_last_player";

    public ObservableCollection<string> Players { get; } = [];

    public ObservableCollection<PoolViewModel> Pools { get; } = [];

    
    public ObservableCollection<GachaItemStat> ItemStats { get; } = [];

    [ObservableProperty]
    public partial string? SelectedPlayer { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    
    public Action? CloudGameFetchRequested { get; set; }

    public bool HasData => Pools.Count > 0;

    public bool HasItemStats => ItemStats.Count > 0;

    
    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }


    public void Initialize()
    {
        
        GachaStore.ImportAllLegacy();
        foreach (string player in GachaStore.LoadPlayers())
        {
            Players.Add(player);
        }
        if (Players.Count > 0)
        {
            
            SelectedPlayer = PickDefaultPlayer();
        }
        else
        {
            LoadPlayerData(null);
        }
    }


    
    
    
    
    
    private string PickDefaultPlayer()
    {
        
        string? gameUid = null;
        try { gameUid = KrSdkAccountService.GetCurrentAccount(GameBiz.WutheringWaves)?.DisplayUid; } catch { }
        if (!string.IsNullOrEmpty(gameUid) && Players.Contains(gameUid))
        {
            return gameUid;
        }
        string? last = DatabaseService.GetValue<string>(LastPlayerKey);
        return last is not null && Players.Contains(last) ? last : Players[^1];
    }


    
    partial void OnSelectedPlayerChanged(string? value)
    {
        
        if (value is not null)
        {
            DatabaseService.SetValue(LastPlayerKey, value);
        }
        LoadPlayerData(value);
    }


    
    
    
    
    
    [RelayCommand]
    private async Task RefreshAsync(string? mode)
    {
        if (IsLoading)
        {
            return;
        }
        
        await RunBusyAsync(async () =>
        {
            GachaParams? parameters;
            if (mode is "cache")
            {
                parameters = SelectedPlayer is null ? null : GachaStore.LoadParams(SelectedPlayer);
                if (parameters is null || string.IsNullOrEmpty(parameters.RecordId))
                {
                    SetStatus(Lang.Instance.GachaLogPage_NoSavedUrl, true);
                    return;
                }
            }
            else
            {
                
                GachaParams? fromLog = null;
                string? logFile = GachaStore.FindGameLogFile();
                if (logFile is not null)
                {
                    string? url = GachaLogUtil.GetLogFileUrl(logFile);
                    if (url is not null)
                    {
                        fromLog = GachaLogUtil.GetParamFromUrl(url);
                    }
                }
                if (fromLog is null || string.IsNullOrEmpty(fromLog.RecordId))
                {
                    fromLog = SelectedPlayer is null ? null : GachaStore.LoadParams(SelectedPlayer);
                }
                if (fromLog is null)
                {
                    SetStatus(Lang.Instance.GachaLogPage_LogNotFound, true);
                    return;
                }
                parameters = fromLog;
            }
            await FetchAndSaveAsync(parameters);
        }, "更新调频记录失败", Lang.Instance.GachaLogPage_UpdateFailed);
    }


    
    [RelayCommand]
    private void ClearSavedUrl()
    {
        if (SelectedPlayer is not { } playerId)
        {
            SetStatus(Lang.Instance.GachaLogPage_NoSavedUrl, true);
            return;
        }
        if (GachaStore.ClearParams(playerId))
        {
            SetStatus(Lang.Instance.GachaLogPage_ClearSavedUrlDone, false);
        }
        else
        {
            SetStatus(Lang.Instance.GachaLogPage_NoSavedUrl, true);
        }
    }


    
    
    
    
    
    
    [RelayCommand]
    private async Task ApplyGachaLanguageAsync(string? code)
    {
        GachaLanguageSettings.SetSelected(string.IsNullOrWhiteSpace(code) ? null : code);

        
        LoadPlayerData(SelectedPlayer);

        var parameters = SelectedPlayer is { } playerId ? GachaStore.LoadParams(playerId) : null;
        if (parameters is null || string.IsNullOrEmpty(parameters.RecordId))
        {
            
            InAppToast.MainWindow?.Success(Lang.Instance.GachaLogPage_Language, Lang.Instance.GachaLogPage_LanguageSaved);
            return;
        }
        await RunBusyAsync(async () =>
        {
            
            int? renamed = await FetchAndSaveAsync(parameters, toastOnFailure: false);
            if (renamed is not null)
            {
                
                string display = code ?? GachaLanguageSettings.GetDisplayName(GachaLanguageSettings.Effective);
                InAppToast.MainWindow?.Success(Lang.Instance.GachaLogPage_Language,
                    string.Format(Lang.Instance.GachaLogPage_LanguageApplied, renamed, display));
            }
            else
            {
                InAppToast.MainWindow?.Success(Lang.Instance.GachaLogPage_Language, Lang.Instance.GachaLogPage_LanguageSaved);
            }
        }, "应用抽卡语言失败", Lang.Instance.GachaLogPage_UpdateFailed);
    }


    
    public async Task ImportFromUrlAsync(string? url)
    {
        if (IsLoading)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("#/record", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus(Lang.Instance.GachaLogPage_UrlInvalid, true);
            return;
        }
        await RunBusyAsync(async () =>
        {
            var parameters = GachaLogUtil.GetParamFromUrl(url.Trim());
            if (string.IsNullOrEmpty(parameters.PlayerId) || string.IsNullOrEmpty(parameters.RecordId))
            {
                SetStatus(Lang.Instance.GachaLogPage_UrlInvalid, true);
                return;
            }
            await FetchAndSaveAsync(parameters);
        }, "导入唤取链接失败", Lang.Instance.GachaLogPage_UpdateFailed);
    }


    
    public async Task ImportFromParamsAsync(GachaParams parameters)
    {
        if (IsLoading)
        {
            return;
        }
        await RunBusyAsync(() => FetchAndSaveAsync(parameters),
            "导入云游戏唤取链接失败", Lang.Instance.GachaLogPage_UpdateFailed);
    }


    
    
    private async Task<int?> FetchAndSaveAsync(GachaParams parameters, bool toastOnFailure = true)
    {
        
        
        parameters.LanguageCode = GachaLanguageSettings.Effective;
        var result = await _apiClient.FetchAllPoolsAsync(parameters);
        if (result.Data.Count == 0)
        {
            if (!toastOnFailure)
            {
                return null;
            }
            if (result.Code != 0)
            {
                
                
                InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Error,
                                                      Lang.Instance.GachaLogPage_LinkExpired,
                                                      string.Format(Lang.Instance.GachaLogPage_LinkExpiredDesc, result.Code, result.Message),
                                                      Lang.Instance.GachaLogPage_RefetchFromCloudGame,
                                                      () => CloudGameFetchRequested?.Invoke());
            }
            else
            {
                SetStatus(Lang.Instance.GachaLogPage_FetchFailed, true);
            }
            return null;
        }

        var newData = result.Data;
        string playerId = parameters.PlayerId;
        
        
        
        int renamedCount = GachaStore.Save(playerId, newData, parameters);

        if (!Players.Contains(playerId))
        {
            Players.Add(playerId);
        }

        
        bool isSamePlayer = playerId == SelectedPlayer;
        SelectedPlayer = playerId;
        if (isSamePlayer)
        {
            LoadPlayerData(playerId);
        }
        
        return renamedCount;
    }


    [RelayCommand]
    private void CopyUrl()
    {
        string? logFile = GachaStore.FindGameLogFile();
        string? url = logFile is null ? null : GachaLogUtil.GetLogFileUrl(logFile);
        if (url is null)
        {
            SetStatus(Lang.Instance.GachaLogPage_UrlNotFound, true);
            return;
        }
        try
        {
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
            package.SetText(url);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            SetStatus(Lang.Instance.GachaLogPage_UrlCopied, false);
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<GachaLogViewModel>().LogError(e, "复制链接失败");
        }
    }


    [RelayCommand]
    private void OpenDataFolder()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = GachaStore.DataFolder,
                UseShellExecute = true,
            });
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<GachaLogViewModel>().LogError(e, "打开数据目录失败");
        }
    }


    [RelayCommand]
    private void DeletePlayer()
    {
        if (SelectedPlayer is not { } playerId)
        {
            return;
        }
        GachaStore.DeletePlayer(playerId);
        int index = Players.IndexOf(playerId);
        if (index >= 0)
        {
            Players.RemoveAt(index);
        }
        SelectedPlayer = Players.Count > 0 ? Players[^1] : null;
        if (SelectedPlayer is null)
        {
            LoadPlayerData(null);
        }
        SetStatus(string.Format(Lang.Instance.GachaLogPage_PlayerDeleted, playerId), false);
    }


    
    public void ApplyVisiblePools()
    {
        LoadPlayerData(SelectedPlayer);
    }


    
    private void LoadPlayerData(string? playerId)
    {
        Pools.Clear();
        var data = playerId is null ? null : GachaStore.LoadPool(playerId);
        if (data is not null)
        {
            var visiblePools = GachaPools.GetVisiblePools();
            
            List<GachaRecord>? StandardProvider(string poolId) => data.TryGetValue(poolId, out var v) ? v : null;

            foreach (string poolId in GachaPools.DisplayOrder)
            {
                if (visiblePools is { Count: > 0 } && !visiblePools.Contains(poolId))
                {
                    continue;
                }
                if (data.TryGetValue(poolId, out var records) && records is { Count: > 0 })
                {
                    Pools.Add(new PoolViewModel(GachaStatService.ComputePool(poolId, records, StandardProvider)));
                }
            }
            BuildItemStats(data);
        }
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(HasItemStats));
    }


    
    private void BuildItemStats(Dictionary<string, List<GachaRecord>> data)
    {
        ItemStats.Clear();
        var counter = new Dictionary<int, GachaItemStat>();
        foreach (var records in data.Values)
        {
            foreach (var record in records)
            {
                if (record.QualityLevel < 4)
                {
                    continue;
                }
                int count = Math.Max(record.Count, 1);
                if (counter.TryGetValue(record.ResourceId, out var stat))
                {
                    stat.Count += count;
                }
                else
                {
                    counter[record.ResourceId] = new GachaItemStat(record.ResourceId, record.Name, record.QualityLevel, count);
                }
            }
        }
        foreach (var stat in counter.Values
                                    .OrderByDescending(x => x.QualityLevel)
                                    .ThenByDescending(x => x.Count)
                                    .ThenBy(x => x.ResourceId))
        {
            ItemStats.Add(stat);
        }
    }


    
    
    
    
    private void SetStatus(string message, bool isError, string? title = null)
    {
        if (isError)
        {
            InAppToast.MainWindow?.Error(title ?? Lang.Instance.GachaLogPage_Refresh, message);
        }
    }


    
    
    
    private async Task RunBusyAsync(Func<Task> action, string logMessage, string failFormat, string? toastTitle = null)
    {
        IsLoading = true;
        try
        {
            await action();
        }
        catch (Exception e)
        {
            AppConfig.GetLogger<GachaLogViewModel>().LogError(e, logMessage);
            SetStatus(string.Format(failFormat, e.Message), true, toastTitle);
        }
        finally
        {
            IsLoading = false;
        }
    }


    
    public string? BuildExportJson(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return null;
        }
        var pool = GachaStore.LoadPool(playerId);
        
        return pool is null ? null : UgfService.Export(playerId, pool, GachaStore.LoadParams(playerId));
    }


    
    public Dictionary<string, List<GachaRecord>>? GetExportPool(string? playerId)
    {
        return string.IsNullOrEmpty(playerId) ? null : GachaStore.LoadPool(playerId);
    }


    
    public async Task ImportRecordsAsync(string json)
    {
        if (IsLoading)
        {
            return;
        }
        await RunBusyAsync(async () =>
        {
            var (filePlayerId, pool) = await Task.Run(() => UgfService.Import(json));
            await ApplyImportAsync(filePlayerId, pool);
        }, "导入抽卡记录失败", Lang.Instance.GachaLogPage_ImportFail, Lang.Instance.GachaLogPage_ImportExport);
    }


    
    public async Task ImportTableRecordsAsync(string fileName, byte[] data)
    {
        if (IsLoading)
        {
            return;
        }
        await RunBusyAsync(async () =>
        {
            var (filePlayerId, pool) = await Task.Run(() => GachaTableImporter.Import(fileName, data));
            await ApplyImportAsync(filePlayerId, pool);
        }, "导入抽卡记录失败", Lang.Instance.GachaLogPage_ImportFail, Lang.Instance.GachaLogPage_ImportExport);
    }


    
    private async Task ApplyImportAsync(string filePlayerId, Dictionary<string, List<GachaRecord>> pool)
    {
        string playerId = string.IsNullOrEmpty(filePlayerId) ? SelectedPlayer ?? "" : filePlayerId;
        if (string.IsNullOrEmpty(playerId))
        {
            SetStatus(Lang.Instance.GachaLogPage_ImportFailNoPlayer, true, Lang.Instance.GachaLogPage_ImportExport);
            return;
        }

        int count = pool.Values.Sum(r => r.Count);
        await Task.Run(() => GachaStore.Save(playerId, pool, GachaStore.LoadParams(playerId) ?? new GachaParams { PlayerId = playerId }));

        if (!Players.Contains(playerId))
        {
            Players.Add(playerId);
        }
        bool isSamePlayer = playerId == SelectedPlayer;
        SelectedPlayer = playerId;
        if (isSamePlayer)
        {
            LoadPlayerData(playerId);
        }
        SetStatus(string.Format(Lang.Instance.GachaLogPage_ImportOk, playerId, count), false, Lang.Instance.GachaLogPage_ImportExport);
    }
}



public partial class PoolViewModel : ObservableObject
{
    public string PoolId { get; }
    public string Name { get; }
    public int Total { get; }
    public string Range { get; }
    public string Count5Text { get; }
    public string Count4Text { get; }
    public string Count3Text { get; }
    public string Avg5Text { get; }
    public bool HasUp { get; }
    public string UpText { get; }

    public ObservableCollection<BarItemViewModel> Items5 { get; } = [];
    public ObservableCollection<BarItemViewModel> Items4 { get; } = [];

    
    [ObservableProperty]
    public partial int SelectedRarityIndex { get; set; }

    partial void OnSelectedRarityIndexChanged(int value)
    {
        Is5Selected = value == 0;
        Is4Selected = value == 1;
    }

    [ObservableProperty]
    public partial bool Is5Selected { get; set; }

    [ObservableProperty]
    public partial bool Is4Selected { get; set; }


    public PoolViewModel(PoolStat stat)
    {
        PoolId = stat.PoolId;
        Name = GachaPools.GetName(stat.PoolId);
        Total = stat.Total;
        Range = stat.Total > 0 ? $"{FormatTime(stat.StartTime)} - {FormatTime(stat.EndTime)}" : "";
        HasUp = stat.UpCount > 0;
        Avg5Text = stat.Count5 > 0 ? stat.Avg5.ToString("0.00") : "--";
        UpText = HasUp ? $"{stat.UpAvg:0.00} ({stat.UpCount})" : "";
        Count5Text = $"{stat.Count5} [{Percent(stat.Count5, stat.Total)}%]";
        Count4Text = $"{stat.Count4} [{Percent(stat.Count4, stat.Total)}%]";
        Count3Text = $"{stat.Count3} [{Percent(stat.Count3, stat.Total)}%]";
        Is5Selected = true;

        
        if (stat.CurrentPity5 > 0)
        {
            Items5.Add(BarItemViewModel.Current(stat.CurrentPity5, isRarity5: true, stat.EndTime));
        }
        foreach (var item in stat.Items5)
        {
            Items5.Add(BarItemViewModel.History(item, isRarity5: true));
        }
        if (stat.CurrentPity4 > 0)
        {
            Items4.Add(BarItemViewModel.Current(stat.CurrentPity4, isRarity5: false, stat.EndTime));
        }
        foreach (var item in stat.Items4)
        {
            Items4.Add(BarItemViewModel.History(item, isRarity5: false));
        }
    }


    private static string Percent(int count, int total)
    {
        return total > 0 ? (100.0 * count / total).ToString("0.00") : "0.00";
    }


    private static string FormatTime(string time)
    {
        return DateTime.TryParse(time, out var t) ? t.ToString("yyyy/MM/dd HH:mm:ss") : time;
    }
}





public abstract class GachaIconItem : System.ComponentModel.INotifyPropertyChanged
{
    public ImageSource? Icon { get; protected set; }

    public Visibility IconVisibility { get; protected set; }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new(propertyName));


    
    protected async Task LoadIconAsync(int resourceId, string name)
    {
        string? file = await GachaIconService.EnsureCachedAsync(resourceId, name).ConfigureAwait(true);
        if (file is null)
        {
            return;
        }
        Icon = GachaIcon.FromFile(file);
        IconVisibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        AppConfig.GetLogger(GetType().FullName ?? "GachaIconItem").LogDebug("补图完成 {Name} #{ResourceId} 线程{ThreadId} 解码{Ok}",
            name, resourceId, Environment.CurrentManagedThreadId, Icon is not null);
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(IconVisibility));
        OnIconUpdated();
    }


    
    protected virtual void OnIconUpdated()
    {
    }
}









public sealed class BarItemViewModel : GachaIconItem
{
    
    private const int Pity5Point = 66;
    private const int Pity5Guarantee = 80;
    private const int Pity4Point = 8;
    private const int Pity4Guarantee = 10;

    private static readonly Color Green = Color.FromArgb(0xFF, 0x00, 0xE0, 0x79);
    private static readonly Color Red = Color.FromArgb(0xFF, 0xC8, 0x3C, 0x23);
    private static readonly Color Transparent = Color.FromArgb(0x00, 0x00, 0x00, 0x00);

    public string Name { get; }
    public string Time { get; }
    public int Pity { get; }
    public Brush BarBrush { get; }
    public Visibility UpVisibility { get; }


    private BarItemViewModel(string name, int resourceId, int pity, bool isRarity5, bool isUp, string time)
    {
        
        
        Name = GachaNameTable.Translate(resourceId, name, GachaLanguageSettings.Effective) ?? name;
        Time = time;
        Pity = pity;
        BarBrush = CreateBarBrush(pity,
            isRarity5 ? Pity5Point : Pity4Point,
            isRarity5 ? Pity5Guarantee : Pity4Guarantee);
        UpVisibility = isUp ? Visibility.Visible : Visibility.Collapsed;
        
        Icon = GachaIcon.Load(resourceId);
        IconVisibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        if (Icon is null && resourceId > 0)
        {
            _ = LoadIconAsync(resourceId, GachaNameTable.GetIconLookupName(resourceId, name));
        }
    }


    public static BarItemViewModel Current(int pity, bool isRarity5, string time) => new(Lang.Instance.GachaLog_PityBuilt, 0, pity, isRarity5, false, time);

    public static BarItemViewModel History(PoolItem item, bool isRarity5) => new(item.Name, item.ResourceId, item.Pity, isRarity5, item.IsUp, item.Time);


    private static Brush CreateBarBrush(int pity, int point, int guarantee)
    {
        double offset = Math.Clamp((double)pity / guarantee, 0, 1);
        Color color = pity < point ? Green : Red;
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            Opacity = 0.4,
        };
        brush.GradientStops.Add(new GradientStop { Color = color, Offset = 0 });
        brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
        brush.GradientStops.Add(new GradientStop { Color = Transparent, Offset = offset });
        return brush;
    }
}



public sealed class GachaItemStat : GachaIconItem
{

    
    private static readonly SolidColorBrush Star5QualityBrush = new(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xD7, 0x51));
    private static readonly SolidColorBrush Star4QualityBrush = new(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xA9, 0x7E, 0xE3));


    public GachaItemStat(int resourceId, string name, int qualityLevel, int count)
    {
        ResourceId = resourceId;
        
        Name = string.IsNullOrWhiteSpace(name)
            ? $"#{resourceId}"
            : GachaNameTable.Translate(resourceId, name, GachaLanguageSettings.Effective) ?? name;
        QualityLevel = qualityLevel;
        QualityBrush = qualityLevel >= 5 ? Star5QualityBrush : Star4QualityBrush;
        Icon = GachaIcon.Load(resourceId);
        IconVisibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        NameVisibility = Icon is null ? Visibility.Visible : Visibility.Collapsed;
        if (Icon is null && resourceId > 0)
        {
            _ = LoadIconAsync(resourceId, GachaNameTable.GetIconLookupName(resourceId, name));
        }
        Count = count;
    }


    
    protected override void OnIconUpdated()
    {
        NameVisibility = Icon is null ? Visibility.Visible : Visibility.Collapsed;
        OnPropertyChanged(nameof(NameVisibility));
    }

    public int ResourceId { get; }

    public string Name { get; }

    public int QualityLevel { get; }

    private int _count;

    
    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            CountText = $"{value} {Lang.Instance.GachaLogPage_Times}";
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(CountText));
        }
    }

    public string CountText { get; private set; } = "";

    
    public Brush QualityBrush { get; }

    public Visibility NameVisibility { get; private set; }

}
