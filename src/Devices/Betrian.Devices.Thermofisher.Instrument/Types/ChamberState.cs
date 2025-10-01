using UnitsNet;

namespace Betrian.Devices.Thermofisher.Instrument.Types;

public record ChamberState
{
    public static ChamberState Zero { get; } = new()
    {
        Pressure = Pressure.Zero,
        State = VacuumCompartmentState.Unknown
    };

    public Pressure Pressure { get; init; }
    public VacuumCompartmentState State { get; init; }
}
