using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tarmi.App.Controls;

internal static class CanvasHelper
{
    public static DependencyObject? GetDirectCanvasChild(DependencyObject? element)
    {
        if (element == null)
        {
            return null;
        }

        DependencyObject? parent = VisualTreeHelper.GetParent(element);
        if (parent is Canvas)
        {
            return element;
        }
        else
        {
            return GetDirectCanvasChild(parent);
        }
    }
}

