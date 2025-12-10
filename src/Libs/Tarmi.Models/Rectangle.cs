using System.Runtime.Serialization;
using Tarmi.Models.Serialization;

using UnitsNet;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public record Rectangle<T>
    where T : struct
{
    [DataMember]
    public required T Bottom { get; init; }

    [DataMember]
    public required T Left { get; init; }

    [DataMember]
    public required T Right { get; init; }

    [DataMember]
    public required T Top { get; init; }

    public override string ToString()
        => $"[{Left},{Top}]-[{Right},{Bottom}]";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record IntRectangle : Rectangle<int>
{
    public static IntRectangle Zero { get; } = new IntRectangle { Bottom = 0, Left = 0, Right = 0, Top = 0 };

    [IgnoreDataMember]
    public int Width => Right - Left;

    [IgnoreDataMember]
    public int Height => Bottom - Top;

    public bool IsPointInsideRectangle(IntPoint point)
    {
        return
            Left <= point.X && point.X < Left + Width &&
            Top <= point.Y && point.Y < Top + Height;
    }
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LengthRectangle : Rectangle<Length>
{
    public static LengthRectangle Zero { get; } = new LengthRectangle { Bottom = Length.Zero, Left = Length.Zero, Right = Length.Zero, Top = Length.Zero };

    [IgnoreDataMember]
    public Length Width => Right - Left;

    [IgnoreDataMember]
    public Length Height => Bottom - Top;

    public bool IsPointInsideRectangle(LengthPoint point)
    {
        return
            Left <= point.X && point.X < Left + Width &&
            Top <= point.Y && point.Y < Top + Height;
    }

    public bool IntersectsWith(LengthRectangle rectangle)
    {
        var x1 = UnitMath.Max(Left, rectangle.Left);
        var x2 = UnitMath.Min(Right, rectangle.Right);
        var y1 = UnitMath.Max(Top, rectangle.Top);
        var y2 = UnitMath.Min(Bottom, rectangle.Bottom);

        return x2 >= x1 && y2 >= y1;
    }

    public override string ToString()
        => $"[{Left.Meters:0.000000}m,{Top.Meters:0.000000}m]-[{Right.Meters:0.000000}m,{Bottom.Meters:0.000000}m]";

    public LengthPoint GetCenter()
    {
        return new LengthPoint
        {
            X = (Left + Right) / 2,
            Y = (Top + Bottom) / 2
        };
    }
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record RatioRectangle : Rectangle<Ratio>
{
    public static RatioRectangle Zero { get; } = new RatioRectangle { Bottom = Ratio.Zero, Left = Ratio.Zero, Right = Ratio.Zero, Top = Ratio.Zero };

    [IgnoreDataMember]
    public Ratio Width => Right - Left;

    [IgnoreDataMember]
    public Ratio Height => Bottom - Top;

    public bool IsPointInsideRectangle(RatioPoint point)
    {
        return
            Left <= point.X && point.X < Left + Width &&
            Top <= point.Y && point.Y < Top + Height;
    }

    public bool IntersectsWith(RatioRectangle rectangle)
    {
        var x1 = UnitMath.Max(Left, rectangle.Left);
        var x2 = UnitMath.Min(Right, rectangle.Right);
        var y1 = UnitMath.Max(Top, rectangle.Top);
        var y2 = UnitMath.Min(Bottom, rectangle.Bottom);

        return x2 >= x1 && y2 >= y1;
    }

    public override string ToString()
        => $"[{Left.DecimalFractions:0.00},{Top.DecimalFractions:0.00}]-[{Right.DecimalFractions:0.00},{Bottom.DecimalFractions:0.00}]";

    public RatioPoint GetCenter()
    {
        return new RatioPoint
        {
            X = (Left + Right) / 2,
            Y = (Top + Bottom) / 2
        };
    }
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record BeamCoordinatesRectangle : Rectangle<double>
{
    public static BeamCoordinatesRectangle Zero { get; } = new BeamCoordinatesRectangle { Bottom = 0, Left = 0, Right = 0, Top = 0 };

    public bool IsValid =>
        Bottom >= BeamCoordinates.MinValue && Bottom <= BeamCoordinates.MaxValue &&
        Left >= BeamCoordinates.MinValue && Left <= BeamCoordinates.MaxValue &&
        Right >= BeamCoordinates.MinValue && Right <= BeamCoordinates.MaxValue &&
        Top >= BeamCoordinates.MinValue && Top <= BeamCoordinates.MaxValue &&
        Bottom < Top && Left < Right;
}
