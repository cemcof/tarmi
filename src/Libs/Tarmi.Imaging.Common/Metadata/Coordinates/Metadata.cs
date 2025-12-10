using System.Runtime.Serialization;
using Tarmi.Models;
using Tarmi.Models.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Coordinates;

[DataContract(Namespace = Helpers.AppNamespace)]
[Namespace(Prefix = "unit", Uri = "http://schemas.datacontract.org/2004/07/UnitsNet")]
public record Metadata
{
    [DataMember]
    public StagePosition ElectronBeamStagePosition { get; init; } = StagePosition.Zero;

    [DataMember]
    public PixelSize PixelSize { get; init; } = PixelSize.Zero;

    [DataMember]
    public StageCameraView CameraView { get; init; } = StageCameraView.Unknown;

    [DataMember]
    public IntSize2d ImageSize { get; init; } = new() { Height = 0, Width = 0 };

    [DataMember]
    public bool ImageIsFlippedOnX { get; set; } = false;

    [DataMember]
    public bool ImageIsFlippedOnY { get; set; } = false;
}
