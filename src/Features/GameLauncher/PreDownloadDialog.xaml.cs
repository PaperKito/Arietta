using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Features.GameLauncher.GameUpdate;
using Arietta.Localization;
using System;
using System.IO;

namespace Arietta.Features.GameLauncher;







[ObservableObject]
public sealed partial class PreDownloadDialog : ContentDialog
{
    public PreDownloadDialog(GameBiz biz, GameUpdateService.GameUpdateState state)
    {
        _biz = biz;
        _totalBytes = state.TotalBytes;
        _stagedBytes = state.PredownloadedBytes;
        TitleText = string.Format(Lang.Instance.GameUpdate_PredownloadTitle, state.TargetVersion);
        PackageSizeText = GameUpdateSession.FormatSize(state.TotalBytes);
        PredownloadedSizeText = GameUpdateSession.FormatSize(_stagedBytes);
        StartButtonText = state.HasStagedProgress
            ? Lang.Instance.GameUpdate_ContinuePredownload
            : Lang.Instance.GameUpdate_PredownloadButton;
        InitializeComponent();
        RefreshSpace();
    }

    private readonly GameBiz _biz;
    private readonly long _totalBytes;
    private readonly long _stagedBytes;

    
    public bool Confirmed { get; private set; }

    public string TitleText { get; }
    public string PackageSizeText { get; }
    public string PredownloadedSizeText { get; }
    public string StartButtonText { get; }
    public Visibility StagedVisible => _stagedBytes > 0 ? Visibility.Visible : Visibility.Collapsed;
    public string AvailableSpaceText { get; private set => SetProperty(ref field, value); } = "--";
    public string? ErrorMessage { get; private set => SetProperty(ref field, value); }
    public bool CanStart => ErrorMessage is null;

    [RelayCommand]
    private void StartPredownload()
    {
        Confirmed = true;
        Hide();
    }

    [RelayCommand]
    private void Close() => Hide();

    
    
    
    
    private void RefreshSpace()
    {
        string? error = null;
        string available = "--";
        string? gameDir = GameUpdateService.GetGameDir(_biz);
        if (gameDir is not null)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(gameDir))!);
                if (drive.IsReady)
                {
                    available = GameUpdateSession.FormatSize(drive.AvailableFreeSpace);
                    long remaining = Math.Max(0, _totalBytes - _stagedBytes);
                    if (drive.AvailableFreeSpace < remaining)
                    {
                        error = Lang.Instance.InstallGameDialog_InsufficientSpace;
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }
        AvailableSpaceText = available;
        ErrorMessage = error;
        OnPropertyChanged(nameof(CanStart));
    }
}
