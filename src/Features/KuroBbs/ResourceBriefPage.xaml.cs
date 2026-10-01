using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;







public sealed partial class ResourceBriefPage : PageBase
{
    
    internal static readonly Windows.UI.Color[] Palette =
    [
        Windows.UI.Color.FromArgb(0xFF, 0xE8, 0x6A, 0x6A),
        Windows.UI.Color.FromArgb(0xFF, 0x5B, 0x8D, 0xEF),
        Windows.UI.Color.FromArgb(0xFF, 0x9A, 0x6A, 0xB8),
        Windows.UI.Color.FromArgb(0xFF, 0x53, 0xB8, 0xA5),
        Windows.UI.Color.FromArgb(0xFF, 0x6B, 0xA8, 0x5A),
        Windows.UI.Color.FromArgb(0xFF, 0xD9, 0xB1, 0x4C),
        Windows.UI.Color.FromArgb(0xFF, 0x8A, 0x7F, 0xD0),
        Windows.UI.Color.FromArgb(0xFF, 0x64, 0xB5, 0xD8),
    ];

    private KuroBriefing? _briefing;
    private string _periodType = "month";
    private bool _loading;

    
    private HashSet<int> _serverPeriodIndexes = [];


    public ResourceBriefPage()
    {
        this.InitializeComponent();
        this.Loaded += ResourceBriefPage_Loaded;
    }


    private async void ResourceBriefPage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= ResourceBriefPage_Loaded;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;
        await LoadBriefingAsync();
    }


    protected override void OnUnloaded()
    {
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        base.OnUnloaded();
    }


    private async void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            _briefing = null;
            await LoadBriefingAsync();
        });
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e)
    {
        _briefing = null;
        await LoadBriefingAsync();
    }


    private async void Button_PeriodType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string type })
        {
            _periodType = type;
            HighlightPeriodTypeButton();
            FillPeriodList();
            await LoadSelectedPeriodAsync();
        }
    }


    private void HighlightPeriodTypeButton()
    {
        Button_Month.Background = _periodType == "month" ? (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"] : new SolidColorBrush(Colors.Transparent);
        Button_Week.Background = _periodType == "week" ? (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"] : new SolidColorBrush(Colors.Transparent);
        Button_Version.Background = _periodType == "version" ? (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"] : new SolidColorBrush(Colors.Transparent);
    }


    private async void ListView_Periods_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await LoadSelectedPeriodAsync();
    }


    
    private async Task LoadBriefingAsync()
    {
        var account = KuroBbsAccountStore.Load();
        if (account is null || !account.IsValid)
        {
            InfoBar_NoAccount.IsOpen = true;
            return;
        }
        InfoBar_NoAccount.IsOpen = false;
        if (_briefing is not null)
        {
            FillPeriodList();
            await LoadSelectedPeriodAsync();
            return;
        }
        ProgressRing_Loading.IsActive = true;
        try
        {
            var result = await KuroBbsClient.Instance.GetBriefingListAsync(account.Token);
            if (result is { IsSuccess: true, Data: { } briefing })
            {
                _briefing = briefing;
                HighlightPeriodTypeButton();
                FillPeriodList();
                if (ListView_Periods.Items.Count > 0)
                {
                    ListView_Periods.SelectedIndex = 0;
                }
            }
            else
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroBbs_ResourceBrief, result.Msg ?? Lang.Instance.RolePage_LoadFailed);
            }
        }
        finally
        {
            ProgressRing_Loading.IsActive = false;
        }
    }


    
    
    
    
    private void FillPeriodList()
    {
        var titles = _periodType switch
        {
            "week" => _briefing?.Weeks,
            "version" => _briefing?.Versions,
            _ => _briefing?.Months,
        } ?? [];
        var items = titles.Select(t => new PeriodItem(t)).ToList();
        _serverPeriodIndexes = titles.Select(t => t.Index).ToHashSet();

        var account = KuroBbsAccountStore.Load();
        if (account is not null)
        {
            foreach (var row in ResourceRecordStore.ListBriefing(account.RoleId, _periodType)
                         .Where(r => !_serverPeriodIndexes.Contains(r.PeriodIndex)))
            {
                items.Add(new PeriodItem(row.PeriodIndex, row.Title ?? row.PeriodIndex.ToString()));
            }
        }

        
        ListView_Periods.ItemsSource = items.OrderByDescending(i => i.Index).ToList();
        
        if (items.Count > 0 && ListView_Periods.SelectedIndex < 0)
        {
            ListView_Periods.SelectedIndex = 0;
        }
    }


    private async Task LoadSelectedPeriodAsync()
    {
        if (ListView_Periods.SelectedItem is not PeriodItem item || _loading)
        {
            return;
        }
        var account = KuroBbsAccountStore.Load();
        if (account is null) return;

        
        if (!_serverPeriodIndexes.Contains(item.Index))
        {
            var archived = ResourceRecordStore.GetBriefing(account.RoleId, _periodType, item.Index);
            if (archived is not null)
            {
                FillDetail(item.Title, archived);
            }
            return;
        }

        _loading = true;
        ProgressRing_Loading.IsActive = true;
        try
        {
            var result = await KuroBbsClient.Instance.GetBriefingDetailAsync(account, item.Index, _periodType);
            if (result is { IsSuccess: true, Data: { } record })
            {
                
                ResourceRecordStore.SaveBriefing(account.RoleId, _periodType, item.Index, item.Title, record);
                FillDetail(item.Title, record);
            }
            else if (result.IsTokenExpired)
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroBbs_TokenExpired, Lang.Instance.KuroBbs_TokenExpiredDesc);
            }
            else
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroBbs_ResourceBrief, result.Msg ?? Lang.Instance.RolePage_LoadFailed);
            }
        }
        finally
        {
            _loading = false;
            ProgressRing_Loading.IsActive = false;
        }
    }


    #region 详情渲染与动画

    private void FillDetail(string periodTitle, KuroBriefingRecord record)
    {
        TextBlock_PeriodTitle.Text = $"{Lang.Instance.ResourceBrief_HistoryData} {periodTitle}";
        TextBlock_TotalStar.Text = record.TotalStar.ToString("N0");
        TextBlock_TotalCoin.Text = record.TotalCoin.ToString("N0");

        FillCompositionBar(StackPanel_StarBar, StackPanel_StarLegend, record.StarList);
        FillCompositionBar(StackPanel_CoinBar, StackPanel_CoinLegend, record.CoinList);
        FillItemList(record.StarList);
    }


    
    private void FillCompositionBar(StackPanel barHost, StackPanel legend, List<KuroBriefingItem>? items)
    {
        barHost.Children.Clear();
        legend.Children.Clear();
        if (items is not { Count: > 0 })
        {
            return;
        }
        long total = items.Sum(i => Math.Max(i.Num, 0));
        if (total <= 0)
        {
            return;
        }

        
        var plan = new List<(Border Segment, double Share)>();
        void AnimateSegments(double totalWidth)
        {
            for (int k = 0; k < plan.Count; k++)
            {
                KuroBbsUi.AnimateWidth(plan[k].Segment, Math.Max(4, totalWidth * plan[k].Share), 200 + k * 120);
            }
        }
        foreach (var (item, i) in items.Select((x, i) => (x, i)))
        {
            if (item.Num <= 0)
            {
                continue;
            }
            var brush = new SolidColorBrush(Palette[i % Palette.Length]);
            var segment = new Border
            {
                Height = 18,
                Width = 0,
                CornerRadius = new CornerRadius(i == 0 ? 9 : 0, 0, 0, i == 0 ? 9 : 0),
                Background = brush,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            barHost.Children.Add(segment);
            plan.Add((segment, (double)item.Num / total));

            
            var legendItem = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            legendItem.Children.Add(new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(2),
                Background = brush,
                VerticalAlignment = VerticalAlignment.Center,
            });
            legendItem.Children.Add(new TextBlock
            {
                Text = $"{item.Type} {item.Num * 100 / total}%",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
            });
            legend.Children.Add(legendItem);
        }

        if (barHost.ActualWidth > 0)
        {
            AnimateSegments(barHost.ActualWidth);
        }
        else
        {
            SizeChangedEventHandler handler = null;
            handler = (s, e) =>
            {
                barHost.SizeChanged -= handler;
                if (e.NewSize.Width > 0)
                {
                    AnimateSegments(e.NewSize.Width);
                }
            };
            barHost.SizeChanged += handler;
        }
    }


    
    private void FillItemList(List<KuroBriefingItem>? items)
    {
        StackPanel_Items.Children.Clear();
        if (items is not { Count: > 0 })
        {
            StackPanel_Items.Children.Add(new TextBlock
            {
                Text = Lang.Instance.ResourceBrief_Empty,
                FontSize = 12,
                Opacity = 0.6,
            });
            return;
        }
        long max = items.Max(i => i.Num);
        foreach (var (item, i) in items.Select((x, i) => (x, i)))
        {
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var name = new TextBlock { Text = item.Type, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 0);
            row.Children.Add(name);

            var num = new TextBlock
            {
                Text = item.Num.ToString("N0"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };
            Grid.SetColumn(num, 1);
            row.Children.Add(num);

            var bar = new Border
            {
                Height = 12,
                Width = 0,
                CornerRadius = new CornerRadius(2, 6, 6, 2),
                Background = new SolidColorBrush(Palette[i % Palette.Length]),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(bar, 2);
            row.Children.Add(bar);
            if (item.Num > 0)
            {
                
                
                int delayMs = 250 + i * 100;
                double ratio = (double)item.Num / max;
                SizeChangedEventHandler handler = null;
                handler = (s, e) =>
                {
                    row.SizeChanged -= handler;
                    double available = Math.Max(0, e.NewSize.Width - 110 - 90 - 16);
                    KuroBbsUi.AnimateWidth(bar, available * ratio, delayMs);
                };
                row.SizeChanged += handler;
            }

            StackPanel_Items.Children.Add(row);
        }
    }

    #endregion

}



public sealed class PeriodItem
{
    public string Title { get; init; } = "";
    public int Index { get; init; }

    public PeriodItem(KuroBriefingTitle title)
    {
        Title = title.Title ?? "";
        Index = title.Index;
    }

    
    public PeriodItem(int index, string title)
    {
        Index = index;
        Title = title;
    }
}
