using Betrian.Imaging.Common;
using Betrian.Models;

namespace Betrian.TileSet.ImageSimulator.Abstractions;

public interface ITileSetImageSimulator
{
    bool IsViewSupported(StageCameraView cameraView);
    StageCameraView CurrentContextCameraView { get; }
    ImageWithMetadata GrabOne();
}
