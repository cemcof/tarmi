using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tarmi.WPF.CeitecStyles.Controls;

public class ChannelIndicator : Control
{
    public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(ChannelIndicator), new PropertyMetadata(20.0));
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(Brush), typeof(ChannelIndicator), new PropertyMetadata());

    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    public Brush Color
    {
        get => (Brush)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
