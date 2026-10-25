using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

public static class DailyBriefingPanel
{

    private const string BundledIconWuwa = "ms-appx:///Assets/Image/Common/1789406285416.png";

    private const string BundledIconPgr = "ms-appx:///Assets/Image/Common/actionIcon.png";

    private const string BattlePassIconUrl = "https://web-static.kurobbs.com/gamerdata/widget/game3/battlePass.png";

    private const string IconWuwaEnergy = "https://web-static.kurobbs.com/gamerdata/widget/game3/energy.png";

    private const string IconWuwaLiveness = "https://web-static.kurobbs.com/gamerdata/widget/game3/liveness.png";

    private const string IconWuwaStoreEnergy = "https://web-static.kurobbs.com/gamerdata/widget/game3/storeEnergy.png";

    private const string IconPgrActionPoint = "https://web-static.kurobbs.com/gamerdata/widget/game2/actionIcon.png";

    private const string IconPgrActive = "https://web-static.kurobbs.com/gamerdata/widget/game2/activeIcon.png";

    private const string IconPgrBoss = "https://web-static.kurobbs.com/gamerdata/widget/game2/bossIcon.png";

    private const string IconPgrArena = "https://web-static.kurobbs.com/gamerdata/widget/game2/arenaIcon.png";

    public static ImageSource IconSource(GameBiz biz)
    {
        bool pgr = biz.IsPunishingGrayRaven();
        return new BitmapImage(new Uri(pgr ? BundledIconPgr : BundledIconWuwa));
    }

    private static readonly Dictionary<string, DailyBriefingData> _cache = new();

    private static string CacheKey(GameBiz biz, KrSdkAccount? account)
        => $"{(int)biz}|{account?.Cuid ?? ""}";

    public static DailyBriefingData? GetCached(GameBiz biz, KrSdkAccount? account)
        => _cache.TryGetValue(CacheKey(biz, account), out var data) ? data : null;

    public static async Task PrefetchAsync(GameBiz biz, KrSdkAccount? account)
    {
        try
        {
            _cache[CacheKey(biz, account)] = await FetchAsync(biz, account);
        }
        catch
        {

        }
    }

    public static async Task<DailyBriefingData> FetchAsync(GameBiz biz, KrSdkAccount? gameAccount)
    {
        var result = new DailyBriefingData();
        string? oauthCode = KrSdkAccountService.GetOauthCode(biz, gameAccount);
        if (oauthCode is null)
        {

            result.NoGameAccount = true;
            return result;
        }
        string playerId = gameAccount?.RoleId ?? "";
        string region = gameAccount?.ServerName ?? "";
        if (playerId.Length == 0 || region.Length == 0)
        {

            result.NoRole = true;
            return result;
        }
        var briefing = await KuroSdkPlayerClient.QueryBriefingAsync(
            KrSdkAccountService.GetApiBase(biz), oauthCode, playerId, region);
        if (briefing is null)
        {
            result.Message = Lang.Instance.DailyBriefing_Error;
            return result;
        }
        result.IsSuccess = true;
        result.RoleName = briefing.RoleName.Length > 0 ? briefing.RoleName : gameAccount?.DisplayName;
        result.ServerName = region;
        result.Level = briefing.Level;
        result.Items.AddRange(biz.IsPunishingGrayRaven() ? PgrItems(briefing) : WuwaItems(briefing));
        return result;
    }

    private static IEnumerable<BriefingItem> WuwaItems(KuroSdkBriefing data)
    {
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        yield return new BriefingItem
        {
            Title = Lang.Instance.SignPage_Energy,
            ValueText = $"{data.Energy}/{data.MaxEnergy}",

            StatusText = FullInText((data.EnergyRecoverMs - nowMs) / 1000),
            IconUrl = IconWuwaEnergy,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.Briefing_Liveness,
            ValueText = $"{data.Liveness}/{data.LivenessMax}",
            IconUrl = IconWuwaLiveness,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.Briefing_StoreEnergy,
            ValueText = $"{data.StoreEnergy}/{data.MaxStoreEnergy}",
            IconUrl = IconWuwaStoreEnergy,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.Briefing_BattlePass,
            ValueText = $"{data.BattlePassWeekExp}/{data.BattlePassWeekMaxExp}",
            StatusText = data.BattlePassOpen ? $"{Lang.Instance.Briefing_Level}.{data.BattlePassLevel}" : Lang.Instance.PgrData_Locked,
            IconUrl = BattlePassIconUrl,
        };
    }

    private static IEnumerable<BriefingItem> PgrItems(KuroSdkBriefing data)
    {
        long nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        yield return new BriefingItem
        {
            Title = Lang.Instance.Briefing_ActionPoint,
            ValueText = $"{data.ActionPoint}/{data.ActionPointTotal}",
            StatusText = FullInText(data.ActionPointFullSeconds - nowSeconds),
            IconUrl = IconPgrActionPoint,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.Briefing_Liveness,
            ValueText = $"{data.Activeness}/{data.ActivenessTotal}",
            IconUrl = IconPgrActive,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.KuroBbs_PgrCage,
            ValueText = $"{data.BossReward}/{data.BossRewardTotal}",
            StatusText = data.BossUnlocked ? "" : Lang.Instance.PgrData_Locked,
            IconUrl = IconPgrBoss,
        };
        yield return new BriefingItem
        {
            Title = Lang.Instance.KuroBbs_PgrWarArea,
            ValueText = $"{data.ArenaReward}/{data.ArenaRewardTotal}",
            StatusText = data.ArenaUnlocked ? "" : Lang.Instance.PgrData_Locked,
            IconUrl = IconPgrArena,
        };
    }

    private static string FullInText(long leftSeconds)
        => leftSeconds > 0 ? string.Format(Lang.Instance.PgrData_FullIn, FormatDuration(leftSeconds)) : "";

    public static async Task ShowAsync(FrameworkElement target, GameBiz biz, KrSdkAccount? gameAccount)
    {
        var panel = new StackPanel { MinWidth = 300, MaxWidth = 340, Spacing = 8 };
        var flyout = new Flyout { Content = panel, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };
        var body = new StackPanel { Spacing = 2 };

        panel.Children.Add(new TextBlock
        {
            Text = Lang.Instance.DailyBriefing_Desc,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        panel.Children.Add(body);

        var refresh = new Button
        {
            Content = Lang.Instance.DailyBriefing_Refresh,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 32,
        };
        panel.Children.Add(refresh);

        async Task ReloadAsync(bool showPlaceholder)
        {
            refresh.IsEnabled = false;
            if (showPlaceholder)
            {
                body.Children.Clear();
                body.Children.Add(new TextBlock { Text = "…", FontSize = 12 });
            }
            var data = await FetchAsync(biz, gameAccount);
            _cache[CacheKey(biz, gameAccount)] = data;
            body.Children.Clear();
            FillBody(body, data, out string? noDataHint);
            if (noDataHint is not null)
            {
                body.Children.Insert(0, new TextBlock
                {
                    Text = noDataHint,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                });
            }
            refresh.IsEnabled = true;
        }

        refresh.Click += async (_, _) => await ReloadAsync(showPlaceholder: false);

        FlyoutBase.SetAttachedFlyout(target, flyout);
        flyout.ShowAt(target);

        var cached = GetCached(biz, gameAccount);
        if (cached is { IsSuccess: true })
        {
            FillBody(body, cached, out _);
            _ = ReloadAsync(showPlaceholder: false);
        }
        else
        {
            await ReloadAsync(showPlaceholder: true);
        }
    }

    private static void FillBody(StackPanel body, DailyBriefingData data, out string? hint)
    {
        hint = null;
        if (data.NoGameAccount)
        {
            hint = Lang.Instance.DailyBriefing_NoGameAccount;
            return;
        }
        if (data.NoRole)
        {
            hint = Lang.Instance.DailyBriefing_NoRole;
            return;
        }
        if (!data.IsSuccess)
        {
            hint = string.IsNullOrWhiteSpace(data.Message)
                ? Lang.Instance.DailyBriefing_Error
                : $"{Lang.Instance.DailyBriefing_Error}：{data.Message}";
            return;
        }

        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        head.Children.Add(new TextBlock { Text = data.RoleName ?? "", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(data.ServerName))
        {
            head.Children.Add(new TextBlock
            {
                Text = data.ServerName,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        if (data.Level > 0)
        {
            head.Children.Add(new TextBlock
            {
                Text = $"Lv.{data.Level}",
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        body.Children.Add(head);

        foreach (var item in data.Items)
        {
            body.Children.Add(BuildRow(item));
        }
    }

    private static Grid BuildRow(BriefingItem item)
    {
        var row = new Grid { ColumnSpacing = 10, Margin = new Thickness(0, 3, 0, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (item.IconUrl is { Length: > 0 })
        {
            var image = new Image
            {
                Width = 26,
                Height = 26,
                VerticalAlignment = VerticalAlignment.Center,
            };
            try { image.Source = LocalIconSource(item.IconUrl) ?? new BitmapImage(new Uri(item.IconUrl)); } catch { }
            row.Children.Add(image);
        }
        else
        {
            row.Children.Add(new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(6),
                Background = (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
            });
        }

        var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock { Text = item.Title, FontSize = 13 });
        if (!string.IsNullOrWhiteSpace(item.StatusText))
        {
            texts.Children.Add(new TextBlock
            {
                Text = item.StatusText,
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        Grid.SetColumn(texts, 1);
        row.Children.Add(texts);

        var value = new TextBlock
        {
            Text = item.ValueText,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };
        Grid.SetColumn(value, 2);
        row.Children.Add(value);
        return row;
    }

    private static ImageSource? LocalIconSource(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }
        try
        {
            string file = System.IO.Path.GetFileName(new Uri(url).LocalPath);
            string localPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Image", "Common", file);
            if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(localPath))
            {
                return null;
            }
            return new BitmapImage(new Uri($"ms-appx:///Assets/Image/Common/{file}"));
        }
        catch
        {
            return null;
        }
    }


    private static string FormatDuration(long seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h{span.Minutes:00}m"
            : $"{span.Minutes}m{span.Seconds:00}s";
    }


}

public sealed class BriefingItem
{
    public string Title { get; init; } = "";
    public string ValueText { get; init; } = "";
    public string StatusText { get; init; } = "";
    public string? IconUrl { get; init; }
}

public sealed class DailyBriefingData
{
    public bool IsSuccess { get; set; }

    public bool NoGameAccount { get; set; }

    public bool NoRole { get; set; }

    public string? Message { get; set; }
    public string? RoleName { get; set; }
    public string? ServerName { get; set; }
    public int Level { get; set; }
    public List<BriefingItem> Items { get; } = [];
}
