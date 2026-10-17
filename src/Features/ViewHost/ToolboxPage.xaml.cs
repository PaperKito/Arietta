using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Arietta.Frameworks;

namespace Arietta.Features.ViewHost;

public sealed partial class ToolboxPage : PageBase
{

    public ToolboxPage()
    {
        this.InitializeComponent();
    }


    private void Button_AdbTool_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Frame?.Navigate(typeof(AdbToolPage));
        }
        catch
        {

        }
    }

}
