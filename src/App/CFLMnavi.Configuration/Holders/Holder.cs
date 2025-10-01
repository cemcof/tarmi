using System.Runtime.Serialization;
using Betrian.Models;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Holders;

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
    public required Length SafeUnknownMoveZ { get; init; }

    [DataMember(IsRequired = true)]
    public required AngleRangeDescriptor SafeTiltRange { get; init; }

    [DataMember(IsRequired = true)]
    public List<AreaOfInterest> Grids { get; init; } = [];
}
