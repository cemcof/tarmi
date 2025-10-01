using Betrian.Models;

namespace Betrian.Imaging.Common;
public interface IMontageImageBase
{
    ImageWithMetadata ImageWithMetadata { get; init; }

    /// <summary> Image center point - in neutral position. </summary>
    LengthPoint ImageCenterNeutralPosition { get; init; }
    
    double Opacity { get; init; }
}
