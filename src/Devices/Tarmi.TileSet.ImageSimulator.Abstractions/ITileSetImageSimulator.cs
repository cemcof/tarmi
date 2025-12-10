using Tarmi.Imaging.Common;
using Tarmi.Models;

namespace Tarmi.TileSet.ImageSimulator.Abstractions;

public interface ITileSetImageSimulator
{
    bool IsViewSupported(StageCameraView cameraView);
    StageCameraView CurrentContextCameraView { get; }
    ImageWithMetadata GrabOne();
}
