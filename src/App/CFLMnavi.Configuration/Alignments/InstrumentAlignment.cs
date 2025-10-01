using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Alignments;

[DataContract(Namespace = Helpers.AppNamespace)]
public record InstrumentAlignment
{
    [DataMember]
    public StageCameraAlignment Sem { get; init; } = new()
    {
        OffsetX = Length.FromMeters(0),
        OffsetY = Length.FromMeters(0),
        OffsetRotation = Angle.FromDegrees(180),
        OffsetTilt = Angle.FromDegrees(0)
    };

    [DataMember]
    public StageCameraAlignment FibMilling { get; init; } = new()
    {
        OffsetX = Length.FromMeters(0),
        OffsetY = Length.FromMeters(0),
        OffsetRotation = Angle.FromDegrees(180),
        OffsetTilt = Angle.FromDegrees(0)
    };

    [DataMember]
    public StageCameraAlignment Lm { get; init; } = new()
    {
        OffsetX = Length.FromMeters(0.0502184),
        OffsetY = Length.FromMeters(0),
        OffsetRotation = Angle.FromDegrees(0),
        OffsetTilt = Angle.FromDegrees(0)
    };

    [DataMember]
    public StageCameraAlignment FibRightAngle { get; init; } = new()
    {
        OffsetX = Length.FromMeters(0),
        OffsetY = Length.FromMeters(0),
        OffsetRotation = Angle.FromDegrees(0),
        OffsetTilt = Angle.FromDegrees(0)
    };

    [DataMember]
    public required LinearStageAlignment LinearStage { get; init; }

    [DataMember]
    public required FilterHandlerAlignment FilterHandler { get; init; }
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record StageCameraAlignment
{
    [DataMember]
    public Length OffsetX { get; init; }

    [DataMember]
    public Length OffsetY { get; init; }

    [DataMember]
    public Angle OffsetRotation { get; init; }

    [DataMember]
    public Angle OffsetTilt { get; init; }
}
