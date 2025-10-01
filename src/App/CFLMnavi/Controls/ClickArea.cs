using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Betrian.WPF;

namespace Betrian.CflmNavi.App.Controls;

public class ClickArea : Border
{
    public static readonly DependencyProperty ClickProperty = DependencyProperty.Register(nameof(Click), typeof(ICommand), typeof(ClickArea), new PropertyMetadata());
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(nameof(Scale), typeof(double), typeof(ClickArea), new PropertyMetadata(1.0));
    private Point? _mouseDownPosition;

    public ICommand? Click
    {
        get => (ICommand?)GetValue(ClickProperty);
        set => SetValue(ClickProperty, value);
    }

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _mouseDownPosition = NativeHelper.GetMousePosition();
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        double scale = Scale;
        Point globalPosition = NativeHelper.GetMousePosition();
        Point clickPosition = e.GetPosition(this);
        Point scaledClickPosition = new(clickPosition.X / scale, clickPosition.Y / scale);

        if (IsClick(globalPosition) && Click?.CanExecute(scaledClickPosition) is true)
        {
            Click.Execute(scaledClickPosition);
            _mouseDownPosition = null;
            e.Handled = true;
        }
        else
        {
            base.OnMouseLeftButtonDown(e);
        }
    }

    private bool IsClick(Point upPosition)
    {
        if (_mouseDownPosition is Point downPosition)
        {
            return Math.Abs(upPosition.X - downPosition.X) <= SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(upPosition.Y - downPosition.Y) <= SystemParameters.MinimumVerticalDragDistance;
        }
        return false;
    }
}
