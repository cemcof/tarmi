using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace Betrian.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record ActiveView : IEquatable<ActiveView>
{
    public static ActiveView Zero { get; } = new()
    {
        Center = LengthPoint.Zero,
        Size = LengthSize2d.Zero,
        LiveStreamIsActive = false
    };

    [DataMember]
    public required LengthPoint Center { get; init; }

    [DataMember]
    public required LengthSize2d Size { get; init; }

    [IgnoreDataMember]
    public LengthRectangle BoundingRectangle => new()
    {
        Top = Center.Y - Size.Height / 2,
        Left = Center.X - Size.Width / 2,
        Right = Center.X + Size.Width / 2,
        Bottom = Center.Y + Size.Height / 2,
    };

    [DataMember]
    public required bool LiveStreamIsActive { get; init; }

    public bool Equals(ActiveView? other)
    {
        return
            other is not null &&
            Comparison.EqualsAbsolute(Center.X.Meters, other.Center.X.Meters, 1E-9) &&
            Comparison.EqualsAbsolute(Center.Y.Meters, other.Center.Y.Meters, 1E-9) &&
            Comparison.EqualsAbsolute(Size.Width.Meters, other.Size.Width.Meters, 1E-9) &&
            Comparison.EqualsAbsolute(Size.Height.Meters, other.Size.Height.Meters, 1E-9);
    }

    public override int GetHashCode()
        => HashCode.Combine(Center.X, Center.Y, Size.Width, Size.Height);

    public override string ToString()
        => $"Center: {Center}, Size: {Size}, Live Stream is active: {LiveStreamIsActive}";
}
