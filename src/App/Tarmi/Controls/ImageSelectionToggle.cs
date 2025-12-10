using System.Windows;
using System.Windows.Controls;

namespace Tarmi.App.Controls;

public enum ImageSelection
{
    Unselected,
    Primary,
    Overlay,
    Secondary
}

public class ImageSelectionToggle : Button
{
    public ImageSelection Selection
    {
        get => (ImageSelection)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(nameof(Selection), typeof(ImageSelection), typeof(ImageSelectionToggle), new PropertyMetadata(ImageSelection.Unselected));
}
