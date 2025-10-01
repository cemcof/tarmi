using System.Windows;
using System.Windows.Controls;

using Betrian.WPF.CeitecStyles.Controls;

namespace Betrian.CflmNavi.App.Controls;

public class ImageStackSlider : ScrollSlider
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(ImageStackSlider), new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}
