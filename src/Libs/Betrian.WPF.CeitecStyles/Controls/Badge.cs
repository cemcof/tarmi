using System.Windows;
using System.Windows.Controls;

namespace Betrian.WPF.CeitecStyles.Controls;

public class Badge : Label
{
    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(nameof(Severity), typeof(Severity), typeof(Badge), new PropertyMetadata(Severity.Information));

    public Severity Severity
    {
        get => (Severity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }
}
