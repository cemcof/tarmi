using Betrian.Devices.Thermofisher.Instrument.Types;

namespace CFLMnavi.VirtualDevices;

public interface IInstrumentObserver
{
    BeamState CurrentBeamState { get; }
    DetectorState CurrentDetectorState { get; }
    ImageFilterState CurrentImageFilterState { get; }
    IObservable<BeamState> Beam { get; }
    IObservable<DetectorState> Detector { get; }
    IObservable<ImageFilterState> ImageFilter { get; }
}
