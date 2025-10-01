using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Betrian.CflmNavi.App.Controls;

public class RelativeSlider : Thumb
{
    public static readonly DependencyProperty DisplayOffsetProperty = DependencyProperty.Register(nameof(DisplayOffset), typeof(double), typeof(RelativeSlider), new PropertyMetadata(0.0));
    public static readonly DependencyProperty MoveProperty = DependencyProperty.Register(nameof(Move), typeof(ICommand), typeof(RelativeSlider), new PropertyMetadata());

    private double _previousChange;

    public double DisplayOffset
    {
        get => (double)GetValue(DisplayOffsetProperty);
        set => SetValue(DisplayOffsetProperty, value);
    }

    public ICommand Move
    {
        get => (ICommand)GetValue(MoveProperty);
        set => SetValue(MoveProperty, value);
    }

    public RelativeSlider()
    {
        DragDelta += OnDragDelta;
        DragStarted += OnDragStarted;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e) => OnMove(e.Delta > 0 ? 1 : -1);

    private void OnDragStarted(object sender, DragStartedEventArgs e)
    {
        _previousChange = 0;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (e.VerticalChange != 0)
        {
            double change = e.VerticalChange - _previousChange;
            _previousChange = e.VerticalChange;
            OnMove(-change);
        }
    }

    private void OnMove(double change)
    {
        if (Move?.CanExecute(change) is true)
        {
            DisplayOffset -= change / ActualHeight;
            Move.Execute(change);
        }
    }
}
