using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record TileSetPreferences
{
    [DataMember]
    public required Ratio FixedHfwImageOverlap { get; init; }

    [DataMember]
    public required Ratio VariableHfwImageOverlap { get; init; }
}
