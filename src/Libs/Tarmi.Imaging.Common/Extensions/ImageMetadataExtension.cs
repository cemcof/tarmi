using System.Reactive.Disposables;
using Tarmi.Models;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.Imaging.Common;

public enum ImageTransformationType
{
    Raw,
    View,
    Maps
}

public static class ImageMetadataExtensions
{
    public static Length ToLength(this Metadata.Thermofisher.XmlFormat.Quantity quantity)
    {
        return quantity.Unit switch
        {
            "m" => Length.FromMeters(quantity.Value),
            "cm" => Length.FromCentimeters(quantity.Value),
            "mm" => Length.FromMillimeters(quantity.Value),
            "um" or "μm" => Length.FromMicrometers(quantity.Value),
            "nm" => Length.FromNanometers(quantity.Value),
            _ => throw new NotSupportedException($"Unit '{quantity.Unit}' is not supported for length conversion.")
        };
    }

    public static PixelSize GetPixelSize(this ImageMetadata metadata)
    {
        if (!metadata.Coordinates.PixelSize.Equals(PixelSize.Zero))
        {
            return metadata.Coordinates.PixelSize;
        }
        else if (metadata.LuminescenceMetadata is not null)
        {
            return new PixelSize
            {
                X = metadata.LuminescenceMetadata.PixelSizeX,
                Y = metadata.LuminescenceMetadata.PixelSizeY
            };
        }
        else if (metadata.FeiXmlMetadata is not null)
        {
            return new PixelSize
            {
                X = metadata.FeiXmlMetadata.BinaryResult!.PixelSize!.X.ToLength(),
                Y = metadata.FeiXmlMetadata.BinaryResult!.PixelSize!.Y.ToLength()
            };
        }
        else if (metadata.FeiIniMetadata is not null)
        {
            return new PixelSize
            {
                X = Length.FromMeters(metadata.FeiIniMetadata.Scan.PixelWidth),
                Y = Length.FromMeters(metadata.FeiIniMetadata.Scan.PixelHeight)
            };
        }
        return PixelSize.Zero;
    }

    public static StagePosition GetStagePosition(this ImageMetadata metadata)
    {
        if (!metadata.Coordinates.Equals(StagePosition.Zero))
        {
            return metadata.Coordinates.ElectronBeamStagePosition;
        }
        else if (metadata.FeiXmlMetadata is not null)
        {
            return metadata.FeiXmlMetadata.StageSettings!.StagePosition;
        }
        return StagePosition.Zero;
    }

    public static StageCameraView GetSource(this ImageMetadata metadata)
    {
        if (metadata.Coordinates.CameraView != StageCameraView.Unknown)
        {
            return metadata.Coordinates.CameraView;
        }
        else if (metadata.LuminescenceMetadata is not null)
        {
            return StageCameraView.LM;
        }
        else if (metadata.ConfocalMetadata is not null)
        {
            return StageCameraView.Confocal;
        }
        return (metadata.FeiXmlMetadata?.Acquisition?.BeamType) switch
        {
            Metadata.Thermofisher.XmlFormat.BeamType.Electron => StageCameraView.SEM,
            Metadata.Thermofisher.XmlFormat.BeamType.Ion => StageCameraView.FIB_Milling,
            _ => StageCameraView.Unknown
        };
    }

    public static Angle GetRawImageRotationAngle(this ImageMetadata metadata)
    {
        var rotationAngle = metadata.GetStagePosition().Rotation;
        var isBeamImage = metadata.Coordinates?.CameraView.IsOneOf(StageCameraView.SEM, StageCameraView.FIB_RightAngle, StageCameraView.FIB_Milling) ?? false;
        if (isBeamImage)
        {
            if (metadata.FeiXmlMetadata is not null)
            {
                var scanRotation = Angle.FromRadians(metadata.FeiXmlMetadata.ScanSettings!.ScanRotation!.Value);
                rotationAngle -= scanRotation;
            }
        }
        return rotationAngle.NormalizeAngle();
    }

    public static LengthSize2d GetFieldSize(this ImageMetadata metadata)
    {
        var pixelSize = metadata.GetPixelSize();
        var imageSize = metadata.Coordinates!.ImageSize;
        return new()
        {
            Height = pixelSize.Y * imageSize.Height,
            Width = pixelSize.X * imageSize.Width,
        };
    }

    public static LengthRectangle GetImageArea(this ImageMetadata metadata, Func<StagePosition, StageCameraView, LengthPoint> transformStageToPlanePosition)
    {
        var stagePosition = metadata.GetStagePosition();
        var stageCameraView = metadata.GetSource();
        var planePosition = transformStageToPlanePosition(stagePosition, stageCameraView);

        var fieldSize = GetFieldSize(metadata);

        var halfFieldWidth = fieldSize.Width / 2;
        var halfFieldHeight = fieldSize.Height / 2;

        return new()
        {
            Bottom = planePosition.Y + halfFieldHeight,
            Left = planePosition.X - halfFieldWidth,
            Right = planePosition.X + halfFieldWidth,
            Top = planePosition.Y - halfFieldHeight
        };
    }


    private static readonly Angle Angle180 = Angle.FromDegrees(180.0);
    private static readonly Angle AngleTolerance = Angle.FromDegrees(1.0);

    private static ImageWithMetadata TransformToRaw(this ImageWithMetadata imageWithMetadata)
    {
        var coordinates = imageWithMetadata.Coordinates;
        var isFlippedOnX = coordinates.ImageIsFlippedOnX;
        var isFlippedOnY = coordinates.ImageIsFlippedOnY;

        return (isFlippedOnX, isFlippedOnY) switch
        {
            (true, true) => imageWithMetadata with { Image = imageWithMetadata.Image.Flip(FlipMode.XY), Coordinates = coordinates with { ImageIsFlippedOnX = false, ImageIsFlippedOnY = false } },
            (true, false) => imageWithMetadata with { Image = imageWithMetadata.Image.Flip(FlipMode.X), Coordinates = coordinates with { ImageIsFlippedOnX = false, ImageIsFlippedOnY = false } },
            (false, true) => imageWithMetadata with { Image = imageWithMetadata.Image.Flip(FlipMode.Y), Coordinates = coordinates with { ImageIsFlippedOnX = false, ImageIsFlippedOnY = false } },
            (false, false) => imageWithMetadata with { Image = imageWithMetadata.Image.Clone() }
        };
    }

    private static void TransformToRawInplace(this ImageWithMetadata imageWithMetadata)
    {
        var coordinates = imageWithMetadata.Coordinates;
        var isFlippedOnX = coordinates.ImageIsFlippedOnX;
        var isFlippedOnY = coordinates.ImageIsFlippedOnY;

        switch (isFlippedOnX, isFlippedOnY)
        {
            case (true, true):
                imageWithMetadata.Image.FlipInplace(FlipMode.XY);
                break;
            case (true, false):
                imageWithMetadata.Image.FlipInplace(FlipMode.X);
                break;
            case (false, true):
                imageWithMetadata.Image.FlipInplace(FlipMode.Y);
                break;
            case (false, false):
                break;
        }
        imageWithMetadata.Coordinates.ImageIsFlippedOnX = false;
        imageWithMetadata.Coordinates.ImageIsFlippedOnY = false;
    }

    private static bool IsInRawForm(this ImageWithMetadata imageWithMetadata)
        => !imageWithMetadata.Coordinates.ImageIsFlippedOnX && !imageWithMetadata.Coordinates.ImageIsFlippedOnY;

    private static ImageWithMetadata TransformToView(this ImageWithMetadata imageWithMetadata)
    {
        var isRaw = imageWithMetadata.IsInRawForm();
        var rawImage = isRaw ? imageWithMetadata : imageWithMetadata.TransformToRaw();
        using var rawImageGuard = Disposable.Create(() =>
        {
            if (!isRaw)
            {
                rawImage.Dispose();
            }
        });

        var rotationAngle = rawImage.GetRawImageRotationAngle();
        var isImageAngle180 = rotationAngle.IsInTolerance(Angle180, AngleTolerance);
        var coordinates = rawImage.Coordinates;
        var cameraView = imageWithMetadata.GetSource();
        if (cameraView.IsOneOf(StageCameraView.LM, StageCameraView.Confocal))
        {
            return imageWithMetadata with { Image = rawImage.Image.Flip(FlipMode.X), Coordinates = coordinates with { ImageIsFlippedOnX = true, ImageIsFlippedOnY = false } };
        }

        if (isImageAngle180)
        {
            return imageWithMetadata with { Image = rawImage.Image.Flip(FlipMode.XY), Coordinates = coordinates with { ImageIsFlippedOnY = true, ImageIsFlippedOnX = true } };
        }
        else
        {
            return imageWithMetadata with { Image = rawImage.Image.Clone(), Coordinates = coordinates with { ImageIsFlippedOnX = false, ImageIsFlippedOnY = false } };
        }
    }

    private static void TransformToViewInplace(this ImageWithMetadata imageWithMetadata)
    {
        if (imageWithMetadata.IsInRawForm())
        {
            imageWithMetadata.TransformToRawInplace();
        }

        var rotationAngle = imageWithMetadata.GetRawImageRotationAngle();
        var isImageAngle180 = rotationAngle.IsInTolerance(Angle180, AngleTolerance);

        if (isImageAngle180)
        {
            imageWithMetadata.Image.FlipInplace(FlipMode.XY);
            imageWithMetadata.Coordinates.ImageIsFlippedOnX = true;
            imageWithMetadata.Coordinates.ImageIsFlippedOnY = true;
        }
    }

    private static ImageWithMetadata TransformToMapsView(this ImageWithMetadata imageWithMetadata)
    {
        var cameraView = imageWithMetadata.GetSource();

        var depth = imageWithMetadata.Image.Depth;

        using var grayImage =
            depth == 8 || depth == 16 ?
                imageWithMetadata with { Image = imageWithMetadata.Image.Clone() } :
                imageWithMetadata with { Image = imageWithMetadata.Image.ToGrayscale() };

        return
            (cameraView == StageCameraView.LM || cameraView == StageCameraView.Confocal) ?
                grayImage.TransformToView() :
                grayImage.TransformToRaw();
    }

    private static void TransformToMapsViewInplace(this ImageWithMetadata imageWithMetadata)
    {
        var cameraView = imageWithMetadata.GetSource();

        var depth = imageWithMetadata.Image.Depth;

        if (depth > 16)
        {
            using var oldImage = imageWithMetadata;
            imageWithMetadata = imageWithMetadata with { Image = imageWithMetadata.Image.ToGrayscale() };
        }

        if (cameraView == StageCameraView.LM)
        {
            imageWithMetadata.TransformToViewInplace();
        }
        else
        {
            imageWithMetadata.TransformToRawInplace();
        }
    }

    public static ImageWithMetadata TransformTo(this ImageWithMetadata imageWithMetadata, ImageTransformationType transformationType)
    {
        return transformationType switch
        {
            ImageTransformationType.Raw => imageWithMetadata.TransformToRaw(),
            ImageTransformationType.View => imageWithMetadata.TransformToView(),
            ImageTransformationType.Maps => imageWithMetadata.TransformToMapsView(),
            _ => throw new NotSupportedException($"Transformation type '{transformationType}' is not supported.")
        };
    }

    public static void TransformToInplace(this ImageWithMetadata imageWithMetadata, ImageTransformationType transformationType)
    {
        switch (transformationType)
        {
            case ImageTransformationType.Raw: imageWithMetadata.TransformToRawInplace(); break;
            case ImageTransformationType.View: imageWithMetadata.TransformToViewInplace(); break;
            case ImageTransformationType.Maps: imageWithMetadata.TransformToMapsViewInplace(); break;
            default: throw new NotSupportedException($"Transformation type '{transformationType}' is not supported.");
        }
    }
}
