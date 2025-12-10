using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record StagePosition : IEquatable<StagePosition>
{
    [IgnoreDataMember]
    public static StagePosition Zero { get; } = new();

    [DataMember]
    public Length X { get; init; } = Length.Zero;

    [DataMember]
    public Length Y{ get; init; } = Length.Zero;

    [DataMember]
    public Length Z { get; init; } = Length.Zero;

    [DataMember]
    public Angle Rotation { get; init; } = Angle.Zero;

    [DataMember]
    public Angle Tilt { get; init; } = Angle.Zero;

    public bool Equals(StagePosition? other, double metersTolerance, double radiansTolerance)
    {
        return
            other is not null &&
            Comparison.EqualsAbsolute(X.Meters, other.X.Meters, metersTolerance) &&
            Comparison.EqualsAbsolute(Y.Meters, other.Y.Meters, metersTolerance) &&
            Comparison.EqualsAbsolute(Z.Meters, other.Z.Meters, metersTolerance) &&
            Math.Abs(Rotation.NormalizeAngle(true).Difference(other.Rotation.NormalizeAngle(true)).Radians) <= radiansTolerance &&
            Math.Abs(Tilt.Difference(other.Tilt).Radians) <= radiansTolerance;
    }

    public bool Equals(StagePosition? other)
        => Equals(other, 1E-5, 1E-3);

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z, Rotation.NormalizeAngle(), Tilt);

    public override string ToString()
        => $"[X: {X.Meters:0.000000}m, Y: {Y.Meters:0.000000}m, Z: {Z.Meters:0.000000}m, R: {Rotation.Radians:0.000000}rad, T: {Tilt.Radians:0.000000}rad]";
}
