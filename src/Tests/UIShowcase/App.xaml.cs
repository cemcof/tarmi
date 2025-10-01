using System.Configuration;
using System.Data;
using System.Windows;

namespace UIShowcase;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
        {
            DefaultValue = Application.Current.FindResource(typeof(Window))
        });
    }
}

