using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;

namespace Betrian.CflmNavi.App.Controls;

public class MoveThumb : Thumb
{
    public ICommand MoveFinished
    {
        get => (ICommand)GetValue(MoveFinishedProperty);
        set => SetValue(MoveFinishedProperty, value);
    }

    public static readonly DependencyProperty MoveFinishedProperty = DependencyProperty.Register(nameof(MoveFinished), typeof(ICommand), typeof(MoveThumb), new PropertyMetadata());

    public MoveThumb()
    {
        DragDelta += MoveThumb_DragDelta;
        DragCompleted += MoveThumb_DragCompleted;
    }

    private void MoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (MoveFinished != null && MoveFinished.CanExecute(null))
        {
            MoveFinished.Execute(null);
        }
    }

    private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is DependencyObject item && CanvasHelper.GetDirectCanvasChild(item) is UIElement canvasChild)
        {
            Canvas.SetLeft(canvasChild, Canvas.GetLeft(canvasChild) + e.HorizontalChange);
            Canvas.SetTop(canvasChild, Canvas.GetTop(canvasChild) + e.VerticalChange);
        }
    }
}

