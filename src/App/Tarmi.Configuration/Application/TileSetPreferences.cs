using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Application;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record TileSetPreferences
{
    [DataMember]
    public required Ratio FixedHfwImageOverlap { get; init; }

    [DataMember]
    public required Ratio VariableHfwImageOverlap { get; init; }
}
