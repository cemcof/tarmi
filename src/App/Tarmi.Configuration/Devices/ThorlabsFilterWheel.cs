using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Devices;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ThorlabsFilterWheel
{
    [DataMember]
    public SerialPort Port { get; init; } = new();

    [DataMember]
    public EmissionFilters EmissionFilters { get; init; } = new();
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record EmissionFilters
{
    [DataMember]
    public EmissionFilter Filter1 { get; init; } = new EmissionFilter { FilterColor = Length.FromNanometers(460), LaserColor = Length.FromNanometers(405) };

    [DataMember]
    public EmissionFilter Filter2 { get; init; } = new EmissionFilter { FilterColor = Length.FromNanometers(535), LaserColor = Length.FromNanometers(488) };

    [DataMember]
    public EmissionFilter Filter3 { get; init; } = new EmissionFilter { FilterColor = Length.FromNanometers(600), LaserColor = Length.FromNanometers(561) };

    [DataMember]
    public EmissionFilter Filter4 { get; init; } = new EmissionFilter { FilterColor = Length.FromNanometers(705), LaserColor = Length.FromNanometers(640) };

}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record EmissionFilter
{
    [DataMember]
    public Length FilterColor { get; init; }

    [DataMember]
    public Length LaserColor { get; init; }
}
