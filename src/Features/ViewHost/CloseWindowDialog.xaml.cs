using Microsoft.UI.Xaml.Controls;

namespace Arietta.Features.ViewHost;

public sealed partial class CloseWindowDialog : ContentDialog
{

    public bool Confirmed { get; private set; }

    public bool ExitOnConfirm { get; private set; }


    public CloseWindowDialog()
    {
        InitializeComponent();
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
