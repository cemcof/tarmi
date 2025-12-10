using System.Windows;
using System.Windows.Controls;

namespace Tarmi.App.Controls;

public class ScaledCanvas : Canvas
{
    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ScaledCanvas),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ScaleChanged));

    private static void ScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScaledCanvas scaledCanvas)
        {
            scaledCanvas.InvalidateArrange();
        }
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        double scale = Scale;
        foreach (UIElement child in InternalChildren)
        {
            if (child == null)
            {
                continue;
            }

            double x = 0;
            double y = 0;
            
            double left = GetLeft(child);
            if (!double.IsNaN(left))
            {
                x = left;
            }
            else
            {
                double right = GetRight(child);

                if (!double.IsNaN(right))
                {
                    x = arrangeSize.Width - child.DesiredSize.Width - right;
                }
            }

            double top = GetTop(child);
            if (!double.IsNaN(top))
            {
                y = top;
            }
            else
            {
                double bottom = GetBottom(child);

                if (!double.IsNaN(bottom))
                {
                    y = arrangeSize.Height - child.DesiredSize.Height - bottom;
                }
            }

            x *= scale;
            y *= scale;

            child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
        }
        return arrangeSize;
    }

}
