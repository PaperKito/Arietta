using Arietta.Core;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Arietta.Features.GameLauncher;

public sealed class SpaceCheckConfig
{
    public double AvailableGB { get; set; } = 59.49;

    public double DownloadGB { get; set; } = 62.523;

    public double UnzipGB { get; set; } = 8.344;

    public bool ForceShow { get; set; } = true;

    public double RequiredGB => DownloadGB + UnzipGB;

    public bool Enough => AvailableGB >= RequiredGB;
}

public sealed partial class SpaceCheckDialog : ContentDialog
{

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly bool _realMode;
    private readonly long _downloadBytes;
    private readonly long _requiredBytes;
    private readonly string _gameDir = "";

    public bool SpaceSatisfied { get; private set; }

    public SpaceCheckDialog()
    {
        InitializeComponent();
        Render(LoadConfig());
    }

    public SpaceCheckDialog(long downloadBytes, long requiredBytes, string gameDir)
    {
        _realMode = true;
        _downloadBytes = downloadBytes;
        _requiredBytes = requiredBytes;
        _gameDir = gameDir;
        InitializeComponent();
        RenderBytes(downloadBytes, requiredBytes, ReadAvailableBytes());
    }

    public static async Task<bool> EnsureSpaceAsync(GameBiz biz, GameUpdateService.GameUpdateState state, XamlRoot xamlRoot)
    {
        string? gameDir = GameUpdateService.GetGameDir(biz);
        if (gameDir is null)
        {
            return true;
        }
        long required = state.RequiredDiskSpace > 0 ? state.RequiredDiskSpace : state.TotalBytes;

        long staged = Math.Min(required, StagedBytesOnDisk(gameDir));
        required -= staged;
        long? available = ReadAvailableBytes(gameDir);
        if (available is null || available >= required)
        {

            return true;
        }
        var dialog = new SpaceCheckDialog(Math.Max(0, state.TotalBytes - staged), required, gameDir)
        {
            XamlRoot = xamlRoot,
        };
        await dialog.ShowAsync();
        return dialog.SpaceSatisfied;
    }

    private static long StagedBytesOnDisk(string gameDir)
    {
        try
        {
            string root = Path.Combine(gameDir, "predownload");
            if (!Directory.Exists(root))
            {
                return 0;
            }
            long total = 0;
            foreach (FileInfo file in new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories))
            {
                total += file.Length;
            }
            return total;
        }
        catch (Exception)
        {

            return 0;
        }
    }

    public static bool HasSimConfig()
    {
        string baseDir = AppContext.BaseDirectory;
        return File.Exists(Path.Combine(baseDir, "space-sim.json"))
            || File.Exists(Path.GetFullPath(Path.Combine(baseDir, "..", "sim.json")));
    }

    public static SpaceCheckConfig LoadConfig()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDir, "space-sim.json"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "sim.json")),
        ];
        foreach (string path in candidates)
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                var cfg = JsonSerializer.Deserialize<SpaceCheckConfig>(File.ReadAllText(path), JsonOptions);
                if (cfg is not null)
                {
                    return cfg;
                }
            }
            catch (Exception ex)
            {
                AppConfig.GetLogger<SpaceCheckDialog>().LogWarning(ex, "Failed to read space check config {Path}", path);
            }
        }
        return new SpaceCheckConfig();
    }


    private static long Bytes(double gb) => (long)(gb * 1024L * 1024L * 1024L);


    private static long? ReadAvailableBytes(string gameDir)
    {
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(gameDir));
            if (root is null)
            {
                return null;
            }
            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.AvailableFreeSpace : null;
        }
        catch (Exception)
        {
            return null;
        }
    }


    private long? ReadAvailableBytes() => ReadAvailableBytes(_gameDir);


    private void Render(SpaceCheckConfig cfg)
        => RenderBytes(Bytes(cfg.DownloadGB), Bytes(cfg.RequiredGB), Bytes(cfg.AvailableGB));


    private void RenderBytes(long download, long required, long? available)
    {
        Text_Download.Text = GameUpdateSession.FormatSize(download);

        Text_Required.Text = GameUpdateSession.FormatSize(required);
        Text_Available.Text = available is long a ? GameUpdateSession.FormatSize(a) : "--";
        long shortBytes = Math.Max(0, required - (available ?? 0));
        Text_Error.Text = available >= required
            ? Lang.Instance.SpaceCheck_Enough
            : string.Format(Lang.Instance.SpaceCheck_Short, GameUpdateSession.FormatSize(shortBytes));
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        if (_realMode)
        {
            long? available = ReadAvailableBytes();
            if (available >= _requiredBytes)
            {
                SpaceSatisfied = true;
                Hide();
                return;
            }
            RenderBytes(_downloadBytes, _requiredBytes, available);
            return;
        }
        var cfg = LoadConfig();
        if (cfg.Enough)
        {
            Hide();
            return;
        }
        Render(cfg);
    }


    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

}
