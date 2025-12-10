using Fei.Imaging.gen;
using Fei.XT.Imaging.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;
internal class ImageStreamEvents : ICompositeImageEvents
{
    private readonly Action _frameCompletedAction;

    public ImageStreamEvents(Action frameCompletedAction)
    {
        _frameCompletedAction = frameCompletedAction;
    }

    public void OnConnect(CompositeImage Server, int ConnectionID)
    {
    }

    public void OnDisconnect(CompositeImage Server, int ConnectionID)
    {
    }

    public void OnStatusChanged(int Active, CompositeImage Server, int ConnectionID)
    {
    }

    public void OnParameterChanged(CompositeImage Server, int ConnectionID)
    {
    }

    public void OnImageUpdate(ref tagRECT UpdateRectangle, int RawImageIndex, int FrameNumber, double TimeStamp, int EndOfFrameEvent, int ConnectionID)
    {
        if (EndOfFrameEvent != 0)
        {
            _frameCompletedAction();
        }
    }
}
