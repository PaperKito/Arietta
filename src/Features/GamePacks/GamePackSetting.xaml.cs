using Arietta.Core;
using Arietta.Frameworks;
using Arietta.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.GamePacks;

public sealed partial class GamePackSetting : PageBase
{

    public ObservableCollection<PackRow> Rows { get; } = new();


    public string RootText => GamePackService.Root;


    public Visibility EmptyVisibility => Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private static readonly HashSet<string> _searched = new();


    public GamePackSetting()
    {
        InitializeComponent();
    }


    protected override void OnLoaded()
    {
        _ = ReloadAsync();
    }


    private async Task ReloadAsync()
    {
        GamePackService.Reload();

        var rows = new List<PackRow>();
        foreach (GamePack pack in GamePackService.Packs)
        {
            var row = new PackRow(pack);
            await LoadIconAsync(row);
            rows.Add(row);
        }
        Rows.Clear();
        foreach (PackRow row in rows)
        {
            Rows.Add(row);
        }
        OnPropertyChanged(nameof(EmptyVisibility));

        List<GamePack> missing = GamePackService.Packs
            .Where(p => p.IsValid && p.ExePath is null && p.MarkerDirs.Length > 0 && !_searched.Contains(p.Id))
            .ToList();
        if (missing.Count > 0)
        {
            _ = AutoFindAsync(missing);
        }
    }


    private async Task AutoFindAsync(List<GamePack> packs)
    {
        foreach (GamePack pack in packs)
        {
            _searched.Add(pack.Id);
            GamePack found = pack;
            string? exe = await Task.Run(() => GamePackService.SearchExe(found, null));
            if (!string.IsNullOrEmpty(exe))
            {
                GamePackService.SetExePath(pack, exe);
                AppConfig.GetLogger("GamePack").LogInformation("Auto found exe for {Id}: {Exe}", pack.Id, exe);
            }
        }
        await ReloadAsync();
    }


    private static async Task LoadIconAsync(PackRow row)
    {
        if (!row.Pack.IsValid || string.IsNullOrEmpty(row.Pack.Icon) || !File.Exists(row.Pack.IconPath))
        {
            return;
        }
        try
        {
            var bitmap = new BitmapImage();
            using var stream = File.OpenRead(row.Pack.IconPath);
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            row.Icon = bitmap;
            row.IconVisibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot load pack icon {Path}", row.Pack.IconPath);
        }
    }


    private async void Rescan_Click(object sender, RoutedEventArgs e)
    {
        _searched.Clear();
        await ReloadAsync();
    }


    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(GamePackService.Root);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{GamePackService.Root}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppConfig.GetLogger("GamePack").LogWarning(ex, "Cannot open pack folder");
        }
    }


    private void Enabled_Toggled(object sender, RoutedEventArgs e)
    {

        GamePack? pack = (sender as FrameworkElement)?.DataContext is PackRow row
            ? row.Pack
            : (sender as FrameworkElement)?.Tag as GamePack;
        if (pack is null)
        {
            return;
        }
        bool on = (sender as ToggleSwitch)?.IsOn ?? true;
        if (GamePackService.IsEnabled(pack) == on)
        {
            return;
        }
        GamePackService.SetEnabled(pack, on);
        AppConfig.GetLogger("GamePack").LogInformation("Pack {Id} enabled={On}", pack.Id, on);
    }

}

public sealed class PackRow
{

    public PackRow(GamePack pack)
    {
        Pack = pack;
        Name = pack.IsValid ? pack.DisplayName : (pack.Id.Length > 0 ? pack.Id : pack.Folder);
        Initial = Name.Length > 0 ? Name[..1] : "?";
        Version = pack.IsValid ? $"v{pack.Version ?? "1"}" : $"broken: {pack.LoadError}";
        Author = !pack.IsValid
            ? "-"
            : pack.AuthorVerified
                ? $"{pack.Author}  ·  {Lang.Instance.GamePackPage_AuthorVerified}"
                : $"{Lang.Instance.GamePackPage_AuthorUnsigned}";
        IsEnabled = GamePackService.IsEnabled(pack);
    }

    public GamePack Pack { get; }
    public string Name { get; }
    public string Initial { get; }
    public string Version { get; }
    public string Author { get; }
    public bool IsEnabled { get; }
    public BitmapImage? Icon { get; set; }
    public Visibility IconVisibility { get; set; } = Visibility.Visible;

}
