using System.Runtime.Serialization;
using Tarmi.Models.Serialization;
using UnitsNet;

namespace Tarmi.Models;

[DataContract(Namespace = Helpers.AppNamespace)]
public sealed record PixelSize
{
    public static PixelSize Zero { get; } = new();

    [DataMember]
    public Length X { get; init; } = Length.Zero;

    [DataMember]
    public Length Y { get; init; } = Length.Zero;

    public override string ToString()
        => $"[{X},{Y}]";

    public static implicit operator LengthPoint(PixelSize pixelSize)
        => new() { X = pixelSize.X, Y = pixelSize.Y };
}
