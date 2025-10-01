using Betrian.Devices.Thermofisher.Instrument.Types;

namespace CFLMnavi.VirtualDevices;

public interface IInstrumentStageObserver
{
    StageState StageState { get; }
    IObservable<StageState> Stage { get; }
}
