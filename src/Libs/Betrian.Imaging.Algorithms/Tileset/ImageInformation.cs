using Betrian.Models;
using OpenCvSharp;
using UnitsNet;

namespace Betrian.Imaging.Algorithms.Tileset;

internal record ImageInformation
{
    public StageCameraView CameraView { get; init; }
    public Length MinX { get; init; }
    public Length MaxX { get; init; }
    public Length MinY { get; init; }
    public Length MaxY { get; init; }
    public Length Z { get; init; }
    public Angle Rotation { get; init; }
    public Angle Tilt { get; init; }
    public Length PixelSizeX { get; init; }
    public Length PixelSizeY { get; init; }
    public Size TileSize { get; init; }
    public MatType ImageType { get; init; }
    public bool ImageIsFlippedOnX { get; init; }
    public bool ImageIsFlippedOnY { get; init; }
    public Common.Metadata.Thermofisher.XmlFormat.Metadata? FeiXmlMetadata { get; init; }
}
