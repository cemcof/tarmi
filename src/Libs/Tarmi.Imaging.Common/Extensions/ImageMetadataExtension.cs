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
    private static readonly Angle Angle180 = Angle.FromDegrees(180.0);
    private static readonly Angle AngleTolerance = Angle.FromDegrees(1.0);

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
        if (isBeamImage && metadata.FeiXmlMetadata is not null)
        {
            var scanRotation = Angle.FromRadians(metadata.FeiXmlMetadata.ScanSettings!.ScanRotation!.Value);
            rotationAngle -= scanRotation;
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

    public static ImageWithMetadata TransformTo(this ImageWithMetadata imageWithMetadata, ImageTransformationType transformationType)
    {
        imageWithMetadata = imageWithMetadata.Clone();
        imageWithMetadata.TransformToInplace(transformationType);
        return imageWithMetadata;
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

    private static void TransformToRawInplace(this ImageWithMetadata imageWithMetadata)
        => imageWithMetadata.TransformToExpectedInplace(false, false);

    private static void TransformToViewInplace(this ImageWithMetadata imageWithMetadata)
    {
        var cameraView = imageWithMetadata.GetSource();
        if (cameraView.IsOneOf(StageCameraView.LM, StageCameraView.Confocal))
        {
            imageWithMetadata.TransformToExpectedInplace(true, false);
            return;
        }
        var rotationAngle = imageWithMetadata.GetRawImageRotationAngle();
        var isImageAngle180 = rotationAngle.IsInTolerance(Angle180, AngleTolerance);

        if (isImageAngle180)
        {
            imageWithMetadata.TransformToExpectedInplace(true, true);
            return;
        }
        imageWithMetadata.TransformToExpectedInplace(false, false);
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

        if (cameraView.IsOneOf(StageCameraView.LM, StageCameraView.Confocal))
        {
            imageWithMetadata.TransformToViewInplace();
        }
        else
        {
            imageWithMetadata.TransformToRawInplace();
        }
    }

    private static void TransformToExpectedInplace(this ImageWithMetadata imageWithMetadata, bool isFlippedOnXExpected, bool isFlippedOnYExpected)
    {
        var coordinates = imageWithMetadata.Coordinates;
        var flipOnX = coordinates.ImageIsFlippedOnX ^ isFlippedOnXExpected;
        var flipOnY = coordinates.ImageIsFlippedOnY ^ isFlippedOnYExpected;

        FlipMode? flipMode = (flipOnX, flipOnY) switch
        {
            (false, false) => null,
            (true, false) => FlipMode.X,
            (false, true) => FlipMode.Y,
            (true, true) => FlipMode.XY
        };

        if (flipMode.HasValue)
        {
            imageWithMetadata.Image.FlipInplace(flipMode.Value);
        }

        coordinates.ImageIsFlippedOnX = isFlippedOnXExpected;
        coordinates.ImageIsFlippedOnY = isFlippedOnYExpected;
    }

    public static ImageWithMetadata Clone(this ImageWithMetadata imageWithMetadata) => imageWithMetadata with
    {
        Image = imageWithMetadata.Image.Clone(),
        Coordinates = imageWithMetadata.Coordinates with { }
    };
}
