using Betrian.Models.Serialization;
using System.Runtime.Serialization;
using UnitsNet;

namespace Betrian.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record StageLimits
{
    [DataMember]
    public required LengthRangeDescriptor X { get; init; }

    [DataMember]
    public required LengthRangeDescriptor Y { get; init; }

    [DataMember]
    public required LengthRangeDescriptor Z { get; init; }

    [DataMember]
    public required AngleRangeDescriptor Rotation { get; init; }

    [DataMember]
    public required AngleRangeDescriptor Tilt { get; init; }

    public bool IsWithinLimits(StagePosition position)
    {
        return
            IsWithinLimits(position.X, X) &&
            IsWithinLimits(position.Y, Y) &&
            IsWithinLimits(position.Z, Z) &&
            // rotation is always valid
            //IsWithinLimits(position.Rotation, Rotation) &&
            IsWithinLimits(position.Tilt, Tilt);
    }

    private static bool IsWithinLimits(Length value, LengthRangeDescriptor limits)
        => value >= limits.Min && value <= limits.Max;

    private static bool IsWithinLimits(Angle value, AngleRangeDescriptor limits)
        => value >= limits.Min && value <= limits.Max;

    public override string ToString()
        => $"[X: {X}, Y: {Y}, Z: {Z}, R: {Rotation}, T: {Tilt}]";
}
