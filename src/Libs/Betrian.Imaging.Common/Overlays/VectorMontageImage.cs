using System.Numerics;
using Betrian.Models;

namespace Betrian.Imaging.Common.Overlays;
public record VectorMontageImage : IMontageImageBase
{
    public required ImageWithMetadata ImageWithMetadata { get; init; }
    public required LengthPoint ImageCenterNeutralPosition { get; init; }
    public required double Opacity { get; init; }
    public required LengthPoint[] PointList { get; init; }
    public required Vector2[] VectorList { get; init; }
}
