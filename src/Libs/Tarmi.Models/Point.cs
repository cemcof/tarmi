using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public record Point<T>
    where T : struct
{
    [DataMember]
    public T X { get; init; }

    [DataMember]
    public T Y { get; init; }

    public void Deconstruct(out T x, out T y)
    {
        x = X;
        y = Y;
    }

    public override string ToString()
        => $"[{X},{Y}]";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LengthPoint : Point<Length>
{
    public static LengthPoint Zero { get; } = new LengthPoint { X = Length.Zero, Y = Length.Zero };

    public override string ToString()
        => $"[{X.Meters:0.000000}m, {Y.Meters:0.000000}m]";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record RatioPoint : Point<Ratio>
{
    public static RatioPoint Zero { get; } = new RatioPoint { X = Ratio.Zero, Y = Ratio.Zero };
    public static RatioPoint Center { get; } = new RatioPoint { X = Ratio.FromDecimalFractions(0.5), Y = Ratio.FromDecimalFractions(0.5) };
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record DoublePoint : Point<double>
{
    public static DoublePoint Zero { get; } = new DoublePoint { X = 0, Y = 0};
    public static DoublePoint Invalid { get; } = new DoublePoint { X = double.NaN, Y = double.NaN };
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record IntPoint : Point<int>
{
    public static IntPoint Zero { get; } = new IntPoint { X = 0, Y = 0 };
    public static DoublePoint Invalid { get; } = new DoublePoint { X = int.MinValue, Y = int.MaxValue };
}
