using Tarmi.Devices.Thermofisher.Instrument.Types;

namespace Tarmi.VirtualDevices;

public interface IInstrumentStageObserver
{
    StageState StageState { get; }
    IObservable<StageState> Stage { get; }
}
