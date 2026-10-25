using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Features.Database;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Arietta.Features.PlayTime;

[INotifyPropertyChanged]
public sealed partial class PlayTimeStatsDialog : ContentDialog
{


    private readonly ILogger<PlayTimeStatsDialog> _logger = AppConfig.GetLogger<PlayTimeStatsDialog>();

    private readonly PlayTimeStatsService _playTimeStatsService = AppConfig.GetService<PlayTimeStatsService>();

    private Dictionary<DateOnly, long> _playTimePerDay = [];



    public PlayTimeStatsDialog()
    {
        this.InitializeComponent();
        this.Loaded += PlayTimeStatsDialog_Loaded;
        this.Unloaded += PlayTimeStatsDialog_Unloaded;
    }


    public GameBiz CurrentGameBiz { get; set; }




    private void PlayTimeStatsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _playTimeStatsService.ConvertItemToStats();
            LoadPlayTimeStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load play time stats: GameBiz {biz}", CurrentGameBiz);
        }
        _playTimeLoaded = true;
    }


    private void PlayTimeStatsDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= PlayTimeStatsDialog_Loaded;
        this.Unloaded -= PlayTimeStatsDialog_Unloaded;
        StatCards?.Clear();
        PlayTimeBarChart.Items = null;
        PlayTimeHeatmap.Days = null;
        _playTimeLoaded = false;
    }


    private bool _playTimeLoaded;

    public string BarTotalText { get; set => SetProperty(ref field, value); }

    public List<StatCardItem>? StatCards { get; set => SetProperty(ref field, value); }



    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }

    private void LoadPlayTimeStats()
    {
        var biz = CurrentGameBiz;
        try
        {
            var sessions = _playTimeStatsService.GetPlayTimeInRange(biz, DateTimeOffset.MinValue, DateTimeOffset.Now);
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            long totalMs = 0;

            long longestSpan = 0, longestStart = 0;

            long lastStart = 0, lastSpan = 0;

            Dictionary<DateOnly, long> timePerDay = new Dictionary<DateOnly, long>();

            foreach (var session in sessions)
            {
                long span = session.EndTime - session.StartTime;
                totalMs += span;
                if (span > longestSpan)
                {
                    longestSpan = span;
                    longestStart = session.StartTime;
                }

                if (session.StartTime > lastStart && now - session.EndTime > 60_000)
                {
                    lastStart = session.StartTime;
                    lastSpan = span;
                }

                DateTimeOffset startTime = DateTimeOffset.FromUnixTimeMilliseconds(session.StartTime).ToLocalTime();
                DateTimeOffset endTime = DateTimeOffset.FromUnixTimeMilliseconds(session.EndTime).ToLocalTime();

                for (DateTime day = startTime.Date; day <= endTime.Date; day = day.AddDays(1))
                {
                    DateTimeOffset dayStart = day == startTime.Date ? startTime : day;
                    DateTimeOffset dayEnd = day == endTime.Date ? endTime : day.AddDays(1).AddTicks(-1);
                    var duration = (long)(dayEnd - dayStart).TotalMilliseconds;
                    var dateOnly = DateOnly.FromDateTime(day);
                    if (timePerDay.ContainsKey(dateOnly))
                    {
                        timePerDay[dateOnly] += duration;
                    }
                    else
                    {
                        timePerDay[dateOnly] = duration;
                    }
                }
            }

            _playTimePerDay = timePerDay;

            string totalTimeText = TimeSpanToString(TimeSpan.FromMilliseconds(totalMs));
            string startUpCountText = totalMs > 0 ? string.Format(Lang.Instance.PlayTimeStatsDialog_Started0Times, sessions.Count) : "";
            DatabaseService.SetValue($"playtime_total_{biz}", TimeSpan.FromMilliseconds(totalMs));

            string averageDayTimeText = timePerDay.Count > 0 ? TimeSpanToString(TimeSpan.FromMilliseconds(totalMs / timePerDay.Count)) : "-";
            string playDaysText = timePerDay.Count > 0 ? string.Format(Lang.Instance.PlayTimeStatsDialog_PlayedFor0Days, timePerDay.Count) : "";

            int longestContinuousDays = 0;
            DateOnly? longestContinuousStart = null;
            DateOnly? longestContinuousEnd = null;
            var orderedDays = timePerDay.Keys.OrderBy(d => d).ToList();

            DateOnly? currentStart = null;
            DateOnly? previousDay = null;
            int currentStreak = 0;

            foreach (var orderedDay in orderedDays)
            {
                if (previousDay.HasValue && orderedDay == previousDay.Value.AddDays(1))
                {

                    currentStreak++;
                }
                else
                {

                    currentStart = orderedDay;
                    currentStreak = 1;
                }

                if (currentStreak > longestContinuousDays)
                {
                    longestContinuousDays = currentStreak;
                    longestContinuousStart = currentStart;
                    longestContinuousEnd = orderedDay;
                }

                previousDay = orderedDay;
            }

            string longestContinuousDaysText = longestContinuousDays > 0
                ? $"{longestContinuousStart:yyyy/MM/dd} - {longestContinuousEnd:yyyy/MM/dd}"
                : "";

            var maxDay = timePerDay.MaxBy(kv => kv.Value);
            long maxDayMs = maxDay.Value;

            string maxDayPlayTimeText = maxDayMs > 0 ? TimeSpanToString(TimeSpan.FromMilliseconds(maxDayMs)) : "-";
            string maxDayPlayDateText = maxDayMs > 0 ? maxDay.Key.ToString("yyyy-MM-dd") : "";

            string longestRunTimeText = TimeSpanToString(TimeSpan.FromMilliseconds(longestSpan));
            string longestRunStartText = longestSpan > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(longestStart).LocalDateTime.ToString("yyyy-MM-dd") : "";
            string lastPlayDurationText;
            string lastPlayTimeText;
            if (lastStart > 0)
            {
                lastPlayDurationText = TimeSpanToString(TimeSpan.FromMilliseconds(Math.Max(lastSpan, 60_000)));
                lastPlayTimeText = DateTimeOffset.FromUnixTimeMilliseconds(lastStart).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                lastPlayDurationText = "-";
                lastPlayTimeText = "";
            }

            StatCards =
            [
                new StatCardItem { Title = Lang.Instance.PlayTimeStatsDialog_TotalPlaytime, Value = totalTimeText, SubText = startUpCountText },
                new StatCardItem { Title = Lang.Instance.PlayTimeStatsDialog_AverageDailyPlaytime, Value = averageDayTimeText, SubText = playDaysText },
                new StatCardItem { Title = Lang.Instance.PlayTimeStatsDialog_LongestStreak, Value = string.Format(Lang.Instance.PlayTimeStatsDialog_0Days, longestContinuousDays), SubText = longestContinuousDaysText },
                new StatCardItem { Title = Lang.Instance.PlayTimeStatsDialog_LongestSession, Value = longestRunTimeText, SubText = longestRunStartText },
                new StatCardItem { Title = Lang.Instance.PlayTimeStatsDialog_LongestDailyPlaytime, Value = maxDayPlayTimeText, SubText = maxDayPlayDateText },
                new StatCardItem { Title = Lang.Instance.PlayTimeButton_LastStartup, Value = lastPlayDurationText, SubText = lastPlayTimeText },
            ];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load play time stats: GameBiz {biz}", biz);
        }

        try
        {
            BuildBarChart();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build bar chart: GameBiz {biz}, range {range}", CurrentGameBiz, Segmented_BarRange.SelectedIndex);
        }

        try
        {
            BuildHeatmap();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build heatmap: GameBiz {biz}", CurrentGameBiz);
        }


        if (Lang.Instance.PlayTimeStatsDialog_AverageDailyPlaytime.Length > 22)
        {
            UniformGridLayout_StatCards.MinItemHeight = 98;
            Grid_BarChartSwitcher.Margin = new Thickness(4, 16, 4, 0);
            PlayTimeHeatmap.Margin = new Thickness(0, 20, 0, 0);
        }

    }

    private void SetBars(List<BarChartItem> items, long total)
    {
        PlayTimeBarChart.Items = items;
        BarTotalText = TimeSpanToString(TimeSpan.FromMilliseconds(total));
    }


    private void BuildBarChart()
    {
        int range = Segmented_BarRange.SelectedIndex;
        var today = DateTime.Today;
        if (range == 1)
        {

            int sinceMonday = ((int)today.DayOfWeek + 6) % 7;
            var firstMonday = today.AddDays(-sinceMonday - 11 * 7);
            var items = new List<BarChartItem>(12);
            long total = 0;
            for (int w = 0; w < 12; w++)
            {
                var weekStart = firstMonday.AddDays(w * 7);
                var weekEnd = weekStart.AddDays(6);
                var actualLast = weekEnd <= today ? DateOnly.FromDateTime(weekEnd) : DateOnly.FromDateTime(today);
                long sum = SumDayRange(DateOnly.FromDateTime(weekStart), DateOnly.FromDateTime(weekEnd));
                total += sum;
                items.Add(new BarChartItem
                {
                    Label = weekStart.ToString("MM-dd"),
                    Value = sum / 60_000.0,
                    Tooltip = $"{weekStart:MM/dd} - {actualLast:MM/dd}\n{TimeSpanToString(TimeSpan.FromMilliseconds(sum))}",
                });
            }
            SetBars(items, total);
        }
        else if (range == 2)
        {

            var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
            var items = new List<BarChartItem>(12);
            long total = 0;
            for (int m = 0; m < 12; m++)
            {
                var monthStart = firstMonth.AddMonths(m);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                long sum = SumDayRange(DateOnly.FromDateTime(monthStart), DateOnly.FromDateTime(monthEnd));
                total += sum;
                items.Add(new BarChartItem
                {
                    Label = monthStart.ToString("MMM", CultureInfo.CurrentUICulture),
                    Value = sum / 60_000.0,
                    Tooltip = $"{monthStart:yyyy-MM}\n{TimeSpanToString(TimeSpan.FromMilliseconds(sum))}",
                });
            }
            SetBars(items, total);
        }
        else
        {
            var firstDay = today.AddDays(-14);
            var items = new List<BarChartItem>(15);
            long total = 0;
            for (int i = 0; i < 15; i++)
            {
                var d = firstDay.AddDays(i);
                long ms = _playTimePerDay.GetValueOrDefault(DateOnly.FromDateTime(d));
                total += ms;
                items.Add(new BarChartItem
                {
                    Label = d.ToString("MM-dd"),
                    Value = Math.Max(0, ms / 60_000.0),
                    Tooltip = $"{d:yyyy-MM-dd}\n{TimeSpanToString(TimeSpan.FromMilliseconds(ms))}",
                });
            }
            SetBars(items, total);
        }
    }


    private void Segmented_BarRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_playTimeLoaded)
        {
            return;
        }
        try
        {
            BuildBarChart();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rebuild bar chart: GameBiz {biz}", CurrentGameBiz);
        }
    }



    private void BuildHeatmap()
    {

        var today = DateTime.Today;

        int sinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var firstDay = today.AddDays(-sinceMonday - 51 * 7);
        int totalDays = (today - firstDay).Days + 1;
        var items = new List<HeatmapDayItem>(totalDays);
        long total = 0;
        for (int i = 0; i < totalDays; i++)
        {
            var d = firstDay.AddDays(i);
            long ms = _playTimePerDay.GetValueOrDefault(DateOnly.FromDateTime(d));
            total += ms;
            items.Add(new HeatmapDayItem
            {
                Date = DateOnly.FromDateTime(d),
                Value = Math.Max(0, ms / 60_000.0),
                Tooltip = $"{d:yyyy-MM-dd}\n{TimeSpanToString(TimeSpan.FromMilliseconds(ms))}",
            });
        }
        PlayTimeHeatmap.Days = items;
    }

    private long SumDayRange(DateOnly start, DateOnly end)
    {
        long sum = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            sum += _playTimePerDay.GetValueOrDefault(d);
        }
        return sum;
    }


    public static string TimeSpanToString(TimeSpan timeSpan)
    {
        int totalMinutes = (int)Math.Round(timeSpan.TotalMinutes);
        if (totalMinutes < 1)
        {
            return "0m";
        }
        if (totalMinutes < 60)
        {
            return $"{totalMinutes}m";
        }
        int hours = totalMinutes / 60, minutes = totalMinutes % 60;
        return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
    }


}
