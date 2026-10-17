using CommunityToolkit.Mvvm.Input;
using Microsoft.Graphics.Display;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Core;
using Arietta.Frameworks;
using Arietta.Helpers;
using Arietta.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Arietta.Features.GameSetting;

public sealed partial class GameSettingPage : PageBase
{

    public GameSettingPage()
    {
        this.InitializeComponent();
        ViewHost.MainView.GameChanged += MainView_GameChanged;
    }


    protected override void OnLoaded()
    {
        InitializeResolutionItem();
        _ = InitializeGameSettingAsync();
    }


    protected override void OnUnloaded()
    {
        ViewHost.MainView.GameChanged -= MainView_GameChanged;
        DisposeDisplayInformation();
    }


    private void MainView_GameChanged(object? sender, GameBiz biz)
    {
        InitializeResolutionItem();
        _ = InitializeGameSettingAsync();
    }



    public bool IsBaseSettingEnable { get; private set => SetProperty(ref field, value); }

    public bool IsLanguageSettingEnable { get; private set => SetProperty(ref field, value); }

    public bool IsHdrSettingEnable { get; private set => SetProperty(ref field, value); }

    public bool IsApplyButtonEnable { get; private set => SetProperty(ref field, value); }

    public string ErrorMessage { get; set => SetProperty(ref field, value); } = Lang.Instance.GameSettingPage_SettingNotEffect;


    public bool EnableFullScreen
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {

                IsApplyButtonEnable = true;
                UpdateResolutionInputState();
            }
        }
    }


    public bool IsPgrFullscreenBlocked { get; private set => SetProperty(ref field, value); }

    public bool IsResolutionListEnabled { get; private set => SetProperty(ref field, value); }

    public bool IsCustomResInputEnabled { get; private set => SetProperty(ref field, value); }


    public bool EnableCustomResolution
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateResolutionInputState();
            }
        }
    }


    private void UpdateResolutionInputState()
    {
        IsResolutionListEnabled = !EnableFullScreen && !EnableCustomResolution;
        IsCustomResInputEnabled = EnableCustomResolution && !EnableFullScreen;
    }


    public int ResolutionWidth
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public int ResolutionHeight
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public int LanguageIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public bool EnableHDR
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public bool HDRNotSupported { get; private set => SetProperty(ref field, value); }

    public bool HDRNotEnabled { get; private set => SetProperty(ref field, value); }



    private bool _loading;


    private async Task InitializeGameSettingAsync()
    {
        try
        {
            _loading = true;
            IsApplyButtonEnable = false;
            var biz = ViewHost.MainView.CurrentGameBiz;
            var entry = GameRegistry.GetEntry(biz);
            if (entry.CloudUrl is not null || !GameSettingService.IsGameInstalled(biz))
            {

                StackPanel_NotInstalled.Visibility = Visibility.Visible;
                IsBaseSettingEnable = false;
                IsLanguageSettingEnable = false;
                IsHdrSettingEnable = false;
                TextBlock_GameNotInstalled.Text = entry.CloudUrl is not null
                    ? Lang.Instance.GameSettingPage_CloudGameNotSupport
                    : Lang.Instance.GameSettingPage_GameNotInstalled;
                return;
            }
            StackPanel_NotInstalled.Visibility = Visibility.Collapsed;

            bool isPgr = biz.IsPunishingGrayRaven();
            IsBaseSettingEnable = true;
            IsLanguageSettingEnable = !isPgr;
            IsHdrSettingEnable = true;
            IsPgrFullscreenBlocked = isPgr && GameSettingService.IsPgrFullscreenBlocked();
            UpdateResolutionInputState();

            var resolution = isPgr ? GameSettingService.GetPgrResolutionSetting(biz) : GameSettingService.GetGameResolutionSetting(biz);
            if (resolution is not null)
            {
                EnableFullScreen = resolution.IsFullScreen;
                if (resolution.Width * resolution.Height > 0)
                {
                    ResolutionWidth = resolution.Width;
                    ResolutionHeight = resolution.Height;
                    EnableCustomResolution = !UpdateResolutionComboBoxSelection(ResolutionWidth, ResolutionHeight);
                }
                else
                {
                    ComboBox_Resolution.SelectedIndex = 0;
                }
            }
            else
            {
                ComboBox_Resolution.SelectedIndex = 0;
            }

            LanguageIndex = isPgr ? -1 : GameSettingService.GetVoiceLanguage(biz) ?? -1;

            EnableHDR = isPgr ? GameSettingService.GetPgrHdrEnabled(biz) : GameSettingService.GetHdrEnabled(biz);
            if (isPgr)
            {

                DisposeDisplayInformation();
                HDRNotSupported = false;
                HDRNotEnabled = false;
            }
            else
            {
                InitializeDisplayInformation();
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GameSettingPage_LoadFailed);
        }
        finally
        {
            _loading = false;
            IsApplyButtonEnable = false;
        }
    }



    private void InitializeResolutionItem()
    {
        try
        {
            _loading = true;
            ComboBox_Resolution.Items.Clear();

            var list = ViewHost.MainView.CurrentGameBiz.IsPunishingGrayRaven()
                ? GameSettingService.GetPgrAvailableResolutions()
                : GameSettingService.GetAvailableResolutions();
            foreach (var (width, height) in list)
            {
                ComboBox_Resolution.Items.Add(new ComboBoxItem
                {
                    Content = $"{width} × {height}",
                });
            }
        }
        catch (Exception ex)
        {
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GameSettingPage_LoadFailed);
        }
        finally
        {
            _loading = false;
        }
    }

    private static bool TryParseSize(string text, out int width, out int height)
    {
        width = 0;
        height = 0;
        var split = text.Split('×', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return split.Length == 2 && int.TryParse(split[0], out width) && int.TryParse(split[1], out height);
    }


    private bool UpdateResolutionComboBoxSelection(int width, int height)
    {
        foreach (ComboBoxItem item in ComboBox_Resolution.Items)
        {
            if (item.Content is string str && TryParseSize(str, out int _width, out int _height))
            {
                if (width == _width && height == _height)
                {
                    ComboBox_Resolution.SelectedItem = item;
                    return true;
                }
            }
        }
        return false;
    }


    private void ComboBox_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }
        if (e.AddedItems.FirstOrDefault() is ComboBoxItem item && item.Content is string str
            && TryParseSize(str, out int width, out int height))
        {
            ResolutionWidth = width;
            ResolutionHeight = height;
            IsApplyButtonEnable = true;
        }
    }


    [RelayCommand]
    private void ApplySetting()
    {
        try
        {
            var biz = ViewHost.MainView.CurrentGameBiz;
            bool isPgr = biz.IsPunishingGrayRaven();
            if (IsBaseSettingEnable)
            {
                if (ResolutionWidth <= 0 || ResolutionHeight <= 0)
                {

                    ErrorMessage = Lang.Instance.GameSettingPage_ResolutionMustBeGreaterThan0;
                    return;
                }
                if (isPgr)
                {
                    GameSettingService.SetPgrResolutionSetting(biz, EnableFullScreen, false, ResolutionWidth, ResolutionHeight);
                }
                else
                {
                    GameSettingService.SetGameResolutionSetting(biz, EnableFullScreen, false, ResolutionWidth, ResolutionHeight);
                }
            }
            if (!isPgr && IsLanguageSettingEnable && LanguageIndex >= 0)
            {
                GameSettingService.SetVoiceLanguage(biz, LanguageIndex);
            }
            if (IsHdrSettingEnable)
            {
                if (isPgr)
                {
                    GameSettingService.SetPgrHdrEnabled(biz, EnableHDR);
                }
                else
                {
                    GameSettingService.SetHdrEnabled(biz, EnableHDR);
                }
            }
            ErrorMessage = Lang.Instance.GameSettingPage_SettingNotEffect;
            IsApplyButtonEnable = false;
            InAppToast.MainWindow?.Success(Lang.Instance.Common_Apply, Lang.Instance.GameSettingPage_Applied);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            InAppToast.MainWindow?.Error(ex, Lang.Instance.GameSettingPage_ApplyFailed);
        }
    }


    private void Button_Apply_Click(object sender, RoutedEventArgs e)
    {
        ApplySettingCommand.Execute(null);
    }



    private DisplayInformation? _displayInformation;


    private void InitializeDisplayInformation()
    {
        try
        {
            DisposeDisplayInformation();
            _displayInformation = DisplayInformation.CreateForWindowId(WindowEx.MainWindowId);
            _displayInformation.AdvancedColorInfoChanged += DisplayInformation_AdvancedColorInfoChanged;
            UpdateHdrState();
        }
        catch { }
    }


    private void DisposeDisplayInformation()
    {
        if (_displayInformation is not null)
        {
            _displayInformation.AdvancedColorInfoChanged -= DisplayInformation_AdvancedColorInfoChanged;
            _displayInformation.Dispose();
            _displayInformation = null;
        }
    }


    private void DisplayInformation_AdvancedColorInfoChanged(DisplayInformation sender, object args)
    {
        UpdateHdrState();
    }


    private void UpdateHdrState()
    {
        try
        {
            HDRNotEnabled = false;
            HDRNotSupported = false;
            if (_displayInformation is null)
            {
                return;
            }
            var info = _displayInformation.GetAdvancedColorInfo();
            if (!info.IsAdvancedColorKindAvailable(DisplayAdvancedColorKind.HighDynamicRange))
            {
                HDRNotSupported = true;
            }
            else if (info.CurrentAdvancedColorKind is not DisplayAdvancedColorKind.HighDynamicRange)
            {
                HDRNotEnabled = true;
            }
        }
        catch { }
    }

}
