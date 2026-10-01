using Microsoft.UI.Xaml.Navigation;
using Arietta.Frameworks;
using Arietta.Localization;

namespace Arietta.Features.KuroBbs;

public sealed partial class KuroBbsBlankPage : PageBase
{

    public KuroBbsBlankPage()
    {
        this.InitializeComponent();
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is KuroBbsFeature feature)
        {
            Icon_Placeholder.Glyph = feature.Glyph;
            TextBlock_Title.Text = string.Format(Lang.Instance.KuroBbs_ComingSoon, feature.Title);
        }
    }

}
