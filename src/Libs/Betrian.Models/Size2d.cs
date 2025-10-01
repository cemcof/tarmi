using System.Runtime.Serialization;
using Betrian.Models.Serialization;
using UnitsNet;

namespace Betrian.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public record Size2d<T>
    where T: notnull
{
    [DataMember]
    public required T Width { get; init; }
    [DataMember]
    public required T Height { get; init; }

    public override string ToString() => $"{Width} x {Height}";
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record IntSize2d : Size2d<int>
{
    public static IntSize2d Zero { get; } = new() { Width = 0, Height = 0 };
}

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record LengthSize2d : Size2d<Length>
{
    public static LengthSize2d Zero { get; } = new() { Width = Length.Zero, Height = Length.Zero };

    public override string ToString()
        => $"Width: {Width.Meters:0.000000}m, Height: {Height.Meters:0.000000}m";
}
