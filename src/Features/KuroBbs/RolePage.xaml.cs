using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Arietta.Features.KuroBbs;





public sealed partial class RolePage : PageBase
{

    public RolePage()
    {
        this.InitializeComponent();
        this.Loaded += RolePage_Loaded;
    }


    private async void RolePage_Loaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= RolePage_Loaded;
        KuroBbsEvents.AccountChanged += KuroBbsEvents_AccountChanged;
        
        StackPanel_Info.SizeChanged += StackPanel_Info_SizeChanged;
        await LoadRolesAsync();
    }


    protected override void OnUnloaded()
    {
        KuroBbsEvents.AccountChanged -= KuroBbsEvents_AccountChanged;
        StackPanel_Info.SizeChanged -= StackPanel_Info_SizeChanged;
        base.OnUnloaded();
    }


    private async void KuroBbsEvents_AccountChanged()
    {
        DispatcherQueue.TryEnqueue(async () => await LoadRolesAsync());
    }


    private async Task LoadRolesAsync()
    {
        var account = KuroBbsAccountStore.Load();
        if (account is null || !account.IsValid)
        {
            InfoBar_NoAccount.IsOpen = true;
            ScrollView_Roles.Visibility = Visibility.Collapsed;
            return;
        }
        InfoBar_NoAccount.IsOpen = false;
        InfoBar_Error.IsOpen = false;
        ProgressRing_Loading.IsActive = true;
        try
        {
            var result = await KuroBbsClient.Instance.GetRoleListAsync(account);
            if (result is { IsSuccess: true, Data: { } roles })
            {
                GridView_Roles.ItemsSource = roles.Select(KuroRoleItem.From).ToList();
                ScrollView_Roles.Visibility = Visibility.Visible;
            }
            else if (result.IsTokenExpired)
            {
                InAppToast.MainWindow?.Warning(Lang.Instance.KuroBbs_TokenExpired, Lang.Instance.KuroBbs_TokenExpiredDesc);
            }
            else
            {
                InfoBar_Error.Message = result.Msg ?? "";
                InfoBar_Error.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.RolePage_Refresh);
        }
        finally
        {
            ProgressRing_Loading.IsActive = false;
        }
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e) => await LoadRolesAsync();


    private async void GridView_Roles_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is KuroRoleItem item)
        {
            await ShowDetailAsync(item.RoleId);
        }
    }

    private async Task ShowDetailAsync(int roleId)
    {
        var account = KuroBbsAccountStore.Load();
        if (account is null) return;
        ProgressRing_Loading.IsActive = true;
        try
        {
            var result = await KuroBbsClient.Instance.GetRoleDetailAsync(account, roleId);
            if (result is { IsSuccess: true, Data: { } detail })
            {
                await FillDetailAsync(detail);
                
                ScrollView_Roles.Visibility = Visibility.Collapsed;
                InfoBar_NoAccount.IsOpen = false;
                Grid_Detail.Visibility = Visibility.Visible;
                
                Grid_Detail.UpdateLayout();
                ApplyPortraitStretch();
            }
            else
            {
                InAppToast.MainWindow?.Error(new InvalidOperationException(result.Msg ?? Lang.Instance.Common_UnknownError), Lang.Instance.RolePage_LoadFailed);
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.RolePage_LoadFailed);
        }
        finally
        {
            ProgressRing_Loading.IsActive = false;
        }
    }


    private async Task FillDetailAsync(KuroRoleDetail detail)
    {
        var role = detail.Role;
        await SetDetailImageAsync(role?.RolePicUrl);
        TextBlock_DetailName.Text = role?.RoleName ?? "";
        TextBlock_DetailLevel.Text = $"LV.{detail.Level}";

        FillAttributes(detail.RoleAttributeList);

        
        var weapon = detail.WeaponData?.Weapon;
        Panel_Weapon.Visibility = weapon is null ? Visibility.Collapsed : Visibility.Visible;
        Border_Weapon.Visibility = weapon is null ? Visibility.Collapsed : Visibility.Visible;
        if (weapon is not null)
        {
            try
            {
                if (!string.IsNullOrEmpty(weapon.WeaponIcon))
                {
                    Image_Weapon.Source = new BitmapImage(new Uri(weapon.WeaponIcon));
                }
            }
            catch { }
            TextBlock_WeaponName.Text = weapon.WeaponName;
            TextBlock_WeaponLevel.Text = string.Format(Lang.Instance.RolePage_WeaponLevel, detail.WeaponData?.Level, detail.WeaponData?.Breach);
            Border_WeaponIcon.Background = BuildWeaponRarityBrush(weapon.WeaponStarLevel);
            TextBlock_WeaponEffectName.Text = weapon.WeaponEffectName;
            TextBlock_WeaponEffect.Text = weapon.EffectDescription;
            Panel_WeaponEffect.Visibility = string.IsNullOrWhiteSpace(weapon.WeaponEffectName) && string.IsNullOrWhiteSpace(weapon.EffectDescription)
                ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            Panel_WeaponEffect.Visibility = Visibility.Collapsed;
        }

        
        
        Border_Skills.Visibility = detail.SkillList is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        Panel_Skills.Children.Clear();
        StackPanel? skillRow = null;
        int skillIndex = 0;
        foreach (var skillData in detail.SkillList ?? [])
        {
            var skill = skillData.Skill;
            if (skill is null) continue;
            if (skillIndex % SkillsPerRow == 0)
            {
                skillRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                Panel_Skills.Children.Add(skillRow);
            }
            skillIndex++;
            var stack = new StackPanel { Spacing = 2 };
            try
            {
                if (!string.IsNullOrEmpty(skill.IconUrl))
                {
                    stack.Children.Add(new Image { Width = 40, Height = 40, HorizontalAlignment = HorizontalAlignment.Center, Source = new BitmapImage(new Uri(skill.IconUrl)) });
                }
            }
            catch { }
            stack.Children.Add(new TextBlock
            {
                Text = $"LV.{skillData.Level}",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            });
            ToolTipService.SetToolTip(stack, $"{skill.Name}\n{skill.Description}");
            skillRow!.Children.Add(stack);
        }

        
        
        var phantomData = detail.PhantomData;
        List<KuroPhantom> phantoms = phantomData?.EquipPhantomList?.Where(p => p is not null).ToList() ?? [];
        Panel_Phantoms.Visibility = phantoms.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TextBlock_PhantomCost.Text = BuildPhantomSummary(phantomData?.Cost, phantoms);
        StackPanel_Phantoms.Children.Clear();
        var phantomGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        phantomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        phantomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < phantoms.Count; i++)
        {
            int row = i / 2;
            while (phantomGrid.RowDefinitions.Count <= row)
            {
                phantomGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            var card = BuildPhantomTag(phantoms[i]);
            Grid.SetRow(card, row);
            Grid.SetColumn(card, i % 2);
            phantomGrid.Children.Add(card);
        }
        StackPanel_Phantoms.Children.Add(phantomGrid);

        
        StackPanel_ChainIcons.Children.Clear();
        foreach (var chain in detail.ChainList ?? [])
        {
            StackPanel_ChainIcons.Children.Add(BuildChainIcon(chain));
        }
    }


    
    
    
    
    
    
    
    
    
    private const double DetailImageMaxWidth = 352;

    
    private double _portraitWidth;

    
    private const int SkillsPerRow = 8;

    
    private const uint AlphaProbeSize = 128;

    
    private const byte AlphaThreshold = 24;

    
    private const int BackdropColorDistance = 36;


    
    
    
    
    
    
    
    
    private async Task SetDetailImageAsync(string? url)
    {
        Image_DetailRole.Source = null;
        Image_DetailRole.Width = 0;
        Image_DetailRole.Height = 0;
        Image_DetailRole.Margin = new Thickness(0);
        _portraitWidth = 0;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }
        try
        {
            var bytes = await KuroBbsClient.DownloadBytesAsync(url);
            if (bytes is { Length: > 0 })
            {
                var cropped = await TryCropImagePaddingAsync(bytes);
                if (cropped is { } c)
                {
                    Image_DetailRole.Source = c.Source;
                    ApplyDetailImageSize(c.Width, c.Height);
                    return;
                }
            }
        }
        catch { }

        
        try
        {
            var bitmap = new BitmapImage(new Uri(url));
            bitmap.ImageOpened += (_, _) =>
            {
                
                if (ReferenceEquals(Image_DetailRole.Source, bitmap))
                {
                    ApplyDetailImageSize(bitmap.PixelWidth, bitmap.PixelHeight);
                }
            };
            Image_DetailRole.Source = bitmap;
        }
        catch { }
    }


    
    
    
    
    
    
    
    
    
    
    
    private static async Task<(SoftwareBitmapSource Source, int Width, int Height)?> TryCropImagePaddingAsync(byte[] bytes)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(stream.GetOutputStreamAt(0));
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
            writer.Dispose();
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            uint fullWidth = decoder.PixelWidth;
            uint fullHeight = decoder.PixelHeight;
            if (fullWidth < 16 || fullHeight < 16)
            {
                return null;
            }

            
            uint probeWidth = Math.Min(AlphaProbeSize, fullWidth);
            uint probeHeight = Math.Max(1, (uint)Math.Round((double)fullHeight * probeWidth / fullWidth));
            var probeData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                new BitmapTransform { ScaledWidth = probeWidth, ScaledHeight = probeHeight },
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            byte[] pixels = probeData.DetachPixelData();

            
            int sample = 4;
            long sumB = 0, sumG = 0, sumR = 0, sumA = 0, count = 0;
            foreach (var (ox, oy) in new[] { (0, 0), ((int)probeWidth - sample, 0), (0, (int)probeHeight - sample), ((int)probeWidth - sample, (int)probeHeight - sample) })
            {
                for (int y = oy; y < oy + sample; y++)
                {
                    for (int x = ox; x < ox + sample; x++)
                    {
                        int i = ((y * (int)probeWidth) + x) * 4;
                        sumB += pixels[i];
                        sumG += pixels[i + 1];
                        sumR += pixels[i + 2];
                        sumA += pixels[i + 3];
                        count++;
                    }
                }
            }
            double bgB = (double)sumB / count, bgG = (double)sumG / count, bgR = (double)sumR / count, bgA = (double)sumA / count;
            bool transparentBackdrop = bgA < 32;

            int minX = (int)probeWidth, minY = (int)probeHeight, maxX = -1, maxY = -1;
            for (int y = 0; y < probeHeight; y++)
            {
                int rowStart = y * (int)probeWidth * 4;
                for (int x = 0; x < probeWidth; x++)
                {
                    int i = rowStart + (x * 4);
                    
                    bool isContent = transparentBackdrop
                        ? pixels[i + 3] > AlphaThreshold
                        : Math.Abs(pixels[i] - bgB) + Math.Abs(pixels[i + 1] - bgG) + Math.Abs(pixels[i + 2] - bgR) > BackdropColorDistance;
                    if (isContent)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (maxX < 0)
            {
                return null;    
            }
            
            if (minX * 2 > probeWidth || maxX * 2 < probeWidth || minY * 2 > probeHeight || maxY * 2 < probeHeight)
            {
                return null;
            }

            
            double scaleX = (double)fullWidth / probeWidth;
            double scaleY = (double)fullHeight / probeHeight;
            double marginX = fullWidth * 0.005;
            double marginY = fullHeight * 0.005;
            int boxX = (int)Math.Max(0, Math.Floor((minX * scaleX) - marginX));
            int boxY = (int)Math.Max(0, Math.Floor((minY * scaleY) - marginY));
            int boxRight = (int)Math.Min(fullWidth, Math.Ceiling(((maxX + 1) * scaleX) + marginX));
            int boxBottom = (int)Math.Min(fullHeight, Math.Ceiling(((maxY + 1) * scaleY) + marginY));
            int boxWidth = boxRight - boxX;
            int boxHeight = boxBottom - boxY;
            if (boxWidth < 16 || boxHeight < 16)
            {
                return null;
            }

            
            var cropped = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                new BitmapTransform
                {
                    Bounds = new BitmapBounds
                    {
                        X = (uint)boxX,
                        Y = (uint)boxY,
                        Width = (uint)boxWidth,
                        Height = (uint)boxHeight,
                    },
                },
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(cropped);
            return (source, cropped.PixelWidth, cropped.PixelHeight);
        }
        catch
        {
            return null;
        }
    }


    
    
    
    
    private void ApplyDetailImageSize(int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }
        
        double scale = Math.Min(DetailImageMaxWidth / pixelWidth, 1.0);
        _portraitWidth = Math.Round(pixelWidth * scale);
        ApplyPortraitStretch();
    }


    
    
    
    
    
    
    
    private void ApplyPortraitStretch()
    {
        if (Image_DetailRole.Source is null || _portraitWidth <= 0)
        {
            return;
        }
        double top = 0;
        double bottom = 0;
        try
        {
            
            bottom = Border_Skills
                .TransformToVisual(StackPanel_Info)
                .TransformPoint(new Point(0, Border_Skills.ActualHeight)).Y;
            if (Border_Weapon.ActualHeight > 0)
            {
                top = Border_Weapon
                    .TransformToVisual(StackPanel_Info)
                    .TransformPoint(new Point(0, 0)).Y;
            }
        }
        catch { }
        if (bottom <= 0 || bottom - top < 40)
        {
            
            top = 0;
            bottom = StackPanel_Info.ActualHeight;
        }
        if (bottom - top <= 0)
        {
            return;
        }
        Image_DetailRole.Margin = new Thickness(0, Math.Round(top), 0, 0);
        Image_DetailRole.Width = _portraitWidth;
        Image_DetailRole.Height = Math.Round(bottom - top);
    }


    private void StackPanel_Info_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyPortraitStretch();


    
    private static LinearGradientBrush BuildWeaponRarityBrush(int starLevel)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
        };
        switch (starLevel)
        {
            case 5:
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0x8C, 0xFA, 0xD9, 0x57), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xE8, 0xF7, 0xD2, 0x3B), Offset = 1.0 });
                break;
            case 4:
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0xD7, 0xAF, 0xFF), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0xF7, 0xBE, 0xFF), Offset = 1.0 });
                break;
            default:
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0xFD, 0xFD, 0xFD), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(0xFF, 0xBA, 0xB9, 0xB9), Offset = 1.0 });
                break;
        }
        return brush;
    }


    
    private void FillAttributes(List<KuroRoleAttribute>? attributes)
    {
        StackPanel_Attributes.Children.Clear();
        if (attributes is not { Count: > 0 })
        {
            return;
        }
        var nameBrush = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        for (int i = 0; i < attributes.Count; i += 2)
        {
            var row = new Grid { ColumnSpacing = 20 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int j = 0; j < 2; j++)
            {
                var cell = new Grid();
                cell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                if (i + j < attributes.Count)
                {
                    var name = new TextBlock
                    {
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = nameBrush,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Text = attributes[i + j].AttributeName,
                    };
                    var value = new TextBlock
                    {
                        FontSize = 12,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = attributes[i + j].AttributeValue,
                    };
                    Grid.SetColumn(value, 1);
                    cell.Children.Add(name);
                    cell.Children.Add(value);
                }
                Grid.SetColumn(cell, j);
                row.Children.Add(cell);
            }
            StackPanel_Attributes.Children.Add(row);
        }
    }


    
    private Border BuildChainIcon(KuroChain chain)
    {
        var image = new Image { Width = 32, Height = 32, Stretch = Stretch.Uniform };
        try
        {
            if (!string.IsNullOrEmpty(chain.IconUrl))
            {
                image.Source = new BitmapImage(new Uri(chain.IconUrl));
            }
        }
        catch { }
        var border = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Opacity = chain.Unlocked ? 1.0 : 0.25,
            
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x59, 0x00, 0x00, 0x00)),
            Child = image,
        };
        ToolTipService.SetPlacement(border, Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Left);
        ToolTipService.SetToolTip(border, $"{chain.Name}\n{chain.Description}");
        return border;
    }


    
    private static readonly Windows.UI.Color[] PropColors =
    [
        Windows.UI.Color.FromArgb(0xFF, 0xD9, 0xB1, 0x4C),
        Windows.UI.Color.FromArgb(0xFF, 0x53, 0xB8, 0xA5),
        Windows.UI.Color.FromArgb(0xFF, 0x5B, 0x8D, 0xEF),
        Windows.UI.Color.FromArgb(0xFF, 0xE8, 0x6A, 0x6A),
        Windows.UI.Color.FromArgb(0xFF, 0x9A, 0x6A, 0xB8),
    ];

    
    
    
    
    private Border BuildPhantomTag(KuroPhantom phantom)
    {
        var prop = phantom.PhantomProp;

        
        var iconStack = new Grid { Width = 56, Height = 56, VerticalAlignment = VerticalAlignment.Top };
        try
        {
            if (!string.IsNullOrEmpty(prop?.IconUrl))
            {
                iconStack.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Child = new Image { Stretch = Stretch.UniformToFill, Source = new BitmapImage(new Uri(prop.IconUrl)) },
                });
            }
        }
        catch { }
        iconStack.Children.Add(new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(4, 0, 4, 0),
            CornerRadius = new CornerRadius(6, 0, 6, 0),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xCC, 0x00, 0x00, 0x00)),
            Child = new TextBlock { FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Microsoft.UI.Colors.White), Text = $"+{phantom.Level}" },
        });

        var content = new StackPanel { Spacing = 4 };

        
        var head = new Grid { ColumnSpacing = 8 };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameText = new TextBlock
        {
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = prop?.Name ?? "",
        };
        var costText = new TextBlock
        {
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            Text = phantom.Cost > 0 ? $"COST {phantom.Cost}" : "",
        };
        Grid.SetColumn(nameText, 0);
        Grid.SetColumn(costText, 1);
        head.Children.Add(nameText);
        head.Children.Add(costText);
        content.Children.Add(head);
        content.Children.Add(BuildDivider());

        
        foreach (var main in phantom.MainProps ?? [])
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var mainName = new TextBlock { FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Bold, TextTrimming = TextTrimming.CharacterEllipsis, Text = main.AttributeName };
            var mainValue = new TextBlock { FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Text = main.AttributeValue };
            Grid.SetColumn(mainName, 0);
            Grid.SetColumn(mainValue, 1);
            row.Children.Add(mainName);
            row.Children.Add(mainValue);
            content.Children.Add(row);
        }

        
        var subs = phantom.SubProps ?? [];
        if (subs.Count > 0)
        {
            content.Children.Add(BuildDivider());
            for (int i = 0; i < subs.Count; i++)
            {
                var sub = subs[i];
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var bar = new Rectangle { Fill = new SolidColorBrush(PropColors[i % PropColors.Length]), RadiusX = 1.5, RadiusY = 1.5 };
                var maxText = sub.AttributeMaxValue > 0 ? $"({sub.AttributeMaxValue:0.#})" : "";
                var subName = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Text = $"{sub.AttributeName}{maxText}" };
                var subValue = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Text = sub.AttributeValue };
                Grid.SetColumn(bar, 0);
                Grid.SetColumn(subName, 1);
                Grid.SetColumn(subValue, 2);
                row.Children.Add(bar);
                row.Children.Add(subName);
                row.Children.Add(subValue);
                content.Children.Add(row);
            }
        }

        var cardGrid = new Grid { ColumnSpacing = 12 };
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(iconStack, 0);
        Grid.SetColumn(content, 1);
        cardGrid.Children.Add(iconStack);
        cardGrid.Children.Add(content);

        return KuroBbsUi.KuroCard(cardGrid, padding: new Thickness(12, 10, 12, 10));
    }


    
    private static Border BuildDivider()
    {
        Brush brush = Application.Current.Resources.TryGetValue("DividerStrokeColorDefaultBrush", out var value) && value is Brush found
            ? found
            : new SolidColorBrush(Windows.UI.Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));
        return KuroBbsUi.MakeDivider(brush, 1);
    }


    
    private static string BuildPhantomSummary(int? cost, List<KuroPhantom> phantoms)
    {
        string text = $"COST {cost}";
        var sets = phantoms
            .Select(p => p.FetterDetail?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n!)
            .Select(g => $"{g.Key} ×{g.Count()}");
        string setText = string.Join(" · ", sets);
        return string.IsNullOrEmpty(setText) ? text : $"{text} · {setText}";
    }


    private void Button_CloseDetail_Click(object sender, RoutedEventArgs e)
    {
        Grid_Detail.Visibility = Visibility.Collapsed;
        
        if (KuroBbsAccountStore.Load() is { IsValid: true })
        {
            ScrollView_Roles.Visibility = Visibility.Visible;
        }
    }

}



public sealed class KuroRoleItem
{
    public int RoleId { get; init; }
    public ImageSource? IconSource { get; init; }
    public Brush StarBrush { get; init; } = new SolidColorBrush(Microsoft.UI.Colors.Gold);
    public string LevelText { get; init; } = "";
    public string ChainText { get; init; } = "";
    public Visibility ChainVisibility { get; init; }


    public static KuroRoleItem From(KuroRole role)
    {
        ImageSource? icon = null;
        try
        {
            if (!string.IsNullOrEmpty(role.RoleIconUrl))
            {
                icon = new BitmapImage(new Uri(role.RoleIconUrl));
            }
        }
        catch { }
        return new KuroRoleItem
        {
            RoleId = role.RoleId,
            IconSource = icon,
            StarBrush = role.StarLevel >= 5
                ? new SolidColorBrush(Microsoft.UI.Colors.Gold)
                : new SolidColorBrush(Microsoft.UI.Colors.MediumPurple),
            LevelText = $"LV.{role.Level}",
            ChainText = role.ChainUnlockNum.ToString(),
            ChainVisibility = role.ChainUnlockNum > 0 ? Visibility.Visible : Visibility.Collapsed,
        };
    }
}
