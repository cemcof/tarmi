using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace Tarmi.WPF.CeitecStyles.Controls;

public class ThreeDotsMenu : ToggleButton
{
    public static readonly DependencyProperty DropDownProperty = DependencyProperty.Register(nameof(DropDown), typeof(ContextMenu), typeof(ThreeDotsMenu), new UIPropertyMetadata());

    public ContextMenu DropDown
    {
        get => (ContextMenu)GetValue(DropDownProperty);
        set => SetValue(DropDownProperty, value);
    }

    public ThreeDotsMenu()
    {
        Binding binding = new("DropDown.IsOpen")
        {
            Source = this,
            Mode = BindingMode.OneWay
        };
        SetBinding(IsCheckedProperty, binding);
    }

    protected override void OnClick()
    {
        if (DropDown != null)
        {
            DropDown.PlacementTarget = this;
            DropDown.Placement = PlacementMode.Bottom;
            DropDown.MinWidth = ActualWidth;
            DropDown.IsOpen = true;
        }
    }
}
