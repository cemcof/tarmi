using System.Windows.Input;
using Tarmi.App.Controls;
using Tarmi.App.ViewModels.Modes;
using Tarmi.App.ViewModels.Modes.LM;
using Tarmi.App.WPF.Controls;

namespace Tarmi.App.Views.Modes.LM;

public partial class MainArea : ApplicationModeControlBase<LuminescenceModeViewModel>
{
    public MainArea()
    {
        InitializeComponent();
    }

    private void ImageViewerWithScaleBar_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ImageViewerWithScaleBar viewer && viewer.DataContext is VirtualDeviceViewModel viewModel)
        {
            if (viewModel.ManualFocusEnabled)
            {
                ForwardManualFocusEvent(e);
            }

            if (viewModel.ManualTiltEnabled)
            {
                ForwardManualTiltEvent(e);
            }
        }
    }

    private void ForwardManualFocusEvent(MouseWheelEventArgs originalEvent) => relativeSliderManualFocus.RaiseEvent(CreateRedirectedEvent(originalEvent));

    private void ForwardManualTiltEvent(MouseWheelEventArgs originalEvent) => relativeSliderManualTilt.RaiseEvent(CreateRedirectedEvent(originalEvent));

    private static MouseWheelEventArgs CreateRedirectedEvent(MouseWheelEventArgs e)
    {
        e.Handled = true;
        return new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent
        };
    }
}
