using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text.Json;

namespace Arietta.Features.GameLauncher;

public sealed class SpaceCheckConfig
{
    public double AvailableGB { get; set; } = 59.49;
    public double DownloadGB { get; set; } = 62.52;
    public double RequiredGB { get; set; } = 70.87;

    public bool ForceShow { get; set; } = true;

    public bool Enough => AvailableGB >= RequiredGB;
}

public sealed partial class SpaceCheckDialog : ContentDialog
{

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };


    public SpaceCheckDialog()
    {
        InitializeComponent();
        Render(LoadConfig());
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


    private void Render(SpaceCheckConfig cfg)
    {
        Run_Required.Text = GameUpdateSession.FormatSize(Bytes(cfg.RequiredGB));
        Run_Available.Text = GameUpdateSession.FormatSize(Bytes(cfg.AvailableGB));
        double shortGB = Math.Max(0, cfg.RequiredGB - cfg.AvailableGB);
        Text_Error.Text = cfg.Enough
            ? Lang.Instance.SpaceCheck_Enough
            : string.Format(Lang.Instance.SpaceCheck_Short, GameUpdateSession.FormatSize(Bytes(shortGB)));
    }

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
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
