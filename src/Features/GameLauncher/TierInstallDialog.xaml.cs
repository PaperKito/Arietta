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
public sealed partial class TierInstallDialog : ContentDialog
{
    public TierInstallDialog(GameBiz biz, string tierName, long tierBytes)
    {
        _biz = biz;
        _tierBytes = tierBytes;
        TitleText = Lang.Instance.LauncherPage_InstallGame;
        MessageText = string.Format(Lang.Instance.GameLauncherSettingDialog_TierDownload,
            GameUpdateService.TierDisplayName(tierName), GameUpdateSession.FormatSize(tierBytes));
        DownloadSizeText = GameUpdateSession.FormatSize(tierBytes);
        InitializeComponent();
        RefreshSpace();
    }

    private readonly GameBiz _biz;
    private readonly long _tierBytes;

    public bool Confirmed { get; private set; }

    public string TitleText { get; }
    public string MessageText { get; }
    public string DownloadSizeText { get; }
    public string AvailableSpaceText { get; private set => SetProperty(ref field, value); } = "--";
    public string? ErrorMessage { get; private set => SetProperty(ref field, value); }
    public bool CanStart => ErrorMessage is null;

    [RelayCommand]
    private void Confirm()
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
                    if (drive.AvailableFreeSpace < _tierBytes)
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
