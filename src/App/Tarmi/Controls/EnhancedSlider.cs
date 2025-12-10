using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Tarmi.App.Controls;

public class EnhancedSlider : Slider
{
    private bool _isDragging;

    public static readonly DependencyProperty SetValueCommandProperty = DependencyProperty.Register(nameof(SetValueCommand), typeof(ICommand), typeof(EnhancedSlider), new(SetValueCommandChanged));

    public ICommand SetValueCommand
    {
        get => (ICommand)GetValue(SetValueCommandProperty);
        set => SetValue(SetValueCommandProperty, value);
    }

    protected override void OnThumbDragStarted(DragStartedEventArgs e) => _isDragging = true;

    protected override void OnThumbDragCompleted(DragCompletedEventArgs e)
    {
        _isDragging = false;
        ExecuteSetCommand();
    }

    protected override void OnValueChanged(double oldValue, double newValue) => HandleValueChanged();

    protected virtual void HandleValueChanged() => ExecuteSetCommand();

    private static void SetValueCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnhancedSlider slider)
        {
            if (e.OldValue is ICommand command)
            {
                command.CanExecuteChanged -= slider.SetValueCommand_CanExecuteChanged;
            }
            if (e.NewValue is ICommand newCommand)
            {
                newCommand.CanExecuteChanged += slider.SetValueCommand_CanExecuteChanged;
            }
        }
    }

    protected virtual void ExecuteSetCommand()
    {
        if (!_isDragging && SetValueCommand is not null && SetValueCommand.CanExecute(null))
        {
            SetValueCommand.Execute(null);
        }
    }

    private void SetValueCommand_CanExecuteChanged(object? sender, EventArgs e) => IsEnabled = sender is not ICommand command || command.CanExecute(null);
}
