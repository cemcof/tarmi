namespace Tarmi.Devices.Thermofisher.Instrument.Types;

public enum VacuumCompartmentState
{
    Unknown = 1,
    Pumping,
    Pumped,
    Venting,
    Vented,
    Error,
    PlasmaCleaning,
    Baking,
    BusyUnknown,
    PumpedForWaferExchange
}
