using System.Runtime.Serialization;
using Tarmi.Models;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Configuration.Holders;

[DataContract(Namespace = Helpers.AppNamespace)]
public record Holder
{
    [DataMember(IsRequired = true)]
    public required string Name { get; init; }

    [DataMember(IsRequired = true)]
    public required Angle PreTilt { get; init; }

    [DataMember(IsRequired = true)]
    public required StagePosition LmModePlanePoint { get; init; }

    [DataMember(IsRequired = true)]
    public required StagePosition SemModePlanePoint { get; init; }

    [DataMember(IsRequired = true)]
    public required StagePosition FibRightAngleModePlanePoint { get; init; }

    [DataMember(IsRequired = true)]
    public required StagePosition FibMillingModePlanePoint { get; init; }
    [DataMember(IsRequired = true)]
    public required StagePosition ConfocalModePlanePoint { get; init; }

    [DataMember(IsRequired = true)]
    public required Length SafeUnknownMoveZ { get; init; }

    [DataMember(IsRequired = true)]
    public required AngleRangeDescriptor SafeTiltRange { get; init; }

    [DataMember(IsRequired = true)]
    public List<AreaOfInterest> Grids { get; init; } = [];
}
