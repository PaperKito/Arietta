using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace Arietta.Features.KuroBbs;

public sealed partial class PgrResourcePage : PageBase
{

    private static readonly Color[] Palette =
    [
        Color.FromArgb(0xFF, 0xC5, 0x6B, 0xE8),
        Color.FromArgb(0xFF, 0x5B, 0x8D, 0xEF),
        Color.FromArgb(0xFF, 0x4C, 0xB0, 0x8A),
    ];

    private KuroPgrResourceReport? _report;
    private long _peakBlack = 1;
    private long _peakDevelop = 1;
    private long _peakTrade = 1;
    private bool _loading;

    private List<KuroPgrResourceMonth> _allMonths = [];

    private HashSet<int> _serverMonthKeys = [];


    public PgrResourcePage()
    {
        this.InitializeComponent();
        this.Loaded += PgrResourcePage_Loaded;
    }


    private void PgrResourcePage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= PgrResourcePage_Loaded;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;
        _ = LoadAsync();
    }


    protected override void OnUnloaded()
    {
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        base.OnUnloaded();
    }


    private void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () => await LoadAsync());
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();


    private Task LoadAsync() => KuroBbsUi.RunPgrLoadAsync(
        InfoBar_NoAccount,
        InfoBar_Error,
        ProgressRing_Loading,
        foldInfoBar: false,
        showRing: false,
        collapseRing: false,
        beforeLoad: null,
        async account =>
        {
            var result = await KuroBbsClient.Instance.GetPgrResourceReportAsync(account);
            if (result is { IsSuccess: true, Data: { } data })
            {
                _report = data;

                ResourceRecordStore.SavePgrReport(account.PgrRoleId, data);
                MergeLocalMonths(account.PgrRoleId);
                ComputePeaks();
                FillPeriodList();

                if (ListView_Periods.Items.Count > 0)
                {
                    ListView_Periods.SelectedIndex = 0;
                }
            }
            else
            {
                HandleError(result.Code, result.Msg);
            }
        },
        Lang.Instance.KuroBbs_PgrResource);

    private void MergeLocalMonths(string roleId)
    {
        var serverMonths = _report?.PerMonthList ?? new List<KuroPgrResourceMonth>();
        _serverMonthKeys = serverMonths.Select(m => m.CurrentYear * 100 + m.CurrentMonth).ToHashSet();
        var localOnly = ResourceRecordStore.ListPgrMonths(roleId)
            .Where(r => !_serverMonthKeys.Contains(r.Year * 100 + r.Month))
            .Select(r => new KuroPgrResourceMonth
            {
                CurrentYear = r.Year,
                CurrentMonth = r.Month,
                Month = string.IsNullOrEmpty(r.Title) ? $"{r.Year}-{r.Month:00}" : r.Title,
                MonthBlackCard = r.BlackCard,
                MonthDevelopResource = r.DevelopResource,
                MonthTradeCredit = r.TradeCredit,
            });
        _allMonths = localOnly.Concat(serverMonths)
            .OrderBy(m => m.CurrentYear * 100 + m.CurrentMonth)
            .ToList();
    }

    private void ComputePeaks()
    {
        _peakBlack = Math.Max(1, _allMonths.Max(m => (long)m.MonthBlackCard));
        _peakDevelop = Math.Max(1, _allMonths.Max(m => (long)m.MonthDevelopResource));
        _peakTrade = Math.Max(1, _allMonths.Max(m => (long)m.MonthTradeCredit));
    }

    private void FillPeriodList()
    {
        if (_report is null)
        {
            return;
        }
        var items = new List<PgrResourcePeriodItem>
        {
            new()
            {
                Title = Lang.Instance.PgrResource_Total,
                Subtitle = _report.TotalBlackCard.ToString("N0"),
                IsCumulative = true,
            }
        };
        for (int i = _allMonths.Count - 1; i >= 0; i--)
        {
            var m = _allMonths[i];
            string subtitle = m.MonthBlackCard.ToString("N0");
            items.Add(new PgrResourcePeriodItem
            {
                Title = string.IsNullOrEmpty(m.Month) ? $"{m.CurrentYear}-{m.CurrentMonth:00}" : m.Month!,
                Subtitle = subtitle,
                IsCumulative = false,
                MonthIndex = i,
            });
        }
        ListView_Periods.ItemsSource = items;
    }


    private void ListView_Periods_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListView_Periods.SelectedItem is not PgrResourcePeriodItem item || _loading)
        {
            return;
        }
        RenderPeriod(item);
    }

    private void RenderPeriod(PgrResourcePeriodItem item)
    {
        if (_report is null)
        {
            return;
        }
        long black, develop, trade;
        if (item.IsCumulative)
        {

            var months = _report?.PerMonthList ?? new List<KuroPgrResourceMonth>();
            black = months.Sum(m => (long)m.MonthBlackCard);
            develop = months.Sum(m => (long)m.MonthDevelopResource);
            trade = months.Sum(m => (long)m.MonthTradeCredit);
        }
        else
        {
            var m = _allMonths[item.MonthIndex];
            black = m.MonthBlackCard;
            develop = m.MonthDevelopResource;
            trade = m.MonthTradeCredit;
        }

        TextBlock_PeriodTitle.Text = item.Title;
        TextBlock_TotalBlackCard.Text = black.ToString("N0");
        TextBlock_TotalDevelop.Text = develop.ToString("N0");
        TextBlock_TotalTrade.Text = trade.ToString("N0");

        FillComposition(item);
        FillMonthlyTable(item);
    }

    private void FillComposition(PgrResourcePeriodItem item)
    {
        StackPanel_Composition.Children.Clear();
        if (item.IsCumulative)
        {
            var months = _report?.PerMonthList ?? new List<KuroPgrResourceMonth>();
            (string Name, string Icon, Func<KuroPgrResourceMonth, long> Value)[] rows =
            [
                (Lang.Instance.PgrResource_BlackCard, "ms-appx:///Assets/Image/Common/blackCard.png", m => m.MonthBlackCard),
                (Lang.Instance.PgrResource_Develop, "ms-appx:///Assets/Image/Common/developResource.png", m => m.MonthDevelopResource),
                (Lang.Instance.PgrResource_Trade, "ms-appx:///Assets/Image/Common/tradeCredit.png", m => m.MonthTradeCredit),
            ];
            foreach (var (name, icon, value) in rows)
            {
                var segments = months
                    .Where(m => value(m) > 0)
                    .Select(m => (Label: $"{m.CurrentMonth}月", Value: value(m)))
                    .ToList();
                if (segments.Count == 0)
                {
                    continue;
                }
                var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                header.Children.Add(new Image { Width = 16, Height = 16, Source = new BitmapImage(new Uri(icon)) });
                header.Children.Add(new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
                StackPanel_Composition.Children.Add(header);
                AddCompositionBar(StackPanel_Composition, segments, ResourceBriefPage.Palette);
            }
        }
        else
        {
            var m = _allMonths[item.MonthIndex];
            var segments = new (string Label, long Value)[]
            {
                (Lang.Instance.PgrResource_BlackCard, m.MonthBlackCard),
                (Lang.Instance.PgrResource_Develop, m.MonthDevelopResource),
                (Lang.Instance.PgrResource_Trade, m.MonthTradeCredit),
            };
            AddCompositionBar(StackPanel_Composition, segments, Palette);
        }

        if (StackPanel_Composition.Children.Count == 0)
        {
            StackPanel_Composition.Children.Add(new TextBlock
            {
                Text = Lang.Instance.ResourceBrief_Empty,
                FontSize = 12,
                Opacity = 0.6,
            });
        }
    }

    private static void AddCompositionBar(StackPanel host, IReadOnlyList<(string Label, long Value)> segments, Color[] palette)
    {
        long total = segments.Sum(s => Math.Max(s.Value, 0));
        if (total <= 0)
        {
            return;
        }

        var rendered = segments.Where(s => s.Value > 0).ToList();
        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, Margin = new Thickness(0, 4, 0, 0) };
        var plan = new List<(Border Segment, double Share)>();
        for (int i = 0; i < rendered.Count; i++)
        {
            var (label, value) = rendered[i];
            var brush = new SolidColorBrush(palette[i % palette.Length]);
            var segment = new Border
            {
                Height = 14,
                Width = 0,
                CornerRadius = new CornerRadius(
                    i == 0 ? 7 : 0,
                    i == rendered.Count - 1 ? 7 : 0,
                    i == rendered.Count - 1 ? 7 : 0,
                    i == 0 ? 7 : 0),
                Background = brush,
            };
            bar.Children.Add(segment);
            plan.Add((segment, (double)value / total));

            var legendItem = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
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
                Text = $"{label} {value * 100 / total}%",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
            });
            legend.Children.Add(legendItem);
        }
        host.Children.Add(bar);
        host.Children.Add(legend);

        void AnimateSegments(double totalWidth)
        {
            for (int k = 0; k < plan.Count; k++)
            {
                KuroBbsUi.AnimateWidth(plan[k].Segment, Math.Max(3, totalWidth * plan[k].Share), 150 + k * 110);
            }
        }
        if (host.ActualWidth > 0)
        {
            AnimateSegments(host.ActualWidth);
        }
        else
        {
            SizeChangedEventHandler handler = null;
            handler = (s, e) =>
            {
                host.SizeChanged -= handler;
                if (e.NewSize.Width > 0)
                {
                    AnimateSegments(e.NewSize.Width);
                }
            };
            host.SizeChanged += handler;
        }
    }

    private void FillMonthlyTable(PgrResourcePeriodItem item)
    {
        StackPanel_Items.Children.Clear();
        if (_allMonths.Count == 0)
        {
            StackPanel_Items.Children.Add(new TextBlock
            {
                Text = Lang.Instance.ResourceBrief_Empty,
                FontSize = 12,
                Opacity = 0.6,
            });
            return;
        }

        (string Icon, Func<KuroPgrResourceMonth, long> Value)[] cols =
        [
            ("ms-appx:///Assets/Image/Common/blackCard.png", m => m.MonthBlackCard),
            ("ms-appx:///Assets/Image/Common/developResource.png", m => m.MonthDevelopResource),
            ("ms-appx:///Assets/Image/Common/tradeCredit.png", m => m.MonthTradeCredit),
        ];
        long[] peaks = [_peakBlack, _peakDevelop, _peakTrade];

        for (int r = 0; r < _allMonths.Count; r++)
        {
            var m = _allMonths[r];
            var row = new Grid { ColumnSpacing = 8 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
            for (int c = 0; c < cols.Length; c++)
            {
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var label = new TextBlock
            {
                Text = string.IsNullOrEmpty(m.Month) ? $"{m.CurrentYear}-{m.CurrentMonth:00}" : m.Month!,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            for (int c = 0; c < cols.Length; c++)
            {
                long value = cols[c].Value(m);
                var cell = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                cell.Children.Add(new Image { Width = 15, Height = 15, Source = new BitmapImage(new Uri(cols[c].Icon)) });
                cell.Children.Add(new TextBlock
                {
                    Text = value.ToString("N0"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                Grid.SetColumn(cell, 1 + c * 2);
                row.Children.Add(cell);

                var bar = new Border
                {
                    Height = 12,
                    Width = 0,
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Palette[c]),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(bar, 2 + c * 2);
                row.Children.Add(bar);
                if (value > 0)
                {
                    KuroBbsUi.AnimateWidth(bar, 180.0 * value / Math.Max(1, peaks[c]), 220 + r * 70 + c * 50);
                }
            }

            bool highlighted = !item.IsCumulative && r == item.MonthIndex;
            var wrapper = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3),
                Background = highlighted
                    ? (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"]
                    : new SolidColorBrush(Colors.Transparent),
                Child = row,
            };
            FadeIn(wrapper, 30 * r);
            StackPanel_Items.Children.Add(wrapper);
        }
    }

    private static void FadeIn(UIElement element, int delayMs)
    {
        try
        {
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Begin();
        }
        catch { }
    }


    private void HandleError(int code, string? msg)
    {
        KuroBbsUi.HandleKuroError(code, msg, null, Lang.Instance.PgrCommon_LoadFailed, InfoBar_Error, foldVisibility: false);
    }
}

public sealed class PgrResourcePeriodItem
{
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public bool IsCumulative { get; init; }

    public int MonthIndex { get; init; }
}
