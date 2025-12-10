using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using Tarmi.Configuration.Holders;
using UnitsNet;

namespace Tarmi.Projects;

public enum AcquisitionStrategy
{
    Linear,
    Spiral
}

public enum FocusStrategy
{
    Fixed,
    Auto,
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record TileSetOptions
{
    [DataMember]
    public required AcquisitionStrategy AcquisitionStrategy { get; init; }

    [DataMember]
    public required FocusStrategy FocusStrategy { get; init; }

    [DataMember]
    public required Ratio Overlap { get; init; }

    [DataMember]
    public required AreaOfInterest AreaOfInterest { get; init; }

    [DataMember]
    public List<FocusPoint> FocusPoints { get; init; } = [];
}
