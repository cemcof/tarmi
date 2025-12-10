using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface IBeamMode : IVirtualDevice, IBeamControllingMode, IInstrumentObserver
{
    Task RestoreImageState(ImageMetadata imageMetadata, CancellationToken cancellation);
    IDisposable UseReducedArea(RatioRectangle rectangle, Duration dwellTime, ImageFilterType imageFilterType = ImageFilterType.None, int frames = 1, int lineIntegration = 1);
    IDisposable UseFullFrameSettings(Duration dwellTime, ImageFilterType imageFilterType = ImageFilterType.None, int frames = 1, int lineIntegration = 1);
}
