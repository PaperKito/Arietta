using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Arietta.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Arietta.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class GameBizIcon
{

    public GameBiz Biz { get; set; }

    public string GameName { get; set; } = "";

    public string ServerName { get; set; } = "";

    public ImageSource? GameIcon { get; set; }

    public ImageSource? ServerIcon { get; set; }

    public string? GameIconPath { get; set; }

    public string? ServerIconPath { get; set; }

    public string ServerCode { get; set; } = "";

    public bool HasServerCode => ServerCode != "";

    public bool HasServerBadge =>
        HasServerCode
        || (!string.IsNullOrEmpty(ServerIconPath)
            && !string.Equals(ServerIconPath, GameIconPath, StringComparison.OrdinalIgnoreCase));

    public string DisplayName => $"{GameName} {ServerName}".Trim();

    public string GameId => Biz.ToString();

    public bool IsPinned { get => field; set => SetProperty(ref field, value); }

    public bool IsSelected { get => field; set => SetProperty(ref field, value); }

    public double MaskOpacity { get => field; set => SetProperty(ref field, value); } = 1;

    public string? InstallPath { get; set => SetProperty(ref field, value); }

    public long TotalSize { get; set => SetProperty(ref field, value); }

    public string TotalSizeText => TotalSize > 0 ? $"{TotalSize / 1024.0 / 1024 / 1024:F2}GB" : "";



    public static ImageSource? LoadIcon(string iconPath)
    {
        try
        {
            string file = Path.Combine(AppContext.BaseDirectory, "Assets", "Image", "Game", Path.GetFileName(iconPath));
            return new BitmapImage(new Uri(file));
        }
        catch
        {
            return null;
        }
    }

}

public sealed class GameBizDisplay
{
    public string GameName { get; set; } = "";

    public ImageSource? Logo { get; set; }

    public ImageSource? Icon { get; set; }

    public List<GameBizIcon> Servers { get; set; } = [];
}
