using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Arietta.Frameworks;

[ObservableObject]
public abstract partial class PageBase : Page
{

    public PageBase()
    {
        Loaded += PageEx_Loaded;
        Unloaded += PageEx_Unloaded;
    }


    private void PageEx_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OnLoaded();
    }


    private void PageEx_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= PageEx_Loaded;
        Unloaded -= PageEx_Unloaded;
        OnUnloaded();
    }


    protected virtual void OnLoaded()
    {

    }


    protected virtual void OnUnloaded()
    {

    }

}
