using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Betrian.CflmNavi.App.Controls;

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
        if (DataContext is FrameworkElement item && CanvasHelper.GetDirectCanvasChild(item) is UIElement canvasChild)
        {
            double deltaVertical;
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Bottom:
                    deltaVertical = Math.Min(-e.VerticalChange, item.Height - item.MinHeight);
                    item.Height -= deltaVertical;
                    break;

                case VerticalAlignment.Top:
                    deltaVertical = Math.Min(e.VerticalChange, item.Height - item.MinHeight);
                    Canvas.SetTop(canvasChild, Canvas.GetTop(canvasChild) + deltaVertical);
                    item.Height -= deltaVertical;
                    break;

                default:
                    break;
            }

            double deltaHorizontal;
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    deltaHorizontal = Math.Min(e.HorizontalChange, item.Width - item.MinWidth);
                    Canvas.SetLeft(canvasChild, Canvas.GetLeft(canvasChild) + deltaHorizontal);
                    item.Width -= deltaHorizontal;
                    break;

                case HorizontalAlignment.Right:
                    deltaHorizontal = Math.Min(-e.HorizontalChange, item.Width - item.MinWidth);
                    item.Width -= deltaHorizontal;
                    break;

                default:
                    break;
            }

            e.Handled = true;
        }
    }
}
