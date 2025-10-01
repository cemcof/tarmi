using Betrian.Models;

namespace Betrian.Imaging.Common.Overlays;
public record AutoMontageImage : IMontageImageBase
{
    public required ImageWithMetadata ImageWithMetadata { get; init; }
    public required LengthPoint ImageCenterNeutralPosition { get; init; }
    public required double Opacity { get; init; }
}
