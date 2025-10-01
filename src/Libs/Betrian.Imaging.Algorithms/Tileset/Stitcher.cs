using Betrian.Imaging.Common;
using Betrian.Imaging.Common.OpenCvWrapper;
using Betrian.Models;
using OpenCvSharp;
using UnitsNet;

namespace Betrian.Imaging.Algorithms.Tileset;

public class Stitcher
{
    public static ImageWithMetadata StitchImage(
        string directory, OpenCvSharp.Scalar background, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition)
    {
        var images = LoadImages(directory);
        return StitchImage(images, background, getPlanePosition);
    }

    public static ImageWithMetadata StitchImage(
        IEnumerable<string> filePaths, OpenCvSharp.Scalar background, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition)
    {
        var images = LoadImages(filePaths);
        return StitchImage(images, background, getPlanePosition);
    }

    private static ImageWithMetadata StitchImage(
        IEnumerable<ImageWithMetadata> images,
        OpenCvSharp.Scalar background,
        Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition)
    {
        var pixelSizes = images
            .Select(image => image.GetPixelSize())
            .ToList();

        PixelSize commonPixelSize = new() { X = pixelSizes.Select(size => size.X).Max(), Y = pixelSizes.Select(size => size.Y).Max() };

        var tiles = images
            .Select(original =>
            {
                var transformed = original with { Image = GetScaledImage(original.Image, original.GetPixelSize(), commonPixelSize) };
                original.Dispose();
                return transformed;
            })
            .ToArray();

        try
        {
            var information = DetectThumbnailInformation(tiles, getPlanePosition);
            var thumbnail = InitializeThumbnail(information, tiles, background);
            foreach (var tile in tiles)
            {
                InsertTile(thumbnail, tile, information, getPlanePosition);
            }
            return thumbnail;
        }
        finally
        {
            foreach (var tile in tiles)
            {
                tile.Dispose();
            }
        }
    }

    private static ImageInformation DetectThumbnailInformation(ImageWithMetadata[] tiles, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition)
    {
        var referenceTile = tiles[0];

        var position = referenceTile.GetStagePosition();

        var positions = tiles
            .Select(tile => getPlanePosition(tile.GetStagePosition(), tile.GetSource()))
            .ToList();

        var pixelSize = referenceTile.GetPixelSize();
        var coordinates = referenceTile.Coordinates;

        var isBeamImage = referenceTile.FeiXmlMetadata?.ScanSettings is not null;
        Common.Metadata.Thermofisher.XmlFormat.Metadata? feiMetadata = isBeamImage ? referenceTile.FeiXmlMetadata : null;

        return new ImageInformation()
        {
            CameraView = coordinates!.CameraView,
            Z = position.Z,
            Rotation = position.Rotation,
            Tilt = position.Tilt,
            MinX = positions.Select(position => position.X).Min(),
            MaxX = positions.Select(position => position.X).Max(),
            MinY = positions.Select(position => position.Y).Min(),
            MaxY = positions.Select(position => position.Y).Max(),
            PixelSizeX = pixelSize.X,
            PixelSizeY = pixelSize.Y,
            TileSize = referenceTile.Image.Size,
            ImageType = referenceTile.Image.Mat.Type(),
            ImageIsFlippedOnX = coordinates.ImageIsFlippedOnX,
            ImageIsFlippedOnY = coordinates.ImageIsFlippedOnY,
            FeiXmlMetadata = feiMetadata
        };
    }

    private static ImageWithMetadata InitializeThumbnail(ImageInformation information, ImageWithMetadata[] tiles, OpenCvSharp.Scalar background)
    {
        // TODO: Check for off by one errors.

        var positions = tiles
            .Select(tile => tile.GetStagePosition())
            .ToList();

        var minX = positions.Select(position => position.X).Min();
        var maxX = positions.Select(position => position.X).Max();
        var minY = positions.Select(position => position.Y).Min();
        var maxY = positions.Select(position => position.Y).Max();
        var minZ = positions.Select(position => position.Z).Min();
        var maxZ = positions.Select(position => position.Z).Max();

        var imageSize = new Size()
        {
            Width = (int)((information.MaxX - information.MinX) / information.PixelSizeX + information.TileSize.Width),
            Height = (int)((information.MaxY - information.MinY) / information.PixelSizeY + information.TileSize.Height)
        };

        return new ImageWithMetadata()
        {
            Image = information.ImageType != MatType.CV_8UC3 ?
                new Image<Gray, byte>(imageSize.Width, imageSize.Height, new Gray { Scalar = background }) :
                new Image<Bgr, byte>(imageSize.Width, imageSize.Height, new Bgr { Scalar = background }),
            MemoryOrigin = true,
            TiffMetadata = new(),
            Coordinates = new()
            {
                ElectronBeamStagePosition = new()
                {
                    X = (minX + maxX) / 2,
                    Y = (minY + maxY) / 2,
                    Z = (minZ + maxZ) / 2,
                    Rotation = information.Rotation,
                    Tilt = information.Tilt,
                },
                PixelSize = new()
                {
                    X = information.PixelSizeX,
                    Y = information.PixelSizeY
                },
                ImageSize = new()
                {
                    Width = imageSize.Width,
                    Height = imageSize.Height
                },
                CameraView = information.CameraView,
                ImageIsFlippedOnX = information.ImageIsFlippedOnX,
                ImageIsFlippedOnY = information.ImageIsFlippedOnY,
            },
            LuminescenceMetadata = ExtractLuminescenceMetadata(information, tiles),
            FeiXmlMetadata = information.FeiXmlMetadata
        };
    }

    private static Common.Metadata.Luminescence.Metadata? ExtractLuminescenceMetadata(ImageInformation information, ImageWithMetadata[] tiles)
    {
        var luminescenceMetadata = tiles[0].LuminescenceMetadata;
        if (luminescenceMetadata is not null)
        {
            var workingDistanceAverage =
                Length.FromMicrometers(tiles.Average(image => image.LuminescenceMetadata!.WorkingDistance.Micrometers));
            luminescenceMetadata = luminescenceMetadata with
            {
                PixelSizeX = information.PixelSizeX,
                PixelSizeY = information.PixelSizeY,
                WorkingDistance = workingDistanceAverage
            };
        }
        return luminescenceMetadata;
    }

    private static void InsertTile(
        ImageWithMetadata thumbnail,
        ImageWithMetadata tile,
        ImageInformation information,
        Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition)
    {
        var position = getPlanePosition(tile.GetStagePosition(), tile.GetSource());
        var roi = new Rect()
        {
            Left = (int)((position.X - information.MinX) / information.PixelSizeX),
            Top = (int)((position.Y - information.MinY) / information.PixelSizeY),
            Width = tile.Image.Width,
            Height = tile.Image.Height
        };
        using var subMat = thumbnail.Image.Mat[roi];
        tile.Image.Mat.CopyTo(subMat);
    }

    private static IEnumerable<ImageWithMetadata> LoadImages(string directory)
    {
        return Directory
            .GetFiles(directory)
            .Where(file =>
                file.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".tif", StringComparison.OrdinalIgnoreCase))
            .Select(TiffImage.Load);
    }

    private static IEnumerable<ImageWithMetadata> LoadImages(IEnumerable<string> paths)
    {
        return paths
            .Where(file =>
                file.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".tif", StringComparison.OrdinalIgnoreCase))
            .Select(filePath =>
            {
                var image = TiffImage.Load(filePath);
                image.TransformToInplace(ImageTransformationType.View);
                return image;
            });
    }

    /// <summary>
    /// Update image by another scaling.
    /// </summary>
    /// <param name="image">Image with source pixel size.</param>
    /// <param name="sourcePixelSize">Source pixel size.</param>
    /// <param name="targetPixelSize">Target pixel size.</param>
    /// <returns>Image with target pixel size.</returns>
    public static IImage GetScaledImage(IImage image, PixelSize sourcePixelSize, PixelSize targetPixelSize)
    {

        if (!Equals(targetPixelSize.X, sourcePixelSize.X) || !Equals(targetPixelSize.Y, sourcePixelSize.Y))
        {
            var secXOnePercent = sourcePixelSize.X.Nanometers / 100;
            var secYOnePercent = sourcePixelSize.Y.Nanometers / 100;
            var percentX = (targetPixelSize.X.Nanometers / secXOnePercent) / 100;
            var percentY = (targetPixelSize.Y.Nanometers / secYOnePercent) / 100;
            var rotatedSize = new Size((int)(image.Width * percentX), (int)(image.Height * percentY));
            return image.Resize(rotatedSize.Width, rotatedSize.Height, InterpolationFlags.Linear);
        }
        else
        {
            return image.Clone();
        }
    }
}
