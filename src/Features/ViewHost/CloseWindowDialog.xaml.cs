using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Localization;

namespace Arietta.Features.ViewHost;

public sealed partial class CloseWindowDialog : ContentDialog
{

    public bool Confirmed { get; private set; }

    public bool ExitOnConfirm { get; private set; }

    public CloseWindowDialog(bool repairExit = false, bool repairConfirm = false)
    {
        InitializeComponent();

        Title = Lang.Instance.CloseDialog_Title;
        Text_Content.Text = Lang.Instance.CloseDialog_Content;
        PrimaryButtonText = Lang.Instance.Common_Confirm;
        CloseButtonText = Lang.Instance.Common_Cancel;
        if (repairConfirm)
        {
            Title = Lang.Instance.RepairConfirmDialog_Title;
            Text_Content.Text = Lang.Instance.RepairConfirmDialog_Content;
            PrimaryButtonText = Lang.Instance.RepairConfirmDialog_Confirm;
            RadioGroup.Visibility = Visibility.Collapsed;
        }
        else if (repairExit)
        {
            Title = Lang.Instance.RepairExitDialog_Title;
            Text_Content.Text = Lang.Instance.RepairExitDialog_Content;
            PrimaryButtonText = Lang.Instance.RepairExitDialog_Confirm;
            RadioGroup.Visibility = Visibility.Collapsed;
        }
        PrimaryButtonClick += CloseWindowDialog_PrimaryButtonClick;
        CloseButtonClick += CloseWindowDialog_CloseButtonClick;
    }

    private void CloseWindowDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Confirmed = true;
        ExitOnConfirm = RadioGroup.SelectedIndex == 1;
    }


    private void CloseWindowDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Confirmed = false;
    }

}
