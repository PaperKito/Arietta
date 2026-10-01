using Arietta.Frameworks;

namespace Arietta.Features.Setting;

public sealed partial class AboutSetting : PageBase
{

    public string AppVersionText => $"v{AppConfig.AppVersion}";


    public AboutSetting()
    {
        this.InitializeComponent();
    }

}
