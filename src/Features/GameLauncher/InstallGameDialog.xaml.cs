using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Arietta.Features.GameLauncher;








[ObservableObject]
public sealed partial class InstallGameDialog : ContentDialog
{
    
    public delegate Task<GameUpdate.GameUpdateService.DownloadPlan?> TierPlanFactory(string tierName);

    public InstallGameDialog(GameBiz biz, GameUpdate.GameUpdateService.DownloadPlan plan, long totalBytes,
        List<GameUpdate.GameUpdateService.GameTierOption>? tiers, TierPlanFactory? tierPlanFactory, string? initialPath)
    {
        _biz = biz;
        _plan = plan;
        TotalBytes = totalBytes;
        InstallationPath = initialPath ?? "";
        
        HardLinkVisibility = AppConfig.GetConfig(GameUpdateService.UseHardLinkConfigKey) == "1"
            ? Visibility.Visible
            : Visibility.Collapsed;
        _tierPlanFactory = tierPlanFactory;
        TierVisibility = tiers is { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        InitializeComponent();
        if (tiers is { Count: > 0 })
        {
            
            
            foreach (var tier in tiers)
            {
                _tierSizes[tier.Name] = tier.SizeBytes;
                Segmented_Tier.Items.Add(new SegmentedItem { Content = TierLabel(tier.Name), Tag = tier.Name });
            }
            SelectedTier = tiers.FirstOrDefault(t => t.Name.Equals("HD", StringComparison.OrdinalIgnoreCase))?.Name
                ?? tiers[0].Name;
            Segmented_Tier.SelectedIndex = tiers.FindIndex(t => t.Name.Equals(SelectedTier, StringComparison.OrdinalIgnoreCase));
            Segmented_Tier.SelectionChanged += Segmented_Tier_SelectionChanged;
            _ = ApplyTierAsync();   
        }
        RefreshSpace();
    }

    private readonly GameBiz _biz;
    private GameUpdate.GameUpdateService.DownloadPlan _plan;
    private readonly TierPlanFactory? _tierPlanFactory;

    
    private readonly Dictionary<string, long> _tierSizes = new();

    
    private int _tierGen;

    
    private long _linkedBytes;

    private CancellationTokenSource? _estimateCts;

    
    public bool Confirmed { get; private set; }

    
    public string? SelectedTier { get; private set; }

    public Visibility TierVisibility { get; private set; }

    
    public long TotalBytes { get; private set; }

    
    
    
    
    public string RequiredBytesText { get; private set => SetProperty(ref field, value); } = "";

    
    public string? HardLinkEstimateText { get; private set => SetProperty(ref field, value); }

    public Visibility EstimateInfoVisibility { get; private set => SetProperty(ref field, value); }

    public string InstallationPath { get => field; set { SetProperty(ref field, value); RefreshSpace(); _ = RecomputeEstimateAsync(); } }

    public string AvailableSpaceText { get; private set => SetProperty(ref field, value); } = "--";

    public bool CanStart => ErrorMessage is null;

    
    public string? ErrorMessage { get; private set => SetProperty(ref field, value); }

    public Visibility HardLinkVisibility { get; private set; }

    [RelayCommand]
    private async Task ChangeInstallationPathAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker,
            WinRT.Interop.WindowNative.GetWindowHandle(ViewHost.MainWindow.Current));
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            InstallationPath = folder.Path;
        }
    }

    [RelayCommand]
    private void StartInstallation()
    {
        Confirmed = true;
        Hide();
    }

    [RelayCommand]
    private void Close()
    {
        Hide();
    }

    
    private void RefreshSpace()
    {
        string? error = null;
        string available = "--";
        if (!string.IsNullOrWhiteSpace(InstallationPath))
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(InstallationPath))!);
                if (drive.IsReady)
                {
                    available = GameUpdateSession.FormatSize(drive.AvailableFreeSpace);
                    long required = TotalBytes - _linkedBytes;
                    if (drive.AvailableFreeSpace < required)
                    {
                        error = Lang.Instance.InstallGameDialog_InsufficientSpace;
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }
        else
        {
            error = Lang.Instance.InstallGameDialog_SelectInstallationPath;
        }
        AvailableSpaceText = available;
        
        bool estimated = _linkedBytes > 0;
        long spaceNeeded = estimated ? TotalBytes - _linkedBytes : TotalBytes;
        RequiredBytesText = GameUpdateSession.FormatSize(spaceNeeded);
        HardLinkEstimateText = estimated
            ? string.Format(Lang.Instance.InstallGameDialog_HardLinkEstimateDesc,
                GameUpdateSession.FormatSize(TotalBytes - _linkedBytes))
            : null;
        EstimateInfoVisibility = estimated ? Visibility.Visible : Visibility.Collapsed;
        ErrorMessage = error;
        
        OnPropertyChanged(nameof(CanStart));
    }

    
    
    
    
    private async Task RecomputeEstimateAsync()
    {
        _estimateCts?.Cancel();
        var cts = _estimateCts = new CancellationTokenSource();
        _linkedBytes = 0;
        RefreshSpace();
        string path = InstallationPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            var progress = new Progress<long>(v =>
            {
                if (!cts.IsCancellationRequested)
                {
                    _linkedBytes = v;
                    RefreshSpace();
                }
            });
            try
            {
                await Task.Run(() => GameUpdateService.EstimateHardLinkBytes(_biz, _plan, path, progress, cts.Token), cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs e)
    {
    }

    
    private static string TierLabel(string name) => name.ToUpperInvariant() switch
    {
        "UHD" => Lang.Instance.InstallGameDialog_TierUHD,
        "HD" => Lang.Instance.InstallGameDialog_TierHD,
        "SD" => Lang.Instance.InstallGameDialog_TierSD,
        _ => name,
    };

    
    
    
    
    
    private async Task ApplyTierAsync()
    {
        if (Segmented_Tier.SelectedItem is not SegmentedItem { Tag: string name })
        {
            return;
        }
        SelectedTier = name;
        int gen = ++_tierGen;
        if (_tierSizes.TryGetValue(name, out var size) && size > 0)
        {
            TotalBytes = size;
            RefreshSpace();
        }
        if (_tierPlanFactory is null)
        {
            return;
        }
        var plan = await _tierPlanFactory(name);
        if (gen != _tierGen || plan is null)
        {
            return;
        }
        _plan = plan;
        TotalBytes = plan.TotalBytes;
        RefreshSpace();
        await RecomputeEstimateAsync();
    }

    private async void Segmented_Tier_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await ApplyTierAsync();
    }
}
