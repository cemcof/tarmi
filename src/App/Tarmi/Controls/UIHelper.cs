using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tarmi.App.Controls;

internal static class UIHelper
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

    public static T? FindAncestor<T>(DependencyObject current)
    {
        return GetSelfAndAncestors(current).OfType<T>().FirstOrDefault();
    }

    public static IEnumerable<DependencyObject> GetSelfAndAncestors(this DependencyObject dependencyObject)
    {
        while (dependencyObject != null)
        {
            yield return dependencyObject;

            dependencyObject = VisualTreeHelper.GetParent(dependencyObject) ?? LogicalTreeHelper.GetParent(dependencyObject);
        }
    }

    public static bool TryGetScaleAwareItem(object? child, [NotNullWhen(true)] out IScaleAwareItem? scaleAwareItem)
    {
        if (child is IScaleAwareItem directItem)
        {
            scaleAwareItem = directItem;
            return true;
        }

        if (child is ContentPresenter pres && pres.Content is IScaleAwareItem contentItem)
        {
            scaleAwareItem = contentItem;
            return true;
        }
        scaleAwareItem = null;
        return false;
    }

}



