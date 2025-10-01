using System.Windows;
using System.Windows.Input;
using Betrian.CflmNavi.App.Controls;
using Betrian.CflmNavi.App.ViewModels.Modes;
using Betrian.CflmNavi.App.ViewModels.Modes.SEM;
using CFLMnavi.WPF.Controls;

namespace Betrian.CflmNavi.App.Views.Modes.SEM;

public partial class MainArea : ApplicationModeControlBase<ElectronBeamModeViewModel>
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
