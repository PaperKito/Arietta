using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
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

    private async void Menu_PeriodType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string type } && _periodType != type)
        {
            _periodType = type;
            TextBlock_PeriodType.Text = type == "version"
                ? Lang.Instance.ResourceBrief_Version
                : Lang.Instance.ResourceBrief_Month;
            FillPeriodList();
            await LoadSelectedPeriodAsync();
        }
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
            ScrollViewer_Content.Visibility = Visibility.Collapsed;
            return;
        }
        InfoBar_NoAccount.IsOpen = false;
        ScrollViewer_Content.Visibility = Visibility.Visible;
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

                ResourceRecordStore.PruneBriefing(account.RoleId, "week", ResourceRecordStore.WeekWindowSize);
                FillPeriodList();
                if (ListView_Periods.Items.Count > 0)
                {
                    ListView_Periods.SelectedIndex = 0;
                }
            }
            else if (result.IsTokenExpired)
            {
                KuroBbsUi.ShowTokenExpiredToast();
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
        var titles = _periodType == "version" ? _briefing?.Versions : _briefing?.Months ?? [];
        var items = titles.Select(t => new PeriodItem(t) { Title = PeriodLabel(_periodType, t.Index, t.Title) }).ToList();
        _serverPeriodIndexes = titles.Select(t => t.Index).ToHashSet();

        var account = KuroBbsAccountStore.Load();
        if (account is not null)
        {
            var local = ResourceRecordStore.ListBriefing(account.RoleId, _periodType);
            foreach (var row in local.Where(r => !_serverPeriodIndexes.Contains(r.PeriodIndex)))
            {
                items.Add(new PeriodItem(row.PeriodIndex, PeriodLabel(_periodType, row.PeriodIndex, row.Title)));
            }

            var stars = local.ToDictionary(r => r.PeriodIndex, r => r.Record.TotalStar);
            foreach (var item in items)
            {
                if (stars.TryGetValue(item.Index, out var star))
                {
                    item.Star = star;
                }
            }
        }

        var ordered = items.OrderByDescending(i => i.Index).ToList();
        ListView_Periods.ItemsSource = ordered;

        if (items.Count > 0 && ListView_Periods.SelectedIndex < 0)
        {
            ListView_Periods.SelectedIndex = 0;
        }
    }

    private static string PeriodLabel(string periodType, int index, string? title)
    {
        if (periodType == "month" && index is >= 100000 and <= 999999)
        {
            return $"{index / 100:D4}-{index % 100:D2}";
        }
        if (periodType == "version" && index >= 10000)
        {
            return $"{index / 10000}.{index % 10000 / 100}";
        }
        return (title ?? "").Replace("版本", "");
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
                item.Star = archived.TotalStar;
                await FillDetailAsync(item.Title, archived, item.Index);
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
                item.Star = record.TotalStar;
                await FillDetailAsync(item.Title, record, item.Index);
            }
            else if (result.IsTokenExpired)
            {
                KuroBbsUi.ShowTokenExpiredToast();
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

    private async Task FillDetailAsync(string periodTitle, KuroBriefingRecord record, int periodIndex)
    {
        TextBlock_PeriodTitle.Text = $"{Lang.Instance.ResourceBrief_HistoryData} {periodTitle}";
        TextBlock_TotalStar.Text = record.TotalStar.ToString("N0");
        TextBlock_TotalCoin.Text = record.TotalCoin.ToString("N0");

        FillCompositionBar(StackPanel_StarBar, StackPanel_StarLegend, record.StarList);
        FillCompositionBar(StackPanel_CoinBar, StackPanel_CoinLegend, record.CoinList);

        if (_periodType != "month")
        {
            TextBlock_DetailTitle.Text = Lang.Instance.ResourceBrief_Detail;
            FillItemList(record.StarList);
            return;
        }

        TextBlock_DetailTitle.Text = Lang.Instance.ResourceBrief_Week;
        StackPanel_Items.Children.Clear();
        var weeks = await GetMonthWeeksAsync(periodIndex);
        RenderWeekRows(weeks);
    }

    private async Task<List<(string Label, KuroBriefingRecord? Record)>> GetMonthWeeksAsync(int monthIndex)
    {
        var monthStart = new DateTime(monthIndex / 100, monthIndex % 100, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var account = KuroBbsAccountStore.Load();
        var local = account is null ? [] : ResourceRecordStore.ListBriefing(account.RoleId, "week");
        var rows = new List<(int Index, string Label, KuroBriefingRecord? Record)>();
        var seen = new HashSet<int>();
        foreach (var row in local)
        {
            if (seen.Add(row.PeriodIndex) && ParseWeekSpan(row.PeriodIndex, row.Title) is { } span
                && span.Start <= monthEnd && span.End >= monthStart)
            {
                rows.Add((row.PeriodIndex, row.Title ?? "", row.Record));
            }
        }
        foreach (var week in _briefing?.Weeks ?? [])
        {
            if (!seen.Add(week.Index) || ParseWeekSpan(week.Index, week.Title) is not { } span
                || span.Start > monthEnd || span.End < monthStart)
            {
                continue;
            }
            rows.Add((week.Index, week.Title ?? "", null));
        }
        rows.Sort((a, b) => a.Index.CompareTo(b.Index));

        var serverWeeks = (_briefing?.Weeks ?? []).Select(w => w.Index).ToHashSet();
        var result = new List<(string Label, KuroBriefingRecord? Record)>();
        foreach (var (index, label, record) in rows)
        {
            var rec = record;
            if (rec is null && account is not null && serverWeeks.Contains(index))
            {
                try
                {
                    var detail = await KuroBbsClient.Instance.GetBriefingDetailAsync(account, index, "week");
                    if (detail is { IsSuccess: true, Data: { } data })
                    {
                        ResourceRecordStore.SaveBriefing(account.RoleId, "week", index, label, data);
                        rec = data;
                    }
                }
                catch
                {

                }
            }
            result.Add((label, rec));
        }
        return result;
    }

    private static (DateTime Start, DateTime End)? ParseWeekSpan(int index, string? title)
    {
        if (title is null)
        {
            return null;
        }
        var m = Regex.Match(title, @"(\d{1,2})月(\d{1,2})日~(\d{1,2})月(\d{1,2})日");
        if (!m.Success)
        {
            return null;
        }
        try
        {
            int year = index / 100;
            int m1 = int.Parse(m.Groups[1].Value), d1 = int.Parse(m.Groups[2].Value);
            int m2 = int.Parse(m.Groups[3].Value), d2 = int.Parse(m.Groups[4].Value);
            return (new DateTime(m1 > m2 ? year - 1 : year, m1, d1), new DateTime(year, m2, d2));
        }
        catch
        {
            return null;
        }
    }

    private void RenderWeekRows(List<(string Label, KuroBriefingRecord? Record)> rows)
    {
        StackPanel_Items.Children.Clear();
        if (rows.Count == 0)
        {
            StackPanel_Items.Children.Add(new TextBlock
            {
                Text = Lang.Instance.ResourceBrief_Empty,
                FontSize = 13,
                Opacity = 0.6,
            });
            return;
        }
        long maxStar = rows.Max(r => r.Record?.TotalStar ?? 0);
        long maxCoin = rows.Max(r => r.Record?.TotalCoin ?? 0);
        for (int i = 0; i < rows.Count; i++)
        {
            var (label, record) = rows[i];
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var date = new TextBlock
            {
                Text = label,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            };
            Grid.SetColumn(date, 0);
            row.Children.Add(date);
            row.Children.Add(WeekIconCell("star.png", 1));
            row.Children.Add(WeekNumCell(record?.TotalStar ?? 0, 2));
            var starBar = WeekBarCell(Windows.UI.Color.FromArgb(0xFF, 0x66, 0xBC, 0xF2), 3);
            row.Children.Add(starBar);
            row.Children.Add(WeekIconCell("coin.png", 4));
            row.Children.Add(WeekNumCell(record?.TotalCoin ?? 0, 5));
            var coinBar = WeekBarCell(Windows.UI.Color.FromArgb(0xFF, 0xF2, 0xDE, 0x77), 6);
            row.Children.Add(coinBar);

            StackPanel_Items.Children.Add(row);

            double starRatio = maxStar > 0 ? (double)(record?.TotalStar ?? 0) / maxStar : 0;
            double coinRatio = maxCoin > 0 ? (double)(record?.TotalCoin ?? 0) / maxCoin : 0;
            KuroBbsUi.WhenMeasured(row, width =>
            {
                double available = Math.Max(0, width - 150 - 24 - 70 - 24 - 110 - 72) / 2;
                KuroBbsUi.AnimateWidth(starBar, available * starRatio, 250 + i * 100);
                KuroBbsUi.AnimateWidth(coinBar, available * coinRatio, 300 + i * 100);
            });
        }
    }


    private static Image WeekIconCell(string icon, int column)
    {
        var image = new Image
        {
            Width = 24,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center,
            Source = new BitmapImage(new Uri($"ms-appx:///Assets/Image/Common/{icon}")),
        };
        Grid.SetColumn(image, column);
        return image;
    }


    private static TextBlock WeekNumCell(long value, int column)
    {
        var text = new TextBlock
        {
            Text = value.ToString("N0"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(text, column);
        return text;
    }

    private static Border WeekBarCell(Windows.UI.Color color, int column)
    {
        var bar = new Border
        {
            Height = 12,
            Width = 0,
            CornerRadius = new CornerRadius(2, 6, 6, 2),
            Background = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(bar, column);
        return bar;
    }

    private void FillCompositionBar(StackPanel barHost, StackPanel legend, List<KuroBriefingItem>? items)
    {
        barHost.Children.Clear();
        legend.Children.Clear();
        if (items is not { Count: > 0 })
        {
            return;
        }

        KuroBbsUi.AddCompositionBar(barHost, legend, barHost,
            items.Select((x, i) => (x.Type ?? "", x.Num,
                (Brush)new SolidColorBrush(Palette[i % Palette.Length]),
                new CornerRadius(i == 0 ? 9 : 0, 0, 0, i == 0 ? 9 : 0))).ToList(),
            barHeight: 18, minWidth: 4, legendItemSpacing: 4, baseDelayMs: 200, stepDelayMs: 120);
    }

    private void FillItemList(List<KuroBriefingItem>? items)
    {
        StackPanel_Items.Children.Clear();
        if (items is not { Count: > 0 })
        {
            StackPanel_Items.Children.Add(new TextBlock
            {
                Text = Lang.Instance.ResourceBrief_Empty,
                FontSize = 13,
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

            var name = new TextBlock { Text = item.Type, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 0);
            row.Children.Add(name);

            var num = new TextBlock
            {
                Text = item.Num.ToString("N0"),
                FontSize = 13,
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
                KuroBbsUi.WhenMeasured(row, width =>
                    KuroBbsUi.AnimateWidth(bar, Math.Max(0, width - 110 - 90 - 16) * ratio, delayMs));
            }

            StackPanel_Items.Children.Add(row);
        }
    }

    #endregion

}

public sealed class PeriodItem : INotifyPropertyChanged
{
    public string Title { get; init; } = "";
    public int Index { get; init; }

    private long? _star;

    public long? Star
    {
        get => _star;
        set
        {
            _star = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StarText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasStar)));
        }
    }

    public string StarText => Star?.ToString("N0") ?? "";
    public Visibility HasStar => Star is null ? Visibility.Collapsed : Visibility.Visible;

    public event PropertyChangedEventHandler? PropertyChanged;

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
