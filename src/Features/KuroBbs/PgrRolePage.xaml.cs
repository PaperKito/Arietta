using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;

namespace Arietta.Features.KuroBbs;

public sealed partial class PgrRolePage : PageBase
{

    public PgrRolePage()
    {
        this.InitializeComponent();
        this.Loaded += PgrRolePage_Loaded;

        this.ActualThemeChanged += PgrRolePage_ActualThemeChanged;
    }

    private KuroPgrCharacter? _lastChar;
    private KuroPgrCharacterDetail? _lastDetail;

    private void PgrRolePage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (_lastChar is null || Grid_Detail.Visibility != Visibility.Visible)
        {
            return;
        }
        FillOverview(_lastChar, _lastDetail);
        if (_lastDetail is { } d)
        {
            FillDeepSections(d);
        }
    }


    private void PgrRolePage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= PgrRolePage_Loaded;
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
        foldInfoBar: true,
        showRing: false,
        collapseRing: true,
        beforeLoad: ResetDetailLayer,
        async account =>
        {
            var result = await KuroBbsClient.Instance.GetPgrRoleListAsync(account);
            if (result is { IsSuccess: true, Data: { } data })
            {
                var list = (data.CharacterList ?? [])
                    .OrderByDescending(c => c.FightAbility)
                    .ThenByDescending(c => c.Priority)
                    .ToList();
                GridView_Roles.ItemsSource = list;
                await FillHeaderAsync(account);
            }
            else
            {
                HandleError(result.Code, result.Msg);
            }
        },
        Lang.Instance.KuroBbs_PgrRole);

    private void ResetDetailLayer()
    {
        Grid_Detail.Visibility = Visibility.Collapsed;
        ScrollViewer_Roles.Visibility = Visibility.Visible;
        StackPanel_TopBar.Visibility = Visibility.Visible;
    }

    private async Task FillHeaderAsync(KuroBbsAccount account)
    {
        var acc = await KuroBbsClient.Instance.GetPgrAccountDataAsync(account);
        if (acc is { IsSuccess: true, Data: { } d })
        {
            if (d.Level > 0)
            {
                account.PgrLevel = d.Level;
            }
            if (!string.IsNullOrEmpty(d.RoleName))
            {
                account.PgrRoleName = d.RoleName;
                KuroBbsAccountStore.AddOrUpdate(account, setCurrent: false);
            }
        }
    }


    private void HandleError(int code, string? msg)
    {
        KuroBbsUi.HandleKuroError(code, msg, null, Lang.Instance.PgrCommon_LoadFailed, InfoBar_Error);
    }


    #region 角色详情（点列表项钻取：概览先用 roleIndex 字段即时渲染，深层分区再走 roleDetail）

    private CancellationTokenSource? _detailCts;

    private void GridView_Roles_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is KuroPgrCharacter character)
        {
            _ = OpenDetailAsync(character);
        }
    }


    private void Button_CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        _detailCts?.Cancel();
        Grid_Detail.Visibility = Visibility.Collapsed;
        ScrollViewer_Roles.Visibility = Visibility.Visible;

        StackPanel_TopBar.Visibility = Visibility.Visible;
    }

    private async Task OpenDetailAsync(KuroPgrCharacter c)
    {
        _detailCts?.Cancel();
        var cts = new CancellationTokenSource();
        _detailCts = cts;
        var ct = cts.Token;
        _lastChar = c;

        FillOverview(c, null);
        StackPanel_Deep.Children.Clear();
        KuroBbsUi.ShowInfoBar(InfoBar_DetailError, false);
        ScrollViewer_Roles.Visibility = Visibility.Collapsed;
        Grid_Detail.Visibility = Visibility.Visible;

        StackPanel_TopBar.Visibility = Visibility.Collapsed;

        var account = KuroBbsAccountStore.Load(pgr: true);
        if (account is null || string.IsNullOrEmpty(account.Token))
        {
            return;
        }

        ProgressRing_Detail.IsActive = true;
        try
        {
            var result = await KuroBbsClient.Instance.GetPgrRoleDetailAsync(account, c.BodyId, ct);

            if (ct.IsCancellationRequested || !ReferenceEquals(_detailCts, cts))
            {
                return;
            }
            if (result is { IsSuccess: true, Data: { Character: { } detail } })
            {
                _lastDetail = detail;
                FillOverview(c, detail);
                FillDeepSections(detail);
            }
            else
            {
                HandleDetailError(result.Code, result.Msg);
            }
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                InAppToast.MainWindow?.Error(ex, Lang.Instance.KuroBbs_PgrRole);
            }
        }
        finally
        {
            if (ReferenceEquals(_detailCts, cts))
            {
                ProgressRing_Detail.IsActive = false;
            }
        }
    }


    private void HandleDetailError(int code, string? msg)
    {
        KuroBbsUi.HandleKuroError(code, msg, null, Lang.Instance.PgrCommon_LoadFailed, InfoBar_DetailError);
    }


    private async void HyperlinkButton_DetailWiki_Click(object sender, RoutedEventArgs e)
    {
        if (HyperlinkButton_DetailWiki.Tag is string url && !string.IsNullOrEmpty(url))
        {
            try
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
            catch { }
        }
    }

    private void FillOverview(KuroPgrCharacter c, KuroPgrCharacterDetail? d)
    {
        var body = d?.Body;

        string bodyName = body?.BodyName ?? c.BodyName ?? "";
        string roleName = body?.RoleName ?? "";
        TextBlock_DetailName.Text = string.IsNullOrEmpty(roleName) ? bodyName : $"{roleName}·{bodyName}";
        TextBlock_DetailLevel.Text = c.Level.HasValue ? $"LV.{c.Level}" : "";

        string? portrait = string.IsNullOrEmpty(body?.ImgUrl) ? c.IconUrl : body!.ImgUrl;
        Image_DetailRole.Source = null;
        try
        {
            if (!string.IsNullOrEmpty(portrait))
            {
                Image_DetailRole.Source = new BitmapImage(new Uri(portrait!));
            }
        }
        catch { }

        string gradeDisplay = !string.IsNullOrEmpty(d?.Grade)
            ? d!.Grade!
            : (!string.IsNullOrEmpty(c.Grade) ? c.Grade! : (c.RoleRank ?? ""));
        var gradeBrush = PgrGradeBrush(gradeDisplay, d?.Quality ?? c.QualityValue);
        Border_Portrait.BorderBrush = gradeBrush;
        TextBlock_DetailGrade.Text = string.IsNullOrEmpty(gradeDisplay) ? "-" : gradeDisplay;
        TextBlock_DetailGrade.Foreground = gradeBrush;
        TextBlock_DetailFight.Text = (d?.FightAbility ?? c.FightAbility).ToString("N0");

        string? career = body?.Career;
        string? element = string.IsNullOrEmpty(body?.Element) ? c.Element : body!.Element;
        StackPanel_DetailBadges.Children.Clear();
        if (!string.IsNullOrEmpty(career))
        {
            StackPanel_DetailBadges.Children.Add(MakeBadge(career!, true));
        }
        if (!string.IsNullOrEmpty(element))
        {

            foreach (var raw in element!.Split(',', '，'))
            {
                var name = raw.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                PgrElementIcons.TryGetValue(name, out var iconKey);
                StackPanel_DetailBadges.Children.Add(MakeIconBadge(iconKey, name));
            }
        }
        if (!string.IsNullOrEmpty(body?.Effect))
        {
            PgrEffectIcons.TryGetValue(body!.Effect!.Trim(), out var effectIcon);
            StackPanel_DetailBadges.Children.Add(MakeIconBadge(effectIcon, body!.Effect!));
        }
        if ((body?.OverrunState ?? 0) > 0)
        {
            StackPanel_DetailBadges.Children.Add(MakeIconBadge(null, $"{Lang.Instance.PgrRoleDetail_Overrun}{body!.OverrunState!.Value}"));
        }

        FillGearIcons(d);
        FillEquipIcons(d);

        if (!string.IsNullOrEmpty(body?.WikiLink))
        {
            HyperlinkButton_DetailWiki.Tag = body!.WikiLink;
            HyperlinkButton_DetailWiki.Visibility = Visibility.Visible;
        }
        else
        {
            HyperlinkButton_DetailWiki.Visibility = Visibility.Collapsed;
        }
    }


    #region 装备一览（只有图标：武器 / 辅助机 / 意识×6）与共鸣角标

    private enum ChevronKind
    {

        Red,

        Light,

        Blue,
    }


    private static readonly Dictionary<ChevronKind, string> PgrChevronFiles = new()
    {
        [ChevronKind.Light] = "PgrChevronLight.png",
        [ChevronKind.Blue] = "PgrChevronBlue.png",
        [ChevronKind.Red] = "PgrChevronRed.png",
    };

    private void FillGearIcons(KuroPgrCharacterDetail? d)
    {
        StackPanel_GearIcons.Children.Clear();
        if (d is null)
        {
            return;
        }

        if (d.WeaponInfo?.Weapon is { } w)
        {
            int resCount = d.WeaponInfo!.ResonanceList?.Count ?? 0;
            var chevrons = Enumerable.Repeat(ChevronKind.Blue, resCount).ToList();
            var icon = MakeIconWithChevrons(w.IconUrl, 64, chevrons);
            ToolTipService.SetToolTip(icon, new TextBlock { FontSize = 12, Text = w.Name ?? "" });
            StackPanel_GearIcons.Children.Add(icon);
        }

        if (d.Partner?.Partner is { } p)
        {
            var icon = MakeIcon(p.IconUrl, 64);
            string tip = p.Name ?? "";
            if (d.Partner!.Level.HasValue)
            {
                tip += "\n" + string.Format(Lang.Instance.PgrDetail_PartnerLevel, d.Partner!.Level.Value);
            }
            ToolTipService.SetToolTip(icon, new TextBlock { FontSize = 12, Text = tip });
            StackPanel_GearIcons.Children.Add(icon);
        }
    }

    private void FillEquipIcons(KuroPgrCharacterDetail? d)
    {
        Grid_EquipIcons.Children.Clear();
        Grid_EquipIcons.ColumnDefinitions.Clear();
        Grid_EquipIcons.RowDefinitions.Clear();

        for (int c = 0; c < PgrChipColumns; c++)
        {
            Grid_EquipIcons.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }
        for (int r = 0; r < PgrChipSlotCount / PgrChipColumns; r++)
        {
            Grid_EquipIcons.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var chips = d?.ChipResonanceList ?? new List<KuroPgrChipResonance>();
        var bySlot = new Dictionary<int, KuroPgrChipResonance>();
        for (int i = 0; i < chips.Count; i++)
        {
            bySlot[chips[i].Site ?? (i + 1)] = chips[i];
        }

        for (int i = 0; i < PgrChipSlotCount; i++)
        {
            int slot = i + 1;
            var cell = BuildChipSlot(slot, bySlot.GetValueOrDefault(slot));
            Grid.SetColumn(cell, i % PgrChipColumns);
            Grid.SetRow(cell, i / PgrChipColumns);
            Grid_EquipIcons.Children.Add(cell);
        }
    }

    private const int PgrChipSlotCount = 6;

    private const int PgrChipColumns = 3;

    private StackPanel BuildChipSlot(int slot, KuroPgrChipResonance? chip)
    {
        const double iconSize = 52;
        var sp = new StackPanel { Spacing = 3, Width = iconSize + 8 };
        if (chip is null)
        {
            sp.Children.Add(MakeIcon(null, iconSize));
        }
        else
        {
            var icon = MakeIconWithChevrons(chip.ChipIconUrl, iconSize, ChipChevrons(chip));
            ToolTipService.SetToolTip(icon, new TextBlock { FontSize = 12, Text = chip.ChipName ?? "" });
            sp.Children.Add(icon);
        }
        sp.Children.Add(new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Text = slot.ToString("00"),
            Foreground = SecondaryBrush(),
            Opacity = chip is null ? 0.5 : 1,
        });
        return sp;
    }

    private static List<ChevronKind> ChipChevrons(KuroPgrChipResonance chip)
    {
        var list = new List<ChevronKind>();
        var kinds = new (string? Icon, bool Awake)[]
        {
            (chip.SuperSlotIconUrl, chip.SuperAwake == true),
            (chip.SubSlotIconUrl, chip.SubAwake == true),
        };
        foreach (var (icon, awake) in kinds)
        {
            if (string.IsNullOrEmpty(icon))
            {
                continue;
            }
            if (!awake)
            {
                list.Add(ChevronKind.Blue);
            }
            else
            {
                list.Add(chip.Defend == true ? ChevronKind.Red : ChevronKind.Light);
            }
        }
        return list;
    }

    private static Grid MakeIconWithChevrons(string? iconUrl, double size, List<ChevronKind> chevrons)
    {
        var grid = new Grid();
        grid.Children.Add(MakeIcon(iconUrl, size));

        if (chevrons.Count > 0)
        {
            var bar = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 4, 0),
            };
            foreach (var kind in chevrons)
            {
                var img = new Image { Width = 18, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 0, -4) };
                try
                {
                    img.Source = new BitmapImage(new Uri($"ms-appx:///Assets/Image/Combat/{PgrChevronFiles[kind]}"));
                }
                catch { }
                bar.Children.Add(img);
            }
            grid.Children.Add(bar);
        }
        return grid;
    }

    #endregion


    #region 深层分区构建（专属武器 / 辅助机 / 意识套装 / 意识共鸣）

    private void FillDeepSections(KuroPgrCharacterDetail d)
    {
        StackPanel_Deep.Children.Clear();

        if (d.WeaponInfo?.Weapon is { } w)
        {
            StackPanel_Deep.Children.Add(BuildWeaponSection(d.WeaponInfo!));
        }
        if (d.Partner?.Partner is { })
        {
            StackPanel_Deep.Children.Add(BuildPartnerSection(d.Partner!));
        }
        if (d.ChipSuitList is { Count: > 0 })
        {
            StackPanel_Deep.Children.Add(BuildChipSuitSection(d.ChipSuitList!));
        }
        if (d.ChipResonanceList is { Count: > 0 })
        {
            StackPanel_Deep.Children.Add(BuildChipResonanceSection(d.ChipResonanceList!, d.ChipExDamage));
        }
    }

    private static Border BuildWeaponSection(KuroPgrWeaponInfo wi)
    {
        var (card, content) = MakeSectionCard(Lang.Instance.PgrDetail_Weapon, null);
        var w = wi.Weapon!;

        var details = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };

        if (!string.IsNullOrEmpty(w.SkillDescription))
        {
            details.Children.Add(MakeDescText(w.SkillDescription!));
        }

        if (wi.Suit is { } suit)
        {
            details.Children.Add(MakeDivider());
            details.Children.Add(MakeSubHeader(Lang.Instance.PgrDetail_WeaponSuit));
            details.Children.Add(MakeIconRow(suit.IconUrl, suit.Name ?? "-", null, null));
            AddSuitLines(details, suit.SkillDescriptionTwo, suit.SkillDescriptionFour, suit.SkillDescriptionSix);
        }

        if (wi.WeaponOverrun2 is { } ov2)
        {
            details.Children.Add(MakeDivider());
            details.Children.Add(MakeSubHeader(Lang.Instance.PgrDetail_WeaponOverrun2));
            details.Children.Add(MakeIconRow(ov2.IconUrl, ov2.Name ?? "-", null, ov2.Desc));
        }

        if (wi.ResonanceList is { Count: > 0 } res)
        {
            details.Children.Add(MakeDivider());
            details.Children.Add(MakeSubHeader(Lang.Instance.PgrDetail_WeaponResonance));
            foreach (var r in res!)
            {
                details.Children.Add(MakeIconRow(r.IconUrl, r.Name ?? "-", null, r.SkillDescription));
            }
        }

        string? badge = (wi.MaxOverRunLevel ?? 0) > 0
            ? string.Format(Lang.Instance.PgrDetail_OverrunLevel, wi.OverRunLevel ?? 0, wi.MaxOverRunLevel ?? 0)
            : null;
        string tooltip = string.IsNullOrEmpty(w.SkillDescription)
            ? (w.Name ?? "")
            : $"{w.Name}\n{w.SkillDescription}";
        var header = MakeCollapsibleHeader(new CollapsibleHeaderInfo
        {
            IconUrl = w.IconUrl,
            IconSize = 48,
            Title = w.Name ?? "",
            Subtitle = string.IsNullOrEmpty(w.SkillName) ? null : $"{Lang.Instance.PgrDetail_WeaponSkill} {w.SkillName}",
            Badge = badge,

            Chevrons = Enumerable.Repeat(ChevronKind.Blue, wi.ResonanceList?.Count ?? 0).ToList(),
            Details = details,
            Tooltip = tooltip,
        });
        content.Children.Add(header);
        content.Children.Add(details);
        return card;
    }

    private static Border BuildPartnerSection(KuroPgrPartnerInfo p)
    {
        var (card, content) = MakeSectionCard(Lang.Instance.PgrDetail_Partner, null);
        var inner = p.Partner!;

        string? subtitle = null;
        if (p.Level.HasValue)
        {
            subtitle = string.Format(Lang.Instance.PgrDetail_PartnerLevel, p.Level.Value);
        }
        if (p.BreakThrough.HasValue)
        {
            subtitle = (subtitle is null ? "" : subtitle + " · ") + string.Format(Lang.Instance.PgrDetail_PartnerBreak, p.BreakThrough.Value);
        }
        if (!string.IsNullOrEmpty(p.Grade))
        {
            subtitle = (subtitle is null ? "" : subtitle + " · ") + p.Grade!;
        }

        var details = new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0), Visibility = Visibility.Collapsed };
        if (p.SkillList is { Count: > 0 } skills)
        {
            foreach (var s in skills!)
            {
                string? sub = s.Level.HasValue ? string.Format(Lang.Instance.PgrDetail_SkillLevel, s.Level.Value) : null;
                details.Children.Add(MakeIconRow(s.IconUrl, s.Name ?? "-", sub, s.Description));
            }
        }

        string tooltip = $"{inner.Name ?? ""}{(string.IsNullOrEmpty(subtitle) ? "" : "\n" + subtitle!)}";
        var header = MakeCollapsibleHeader(new CollapsibleHeaderInfo
        {
            IconUrl = inner.IconUrl,
            IconSize = 48,
            Title = inner.Name ?? "",
            Subtitle = subtitle,
            Badge = null,
            Details = details,
            Tooltip = tooltip,
        });
        content.Children.Add(header);
        content.Children.Add(details);
        return card;
    }

    private static Border BuildChipSuitSection(List<KuroPgrChipSuit> list)
    {
        var (card, content) = MakeSectionCard(Lang.Instance.PgrDetail_ChipSuit, null);
        foreach (var s in list)
        {
            string? subtitle = s.Num.HasValue ? string.Format(Lang.Instance.PgrDetail_SuitCount, s.Num.Value) : null;
            content.Children.Add(MakeIconRow(s.IconUrl, s.Name ?? "-", subtitle, null));
            AddSuitLines(content, s.DescriptionTwo, s.DescriptionFour, s.DescriptionSix);
        }
        return card;
    }

    private static Border BuildChipResonanceSection(List<KuroPgrChipResonance> list, string? exDamage)
    {
        string? right = string.IsNullOrEmpty(exDamage) ? null : $"{Lang.Instance.PgrDetail_ChipExDamage} {exDamage}";
        var (card, content) = MakeSectionCard(Lang.Instance.PgrDetail_ChipResonance, right);

        var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        for (int i = 0; i < 3; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var bySlot = new Dictionary<int, KuroPgrChipResonance>();
        for (int i = 0; i < list.Count; i++)
        {
            bySlot[list[i].Site ?? (i + 1)] = list[i];
        }

        for (int i = 0; i < PgrChipSlotCount; i++)
        {
            int slot = i + 1;
            var r = bySlot.GetValueOrDefault(slot);
            var cell = new Border
            {
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(8),
                Background = (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
            };
            var sp = new StackPanel { Spacing = 4 };

            var head = new Grid { ColumnSpacing = 8 };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            FrameworkElement chipIcon = r is null
                ? MakeIcon(null, 36)
                : MakeIconWithChevrons(r.ChipIconUrl, 36, ChipChevrons(r));
            chipIcon.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(chipIcon, 0);
            head.Children.Add(chipIcon);

            var titles = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
            titles.Children.Add(new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = SecondaryBrush(),
                Text = slot.ToString("00"),
            });
            titles.Children.Add(new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Text = r?.ChipName ?? "-",
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            Grid.SetColumn(titles, 1);
            head.Children.Add(titles);
            sp.Children.Add(head);

            if (r is null)
            {
                cell.Opacity = 0.5;
            }
            else
            {
                sp.Children.Add(MakeSlotLine(Lang.Instance.PgrDetail_SuperSlot, r.SuperSlotIconUrl, r.SuperAwake == true, r.SuperDescription));
                sp.Children.Add(MakeSlotLine(Lang.Instance.PgrDetail_SubSlot, r.SubSlotIconUrl, r.SubAwake == true, r.SubDescription));
            }

            cell.Child = sp;
            Grid.SetColumn(cell, i % 3);
            Grid.SetRow(cell, i / 3);
            grid.Children.Add(cell);
        }
        content.Children.Add(grid);
        return card;
    }

    private static void AddSuitLines(StackPanel parent, string? two, string? four, string? six)
    {
        AddSuitLine(parent, Lang.Instance.PgrDetail_Suit2, two);
        AddSuitLine(parent, Lang.Instance.PgrDetail_Suit4, four);
        AddSuitLine(parent, Lang.Instance.PgrDetail_Suit6, six);
    }


    private static void AddSuitLine(StackPanel parent, string label, string? desc)
    {
        if (string.IsNullOrEmpty(desc))
        {
            return;
        }
        var sp = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
        sp.Children.Add(new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Text = label });
        sp.Children.Add(new TextBlock
        {
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = SecondaryBrush(),
            Text = desc!,
        });
        parent.Children.Add(sp);
    }

    private static StackPanel MakeSlotLine(string label, string? iconUrl, bool awake, string? desc)
    {
        var sp = new StackPanel { Spacing = 1, Opacity = awake ? 1 : 0.45 };
        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        if (!string.IsNullOrEmpty(iconUrl))
        {
            var icon = MakeIcon(iconUrl, 14);
            icon.VerticalAlignment = VerticalAlignment.Center;
            head.Children.Add(icon);
        }
        head.Children.Add(new TextBlock
        {
            FontSize = 10,
            Foreground = SecondaryBrush(),
            VerticalAlignment = VerticalAlignment.Center,
            Text = label,
        });
        if (!awake)
        {
            head.Children.Add(new TextBlock
            {
                FontSize = 10,
                Foreground = SecondaryBrush(),
                VerticalAlignment = VerticalAlignment.Center,
                Text = "· " + Lang.Instance.PgrDetail_Inactive,
            });
        }
        sp.Children.Add(head);
        sp.Children.Add(new TextBlock
        {
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Text = string.IsNullOrEmpty(desc) ? "-" : desc!,
        });
        return sp;
    }

    private static (Border Card, StackPanel Content) MakeSectionCard(string header, string? rightText)
    {
        var content = new StackPanel { Spacing = 6 };

        var headerGrid = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 0, 0, 2) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.Children.Add(new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Text = header,
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (!string.IsNullOrEmpty(rightText))
        {
            var rt = new TextBlock
            {
                FontSize = 12,
                Foreground = SecondaryBrush(),
                Text = rightText!,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(rt, 1);
            headerGrid.Children.Add(rt);
        }
        content.Children.Add(headerGrid);

        var card = KuroBbsUi.KuroCard(content, cornerRadius: 12, padding: new Thickness(16));
        return (card, content);
    }

    private static StackPanel MakeIconRow(string? iconUrl, string title, string? subtitle, string? desc)
    {
        var panel = new StackPanel { Spacing = 4 };
        var row = new Grid { ColumnSpacing = 10 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = MakeIcon(iconUrl, 36);
        Grid.SetColumn(icon, 0);
        row.Children.Add(icon);

        var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        head.Children.Add(new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Text = title,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        if (!string.IsNullOrEmpty(subtitle))
        {
            head.Children.Add(new TextBlock
            {
                FontSize = 11,
                Foreground = SecondaryBrush(),
                VerticalAlignment = VerticalAlignment.Center,
                Text = subtitle!,
            });
        }
        Grid.SetColumn(head, 1);
        row.Children.Add(head);
        panel.Children.Add(row);

        if (!string.IsNullOrEmpty(desc))
        {
            panel.Children.Add(MakeDescText(desc!));
        }
        return panel;
    }

    private static Border MakeIcon(string? url, double size)
    {
        var img = new Image { Width = size, Height = size, Stretch = Stretch.Uniform };
        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                img.Source = new BitmapImage(new Uri(url!));
            }
            catch { }
        }
        return new Border
        {
            Width = size + 8,
            Height = size + 8,
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["ControlFillColorSecondaryBrush"],
            Child = img,
        };
    }


    private static TextBlock MakeDescText(string text) => new()
    {
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Foreground = SecondaryBrush(),
        Text = text,
    };


    private static TextBlock MakeSubHeader(string text) => new()
    {
        FontSize = 12,
        FontWeight = FontWeights.SemiBold,
        Foreground = SecondaryBrush(),
        Text = text,
    };


    private static Border MakeDivider() => KuroBbsUi.MakeDivider("CardStrokeColorDefaultBrush", 1, new Thickness(0, 6, 0, 2));


    private static Border MakeBadge(string text, bool accent)
    {
        string key = accent ? "AccentFillColorDefaultBrush" : "SubtleFillColorSecondaryBrush";
        return new Border
        {
            Padding = new Thickness(6, 1, 6, 1),
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources[key],
            Child = new TextBlock { FontSize = 11, Text = text },
        };
    }


    private static Brush SecondaryBrush() => (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

    private static readonly Dictionary<string, string> PgrElementIcons = new()
    {
        ["物理"] = "Element_Physical",
        ["火"] = "Element_Fire",
        ["冰"] = "Element_Ice",
        ["雷"] = "Element_Lightning",
        ["暗"] = "Element_Dark",
        ["空"] = "Element_Void",
        ["独域"] = "Element_Uniframe",
    };

    private static readonly Dictionary<string, string> PgrEffectIcons = new()
    {
        ["紊乱"] = "Effect_Clutter",
        ["焚烧"] = "Effect_Burn",
        ["真意斩"] = "Effect_TrueSlash",
        ["电束"] = "Effect_Bolt",
        ["覆冰"] = "Effect_Frost",
        ["黯衍"] = "Effect_DimDeriv",
        ["熠光"] = "Effect_Glow",
    };

    private Border MakeIconBadge(string? iconKey, string text)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        if (!string.IsNullOrEmpty(iconKey))
        {
            var img = KuroBbsUi.MakeThemeIcon(this, iconKey!, 13);
            img.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(img);
        }
        sp.Children.Add(new TextBlock { FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Text = text });
        return new Border
        {
            Padding = new Thickness(6, 2, 6, 2),
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
            Child = sp,
        };
    }

    private sealed class CollapsibleHeaderInfo
    {
        public string? IconUrl { get; init; }
        public double IconSize { get; init; } = 48;
        public string Title { get; init; } = "";
        public string? Subtitle { get; init; }
        public string? Badge { get; init; }
        public List<ChevronKind> Chevrons { get; init; } = new();
        public StackPanel Details { get; init; } = new();
        public string? Tooltip { get; init; }
    }

    private static Grid MakeCollapsibleHeader(CollapsibleHeaderInfo info)
    {
        var head = new Grid { ColumnSpacing = 10 };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        FrameworkElement icon = info.Chevrons.Count > 0
            ? MakeIconWithChevrons(info.IconUrl, info.IconSize, info.Chevrons)
            : MakeIcon(info.IconUrl, info.IconSize);
        icon.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(icon, 0);
        head.Children.Add(icon);

        var titles = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Text = info.Title,
            TextTrimming = TextTrimming.CharacterEllipsis,
        });
        if (!string.IsNullOrEmpty(info.Subtitle))
        {
            titles.Children.Add(new TextBlock
            {
                FontSize = 11,
                Foreground = SecondaryBrush(),
                Text = info.Subtitle!,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
        }
        Grid.SetColumn(titles, 1);
        head.Children.Add(titles);

        if (!string.IsNullOrEmpty(info.Badge))
        {
            var badge = MakeBadge(info.Badge!, true);
            badge.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(badge, 2);
            head.Children.Add(badge);
        }

        var chevron = new FontIcon { FontSize = 12, Glyph = "\uE70E" };
        chevron.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(chevron, 3);
        head.Children.Add(chevron);

        if (!string.IsNullOrEmpty(info.Tooltip))
        {
            ToolTipService.SetToolTip(head, new TextBlock
            {
                MaxWidth = 320,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Text = info.Tooltip!,
            });
        }

        bool open = false;
        head.Background = (Brush)Application.Current.Resources["SubtleFillColorTransparentBrush"];
        head.Tapped += (_, _) =>
        {
            open = !open;
            info.Details.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            chevron.Glyph = open ? "\uE70D" : "\uE70E";
        };
        return head;
    }

    #endregion

    private static Brush PgrGradeBrush(string? grade, int quality)
    {
        var g = grade?.Trim();
        return g switch
        {
            "A" => new SolidColorBrush(Color.FromArgb(0xFF, 0xB9, 0x2D, 0xCB)),
            "B" => new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x9E, 0xFD)),
            "S" or "S5" or "SS" or "SS3" => new SolidColorBrush(Color.FromArgb(0xFF, 0xDF, 0x32, 0x5F)),
            "SSS" or "SSS3" or "SSS6" => new SolidColorBrush(Color.FromArgb(0xFF, 0xFD, 0x9C, 0x34)),
            "SSS+" => new SolidColorBrush(Color.FromArgb(0xFF, 0xE3, 0x3B, 0x00)),
            _ => PgrQualityBrush(quality),
        };
    }

    private static Brush PgrQualityBrush(int quality) => new SolidColorBrush(PgrQualityColor(quality));

    private static Color PgrQualityColor(int quality) => quality switch
    {
        6 => Color.FromArgb(0xFF, 0xFA, 0x58, 0x26),
        5 => Color.FromArgb(0xFF, 0xFF, 0x8D, 0x1E),
        4 => Color.FromArgb(0xFF, 0xCC, 0x68, 0xC1),
        3 => Color.FromArgb(0xFF, 0x3E, 0x70, 0xBB),
        2 => Color.FromArgb(0xFF, 0x72, 0xB7, 0x72),
        _ => Color.FromArgb(0xFF, 0x80, 0x80, 0x80),
    };


    #endregion
}
