using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.Logging;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace Arietta.Features.KuroBbs;


public sealed class CombatPeriodItem
{
    
    public string DateLabel { get; init; } = "";

    
    public string ValueLabel { get; init; } = "";

    
    public Visibility StarIconVisibility { get; init; } = Visibility.Collapsed;

    
    public Visibility ValueVisibility { get; init; } = Visibility.Visible;

    
    public ImageSource? RankSource { get; init; }

    
    public Visibility RankIconVisibility { get; init; } = Visibility.Collapsed;

    
    
    
    
    public double RankIconSize { get; init; } = 18;

    
    public long StartTs { get; init; }

    public CombatRecordEntry Entry { get; init; } = new();
}











public sealed partial class CombatPage : PageBase
{
    private static ILogger Log => AppConfig.GetLogger("CombatPage");

    private string _mode = CombatRecordStore.ModeTower;

    
    private long _selectedPeriodEnd = -1;

    

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };


    #region 海墟（冥歌海墟）资源与状态

    
    private const int SlashKindForbidden = 0;
    private const int SlashKindRift = 1;
    private const int SlashKindTurbulence = 2;

    
    private const double SlashRowHeight = 56;

    
    private const double SlashRankColumnWidth = 38;

    
    private static readonly Dictionary<string, string> SlashRankIconFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["B"] = "SlashRankB.png",
        ["A"] = "SlashRankA.png",
        ["S"] = "SlashRankS.png",
        ["SS"] = "SlashRankSS.png",
        ["SSS"] = "SlashRankSSS.png",
    };

    
    
    
    
    
    private const double SlashRolePhotoSize = 84;

    
    private const double SlashBranchIconSize = 20;

    
    private const double SlashRoleRowSpacing = 6;

    
    private const string SlashBranchIconFile = "SlashBranchIcon.png";

    
    private static readonly Dictionary<int, string> SlashBuffLvFiles = new()
    {
        [1] = "SlashBuffLv1.png",
        [2] = "SlashBuffLv2.png",
        [3] = "SlashBuffLv3.png",
        [4] = "SlashBuffLv4.png",
        [5] = "SlashBuffLv5.png",
    };

    
    private KuroSlashData? _slashCache;

    
    private Dictionary<int, int>? _slashChainMap;

    
    private Dictionary<int, int>? _matrixChainMap;

    
    private Dictionary<int, int>? _matrixStarMap;

    
    
    
    
    
    private int _loadGeneration;

    #endregion


    public CombatPage()
    {
        this.InitializeComponent();
    }


    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is KuroBbsFeature feature)
        {
            _mode = feature.Title switch
            {
                var t when t == Lang.Instance.KuroBbs_TowerOfAdversity => CombatRecordStore.ModeTower,
                var t when t == Lang.Instance.KuroBbs_DoomsdayMatrix => CombatRecordStore.ModeNewTower,
                var t when t == Lang.Instance.KuroBbs_SongOfVoidSea => CombatRecordStore.ModeSlash,
                _ => CombatRecordStore.ModeTower,
            };
            TextBlock_Title.Text = feature.Title;
        }
        _selectedPeriodEnd = -1;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;
        _ = LoadAsync();
    }


    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        base.OnNavigatedFrom(e);
    }


    private async void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            _selectedPeriodEnd = -1;
            _loadGeneration++; 
            
            
            
            
            StackPanel_Content.Children.Clear();
            TextBlock_SeasonTime.Text = "";
            await LoadAsync();
        });
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();


    private async Task LoadAsync()
    {
        var account = KuroBbsAccountStore.Load();
        if (account is null || !account.IsValid)
        {
            InfoBar_NoAccount.IsOpen = true;
            Scroll_Periods.Visibility = Visibility.Collapsed;
            return;
        }
        InfoBar_NoAccount.IsOpen = false;

        int generation = _loadGeneration;
        
        PopulatePeriodList(account, _selectedPeriodEnd);
        ProgressRing_Loading.IsActive = true;
        try
        {
            switch (_mode)
            {
                case CombatRecordStore.ModeNewTower:
                    await FetchAndRecordNewTowerAsync(account, generation);
                    break;
                case CombatRecordStore.ModeSlash:
                    await FetchAndRecordSlashAsync(account, generation);
                    break;
                default:
                    await FetchAndRecordTowerAsync(account, generation);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "战绩拉取/渲染失败 mode={Mode}", _mode);
            InAppToast.MainWindow?.Error(ex, TextBlock_Title.Text);
        }
        finally
        {
            ProgressRing_Loading.IsActive = false;
        }
    }


    #region 当期拉取 + 落库（三个玩法）

    
    
    
    
    
    
    
    
    
    private async Task<T?> FetchCombatDataAsync<T>(Func<Task<KuroBbsResponse<T>>> fetch, Func<T, bool> unlocked, int generation) where T : class
    {
        var result = await fetch();
        if (generation != _loadGeneration)
        {
            return default; 
        }
        if (result is { IsSuccess: true } && result.Data is null)
        {
            AddNoDataCard();
            return default;
        }
        if (result is not { IsSuccess: true, Data: { } data })
        {
            HandleError(result.Code, result.Msg);
            return default;
        }
        if (!unlocked(data))
        {
            AddLockedCard(TextBlock_Title.Text);
            return default;
        }
        return data;
    }


    private async Task FetchAndRecordTowerAsync(KuroBbsAccount account, int generation)
    {
        var data = await FetchCombatDataAsync(() => KuroBbsClient.Instance.GetTowerDataAsync(account), d => d.IsUnlock, generation);
        if (data is null)
        {
            return;
        }

        
        var difficulty = data.DifficultyList?.FirstOrDefault(d => d.DifficultyName == "深境区")
            ?? data.DifficultyList?.FirstOrDefault(d => d.Difficulty == 3)
            ?? data.DifficultyList?.FirstOrDefault();
        if (difficulty?.TowerAreaList is not { Count: > 0 } areas)
        {
            AddNoDataCard();
            return;
        }

        
        
        var (chainMap, _) = await BuildRoleAuxMapsAsync(account);
        var widget = await ResolveTowerWidgetAsync(account, data.SeasonEndTime);

        
        int star = widget.Max > 0 ? widget.Cur : areas.Sum(a => a.Star);
        int maxStar = widget.Max > 0 ? widget.Max : areas.Sum(a => a.MaxStar);
        long periodEnd = ResolvePeriodEnd(widget.RefreshAt, data.SeasonEndTime);
        if (generation != _loadGeneration)
        {
            return; 
        }
        SaveRecord(account, CombatRecordStore.ModeTower, periodEnd,
            star, maxStar, 0, JsonSerializer.Serialize(data, JsonOptions), JsonSerializer.Serialize(chainMap, JsonOptions));

        
        CombatRecordStore.SaveTowerFloors(account.RoleId, periodEnd, data);

        PopulatePeriodList(account, _selectedPeriodEnd);
    }


    private async Task FetchAndRecordNewTowerAsync(KuroBbsAccount account, int generation)
    {
        var data = await FetchCombatDataAsync(() => KuroBbsClient.Instance.GetNewTowerDataAsync(account), d => d.IsUnlock, generation);
        if (data is null)
        {
            return;
        }
        var modes = (data.ModeDetails ?? []).Where(m => m.IsUnlock).ToList();
        if (modes.Count == 0 || modes.All(m => !m.HasRecord))
        {
            AddNoDataCard();
            return;
        }

        long periodEnd = await ResolveWidgetPeriodEndAsync(account, _mode, data.EndTime);
        
        
        var (chainMap, starMap) = await BuildRoleAuxMapsAsync(account);
        if (generation != _loadGeneration)
        {
            return; 
        }
        
        
        var headline = modes.FirstOrDefault(m => m.ModeId == MatrixModeSingularity && m.HasRecord)
            ?? modes.FirstOrDefault(m => m.HasRecord);
        SaveRecord(account, _mode, periodEnd, 0, 0, headline?.Score ?? 0,
            JsonSerializer.Serialize(data, JsonOptions),
            JsonSerializer.Serialize(new MatrixRoleAuxMaps { Chain = chainMap, Star = starMap }, JsonOptions));

        PopulatePeriodList(account, _selectedPeriodEnd);
    }


    private async Task FetchAndRecordSlashAsync(KuroBbsAccount account, int generation)
    {
        var data = await FetchCombatDataAsync(() => KuroBbsClient.Instance.GetSlashDataAsync(account), d => d.IsUnlock, generation);
        if (data is null)
        {
            return;
        }
        var difficulties = (data.DifficultyList ?? []).Where(d => d.ChallengeList is { Count: > 0 }).ToList();
        if (difficulties.Count == 0)
        {
            AddNoDataCard();
            return;
        }

        long periodEnd = await ResolveWidgetPeriodEndAsync(account, _mode, data.SeasonEndTime);
        
        var (chainMap, _) = await BuildRoleAuxMapsAsync(account);
        if (generation != _loadGeneration)
        {
            return; 
        }
        
        
        SaveRecord(account, _mode, periodEnd, 0, 0, ResolveSlashHeadlineScore(data),
            JsonSerializer.Serialize(data, JsonOptions), JsonSerializer.Serialize(chainMap, JsonOptions));

        PopulatePeriodList(account, _selectedPeriodEnd);
    }


    
    private static void SaveRecord(KuroBbsAccount account, string mode, long periodEnd,
        int star, int maxStar, int score, string detail, string? chain)
    {
        CombatRecordStore.Save(new CombatRecordEntry
        {
            Mode = mode,
            RoleId = account.RoleId,
            PeriodEnd = periodEnd,
            Star = star,
            MaxStar = maxStar,
            Score = score,
            Detail = detail,
            Chain = chain,
            UpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        });
    }


    
    
    private static long ResolvePeriodEnd(long primary, long fallback)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const long min = 1_704_067_200; 
        long max = now + 2L * 366 * 86400;
        bool Plausible(long v) => v > min && v < max;
        if (Plausible(primary))
        {
            return primary;
        }
        return Plausible(fallback) ? fallback : 0;
    }


    
    private static async Task<long> ResolveWidgetPeriodEndAsync(KuroBbsAccount account, string mode, long fallbackEndTime)
    {
        long primary = 0;
        try
        {
            var widget = await KuroBbsClient.Instance.GetWuwaWidgetDataAsync(account);
            var item = mode == CombatRecordStore.ModeNewTower ? widget.Data?.NewTowerData
                : mode == CombatRecordStore.ModeSlash ? widget.Data?.SlashTowerData
                : widget.Data?.TowerData;
            primary = item?.RefreshTimeStamp ?? 0;
        }
        catch
        {
            
        }
        return ResolvePeriodEnd(primary, fallbackEndTime);
    }

    #endregion


    #region 期次列表 + 快照渲染

    
    private void PopulatePeriodList(KuroBbsAccount account, long selectPeriodEnd)
    {
        var records = CombatRecordStore.GetList(_mode, account.RoleId);
        if (records.Count == 0)
        {
            ListView_Periods.ItemsSource = null;
            Scroll_Periods.Visibility = Visibility.Collapsed;
            return;
        }

        var starts = CombatRecordStore.ComputePeriodStarts(records, _mode);
        var items = new List<CombatPeriodItem>();
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            
            
            var rankSource = _mode == CombatRecordStore.ModeSlash ? ResolveSlashPeriodRank(record) : null;
            var rankSize = 18.0;
            var valueScore = record.Score;
            if (_mode == CombatRecordStore.ModeNewTower)
            {
                
                
                (valueScore, rankSource) = ResolveMatrixPeriodInfo(record);
                rankSize = MatrixPeriodRankSize;
            }
            else if (_mode == CombatRecordStore.ModeSlash)
            {
                
                
                valueScore = ResolveSlashPeriodScore(record);
            }
            items.Add(new CombatPeriodItem
            {
                Entry = record,
                StartTs = starts[i],
                DateLabel = FormatPeriodDate(record, starts[i]),
                ValueLabel = _mode == CombatRecordStore.ModeTower ? record.Star.ToString() : valueScore.ToString("N0"),
                StarIconVisibility = _mode == CombatRecordStore.ModeTower ? Visibility.Visible : Visibility.Collapsed,
                ValueVisibility = _mode != CombatRecordStore.ModeTower && valueScore <= 0 ? Visibility.Collapsed : Visibility.Visible,
                RankSource = rankSource,
                RankIconVisibility = rankSource is null ? Visibility.Collapsed : Visibility.Visible,
                RankIconSize = rankSize,
            });
        }

        ListView_Periods.ItemsSource = items;
        Scroll_Periods.Visibility = Visibility.Visible;

        var target = items.FirstOrDefault(it => it.Entry.PeriodEnd == selectPeriodEnd) ?? items[0];
        ListView_Periods.SelectedItem = target;
        ListView_Periods.ScrollIntoView(target);
    }


    private static string FormatPeriodDate(CombatRecordEntry record, long startTs)
    {
        try
        {
            
            long labelTs = startTs > 0 && record.PeriodEnd > startTs ? startTs
                : record.PeriodEnd > 0 ? record.PeriodEnd
                : DateTimeOffset.FromUnixTimeMilliseconds(record.UpdateTime).ToUnixTimeSeconds();
            return DateTimeOffset.FromUnixTimeSeconds(labelTs).LocalDateTime.ToString("yyyy-MM-dd");
        }
        catch
        {
            return "";
        }
    }


    
    private static ImageSource? ResolveSlashPeriodRank(CombatRecordEntry record)
    {
        try
        {
            if (JsonSerializer.Deserialize<KuroSlashData>(record.Detail, JsonOptions) is { } slash
                && FindSlashHeadline(slash) is { } headline)
            {
                return SlashRankImageSource(headline.Challenge.Rank);
            }
        }
        catch
        {
            
        }
        return null;
    }


    
    
    
    
    private static (int Score, ImageSource? Rank) ResolveMatrixPeriodInfo(CombatRecordEntry record)
    {
        int score = record.Score;
        ImageSource? rank = null;
        try
        {
            if (JsonSerializer.Deserialize<KuroNewTowerData>(record.Detail, JsonOptions) is { } data)
            {
                var modes = data.ModeDetails ?? [];
                var mode = modes.FirstOrDefault(m => m.ModeId == MatrixModeSingularity)
                    ?? modes.FirstOrDefault(m => m.HasRecord);
                if (mode is { HasRecord: true })
                {
                    score = mode.Score;
                    rank = MatrixRankImageSource(mode.Rank);
                }
            }
        }
        catch
        {
            
        }
        return (score, rank);
    }


    private void ListView_Periods_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListView_Periods.SelectedItem is CombatPeriodItem item)
        {
            RenderFromEntry(item);
        }
    }


    
    private void RenderFromEntry(CombatPeriodItem item)
    {
        var entry = item.Entry;
        _selectedPeriodEnd = entry.PeriodEnd;
        StackPanel_Content.Children.Clear();
        TextBlock_SeasonTime.Text = "";
        try
        {
            switch (entry.Mode)
            {
                case CombatRecordStore.ModeNewTower:
                    if (JsonSerializer.Deserialize<KuroNewTowerData>(entry.Detail, JsonOptions) is { } newTower)
                    {
                        
                        
                        _matrixChainMap = [];
                        _matrixStarMap = [];
                        if (entry.Chain is string aux)
                        {
                            try
                            {
                                var maps = JsonSerializer.Deserialize<MatrixRoleAuxMaps>(aux, JsonOptions);
                                if (maps?.Chain is not null)
                                {
                                    _matrixChainMap = maps.Chain;
                                    _matrixStarMap = maps.Star ?? [];
                                }
                                else
                                {
                                    _matrixChainMap = JsonSerializer.Deserialize<Dictionary<int, int>>(aux, JsonOptions) ?? [];
                                }
                            }
                            catch
                            {
                                
                            }
                        }
                        ShowPeriodRange(item);
                        RenderNewTower(newTower);
                    }
                    break;
                case CombatRecordStore.ModeSlash:
                    if (JsonSerializer.Deserialize<KuroSlashData>(entry.Detail, JsonOptions) is { } slash)
                    {
                        
                        var slashChain = entry.Chain is string slashChainJson
                            ? JsonSerializer.Deserialize<Dictionary<int, int>>(slashChainJson, JsonOptions) ?? []
                            : [];
                        ShowPeriodRange(item);
                        RenderSlash(slash, slashChain);
                    }
                    break;
                default:
                    if (JsonSerializer.Deserialize<KuroTowerData>(entry.Detail, JsonOptions) is { } tower)
                    {
                        var chainMap = entry.Chain is string chain
                            ? JsonSerializer.Deserialize<Dictionary<int, int>>(chain, JsonOptions) ?? []
                            : [];
                        var widget = new TowerWidgetInfo(entry.PeriodEnd, entry.Star, entry.MaxStar);
                        
                        CombatRecordStore.MergeAccumulatedFloors(tower, entry.RoleId, entry.PeriodEnd);
                        ShowPeriodRange(item);
                        RenderTower(tower, widget, chainMap);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "期次快照渲染失败 mode={Mode} period={Period}", entry.Mode, entry.PeriodEnd);
            InAppToast.MainWindow?.Error(ex, TextBlock_Title.Text);
        }
    }


    
    private void ShowPeriodRange(CombatPeriodItem item)
    {
        try
        {
            if (item.StartTs <= 0 || item.Entry.PeriodEnd <= item.StartTs)
            {
                TextBlock_SeasonTime.Text = "";
                return;
            }
            TextBlock_SeasonTime.Text = string.Format(Lang.Instance.CombatPage_StatsPeriod,
                DateTimeOffset.FromUnixTimeSeconds(item.StartTs).LocalDateTime.ToString("yyyy/MM/dd"),
                DateTimeOffset.FromUnixTimeSeconds(item.Entry.PeriodEnd).LocalDateTime.ToString("yyyy/MM/dd"));
        }
        catch
        {
            TextBlock_SeasonTime.Text = "";
        }
    }


    private void RenderTower(KuroTowerData data, TowerWidgetInfo widget, Dictionary<int, int> chainMap)
    {
        
        var difficulty = data.DifficultyList?.FirstOrDefault(d => d.DifficultyName == "深境区")
            ?? data.DifficultyList?.FirstOrDefault(d => d.Difficulty == 3)
            ?? data.DifficultyList?.FirstOrDefault();
        if (difficulty?.TowerAreaList is not { Count: > 0 } areas)
        {
            AddNoDataCard();
            return;
        }
        StackPanel_Content.Children.Clear();
        StackPanel_Content.Children.Add(CreateTowerHeader(difficulty, widget));
        StackPanel_Content.Children.Add(CreateTowerAreasGrid(areas, chainMap));
    }


    
    
    
    
    
    
    
    
    
    
    
    private void RenderNewTower(KuroNewTowerData data)
    {
        
        
        var modes = (data.ModeDetails ?? []).Where(m => m.IsUnlock).ToList();
        var mode = modes.FirstOrDefault(m => m.ModeId == MatrixModeSingularity)
            ?? modes.FirstOrDefault();
        if (mode is null)
        {
            AddNoDataCard();
            return;
        }

        StackPanel_Content.Children.Clear();
        StackPanel_Content.Children.Add(CreateMatrixOverview(mode));
        StackPanel_Content.Children.Add(CreateMatrixTeamList(mode));
    }


    
    
    
    
    
    private FrameworkElement CreateMatrixOverview(KuroNewTowerModeDetail mode)
    {
        var root = new Grid();
        root.Children.Add(BuildMatrixOverviewCard(mode));

        
        
        var button = new Button
        {
            Height = 32,
            MinWidth = 0,
            Padding = new Thickness(14, 0, 14, 0),
            CornerRadius = new CornerRadius(16),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 10, 12, 0),
            Content = new TextBlock { Text = Lang.Instance.MatrixPage_Overview, FontSize = 13 },
        };
        button.Click += async (sender, e) => await GenerateMatrixShareImageAsync(mode);
        root.Children.Add(button);
        return root;
    }


    
    private FrameworkElement BuildMatrixOverviewCard(KuroNewTowerModeDetail mode) =>
        KuroBbsUi.KuroCard(BuildMatrixOverviewContent(mode), cornerRadius: 12);

    
    private Grid BuildMatrixOverviewContent(KuroNewTowerModeDetail mode)
    {
        
        
        var grid = new Grid { Height = MatrixOverviewHeight, Padding = new Thickness(16, 0, 24, 0), ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 

        if (MatrixRankDetailImageSource(mode.Rank) is { } rankSrc)
        {
            
            grid.Children.Add(new Image
            {
                Source = rankSrc,
                Height = MatrixOverviewHeight,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        else
        {
            
            grid.Children.Add(new Grid { Width = MatrixOverviewHeight, Height = MatrixOverviewHeight });
        }

        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 8 };

        
        var scoreRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        scoreRow.Children.Add(new TextBlock
        {
            Text = Lang.Instance.MatrixPage_TotalScore,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Bottom,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });
        scoreRow.Children.Add(new TextBlock
        {
            Text = mode.Score.ToString("N0"),
            FontSize = 24,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, -2),
            Foreground = MatrixGoldBrush,
        });
        info.Children.Add(scoreRow);

        
        var progressRow = new Grid();
        progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        progressRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        progressRow.Children.Add(new TextBlock
        {
            Text = Lang.Instance.MatrixPage_Progress,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });

        var right = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        
        if (mode.Round > 0)
        {
            right.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.MatrixPage_Round, mode.Round),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            });
            right.Children.Add(KuroBbsUi.MakeDivider(new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)), 18, width: 1, verticalCenter: true));
        }
        if (BundledImage("MatrixBossIcon.png") is { } boss)
        {
            right.Children.Add(new Image
            {
                Source = boss,
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, -2, 0),
            });
        }
        right.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.MatrixPage_BossFormat, mode.PassBoss, mode.BossCount),
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = MatrixGoldBrush,
        });
        Grid.SetColumn(right, 2);
        progressRow.Children.Add(right);
        info.Children.Add(progressRow);

        
        
        var track = new Grid { Height = 8, CornerRadius = new CornerRadius(4) };
        track.Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
        double ratio = mode.BossCount > 0 ? Math.Clamp((double)mode.PassBoss / mode.BossCount, 0, 1) : 0;
        var fill = new Border
        {
            CornerRadius = new CornerRadius(4),
            Background = MatrixGoldBrush,
        };
        
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ratio, GridUnitType.Star) });
        track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(1 - ratio, 0.02), GridUnitType.Star) });
        Grid.SetColumn(fill, 0);
        track.Children.Add(fill);
        info.Children.Add(track);

        Grid.SetColumn(info, 1);
        grid.Children.Add(info);
        return grid;
    }


    
    private const int MatrixModeSingularity = 1;

    
    private const double MatrixOverviewHeight = 120;

    
    
    
    
    
    private const int MatrixRankSize = 128;

    
    
    
    private const int MatrixPeriodRankSize = 18;

    
    private const double MatrixTeamRowHeight = 92;

    
    private const double MatrixTeamIndexWidth = 36;

    
    
    
    
    
    private const double MatrixRolePhotoSize = 80;

    
    private const double MatrixBuffSize = 64;

    
    private const double MatrixOverviewAvatarSize = 40;
    private const double MatrixOverviewBuffSize = 28;

    
    private const double MatrixShareImageWidth = 686;

    
    private static readonly SolidColorBrush MatrixGoldBrush = new(Color.FromArgb(0xFF, 0xFF, 0xF3, 0xB9));


    
    
    
    
    private static ImageSource? MatrixRankImageSource(int rank) =>
        rank is < 1 or > 7 ? null : MatrixRankImageSourceCore($"ms-appx:///Assets/Image/Combat/MatrixRank{rank}.png");

    
    private static ImageSource? MatrixRankDetailImageSource(int rank) =>
        rank is < 1 or > 7 ? null : MatrixRankImageSourceCore($"ms-appx:///Assets/Image/Combat/MatrixRankDetail{rank}.png");

    private static ImageSource? MatrixRankImageSourceCore(string uri)
    {
        try
        {
            return new BitmapImage(new Uri(uri))
            {
                DecodePixelType = DecodePixelType.Logical,
                
                DecodePixelWidth = MatrixRankSize,
            };
        }
        catch
        {
            return null;
        }
    }


    
    
    
    
    
    private FrameworkElement CreateMatrixTeamList(KuroNewTowerModeDetail mode)
    {
        var list = new StackPanel { Spacing = 8 };
        var teams = mode.Teams ?? [];
        if (teams.Count == 0)
        {
            list.Children.Add(new TextBlock
            {
                Text = Lang.Instance.MatrixPage_NoRecord,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            return list;
        }
        for (int i = 0; i < teams.Count; i++)
        {
            list.Children.Add(CreateMatrixTeamRow(teams[i], i + 1, MeasureRoundColumnWidth(teams, 13)));
        }
        return list;
    }


    
    private static double MeasureRoundColumnWidth(List<KuroNewTowerTeam> teams, double fontSize)
    {
        double max = 0;
        var probe = new TextBlock { FontSize = fontSize };
        foreach (var team in teams)
        {
            probe.Text = string.Format(Lang.Instance.MatrixPage_Round, team.Round);
            probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            max = Math.Max(max, probe.DesiredSize.Width);
        }
        return Math.Ceiling(max);
    }

    
    private FrameworkElement CreateMatrixTeamRow(KuroNewTowerTeam team, int index, double roundColumnWidth)
    {
        var row = new Grid
        {
            Height = MatrixTeamRowHeight,
            Padding = new Thickness(14, 0, 24, 0),
            ColumnSpacing = 8,
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(MatrixTeamIndexWidth) }); 
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  
        
        
        
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(roundColumnWidth) });      
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });                   
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });                    

        
        row.Children.Add(new TextBlock
        {
            Text = index.ToString("D2"),
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });

        
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var roles = (team.RoleList ?? []).Where(r => !string.IsNullOrEmpty(r.IconUrl)).ToList();
        if (roles.Count == 0)
        {
            foreach (var icon in team.RoleIcons ?? [])
            {
                left.Children.Add(CreateRolePhoto(icon, 0, 0, null, MatrixRolePhotoSize, 24, decodeToSize: true, showRarityBar: true, placeholderBackground: true));
            }
        }
        else
        {
            foreach (var role in roles)
            {
                int chain = _matrixChainMap is { } m && m.TryGetValue(role.RoleId, out var c) ? c : 0;
                int star = _matrixStarMap is { } sm && sm.TryGetValue(role.RoleId, out var st) ? st : 0;
                left.Children.Add(CreateRolePhoto(role.IconUrl, chain, star, role.SkillBranchIndex, MatrixRolePhotoSize, 24, decodeToSize: true, showRarityBar: true, placeholderBackground: true));
            }
        }
        
        var buffs = (team.Buffs ?? []).Where(b => !string.IsNullOrEmpty(b.BuffIcon) || !string.IsNullOrEmpty(b.BuffName)).ToList();
        if (buffs.Count > 0)
        {
            left.Children.Add(KuroBbsUi.MakeDivider(new SolidColorBrush(Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)), 62, new Thickness(6, 0, 8, 0), width: 1, verticalCenter: true));
            foreach (var buff in buffs)
            {
                left.Children.Add(CreateMatrixBuffBox(buff));
            }
        }
        Grid.SetColumn(left, 1);
        row.Children.Add(left);

        
        if (team.Round > 0)
        {
            var round = new TextBlock
            {
                Text = string.Format(Lang.Instance.MatrixPage_Round, team.Round),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            };
            Grid.SetColumn(round, 3);
            row.Children.Add(round);
        }
        
        
        var capsule = CreateMatrixBossCapsule(team);
        capsule.Margin = new Thickness(20, 0, 0, 0);
        Grid.SetColumn(capsule, 4);
        row.Children.Add(capsule);

        
        var scoreRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        scoreRow.Children.Add(new TextBlock
        {
            Text = "+",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });
        scoreRow.Children.Add(new TextBlock
        {
            Text = team.Score.ToString(),
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });
        Grid.SetColumn(scoreRow, 5);
        row.Children.Add(scoreRow);

        
        
        var root = new Grid();
        root.Children.Add(KuroBbsUi.KuroCard());
        root.Children.Add(row);
        return root;
    }


    
    private static FrameworkElement CreateMatrixBossCapsule(KuroNewTowerTeam team)
    {
        
        var grid = new Grid { ColumnSpacing = 4 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (BundledImage("MatrixBossIcon.png") is { } boss)
        {
            grid.Children.Add(new Image
            {
                Source = boss,
                Width = 28,
                Height = 28,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(-20, 0, 0, 0),
            });
        }
        var count = new TextBlock
        {
            Text = string.Format(Lang.Instance.MatrixPage_BossFormat, team.PassBoss, team.BossCount),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB3, 0xB3, 0xB3)),
        };
        Grid.SetColumn(count, 1);
        grid.Children.Add(count);

        return new Border
        {
            Height = 28,
            MinWidth = 78,
            Padding = new Thickness(24, 0, 14, 0),
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)),
            VerticalAlignment = VerticalAlignment.Center,
            Child = grid,
        };
    }


    
    
    
    
    
    private async Task GenerateMatrixShareImageAsync(KuroNewTowerModeDetail mode)
    {
        InMemoryRandomAccessStream? png = null;
        try
        {
            var image = BuildMatrixShareImage(mode);

            
            
            
            
            var pageRoot = (Grid)Content;
            var host = new Canvas
            {
                IsHitTestVisible = false,
                RenderTransform = new TranslateTransform { X = -4000 },
            };
            host.Children.Add(image);
            pageRoot.Children.Add(host);
            RenderTargetBitmap rtb;
            try
            {
                host.UpdateLayout();
                
                await Task.Delay(400);

                rtb = new RenderTargetBitmap();
                await rtb.RenderAsync(image);

                png = new InMemoryRandomAccessStream();
                var pixels = await rtb.GetPixelsAsync();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, png);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                    (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, 96, 96, pixels.ToArray());
                await encoder.FlushAsync();
            }
            finally
            {
                pageRoot.Children.Remove(host);
            }

            await ShowMatrixSharePreviewAsync(rtb, png);
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "矩阵分享图生成失败");
            InAppToast.MainWindow?.Error(ex, TextBlock_Title.Text);
        }
    }


    
    
    
    
    private FrameworkElement BuildMatrixShareImage(KuroNewTowerModeDetail mode)
    {
        var body = new StackPanel { Spacing = 12 };

        
        body.Children.Add(new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(18, 9, 18, 9),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            Child = new TextBlock
            {
                Text = Lang.Instance.MatrixPage_Singularity,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x1B, 0x28, 0x36)),
            },
        });

        
        body.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x25, 0x37, 0x4A)),
            Child = BuildMatrixOverviewContent(mode),
        });

        var rows = new StackPanel { Spacing = 8 };
        var teams = mode.Teams ?? [];
        if (teams.Count == 0)
        {
            rows.Children.Add(new TextBlock
            {
                Text = Lang.Instance.MatrixPage_NoRecord,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB3, 0xB3, 0xB3)),
            });
        }
        else
        {
            for (int i = 0; i < teams.Count; i++)
            {
                rows.Children.Add(CreateMatrixShareRow(teams[i], i + 1, MeasureRoundColumnWidth(teams, 12)));
            }
        }
        body.Children.Add(rows);

        return new Border
        {
            Width = MatrixShareImageWidth,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x2B, 0x3A)),
            Padding = new Thickness(20, 18, 20, 20),
            Child = body,
        };
    }


    
    private FrameworkElement CreateMatrixShareRow(KuroNewTowerTeam team, int index, double roundColumnWidth)
    {
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(MatrixTeamIndexWidth) }); 
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                      
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
        
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(roundColumnWidth) });      
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });                   
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });                   

        row.Children.Add(new TextBlock
        {
            Text = index.ToString("D2"),
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        });

        
        
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var roles = (team.RoleList ?? []).Where(r => !string.IsNullOrEmpty(r.IconUrl)).ToList();
        if (roles.Count == 0)
        {
            foreach (var icon in team.RoleIcons ?? [])
            {
                left.Children.Add(CreateRolePhoto(icon, 0, 0, null, MatrixOverviewAvatarSize, 14, decodeToSize: true, showRarityBar: false, placeholderBackground: true, badgeFontSize: 9));
            }
        }
        else
        {
            foreach (var role in roles)
            {
                int chain = _matrixChainMap is { } m && m.TryGetValue(role.RoleId, out var c) ? c : 0;
                int star = _matrixStarMap is { } sm && sm.TryGetValue(role.RoleId, out var st) ? st : 0;
                left.Children.Add(CreateRolePhoto(role.IconUrl, chain, star, role.SkillBranchIndex, MatrixOverviewAvatarSize, 14, decodeToSize: true, showRarityBar: false, placeholderBackground: true, badgeFontSize: 9));
            }
        }
        foreach (var buff in (team.Buffs ?? []).Where(b => !string.IsNullOrEmpty(b.BuffIcon) || !string.IsNullOrEmpty(b.BuffName)))
        {
            left.Children.Add(CreateMatrixBuffBox(buff, MatrixOverviewBuffSize));
        }
        Grid.SetColumn(left, 1);
        row.Children.Add(left);

        
        if (team.Round > 0)
        {
            var round = new TextBlock
            {
                Text = string.Format(Lang.Instance.MatrixPage_Round, team.Round),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            };
            Grid.SetColumn(round, 3);
            row.Children.Add(round);
        }
        var progress = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        if (BundledImage("MatrixBossIcon.png") is { } boss)
        {
            progress.Children.Add(new Image
            {
                Source = boss,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        progress.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.MatrixPage_BossFormat, team.PassBoss, team.BossCount),
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xB3, 0xB3, 0xB3)),
        });
        Grid.SetColumn(progress, 4);
        row.Children.Add(progress);

        var score = new TextBlock
        {
            Text = "+" + team.Score.ToString(),
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
        };
        Grid.SetColumn(score, 5);
        row.Children.Add(score);

        return new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x24, 0x33, 0x43)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 8, 14, 8),
            Child = row,
        };
    }


    
    private async Task ShowMatrixSharePreviewAsync(RenderTargetBitmap rtb, InMemoryRandomAccessStream png)
    {
        var dialog = new ContentDialog
        {
            Title = TextBlock_Title.Text,
            Content = new Image
            {
                Source = rtb,
                MaxHeight = 440,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
            PrimaryButtonText = Lang.Instance.Common_Save,
            SecondaryButtonText = Lang.Instance.Common_Copy,
            CloseButtonText = Lang.Instance.Common_Close,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };
        dialog.Resources["ContentDialogMaxWidth"] = 680d;
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await SaveMatrixShareImageAsync(png);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            png.Seek(0);
            var package = new Windows.ApplicationModel.DataTransfer.DataPackage
            {
                RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy,
            };
            package.SetBitmap(Windows.Storage.Streams.RandomAccessStreamReference.CreateFromStream(png));
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            InAppToast.MainWindow?.Success(Lang.Instance.Common_Copy);
        }
    }


    
    private async Task SaveMatrixShareImageAsync(InMemoryRandomAccessStream png)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedFileName = $"终焉矩阵_{DateTimeOffset.FromUnixTimeSeconds(_selectedPeriodEnd):yyyyMMdd}",
        };
        WinRT.Interop.InitializeWithWindow.Initialize(
            picker, WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
        picker.FileTypeChoices.Add("PNG", new List<string> { ".png" });
        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }
        png.Seek(0);
        var reader = new Windows.Storage.Streams.DataReader(png.GetInputStreamAt(0));
        await reader.LoadAsync((uint)png.Size);
        var bytes = new byte[(long)png.Size];
        reader.ReadBytes(bytes);
        await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);
        InAppToast.MainWindow?.Success(Lang.Instance.Common_Save);
    }


    
    
    
    
    
    private FrameworkElement CreateRolePhoto(string? url, int chain, int star, int? branchIndex,
        double size, double starIconSize, bool decodeToSize, bool showRarityBar, bool placeholderBackground,
        double badgeFontSize = 11)
    {
        var root = new Grid { Width = size, Height = size };

        var photo = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(8),
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
        };
        if (placeholderBackground)
        {
            photo.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x73, 0x8C, 0xA3));
        }
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                var source = new BitmapImage(new Uri(url));
                if (decodeToSize)
                {
                    
                    
                    source.DecodePixelType = DecodePixelType.Logical;
                    source.DecodePixelWidth = (int)size;
                    source.DecodePixelHeight = (int)size;
                }
                photo.Child = new Image { Source = source, Stretch = Stretch.UniformToFill };
            }
            catch
            {
                
            }
        }
        root.Children.Add(photo);

        
        if (showRarityBar && star > 0)
        {
            root.Children.Add(new Border
            {
                Height = 3,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Background = star >= 5
                    ? new SolidColorBrush(Microsoft.UI.Colors.Gold)
                    : new SolidColorBrush(Microsoft.UI.Colors.MediumPurple),
            });
        }

        
        if (chain > 0)
        {
            root.Children.Add(KuroBbsUi.MakeCornerBadge(chain.ToString(), badgeFontSize, new Thickness(4, 0, 4, 1)));
        }

        if (branchIndex is not null && BundledImage(SlashBranchIconFile) is { } starSrc)
        {
            var modal = new Image
            {
                Source = starSrc,
                Width = starIconSize,
                Height = starIconSize,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                
                Margin = new Thickness(0, 0, -3, -3),
            };
            if (branchIndex == 1)
            {
                modal.RenderTransform = new ScaleTransform { ScaleX = -1 };
            }
            root.Children.Add(modal);
        }
        return root;
    }


    
    
    
    
    
    
    
    private static FrameworkElement CreateMatrixBuffBox(KuroNewTowerBuff buff, double? size = null)
    {
        var s = size ?? MatrixBuffSize;
        var box = new Border
        {
            Width = s,
            Height = s,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x73, 0x8C, 0xA3)),
        };
        if (!string.IsNullOrEmpty(buff.BuffIcon))
        {
            try
            {
                box.Child = new Image
                {
                    
                    Source = new BitmapImage(new Uri(buff.BuffIcon))
                    {
                        DecodePixelType = DecodePixelType.Logical,
                        DecodePixelWidth = (int)s,
                        DecodePixelHeight = (int)s,
                    },
                    Stretch = Stretch.UniformToFill,
                };
            }
            catch
            {
                
            }
        }

        var root = new Grid { Width = s, Height = s };
        root.Children.Add(box);

        if (!string.IsNullOrEmpty(buff.BuffName))
        {
            ToolTipService.SetPlacement(root, PlacementMode.Top);
            ToolTipService.SetToolTip(root, new ToolTip
            {
                Content = new TextBlock
                {
                    Text = string.IsNullOrEmpty(buff.Desc) ? buff.BuffName : $"{buff.BuffName}\n{buff.Desc}",
                    MaxWidth = 340,
                    TextWrapping = TextWrapping.Wrap,
                },
                MaxWidth = 380,
            });
        }
        return root;
    }


    
    
    
    
    private void RenderSlash(KuroSlashData data, Dictionary<int, int>? chainMap = null)
    {
        _slashCache = data;
        _slashChainMap = chainMap;
        BuildSlashContent();
    }


    
    private static (KuroSlashDifficulty Difficulty, KuroSlashChallenge Challenge)? FindSlashHeadline(KuroSlashData data)
    {
        var pool = (data.DifficultyList ?? [])
            .Where(d => (d.DifficultyName ?? "").Contains("湍渊") && d.ChallengeList is { Count: > 0 })
            .ToList();

        (KuroSlashDifficulty, KuroSlashChallenge)? best = null;
        foreach (var difficulty in pool)
        {
            foreach (var challenge in difficulty.ChallengeList ?? [])
            {
                if (best is null || challenge.ChallengeId > best.Value.Item2.ChallengeId)
                {
                    best = (difficulty, challenge);
                }
            }
        }
        return best;
    }


    
    private static int ResolveSlashHeadlineScore(KuroSlashData data)
    {
        if (FindSlashHeadline(data) is { } headline)
        {
            int score = headline.Challenge.Score > 0 ? headline.Challenge.Score : headline.Difficulty.AllScore;
            if (score > 0)
            {
                return score;
            }
        }
        return 0;
    }


    
    private static int ResolveSlashPeriodScore(CombatRecordEntry record)
    {
        try
        {
            if (JsonSerializer.Deserialize<KuroSlashData>(record.Detail, JsonOptions) is { } slash)
            {
                
                return ResolveSlashHeadlineScore(slash);
            }
        }
        catch
        {
            
        }
        return record.Score;
    }


    
    private void BuildSlashContent()
    {
        StackPanel_Content.Children.Clear();
        if (_slashCache is not { } data)
        {
            return;
        }

        
        
        var difficulties = (data.DifficultyList ?? [])
            .Where(d => d.ChallengeList is { Count: > 0 })
            .OrderByDescending(d => d.Difficulty)
            .ToList();
        if (difficulties.Count == 0)
        {
            AddNoDataCard();
            return;
        }

        var regenerative = difficulties.Where(d => (d.DifficultyName ?? "").Contains("再生海域")).ToList();
        if (regenerative.Count == 0)
        {
            AddNoDataCard();
            return;
        }
        AddSlashGroup(regenerative);
    }


    
    
    private void AddSlashGroup(List<KuroSlashDifficulty> group)
    {
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(new TextBlock
        {
            Text = SlashGroupTitle(group),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        foreach (var difficulty in group.OrderBy(SlashKind))
        {
            string name = SlashShortName(difficulty);
            string title = SlashGroupTitle(group);
            header.Children.Add(new TextBlock
            {
                Text = name.Length > 0 && name != title
                    ? string.Format(Lang.Instance.SlashPage_GroupScoreNamed, name, difficulty.AllScore, difficulty.MaxScore)
                    : string.Format(Lang.Instance.SlashPage_GroupScore, difficulty.AllScore, difficulty.MaxScore),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        StackPanel_Content.Children.Add(header);

        var rows = new StackPanel { Spacing = 6 };
        foreach (var difficulty in group)
        {
            foreach (var challenge in (difficulty.ChallengeList ?? []).OrderByDescending(c => c.ChallengeId))
            {
                var block = new StackPanel { Spacing = 4 };
                block.Children.Add(CreateSlashChallengeRow(challenge));
                
                block.Children.Add(CreateSlashTeamList(challenge));
                rows.Children.Add(block);
            }
        }
        StackPanel_Content.Children.Add(rows);
    }


    
    
    
    
    private FrameworkElement CreateSlashChallengeRow(KuroSlashChallenge challenge)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SlashRankColumnWidth) }); 
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       

        
        var rank = CreateSlashRankIcon(challenge.Rank);
        if (rank is not null)
        {
            rank.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(rank, 0);
            grid.Children.Add(rank);
        }

        
        var score = new TextBlock
        {
            Text = string.Format(Lang.Instance.SlashPage_ScoreLabel, challenge.Score),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xA7, 0x57)),
        };
        Grid.SetColumn(score, 1);
        grid.Children.Add(score);

        
        var floor = new TextBlock
        {
            Text = string.Format(Lang.Instance.TowerPage_Floor, challenge.ChallengeId),
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        };
        Grid.SetColumn(floor, 3);
        grid.Children.Add(floor);

        var name = new TextBlock
        {
            Text = string.IsNullOrEmpty(challenge.ChallengeName)
                ? string.Format(Lang.Instance.SlashPage_ChallengeFallback, challenge.ChallengeId)
                : challenge.ChallengeName,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(name, 4);
        grid.Children.Add(name);

        return KuroBbsUi.KuroCard(grid, padding: new Thickness(12, 0, 16, 0), height: SlashRowHeight);
    }


    
    
    
    
    private static ImageSource? SlashRankImageSource(string? rank)
    {
        string text = (rank ?? "").Trim().ToUpperInvariant();
        if (text.Length == 0 || !SlashRankIconFiles.TryGetValue(text, out var file))
        {
            return null;
        }
        return BundledImage(file);
    }


    
    private static FrameworkElement? CreateSlashRankIcon(string? rank)
    {
        if (SlashRankImageSource(rank) is not { } src)
        {
            return null;
        }
        return new Image
        {
            Source = src,
            Width = 30,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }


    
    
    
    
    
    private FrameworkElement CreateSlashTeamList(KuroSlashChallenge challenge)
    {
        var halves = challenge.HalfList ?? [];
        if (halves.Count == 0)
        {
            return new TextBlock
            {
                Text = Lang.Instance.TowerPage_NoChallengeData,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            };
        }

        
        var teams = new Grid { ColumnSpacing = 24 };
        int cols = halves.Count >= 2 ? 2 : 1;
        for (int c = 0; c < cols; c++)
        {
            teams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        for (int i = 0; i < halves.Count && i < cols; i++)
        {
            var block = CreateSlashTeamBlock(halves[i], i);
            block.HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            Grid.SetColumn(block, i);
            teams.Children.Add(block);
        }

        return KuroBbsUi.KuroCard(teams, padding: new Thickness(12, 10, 12, 10), secondaryBackground: true, margin: new Thickness(0, 2, 0, 0));
    }


    
    
    
    
    private FrameworkElement CreateSlashTeamBlock(KuroSlashHalf half, int index)
    {
        
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        head.Children.Add(new TextBlock
        {
            Text = SlashTeamLabel(index),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        if (half.Score > 0)
        {
            head.Children.Add(new TextBlock
            {
                Text = half.Score.ToString(),
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xBD, 0xA7, 0x57)),
            });
        }

        var root = new StackPanel { Spacing = 4 };
        root.Children.Add(head);

        
        
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = SlashRoleRowSpacing, VerticalAlignment = VerticalAlignment.Center };
        foreach (var role in half.RoleList ?? [])
        {
            int chain = role.RoleId is int id && _slashChainMap is { } m && m.TryGetValue(id, out var c) ? c : 0;
            row.Children.Add(CreateRolePhoto(role.IconUrl, chain, 0, role.SkillBranchIndex, SlashRolePhotoSize, SlashBranchIconSize, decodeToSize: false, showRarityBar: false, placeholderBackground: false));
        }
        if (!string.IsNullOrEmpty(half.BuffName) || !string.IsNullOrEmpty(half.BuffIcon))
        {
            row.Children.Add(CreateSlashBuffBox(half));
        }
        root.Children.Add(row);
        return root;
    }


    
    
    
    
    
    
    private static FrameworkElement CreateSlashBuffBox(KuroSlashHalf half)
    {
        
        
        const double size = 44;

        
        const double barHeight = size * 0.20;
        const double iconSize = size - (2 * barHeight) - 4;

        var root = new Grid { Width = size, Height = size };

        
        var box = new Border
        {
            CornerRadius = new CornerRadius(4),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x74, 0x72, 0x6F)),
            BorderThickness = new Thickness(1),
            Background = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(0, 1),
                GradientStops =
                {
                    new GradientStop { Color = Color.FromArgb(0xFF, 0x1C, 0x2B, 0x39), Offset = 0 },
                    new GradientStop { Color = Color.FromArgb(0xFF, 0x89, 0x86, 0x85), Offset = 1 },
                },
            },
        };
        
        if (!string.IsNullOrEmpty(half.BuffIcon))
        {
            try
            {
                box.Child = new Image
                {
                    Source = new BitmapImage(new Uri(half.BuffIcon)),
                    Stretch = Stretch.Uniform,
                    Width = iconSize,
                    Height = iconSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
            }
            catch
            {
                
            }
        }
        root.Children.Add(box);

        
        if (half.BuffQuality > 0 && SlashBuffLvFiles.TryGetValue(half.BuffQuality, out var lvFile) && BundledImage(lvFile) is { } lv)
        {
            root.Children.Add(new Image
            {
                Source = lv,
                Stretch = Stretch.Fill,
                Height = barHeight,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
            });
        }

        if (!string.IsNullOrEmpty(half.BuffName))
        {
            ToolTipService.SetToolTip(root, string.IsNullOrEmpty(half.BuffDescription)
                ? half.BuffName
                : $"{half.BuffName}\n{half.BuffDescription}");
        }
        return root;
    }


    
    private static int SlashKind(KuroSlashDifficulty difficulty)
    {
        string name = difficulty.DifficultyName ?? "";
        if (name.Contains("湍渊"))
        {
            return SlashKindTurbulence;
        }
        if (name.Contains("海隙"))
        {
            return SlashKindRift;
        }
        if (name.Contains("禁忌"))
        {
            return SlashKindForbidden;
        }
        return Math.Clamp(difficulty.Difficulty, 0, 2);
    }


    private static ImageSource? BundledImage(string file)
    {
        try
        {
            return new BitmapImage(new Uri($"ms-appx:///Assets/Image/Combat/{file}"));
        }
        catch
        {
            return null;
        }
    }


    
    private static string SlashGroupTitle(List<KuroSlashDifficulty> group)
    {
        foreach (var difficulty in group)
        {
            string name = (difficulty.DifficultyName ?? "").Trim();
            int dash = name.IndexOf('-');
            if (dash > 0)
            {
                return name[..dash];
            }
            if (name.Length > 0)
            {
                return name;
            }
        }
        return Lang.Instance.KuroBbs_SongOfVoidSea;
    }


    
    private static string SlashShortName(KuroSlashDifficulty difficulty)
    {
        string name = (difficulty.DifficultyName ?? "").Trim();
        int dash = name.IndexOf('-');
        return dash > 0 && dash < name.Length - 1 ? name[(dash + 1)..] : name;
    }


    private static string SlashTeamLabel(int index)
    {
        return index switch
        {
            0 => Lang.Instance.SlashPage_Team1,
            1 => Lang.Instance.SlashPage_Team2,
            _ => string.Format(Lang.Instance.SlashPage_TeamN, index + 1),
        };
    }

    #endregion


    #region 逆境深塔（深境区）

    
    
    
    
    private const string TowerStarFile = "TowerStar.png";

    
    private const string TowerStarEmptyFile = "TowerStarEmpty.png";

    
    
    
    private const double TowerStarSize = 16;

    
    
    
    
    private readonly record struct TowerWidgetInfo(long RefreshAt, int Cur, int Max);


    
    
    
    
    
    
    
    private static async Task<TowerWidgetInfo> ResolveTowerWidgetAsync(KuroBbsAccount account, long seasonEndTime)
    {
        try
        {
            var widget = await KuroBbsClient.Instance.GetWuwaWidgetDataAsync(account);
            if (widget.Data?.TowerData is { } tower)
            {
                long at = tower.RefreshTimeStamp is long ts && ts > 0 ? ts : 0;
                if (at == 0)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    at = seasonEndTime > now && seasonEndTime < now + 120L * 86400 ? seasonEndTime : 0;
                }
                return new TowerWidgetInfo(at, tower.Cur, tower.Total);
            }
        }
        catch
        {
            
        }

        long nowTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long fallback = seasonEndTime > nowTs && seasonEndTime < nowTs + 120L * 86400 ? seasonEndTime : 0;
        return new TowerWidgetInfo(fallback, 0, 0);
    }


    
    
    private static async Task<(Dictionary<int, int> Chain, Dictionary<int, int> Star)> BuildRoleAuxMapsAsync(KuroBbsAccount account)
    {
        var chain = new Dictionary<int, int>();
        var star = new Dictionary<int, int>();
        try
        {
            var roles = await KuroBbsClient.Instance.GetRoleListAsync(account);
            foreach (var role in roles.Data ?? [])
            {
                chain[role.RoleId] = role.ChainUnlockNum;
                star[role.RoleId] = role.StarLevel;
            }
        }
        catch
        {
            
        }
        return (chain, star);
    }


    
    private FrameworkElement CreateTowerHeader(KuroTowerDifficulty difficulty, TowerWidgetInfo widget)
    {
        var areas = difficulty.TowerAreaList ?? [];
        
        int totalStar = difficulty.Difficulty == 3 && widget.Max > 0 ? widget.Cur : areas.Sum(a => a.Star);
        int maxStar = difficulty.Difficulty == 3 && widget.Max > 0 ? widget.Max : areas.Sum(a => a.MaxStar);
        if (maxStar <= 0) maxStar = areas.Count * 12;

        var left = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        left.Children.Add(new TextBlock
        {
            Text = difficulty.DifficultyName ?? "深境区",
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        left.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.TowerPage_TotalStarCount, totalStar, maxStar),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        left.Children.Add(new ProgressBar
        {
            Value = totalStar,
            Maximum = maxStar,
            Width = 200,
            Height = 8,
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
            Background = (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
        });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        
        var countdown = FormatTowerRefreshCountdown(widget.RefreshAt);
        if (!string.IsNullOrEmpty(countdown))
        {
            var pillContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            pillContent.Children.Add(new FontIcon
            {
                Glyph = "\xE823",
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            });
            pillContent.Children.Add(new TextBlock
            {
                Text = countdown,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"],
            });

            var pill = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)Application.Current.Resources["AccentFillColorTertiaryBrush"],
                Child = pillContent,
            };
            Grid.SetColumn(pill, 1);
            grid.Children.Add(pill);
        }

        return KuroBbsUi.KuroCard(grid, cornerRadius: 10, padding: new Thickness(16, 14, 16, 14));
    }


    private string FormatTowerRefreshCountdown(long seasonEndTime)
    {
        const long MinValidUnixSeconds = 1_704_067_200;
        try
        {
            if (seasonEndTime <= 0) return "";
            var end = seasonEndTime > 1_000_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(seasonEndTime)
                : DateTimeOffset.FromUnixTimeSeconds(seasonEndTime);
            if (end.ToUnixTimeSeconds() < MinValidUnixSeconds) return "";

            var remaining = end - DateTimeOffset.Now;
            if (remaining.TotalSeconds <= 0) return "";
            int days = (int)remaining.TotalDays;
            int hours = (int)remaining.TotalHours % 24;
            return string.Format(Lang.Instance.TowerPage_RefreshAfter, days, hours);
        }
        catch
        {
            return "";
        }
    }


    
    private FrameworkElement CreateTowerAreasGrid(List<KuroTowerArea> areas, Dictionary<int, int> chainMap)
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < areas.Count && i < 3; i++)
        {
            var card = CreateTowerAreaCard(areas[i], i, chainMap);
            Grid.SetColumn(card, i);
            grid.Children.Add(card);
        }
        return grid;
    }


    private Border CreateTowerAreaCard(KuroTowerArea area, int index, Dictionary<int, int> chainMap)
    {
        var accent = GetTowerAreaAccentColor(index);

        
        
        var floors = (area.FloorList ?? [])
            .Where(f => (f.Floor ?? 0) > 0)
            .OrderBy(f => f.Floor)
            .ToList();

        var header = new StackPanel { Spacing = 2 };
        header.Children.Add(new TextBlock
        {
            Text = area.AreaName ?? string.Format(Lang.Instance.CombatPage_AreaFallback, area.AreaId),
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        
        var starCount = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        starCount.Children.Add(new TextBlock
        {
            Text = $"{area.Star} / {area.MaxStar}",
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xA3, 0x3D)),
        });
        starCount.Children.Add(new Image
        {
            Source = BundledImage(TowerStarFile),
            Width = TowerStarSize,
            Height = TowerStarSize,
            VerticalAlignment = VerticalAlignment.Center,
        });
        header.Children.Add(starCount);

        var floorPanel = new StackPanel { Spacing = 8, Margin = new Thickness(0, 12, 0, 0) };
        if (floors.Count > 0)
        {
            foreach (var floor in floors)
            {
                floorPanel.Children.Add(CreateTowerFloorRow(floor, chainMap));
            }
        }
        else
        {
            floorPanel.Children.Add(new TextBlock
            {
                Text = Lang.Instance.TowerPage_NoChallengeData,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }

        var root = new StackPanel();
        root.Children.Add(header);
        root.Children.Add(floorPanel);

        return KuroBbsUi.KuroCard(root, cornerRadius: 10, padding: new Thickness(14), borderBrush: new SolidColorBrush(accent));
    }


    
    private FrameworkElement CreateTowerFloorRow(KuroTowerFloor floor, Dictionary<int, int> chainMap)
    {
        int floorNo = floor.Floor ?? 0;
        int star = Math.Clamp(floor.Star, 0, 3);
        var roles = floor.RoleList ?? [];

        var left = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.TowerPage_Floor, floorNo),
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });

        
        
        
        var starsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
        for (int i = 0; i < 3; i++)
        {
            starsPanel.Children.Add(new Image
            {
                Source = BundledImage(i < star ? TowerStarFile : TowerStarEmptyFile),
                Width = TowerStarSize,
                Height = TowerStarSize,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        left.Children.Add(starsPanel);

        
        var avatars = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        for (int i = 0; i < 3; i++)
        {
            var role = i < roles.Count ? roles[i] : null;
            int chain = 0;
            if (role?.RoleId is int rid)
            {
                chainMap.TryGetValue(rid, out chain);
            }
            avatars.Children.Add(CreateTowerAvatar(role is null ? null : role.IconUrl, chain));
        }

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(left, 0);
        row.Children.Add(left);
        Grid.SetColumn(avatars, 1);
        row.Children.Add(avatars);

        return new Border
        {
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            Child = row,
        };
    }


    
    private FrameworkElement CreateTowerAvatar(string? url, int chain)
    {
        var root = new Grid { Width = 38, Height = 38 };

        if (!string.IsNullOrEmpty(url))
        {
            root.Children.Add(new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Child = new Image
                {
                    Source = new BitmapImage(new Uri(url)),
                    Stretch = Stretch.UniformToFill,
                },
            });
        }
        else
        {
            root.Children.Add(new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Opacity = 0.5,
            });
        }

        
        if (!string.IsNullOrEmpty(url) && chain > 0)
        {
            root.Children.Add(new Border
            {
                MinWidth = 15,
                Height = 15,
                CornerRadius = new CornerRadius(7.5),
                Padding = new Thickness(3, 0, 3, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xA3, 0x3D)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0xA0, 0x14, 0x14, 0x14)),
                Child = new TextBlock
                {
                    Text = chain.ToString(),
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A)),
                },
            });
        }

        return root;
    }


    private static Color GetTowerAreaAccentColor(int index)
    {
        return index switch
        {
            0 => Color.FromArgb(0xFF, 0x5B, 0x9B, 0xD5), 
            1 => Color.FromArgb(0xFF, 0x9E, 0x6F, 0xDD), 
            2 => Color.FromArgb(0xFF, 0x70, 0xAD, 0x47), 
            _ => Color.FromArgb(0xFF, 0x9E, 0x9E, 0x9E),
        };
    }

    #endregion


    #region 构建卡片的辅助方法

    private void HandleError(int code, string? msg)
    {
        KuroBbsUi.HandleKuroError(code, msg, TextBlock_Title.Text, Lang.Instance.RolePage_LoadFailed);
    }


    private void AddLockedCard(string title)
    {
        
        if (StackPanel_Content.Children.Count > 0)
        {
            return;
        }
        StackPanel_Content.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.CombatPage_Locked, title),
            FontSize = 13,
            Opacity = 0.6,
        });
    }


    
    private void AddNoDataCard()
    {
        
        if (StackPanel_Content.Children.Count > 0)
        {
            return;
        }
        StackPanel_Content.Children.Add(new TextBlock
        {
            Text = string.Format(Lang.Instance.CombatPage_NoData, TextBlock_Title.Text),
            FontSize = 13,
            Opacity = 0.6,
            TextWrapping = TextWrapping.Wrap,
        });
    }


    #endregion

}




internal sealed class MatrixRoleAuxMaps
{
    
    public Dictionary<int, int>? Chain { get; set; }

    
    public Dictionary<int, int>? Star { get; set; }
}
