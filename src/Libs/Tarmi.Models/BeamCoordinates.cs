using Tarmi.Models.Serialization;
using System.Runtime.Serialization;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record BeamCoordinates
{
    public const double MinValue = 0.0;
    public const double MaxValue = 1.0;
    public const double CenterValue = 0.5;

    public double X { get; init; }
    public double Y { get; init; }

    public bool IsValid => X >= MinValue && X <= MaxValue && Y >= MinValue && Y <= MaxValue;
    public static BeamCoordinates CenterCoordinates { get; } = new BeamCoordinates { X = CenterValue, Y = CenterValue };

    public override string ToString()
        => $"[{X},{Y}]";
}
