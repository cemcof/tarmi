using System.Windows;
using Fluxera.Extensions.Hosting;

namespace Tarmi.App;

public class WpfApplicationInitializer : IWpfApplicationInitializer
{
    public void Initialize(Application application)
    {
        // Allows the entry of decimal numbers to TextBoxes with binding source update set to PropertyChanged
        FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;

        _ = application.AddResourceDictionary(new Uri("pack://application:,,,/Tarmi.WPF.CeitecStyles;component/CeitecTheme.xaml", UriKind.RelativeOrAbsolute))
            .AddResourceDictionary(new Uri("Resources/Styles/Resources.xaml", UriKind.RelativeOrAbsolute));

        FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
        {
            DefaultValue = application.FindResource(typeof(Window))
        });
    }
}
