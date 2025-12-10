using Tarmi.Devices.Thermofisher.Instrument.Types;

namespace Tarmi.VirtualDevices;

public interface IInstrumentObserver
{
    BeamState CurrentBeamState { get; }
    DetectorState CurrentDetectorState { get; }
    ImageFilterState CurrentImageFilterState { get; }
    IObservable<BeamState> Beam { get; }
    IObservable<DetectorState> Detector { get; }
    IObservable<ImageFilterState> ImageFilter { get; }
}
