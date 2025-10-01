using Betrian.Models;

namespace Betrian.Imaging.Common.Overlays;
public record FiducialMontageImage : IMontageImageBase
{
    public required ImageWithMetadata ImageWithMetadata { get; init; }
    public required LengthPoint ImageCenterNeutralPosition { get; init; }
    public required double Opacity { get; init; }
    
    /// <summary>
    /// List of fiducial points in pixels measured from the image left top corner with their identity as key.
    /// </summary>
    public required List<KeyValuePair<Guid, DoublePoint>> FiducialList { get; init; }
}
