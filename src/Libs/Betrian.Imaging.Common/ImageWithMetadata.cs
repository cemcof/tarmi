using Betrian.Imaging.Common.Metadata;
using Betrian.Models;
using UnitsNet;
using Betrian.Imaging.Common.OpenCvWrapper;

namespace Betrian.Imaging.Common;

public record ImageMetadata
{
    public required TiffMetadata TiffMetadata { get; init; }
    public Guid ImageId { get; init; } = Guid.Empty;
    public Guid LayerId { get; init; } = Guid.Empty;
    public Guid RegionOfInterestId { get; init; } = Guid.Empty;
    public Metadata.Thermofisher.XmlFormat.Metadata? FeiXmlMetadata { get; init; }
    public Metadata.Thermofisher.IniFormat.Metadata? FeiIniMetadata { get; init; }
    public Metadata.Thermofisher.MementoFormat.Metadata? FeiMementoMetadata { get; init; }
    public Metadata.Luminescence.Metadata? LuminescenceMetadata { get; init; }
    public Metadata.LegacyIflm.Metadata? LegacyIflmMetadata { get; init; }
    public required Metadata.Coordinates.Metadata Coordinates { get; init; }
}

public record ImageWithMetadata : ImageMetadata, IDisposable
{
    // always create new as it's always disposed
    public static ImageWithMetadata Empty => new()
    {
        Image = new Image<Gray, byte>(256, 256, new(0)),
        MemoryOrigin = true,
        TiffMetadata = new(),
        Coordinates = new()
        {
            ElectronBeamStagePosition = StagePosition.Zero,
            ImageSize = new() { Width = 256, Height = 256 },
            CameraView = StageCameraView.Unknown,
            PixelSize = new() { X = Length.FromPicometers(1), Y = Length.FromPicometers(1) },
        }
    };

    public required IImage Image { get; set; }

    public required bool MemoryOrigin { get; set; }

    public void Dispose()
    {
        Image.Dispose();
        GC.SuppressFinalize(this);
    }
}
