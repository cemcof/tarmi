using System.Security.Cryptography.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Tarmi.App.Controls;

public class ResizeThumb : Thumb
{
    public ICommand ResizeFinished
    {
        get => (ICommand)GetValue(ResizeFinishedProperty);
        set => SetValue(ResizeFinishedProperty, value);
    }

    public static readonly DependencyProperty ResizeFinishedProperty = DependencyProperty.Register(nameof(ResizeFinished), typeof(ICommand), typeof(ResizeThumb), new PropertyMetadata());

    public ResizeThumb()
    {
        DragDelta += ResizeThumb_DragDelta;
        DragCompleted += ResizeThumb_DragCompleted;

    }

    private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (ResizeFinished != null && ResizeFinished.CanExecute(null))
        {
            ResizeFinished.Execute(null);
        }
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is DependencyObject item && UIHelper.FindAncestor<TransformCanvas>(item) is TransformCanvas canvas && UIHelper.TryGetScaleAwareItem(UIHelper.GetDirectCanvasChild(item), out IScaleAwareItem? scaleAwareItem))
        {
            double offsetHorizontal = 0;
            double offsetVertical = 0;
            double deltaHorizontal = 0;
            double deltaVertical = 0;
            double deltaX = e.HorizontalChange;
            double deltaY = e.VerticalChange;



            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    deltaHorizontal = -deltaX;
                    offsetHorizontal = deltaX;
                    break;

                case HorizontalAlignment.Right:
                    deltaHorizontal = deltaX;
                    break;

                default:
                    break;
            }

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Bottom:
                    deltaVertical = deltaY;
                    break;

                case VerticalAlignment.Top:
                    deltaVertical = -deltaY;
                    offsetVertical = deltaY;
                    break;

                default:
                    break;
            }
            scaleAwareItem.Move(offsetHorizontal, offsetVertical);
            scaleAwareItem.Resize(deltaHorizontal, deltaVertical);
            canvas.InvalidateMeasure();
            
            e.Handled = true;
        }
    }
}
