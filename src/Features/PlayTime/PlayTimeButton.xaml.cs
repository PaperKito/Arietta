using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Features.Database;
using System;
using System.Threading.Tasks;

namespace Arietta.Features.PlayTime;








[ObservableObject]
public sealed partial class PlayTimeButton : UserControl
{
    private readonly ILogger<PlayTimeButton> _logger = AppConfig.GetLogger<PlayTimeButton>();

    public PlayTimeButton()
    {
        this.InitializeComponent();
    }

    public GameBiz CurrentGameBiz { get; set; }

    public TimeSpan PlayTimeTotal { get; set => SetProperty(ref field, value); }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }

    public void Reload() => InitializePlayTime();

    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = DatabaseService.GetValue<TimeSpan>($"playtime_total_{CurrentGameBiz}", out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize play time");
        }
    }

    public static string TimeSpanToString(TimeSpan timeSpan)
    {
        int totalMinutes = (int)Math.Round(timeSpan.TotalMinutes);
        int hours = totalMinutes / 60, minutes = totalMinutes % 60;
        return $"{hours}h {minutes}m";
    }

    private async Task OpenStatsDialogAsync()
    {
        await new PlayTimeStatsDialog
        {
            CurrentGameBiz = CurrentGameBiz,
            XamlRoot = this.XamlRoot,
        }.ShowAsync();
        InitializePlayTime();
    }

    private async void Button_OpenStats_Click(object sender, RoutedEventArgs e)
    {
        await OpenStatsDialogAsync();
    }
}
