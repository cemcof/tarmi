using System.Windows;
using Tarmi.WPF;
using Tarmi.WPF.CeitecStyles.Controls;
using Tarmi.App.WPF;
using Tarmi.App.WPF.Controls;

namespace Tarmi.App.Controls;

public class ModeSelector : ContentSelector
{
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(ApplicationMode), typeof(ModeSelector), new PropertyMetadata(OnModeChanged));

    private readonly SemaphoreSlim _modeChangeSemaphore = new(1, 1);

    public ApplicationMode Mode
    {
        get => (ApplicationMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public override async void OnApplyTemplate()
    {
        await ChangeModeAsync(ApplicationMode.Viewer, Mode);
        base.OnApplyTemplate();
    }

    private static async void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ModeSelector modeSelector && e.NewValue is ApplicationMode nextMode)
        {
            ApplicationMode prevMode = (e.OldValue is ApplicationMode om) ? om : ApplicationMode.Viewer;

            await modeSelector.ChangeModeAsync(prevMode, nextMode);
        }
    }

    private async Task ChangeModeAsync(ApplicationMode prevMode, ApplicationMode nextMode)
    {
        using var guard = await _modeChangeSemaphore.UseOnceAsync();


        if (SelectedItem is IApplicationModeControlBase oldApplicationModeControl)
        {
            await oldApplicationModeControl.OnDeactivated(nextMode);
        }
        else if (SelectedItem is IControlBase oldControl)
        {
            await oldControl.OnDeactivated();
        }

        SelectedItem = Items.OfType<FrameworkElement>().FirstOrDefault(x => x.Tag is ApplicationMode applicationMode && applicationMode == nextMode);

        if (SelectedItem is IApplicationModeControlBase newApplicationModeControl)
        {
            await newApplicationModeControl.OnActivated(prevMode);
        }
        else if (SelectedItem is IControlBase newControl)
        {
            await newControl.OnActivated();
        }
    }
}
