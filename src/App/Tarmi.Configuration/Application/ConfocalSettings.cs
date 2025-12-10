using System.Runtime.Serialization;
using Tarmi.Models;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record class ConfocalSettings
{
    [DataMember]
    public List<Length> PixelSizes { get; init; } = [];

    [DataMember]
    public List<ElectricPotential> ADCRanges { get; init; } = [];

    [DataMember]
    public List<Duration> DwellRanges { get; init; } = [];

    [DataMember]
    public RangeDescriptor<Level> GainRanges { get; init; } = new RangeDescriptor<Level>() { Min = Level.Zero, Max = Level.Zero };
}
