using System.Runtime.Serialization;
using Betrian.Models;
using Betrian.Models.Serialization;
using UnitsNet;

namespace CFLMnavi.Configuration.Holders;

[DataContract(Namespace = Helpers.AppNamespace)]
[KnownType(typeof(CircleAreaOfInterest))]
[KnownType(typeof(RectangularAreaOfInterest))]
public abstract record AreaOfInterest
{
    [DataMember(IsRequired = true)]
    public required string Name { get; set; }

    [IgnoreDataMember]
    public abstract LengthRectangle BoundingRectangle { get; }

    public abstract bool Overlaps(LengthRectangle rectangle);

    public abstract LengthPoint GetDefaultViewPosition();
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record RectangularAreaOfInterest : AreaOfInterest
{
    [DataMember(IsRequired = true)]
    public required Length Width { get; init; }

    [DataMember(IsRequired = true)]
    public required Length Height { get; init; }

    [DataMember(IsRequired = true)]
    public required LengthPoint Center { get; init; }

    public override LengthRectangle BoundingRectangle => new()
    {
        Top = Center.Y - Height / 2,
        Left = Center.X - Width / 2,
        Right = Center.X + Width / 2,
        Bottom = Center.Y + Height / 2,
    };

    public override LengthPoint GetDefaultViewPosition() => Center;

    public override bool Overlaps(LengthRectangle area)
        => BoundingRectangle.IntersectsWith(area);
}

[DataContract(Namespace = Helpers.AppNamespace)]
public record CircleAreaOfInterest : AreaOfInterest
{
    [DataMember(IsRequired = true)]
    public required Length Radius { get; set; }

    [DataMember(IsRequired = true)]
    public required LengthPoint Center { get; set; }

    [IgnoreDataMember]
    public override LengthRectangle BoundingRectangle => new()
    {
        Top = Center.Y - Radius,
        Left = Center.X - Radius,
        Right = Center.X + Radius,
        Bottom = Center.Y + Radius,
    };

    public override bool Overlaps(LengthRectangle area)
    {
        var closestX = UnitMath.Clamp(Center.X, area.Left, area.Right);
        var closestY = UnitMath.Clamp(Center.Y, area.Top, area.Bottom);

        var xDifference = closestX - Center.X;
        var yDifference = closestY - Center.Y;

        return xDifference * xDifference + yDifference * yDifference <= Radius * Radius;
    }

    public override LengthPoint GetDefaultViewPosition() => Center;
}
