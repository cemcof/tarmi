using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tarmi.App.Controls;

public class TransformCanvas : Canvas
{
    public static readonly DependencyProperty TransformProperty = DependencyProperty.Register(nameof(Transform), typeof(Matrix), typeof(TransformCanvas), new FrameworkPropertyMetadata(Matrix.Identity, FrameworkPropertyMetadataOptions.AffectsMeasure, TransformChanged));

    public Matrix Transform
    {
        get => (Matrix)GetValue(TransformProperty);
        set => SetValue(TransformProperty, value);
    }

    private static void TransformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TransformCanvas canvas)
        {
            canvas.InvalidateMeasure();
        }
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        double scale = Transform.M11;
        Point offset = UIHelper.FindAncestor<ImageControl>(this)?.GetOffset(this) ?? new Point();

        foreach (UIElement child in InternalChildren)
        {
            if (child == null) { continue; }

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

            Point position = new(x * scale + offset.X, y * scale + offset.Y);
            child.Arrange(new Rect(position, child.DesiredSize));
        }
        return arrangeSize;
    }

    protected override Size MeasureOverride(Size constraint)
    {
        double scale = Transform.M11;
        foreach (UIElement child in InternalChildren)
        {
            if (child == null) { continue; }

            if (child is IScaleAwareItem scaleAwareChild)
            {
                scaleAwareChild.Scale(scale);
            }

            if (child is ContentPresenter pres && pres.Content is IScaleAwareItem scaleAwareItem)
            {
                scaleAwareItem.Scale(scale);
            }
        }
        return base.MeasureOverride(constraint);
    }
}
