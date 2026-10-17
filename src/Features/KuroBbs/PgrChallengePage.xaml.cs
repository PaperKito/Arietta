using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Arietta.Features.KuroBbs;

public sealed partial class PgrChallengePage : PageBase
{

    private string _mode = "cage";

    private string _moduleTitle = "";


    public PgrChallengePage()
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
                var t when t == Lang.Instance.KuroBbs_PgrCage => "cage",
                var t when t == Lang.Instance.KuroBbs_PgrWarArea => "wararea",
                var t when t == Lang.Instance.KuroBbs_PgrTransfinite => "transfinite",
                _ => "cage",
            };
            _moduleTitle = feature.Title;
        }
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
        DispatcherQueue.TryEnqueue(async () => await LoadAsync());
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();


    private Task LoadAsync() => KuroBbsUi.RunPgrLoadAsync(
        InfoBar_NoAccount,
        InfoBar_Error,
        ProgressRing_Loading,
        foldInfoBar: true,
        showRing: true,
        collapseRing: true,
        beforeLoad: null,
        async account =>
        {
            switch (_mode)
            {
                case "wararea":
                    await FillWarAreaAsync(account);
                    break;
                case "transfinite":
                    await FillTransfiniteAsync(account);
                    break;
                default:
                    await FillCageAsync(account);
                    break;
            }
        },
        _moduleTitle);


    private void SetSubtitle(string text)
    {
        TextBlock_Subtitle.Text = text;
        TextBlock_Subtitle.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }


    #region 幻痛囚笼

    private async Task FillCageAsync(KuroBbsAccount account)
    {
        var result = await KuroBbsClient.Instance.GetPgrCageDataAsync(account);
        ClearBody();
        if (result is not { IsSuccess: true, Data: { } data })
        {
            HandleError(result.Code, result.Msg);
            return;
        }
        if (!data.IsUnlock)
        {
            AddHint(Lang.Instance.PgrChallenge_Locked);
            return;
        }
        if (!data.IsOpen)
        {
            AddHint(Lang.Instance.PgrChallenge_NotOpen);
            return;
        }
        if (!data.ChooseArea || string.IsNullOrEmpty(data.ChallengeArea))
        {
            AddHint(Lang.Instance.PgrChallenge_NoArea);
            return;
        }

        if (data.PrisonerCage is null && data.FightHardZone is null)
        {
            AddHint(Lang.Instance.PgrChallenge_NoRecord);
            return;
        }
        if (data.PrisonerCage is { } cage)
        {
            var header = BuildCageHeader(cage, data.ChallengeArea, data.ChallengeLevel);
            StackPanel_Body.Children.Add(header);
            foreach (var boss in cage.BossFightInfoList ?? [])
            {
                StackPanel_Body.Children.Add(BuildCageBossCard(boss));
            }
        }
        if (data.FightHardZone is { } hard)
        {
            AddSection("鏖战点", hard);
        }
    }

    private static string? CageAreaIconUrl(string? area) => area?.Trim() switch
    {
        "初级区" => "https://web-static.kurobbs.com/pns-box/assets/area-level1-2d36089f.png",
        "中级区" => "https://web-static.kurobbs.com/pns-box/assets/area-level2-d14dcf8a.png",
        "高级区" => "https://web-static.kurobbs.com/pns-box/assets/area-level3-dd7c3e33.png",
        "终极区" => "https://web-static.kurobbs.com/pns-box/assets/area-level4-4f57106c.png",
        _ => null,
    };

    private FrameworkElement BuildCageHeader(KuroPgrCageInfo cage, string? area, string? level)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };

        var medal = new Image { Width = 34, Height = 45, VerticalAlignment = VerticalAlignment.Center };
        try
        {
            if (CageAreaIconUrl(area) is { } medalUrl)
            {
                medal.Source = new BitmapImage(new Uri(medalUrl));
            }
        }
        catch { }
        left.Children.Add(medal);
        var nameStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(new TextBlock
        {
            Text = area ?? "",
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });
        if (!string.IsNullOrEmpty(level))
        {
            nameStack.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.PgrCage_Level, level),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        left.Children.Add(nameStack);

        var right = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        right.Children.Add(MakeStatLine(Lang.Instance.PgrCage_TotalPoint, string.Format("{0:N0}", cage.TotalPoint), 16));
        right.Children.Add(MakeStatLine(Lang.Instance.PgrWarArea_TotalTimesLabel, string.Format("{0:N0}", cage.TotalChallengeTimes), 16));

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);

        return KuroBbsUi.KuroCard(grid, padding: new Thickness(18, 14, 18, 14));
    }

    private static StackPanel MakeStatLine(string label, string value, double valueFontSize, bool centerPanel = false, double spacing = 8, double labelFontSize = 13)
    {
        var line = centerPanel
            ? new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center }
            : new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        line.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = labelFontSize,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        line.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = valueFontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
        });
        return line;
    }

    private static StackPanel MakeStatBlock(string label, string value, double valueFontSize,
        double labelFontSize = 12, HorizontalAlignment? panelHAlign = null, string? suffix = null)
    {
        var block = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        if (panelHAlign is { } hAlign)
        {
            block.HorizontalAlignment = hAlign;
        }
        block.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = labelFontSize,
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        if (suffix is null)
        {
            block.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = valueFontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
            });
        }
        else
        {
            var valueRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            valueRow.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = valueFontSize,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
            });
            valueRow.Children.Add(new TextBlock
            {
                Text = suffix,
                FontSize = 13,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            block.Children.Add(valueRow);
        }
        return block;
    }

    private FrameworkElement BuildCageBossCard(KuroPgrCageBossFight boss)
    {

        var artBrush = new ImageBrush { Stretch = Stretch.UniformToFill };
        try
        {
            if (!string.IsNullOrEmpty(boss.Boss?.IconUrl))
            {
                artBrush.ImageSource = new BitmapImage
                {
                    DecodePixelWidth = 240,
                    UriSource = new Uri(boss.Boss.IconUrl!),
                };
            }
        }
        catch { }

        var info = new Grid { Padding = new Thickness(14, 10, 16, 10) };
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        info.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameStack = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(new TextBlock
        {
            Text = boss.Boss?.Name ?? "",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });

        var weakness = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        foreach (var w in boss.Boss?.WeaknessList ?? [])
        {
            if (string.IsNullOrEmpty(w.Name))
            {
                continue;
            }
            var chip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            chip.Children.Add(CreateIcon(w.Icon, 14));
            chip.Children.Add(new TextBlock
            {
                Text = w.Name!,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            weakness.Children.Add(chip);
        }
        nameStack.Children.Add(weakness);
        Grid.SetColumn(nameStack, 0);
        info.Children.Add(nameStack);

        var rightStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        rightStack.Children.Add(MakeStatBlock(Lang.Instance.PgrCage_TotalPoint, boss.TotalPoint > 0 ? string.Format("{0:N0}", boss.TotalPoint) : "-", 17));

        var chevron = new FontIcon
        {
            Glyph = "\uE70E",
            FontSize = 12,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
            Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
        };
        var toggle = new Button
        {
            Content = chevron,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 4, 6, 4),
            VerticalAlignment = VerticalAlignment.Center,
        };
        rightStack.Children.Add(toggle);
        Grid.SetColumn(rightStack, 1);
        info.Children.Add(rightStack);

        var bodyGrid = new Grid();
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var art = new Border
        {
            Background = artBrush,
            Width = 110,
            CornerRadius = new CornerRadius(8, 0, 0, 8),
        };
        Grid.SetColumn(art, 0);
        Grid.SetRowSpan(art, 2);
        bodyGrid.Children.Add(art);
        Grid.SetColumn(info, 1);
        bodyGrid.Children.Add(info);

        var root = new Grid();
        root.Children.Add(bodyGrid);

        var stages = boss.StageInfoList ?? [];
        if (stages.Count > 0)
        {

            var stageList = new StackPanel { Spacing = 8, Margin = new Thickness(0, 0, 0, 12), Visibility = Visibility.Collapsed };
            foreach (var stage in stages)
            {
                stageList.Children.Add(BuildCageStageRow(stage, badgeScale: 0.85));
            }
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(stageList, 1);
            Grid.SetColumn(stageList, 1);
            root.Children.Add(stageList);

            toggle.Click += (_, _) =>
            {
                bool expand = stageList.Visibility == Visibility.Collapsed;
                stageList.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
                chevron.Glyph = expand ? "\uE70D" : "\uE70E";
            };
        }
        else
        {
            var hint = new TextBlock
            {
                Text = Lang.Instance.PgrChallenge_NoRecord,
                FontSize = 12,
                Opacity = 0.6,
                Margin = new Thickness(0, 0, 0, 12),
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(hint, 1);
            Grid.SetColumn(hint, 1);
            root.Children.Add(hint);
        }

        return KuroBbsUi.KuroCard(root);
    }

    private FrameworkElement BuildCageStageRow(KuroPgrCageStage stage, double badgeScale = 1.0)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };

        var avatars = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        foreach (var body in stage.BodyList ?? [])
        {
            avatars.Children.Add(BuildWarAreaAvatar(body, 72, badgeScale));
        }
        left.Children.Add(avatars);
        if (avatars.Children.Count > 0)
        {
            left.Children.Add(KuroBbsUi.MakeDivider("DividerStrokeColorDefaultBrush", 44, new Thickness(2, 0, 2, 0), width: 1, verticalCenter: true));
        }

        var nameStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(new TextBlock { Text = stage.StageName ?? "", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        if (stage.FightTime > 0)
        {
            nameStack.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.PgrCage_FightTime, stage.FightTime),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        left.Children.Add(nameStack);

        var pointStack = MakeStatBlock(Lang.Instance.PgrCage_Point, string.Format("{0:N0}", stage.Point), 16, panelHAlign: HorizontalAlignment.Right);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(pointStack, 1);
        grid.Children.Add(left);
        grid.Children.Add(pointStack);

        return KuroBbsUi.KuroCard(grid, padding: new Thickness(14, 10, 14, 10), secondaryBackground: true);
    }

    #endregion


    #region 纷争战区

    private async Task FillWarAreaAsync(KuroBbsAccount account)
    {
        var result = await KuroBbsClient.Instance.GetPgrWarAreaDataAsync(account);
        ClearBody();
        if (result is not { IsSuccess: true, Data: { } data })
        {
            HandleError(result.Code, result.Msg);
            return;
        }
        if (!data.IsUnlock)
        {
            AddHint(Lang.Instance.PgrChallenge_Locked);
            return;
        }
        if (!data.IsOpen)
        {
            AddHint(Lang.Instance.PgrChallenge_NotOpen);
            return;
        }

        SetSubtitle("");
        StackPanel_Body.Children.Add(BuildWarAreaHeader(data));

        var stages = data.AreaInfo?.StageFightInfoList ?? [];
        if (stages.Count == 0)
        {
            AddHint(Lang.Instance.PgrChallenge_NoRecord);
            return;
        }
        foreach (var stage in stages)
        {
            StackPanel_Body.Children.Add(BuildWarAreaStageCard(stage));
        }
    }

    private static readonly Dictionary<string, string[]> WarAreaElementKeys = new()
    {
        ["Lightning"] = ["雷", "镭", "电"],
        ["Fire"] = ["火", "焰", "燃"],
        ["Frost"] = ["冰", "寒", "霜", "冻"],
        ["Glow"] = ["光", "熠"],
        ["DimDeriv"] = ["暗", "黯", "蚀"],
        ["TrueSlash"] = ["斩"],
        ["Clutter"] = ["紊"],
    };


    private static string? MatchWarAreaElementKey(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }
        foreach (var pair in WarAreaElementKeys)
        {
            if (pair.Value.Any(name.Contains))
            {
                return pair.Key;
            }
        }
        return null;
    }

    private FrameworkElement BuildWarAreaHeader(KuroPgrWarAreaData data)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, VerticalAlignment = VerticalAlignment.Center };
        try
        {
            left.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri("ms-appx:///Assets/Image/Combat/PgrWarArea_TeamIcon.png")),
                Width = 52,
                Height = 36,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        catch { }
        var nameStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(new TextBlock
        {
            Text = data.GroupName ?? "",
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });
        if (!string.IsNullOrEmpty(data.GroupLevel))
        {
            nameStack.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.PgrChallenge_Level, data.GroupLevel),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        left.Children.Add(nameStack);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 28, VerticalAlignment = VerticalAlignment.Center };
        if (data.AreaInfo?.TotalPoint is > 0)
        {
            right.Children.Add(MakeStatLine(Lang.Instance.PgrWarArea_TotalPointLabel, string.Format("{0:N0}", data.AreaInfo.TotalPoint), 18, centerPanel: true));
        }
        if (data.AreaInfo?.TotalChallengeTimes is > 0)
        {
            right.Children.Add(MakeStatLine(Lang.Instance.PgrWarArea_TotalTimesLabel, string.Format("{0:N0}", data.AreaInfo.TotalChallengeTimes), 18, centerPanel: true));
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);

        return KuroBbsUi.KuroCard(grid, padding: new Thickness(18, 14, 18, 14));
    }

    private FrameworkElement BuildWarAreaStageCard(KuroPgrStageFightInfo stage)
    {
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };

        var elementKey = MatchWarAreaElementKey(stage.StageName);
        if (elementKey is not null)
        {
            head.Children.Add(KuroBbsUi.MakeThemeIcon(this, $"Element_{elementKey}", 26, centered: true));
        }
        else
        {
            head.Children.Add(CreateIcon(stage.StageIconUrl, 26));
        }

        var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(new TextBlock
        {
            Text = stage.StageName ?? $"#{stage.StageId}",
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (!string.IsNullOrEmpty(stage.Description))
        {
            title.Children.Add(new TextBlock
            {
                Text = stage.Description!,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        head.Children.Add(title);

        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(head, 0);
        headGrid.Children.Add(head);

        if (stage.Point is > 0)
        {
            var score = MakeStatLine(Lang.Instance.PgrWarArea_TotalPointLabel, string.Format("{0:N0}", stage.Point), 15, centerPanel: true, spacing: 6, labelFontSize: 12);
            Grid.SetColumn(score, 1);
            headGrid.Children.Add(score);
        }

        var body = new StackPanel { Spacing = 10 };
        var buffRows = 0;
        foreach (var buff in stage.AreaBuffFightInfoList ?? [])
        {
            buffRows++;
            body.Children.Add(BuildWarAreaRow(stage, buff));
        }
        if (buffRows == 0)
        {
            var empty = new TextBlock
            {
                Text = Lang.Instance.PgrChallenge_NoRecord,
                FontSize = 12,
                Opacity = 0.6,
            };
            body.Children.Add(empty);
        }

        var root = new StackPanel { Spacing = 10 };
        root.Children.Add(headGrid);
        root.Children.Add(body);

        return KuroBbsUi.KuroCard(root, padding: new Thickness(16, 12, 16, 14));
    }

    private FrameworkElement BuildWarAreaRow(KuroPgrStageFightInfo stage, KuroPgrBuffFightInfo buff)
    {
        var avatars = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        foreach (var body in buff.BodyList ?? [])
        {
            avatars.Children.Add(BuildWarAreaAvatar(body, 84));
        }
        var buffStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };

        var elementKey = MatchWarAreaElementKey(buff.BuffName) ?? MatchWarAreaElementKey(stage.StageName);
        FrameworkElement buffIcon;
        if (elementKey is not null)
        {
            buffIcon = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(10),
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                Child = KuroBbsUi.MakeThemeIcon(this, $"Element_{elementKey}", 38, centered: true),
            };
        }
        else
        {
            buffIcon = CreateIcon(buff.BuffIconUrl, 48);
        }
        buffStack.Children.Add(buffIcon);

        var nameStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };

        var supportNames = (buff.SupportBuffList ?? [])
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList();
        nameStack.Children.Add(new TextBlock
        {
            Text = supportNames.Count > 0 ? string.Join(" · ", supportNames) : buff.BuffName ?? "",
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });
        buffStack.Children.Add(nameStack);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };

        var waves = buff.NpcGroup ?? stage.NpcGroup;
        if (waves is > 0)
        {
            right.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.PgrWarArea_Waves, waves),
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(avatars, 0);
        Grid.SetColumn(buffStack, 2);
        Grid.SetColumn(right, 2);
        grid.Children.Add(avatars);

        if (avatars.Children.Count > 0)
        {
            var divider = KuroBbsUi.MakeDivider("DividerStrokeColorDefaultBrush", 44, new Thickness(14, 0, 14, 0), width: 1, verticalCenter: true);
            Grid.SetColumn(divider, 1);
            grid.Children.Add(divider);
            Grid.SetColumn(buffStack, 2);
        }
        grid.Children.Add(buffStack);
        grid.Children.Add(right);

        return grid;
    }

    private FrameworkElement BuildWarAreaAvatar(KuroPgrWarAreaBodyEntry body, double size, double badgeScale = 1.0)
    {

        var brush = new ImageBrush { Stretch = Stretch.UniformToFill };
        try
        {
            if (!string.IsNullOrEmpty(body.IconUrl))
            {
                brush.ImageSource = new BitmapImage
                {
                    DecodePixelWidth = (int)Math.Clamp(size * 3, 96, 512),
                    UriSource = new Uri(body.IconUrl!),
                };
            }
        }
        catch { }
        var radius = size * 0.14;
        var tile = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(radius),
            Background = brush,
        };

        var root = new Grid { Width = size, Height = size };
        root.Children.Add(tile);

        var grade = body.Grade?.Trim();
        if (!string.IsNullOrEmpty(grade))
        {

            root.Children.Add(KuroBbsUi.MakeCornerBadge(
                grade,
                11 * badgeScale,
                new Thickness(6 * badgeScale, 1 * badgeScale, 6 * badgeScale, 2 * badgeScale),
                new CornerRadius(0, radius, 0, radius),
                italic: true,
                foreground: (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"]));
        }

        return root;
    }


    #endregion


    #region 历战映射

    private async Task FillTransfiniteAsync(KuroBbsAccount account)
    {
        var result = await KuroBbsClient.Instance.GetPgrTransfiniteDataAsync(account);
        ClearBody();
        if (result is not { IsSuccess: true, Data: { } data })
        {
            HandleError(result.Code, result.Msg);
            return;
        }
        if (!data.IsUnlock)
        {
            AddHint(Lang.Instance.PgrChallenge_Locked);
            return;
        }
        if (!data.IsOpen)
        {
            AddHint(Lang.Instance.PgrChallenge_NotOpen);
            return;
        }

        SetSubtitle("");

        StackPanel_Body.Children.Add(BuildTransfiniteHeader(data));

        var characters = data.CharacterList ?? [];
        if (characters.Count == 0)
        {
            AddHint(Lang.Instance.PgrChallenge_NoRecord);
            return;
        }
        StackPanel_Body.Children.Add(BuildTransfiniteTeam(characters));
    }

    private FrameworkElement BuildTransfiniteHeader(KuroPgrTransfiniteData data)
    {
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        try
        {
            if (!string.IsNullOrEmpty(data.BossIconUrl))
            {
                left.Children.Add(CreateIcon(data.BossIconUrl, 52));
            }
        }
        catch { }
        var nameStack = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        nameStack.Children.Add(new TextBlock
        {
            Text = data.ChallengeArea ?? "",
            FontSize = 17,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
        });
        if (!string.IsNullOrEmpty(data.ChallengeLevel))
        {
            nameStack.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.Instance.PgrCage_Level, data.ChallengeLevel),
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
        }
        left.Children.Add(nameStack);

        var right = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
        var pair = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        pair.Children.Add(MakeStatLine(Lang.Instance.PgrTransfinite_MaxProgress, string.Format("{0} / 14", data.Process), 16));
        if (data.FightTime > 0)
        {
            var ts = TimeSpan.FromSeconds(data.FightTime);
            var text = ts.TotalHours >= 1
                ? string.Format("{0:d1\\.hh\\:mm\\:ss}", ts)
                : string.Format("{0:m\\:ss}", ts);
            var timeLine = MakeStatLine(Lang.Instance.PgrTransfinite_TimeCost, text, 16);

            ((TextBlock)timeLine.Children[1]).Foreground =
                (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
            pair.Children.Add(timeLine);
        }
        right.Children.Add(pair);

        right.Children.Add(MakeStatBlock(Lang.Instance.PgrTransfinite_OperatorLabel, string.Format("{0:N0}", data.OperatorCount), 18, labelFontSize: 13, suffix: " / 1500"));

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);

        return KuroBbsUi.KuroCard(grid, padding: new Thickness(18, 14, 18, 14));
    }

    private FrameworkElement BuildTransfiniteTeam(List<KuroPgrCharacter> characters)
    {
        var root = new StackPanel { Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
        root.Children.Add(new TextBlock
        {
            Text = Lang.Instance.PgrTransfinite_TeamTitle,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });

        var team = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        foreach (var c in characters)
        {
            const double size = 132;

            var brush = new ImageBrush { Stretch = Stretch.UniformToFill };
            try
            {
                if (!string.IsNullOrEmpty(c.IconUrl))
                {
                    brush.ImageSource = new BitmapImage
                    {
                        DecodePixelWidth = (int)Math.Clamp(size * 2, 96, 512),
                        UriSource = new Uri(c.IconUrl!),
                    };
                }
            }
            catch { }
            var radius = size * 0.14;

            var cell = new Grid { Width = size, Height = size };
            cell.Children.Add(new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(radius),
                Background = brush,
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
            });

            if (!string.IsNullOrEmpty(c.Grade))
            {

                cell.Children.Add(KuroBbsUi.MakeCornerBadge(
                    c.Grade!,
                    13,
                    new Thickness(8, 2, 8, 4),
                    new CornerRadius(0, radius, 0, radius),
                    italic: true,
                    foreground: (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"]));
            }

            team.Children.Add(cell);
        }
        root.Children.Add(team);
        return root;
    }

    #endregion




    #region 通用渲染

    private void ClearBody()
    {
        StackPanel_Body.Children.Clear();
    }


    private void AddHint(string text)
    {
        StackPanel_Body.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Opacity = 0.6,
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private void AddSection(string title, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        var card = CreateCard(title, "", null);
        var panel = (StackPanel)card.Tag;
        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                continue;
            }
            var row = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var name = new TextBlock
            {
                Text = prop.Name,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            };
            var value = new TextBlock { Text = JsonText(prop.Value), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(value, 1);
            row.Children.Add(name);
            row.Children.Add(value);
            panel.Children.Add(row);
        }
    }


    private static string JsonText(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? "",
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "是",
            JsonValueKind.False => "否",
            _ => "",
        };
    }


    private Border CreateCard(string title, string summary, string? iconUrl)
    {
        var content = new StackPanel { Spacing = 4 };
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        if (!string.IsNullOrEmpty(iconUrl))
        {
            header.Children.Add(CreateIcon(iconUrl, 28));
        }
        var textStack = new StackPanel { Spacing = 1 };
        textStack.Children.Add(new TextBlock { Text = title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
        if (!string.IsNullOrEmpty(summary))
        {
            textStack.Children.Add(new TextBlock
            {
                Text = summary,
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
            });
        }
        header.Children.Add(textStack);

        var root = new StackPanel { Spacing = 8 };
        root.Children.Add(header);
        root.Children.Add(content);
        var border = KuroBbsUi.KuroCard(root, padding: new Thickness(16));
        border.Tag = content;
        StackPanel_Body.Children.Add(border);
        return border;
    }


    private static FrameworkElement CreateIcon(string? url, double size)
    {
        try
        {
            if (!string.IsNullOrEmpty(url))
            {
                return new Border
                {
                    Width = size,
                    Height = size,
                    CornerRadius = new CornerRadius(size / 4),
                    Child = new Image { Source = new BitmapImage(new Uri(url)), Stretch = Stretch.UniformToFill },
                };
            }
        }
        catch { }
        return new Grid { Width = size, Height = size };
    }


    private void HandleError(int code, string? msg)
    {
        KuroBbsUi.HandleKuroError(code, msg, _moduleTitle, Lang.Instance.PgrCommon_LoadFailed);
    }

    #endregion
}
