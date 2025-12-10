using System.Collections.Immutable;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Overlays;
using Tarmi.Models;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.PointMapping;

public static class ImagesAnd5DPoints
{
    private static readonly Angle FlipAngle = Angle.FromDegrees(180);

    // angle between SEM and FIB mill = 52
    // lamella angle = 15
    // fib milling angle = (90 - 52) - (15 / 2)
    private static readonly Angle FibMillingAngle = Angle.FromDegrees(38 + 15);

    private static void ThrowIfNotSupportedCameraView(StageCameraView cameraView)
    {
        if (!cameraView.IsOneOf(StageCameraView.SEM, StageCameraView.FIB_RightAngle, StageCameraView.FIB_Milling, StageCameraView.LM, StageCameraView.Confocal))
        {
            throw new NotSupportedException($"Camera view {cameraView} is not supported.");
        }
    }

    /// <summary>
    /// Merge main image with other images by center points.
    /// Scaling images is included.
    /// Presumption : ref image is SEM image
    /// </summary>
    /// <param name="refAutoImage">Reference image for autocorrelation.</param>
    /// <param name="images">List of AutoMontageImage objects.</param>
    /// <param name="viewsPretilt">List of views pretilt.</param>
    /// <param name="gamma">Gamma correction used for AddWeighted images method.</param>
    /// <returns>Main image merged with other images by center point.</returns>
    public static ImageWithMetadata MergeImagesAutocorrelation(
        AutoMontageImage refAutoImage,
        ReadOnlySpan<AutoMontageImage> images,
        ImmutableDictionary<StageCameraView, Angle> viewsPretilt,
        double gamma = 0.9)
    {
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        ImageWithMetadata refImage = refAutoImage.ImageWithMetadata;

        if (images.Length == 0)
        {
            return refImage with
            {
                Image = refImage.Image,
                Coordinates = refImage.Coordinates
            };
        }

        var mainImage = refImage.Image;
        var mainImageSizeOrigin = mainImage.Size;
        var mainPixelSize = refImage.GetPixelSize();
        var mainImageSize = new Size(mainImageSizeOrigin.Width * mainPixelSize.X.Nanometers, mainImageSizeOrigin.Height * mainPixelSize.Y.Nanometers);
        var refImageCenter = refAutoImage.ImageCenterNeutralPosition.Get2dPointFromLengthPoint();
        Point2d refLeftTopCorner = refImageCenter.GetImageLeftTopCorner(mainImageSize);
        List<IImage> rotatedImages = [];
        List<Point> imagePositions = [];
        List<double> opacities = [];

        StagePosition mainImageCenter = refImage.GetStagePosition();
        var mainCameraView = refImage.Coordinates!.CameraView;
        ThrowIfNotSupportedCameraView(mainCameraView);

        Angle mainImageTilt = mainImageCenter.Tilt - viewsPretilt[mainCameraView];
        bool mainImageIsMilling = false;

        if (mainCameraView == StageCameraView.FIB_Milling)
        {
            mainImageTilt -= FibMillingAngle;
            mainImageIsMilling = true;
        }

        for (int i = 0; i < images.Length; i++)
        {
            var imageWithMetadata = images[i].ImageWithMetadata;
            using var scaledImage = imageWithMetadata.Image.GetScaledImage(imageWithMetadata.GetPixelSize(), mainPixelSize, out _, out _);

            using ImageWithMetadata scaledImageWithMetadata = imageWithMetadata with { Image = scaledImage };
            ImageWithMetadata viewImageWithMetadata = scaledImageWithMetadata.TransformTo(ImageTransformationType.View);

            var secCameraView = imageWithMetadata.Coordinates.CameraView;
            ThrowIfNotSupportedCameraView(secCameraView);

            Angle secondaryTilt = viewImageWithMetadata.GetStagePosition().Tilt - viewsPretilt[secCameraView];
            bool transformToMilling = mainImageIsMilling;

            if (secCameraView == StageCameraView.FIB_Milling)
            {
                secondaryTilt -= FibMillingAngle;
                transformToMilling = false;
            }

            secondaryTilt -= mainImageTilt;
            IImage rotatedImage = secondaryTilt.IsInTolerance(Angle.Zero) ?
                viewImageWithMetadata.Image :
                viewImageWithMetadata.Image.FlipImageByZAndTiltByX(Angle.Zero, secondaryTilt, transformToMilling, out _);

            var rotatedImageSizeOrigin = rotatedImage.Size;
            var rotatedImageSize = new Size(rotatedImageSizeOrigin.Width * mainPixelSize.X.Nanometers, rotatedImageSizeOrigin.Height * mainPixelSize.Y.Nanometers);
            Point2d rotatedLeftTopCorner = images[i].ImageCenterNeutralPosition.Get2dPointFromLengthPoint().GetImageLeftTopCorner(rotatedImageSize); // from ref image center
            Point2d move = rotatedLeftTopCorner - refLeftTopCorner;
            move.X /= mainPixelSize.X.Nanometers;
            move.Y /= mainPixelSize.Y.Nanometers;

            if (move.X > -rotatedImageSizeOrigin.Width && move.X < mainImageSizeOrigin.Width &&
                move.Y > -rotatedImageSizeOrigin.Height && move.Y < mainImageSizeOrigin.Height)
            {
                imagePositions.Add(new(move.X, move.Y));
                rotatedImages.Add(rotatedImage);
                opacities.Add(images[i].Opacity);
            }
        }

        if (rotatedImages.Count == 0)
        {
            return refImage with
            {
                Image = refImage.Image,
                Coordinates = refImage.Coordinates
            };
        }

        // move second image by common point and merge with main image
        var resultImage = mainImage.ApplyPositioningOverlayWithOpacity([.. rotatedImages], [.. imagePositions], [.. opacities], gamma);
        rotatedImages.Dispose();

        return refImage with
        {
            Image = resultImage,
        };
    }

    /// <summary>
    /// Merge main image with other images by fiducial points.
    /// Scaling images is included.
    /// </summary>
    /// <param name="refMontageImage">Main fiducial montage image.</param>
    /// <param name="images">List of secondary fiducial montage images.</param>
    /// <param name="viewsPretilt">List of views pretilt.</param>
    /// <param name="gamma">Gamma correction used for AddWeighted images method.</param>
    /// <returns>Main image merged with other images by fiducial points.</returns>
    public static ImageWithMetadata MergeImagesByFiducialsPoints(
        FiducialMontageImage refMontageImage,
        ReadOnlySpan<FiducialMontageImage> images,
        ImmutableDictionary<StageCameraView, Angle> viewsPretilt,
        double gamma = 0.9)
    {
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        // expect refMontageImage to be in View orientation
        // it's coming correctly rotated from pipeline
        var refImage = refMontageImage.ImageWithMetadata;

        if (images.Length == 0 || refMontageImage.FiducialList.Count == 0)
        {
            return refImage with
            {
                Image = refImage.Image,
                Coordinates = refImage.Coordinates
            };
        }

        var mainImage = refImage.Image;
        var mainImageSizeOrigin = mainImage.Size;
        var mainPixelSize = refImage.GetPixelSize();

        var mainCameraView = refImage.Coordinates.CameraView;

        ThrowIfNotSupportedCameraView(mainCameraView);

        List<IImage> rotatedImages = [];
        List<Point> imagePositions = [];
        List<double> opacities = [];

        Angle mainImageTilt = refImage.GetStagePosition().Tilt - viewsPretilt[mainCameraView];
        bool mainImageIsMilling = false;

        if (mainCameraView == StageCameraView.FIB_Milling)
        {
            mainImageTilt -= FibMillingAngle;
            mainImageIsMilling = true;
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].FiducialList.Count == 0)
            {
                continue;
            }

            var imageWithMetadata = images[i].ImageWithMetadata;
            var scaledImage = imageWithMetadata.Image.GetScaledImage(imageWithMetadata.GetPixelSize(), mainPixelSize, out double percentX, out double percentY);
            
            var fiducialList = images[i].FiducialList
                .OrderBy(kv => kv.Key)
                .ToList();

            var mainFiducialList = refMontageImage.FiducialList
                .Where(kv => fiducialList.Any(fkv => fkv.Key == kv.Key))
                .OrderBy(kv => kv.Key)
                .ToList();

            var mainWarpPoints = mainFiducialList
                .Select(fp => new Point2f((float)fp.Value.X, (float)fp.Value.Y))
                .ToArray();

            var firstMainFiducialPoint = mainFiducialList.Select(kv => new Point2d(kv.Value.X, kv.Value.Y)).First();

            var fiducials2dPoints = fiducialList
                .Select(fp => new Point2d(fp.Value.X * percentX, fp.Value.Y * percentY))
                .ToArray();

            bool transformToMilling = mainImageIsMilling;

            IImage rotatedImage;
            Point2d[] rotatedFiducials = new Point2d[fiducialList.Count];
            var secCameraView = imageWithMetadata.Coordinates.CameraView;

            ThrowIfNotSupportedCameraView(secCameraView);

            for (int j = 0; j < fiducialList.Count; j++)
            {
                rotatedFiducials[j] = new Point2d(fiducials2dPoints[j].X, fiducials2dPoints[j].Y);
            }

            if (mainCameraView == secCameraView)
            {
                rotatedImage = scaledImage.Clone();
            }
            else
            {
                using ImageWithMetadata scaledImageWithMetadata = imageWithMetadata with { Image = scaledImage };
                // TODO: not necessary
                ImageWithMetadata viewImageWithMetadata = scaledImageWithMetadata.TransformTo(ImageTransformationType.View);
                Angle secondaryTilt = viewImageWithMetadata.GetStagePosition().Tilt - viewsPretilt[secCameraView];

                if (secCameraView == StageCameraView.FIB_Milling)
                {
                    secondaryTilt -= FibMillingAngle;
                    transformToMilling = false;
                }

                secondaryTilt -= mainImageTilt;

                if (secondaryTilt.IsInTolerance(Angle.Zero))
                {
                    rotatedImage = viewImageWithMetadata.Image;
                }
                else
                {
                    rotatedImage = viewImageWithMetadata.Image.FlipImageByZAndTiltByX(Angle.Zero, secondaryTilt, transformToMilling, out Mat tiltMatrix);

                    for (int j = 0; j < rotatedFiducials.Length; j++)
                    {
                        rotatedFiducials[j] = rotatedFiducials[j].Rotate2dPointAroundX(tiltMatrix);
                    }
                }
            }

            var rotatedImageSizeOrigin = rotatedImage.Size;
            var fiducialMove = rotatedFiducials[0] - firstMainFiducialPoint;
            Point2d move = -fiducialMove;

            if (rotatedFiducials.Length > 1)
            {
                var rotatedWarpPoints = rotatedFiducials.Get2fPointsForWarp(mainWarpPoints, out Point2f[] updatedMainWarpPoints);
                var size = new Size(
                        Math.Max(rotatedImageSizeOrigin.Width, mainImageSizeOrigin.Width),
                        Math.Max(rotatedImageSizeOrigin.Height, mainImageSizeOrigin.Height)

                    );

                if (rotatedWarpPoints.Length >= 4)
                {
                    var warpMatrix = Cv2.FindHomography(Mat.FromArray(rotatedWarpPoints), Mat.FromArray(updatedMainWarpPoints), 0);
                    Cv2.WarpPerspective(rotatedImage.InputArray, rotatedImage.OutputArray, warpMatrix, size);
                }
                else
                {
                    var warpMatrix = Cv2.GetAffineTransform(rotatedWarpPoints, updatedMainWarpPoints);
                    Cv2.WarpAffine(rotatedImage.InputArray, rotatedImage.OutputArray, warpMatrix!, size, InterpolationFlags.Cubic);
                }

                move = new Point2d(0.0, 0.0);
            }

            if (move.X > -rotatedImageSizeOrigin.Width && move.X < mainImageSizeOrigin.Width &&
                move.Y > -rotatedImageSizeOrigin.Height && move.Y < mainImageSizeOrigin.Height)
            {
                imagePositions.Add(new(move.X, move.Y));
                rotatedImages.Add(rotatedImage);
                opacities.Add(images[i].Opacity);
            }
        }

        if (rotatedImages.Count == 0)
        {
            return refImage with
            {
                Image = refImage.Image,
                Coordinates = refImage.Coordinates
            };
        }

        // move second image by common point and merge with main image
        var resultImage = mainImage.ApplyPositioningOverlayWithOpacity(rotatedImages.ToArray(), imagePositions.ToArray(), opacities.ToArray(), gamma);
        rotatedImages.Dispose();

        return refImage with
        {
            Image = resultImage,
        };
    }

    /// <summary>
    /// Rotate image corner points around X axis.
    /// </summary>
    /// <param name="image">Image.</param>
    /// <param name="tilt">Tilt angle.</param>
    /// <param name="rotatedCorners">Output rotated corners points.</param>
    /// <param name="newImageSize">New image size after rotation by X.</param>
    /// <returns>Image corners points.</returns>
    public static Point2f[] RotateCornersAroundX(
        this IImage image,
        Angle tilt,
        bool transformToMilling,
        out Point2f[] rotatedCorners,
        out Size newImageSize
    )
    {
        // 3D rotation around X-axis
        double cosA = Math.Cos(tilt.Radians);

        Size imageSize = image.Size;

        double widthHalf = (double)(imageSize.Width / 2.0);
        double heightHalf = (double)(imageSize.Height / 2.0);

        Point2f[] corners =
            [
                new ((float)-widthHalf, (float)-heightHalf),
                new ((float) widthHalf, (float)-heightHalf),
                new ((float) widthHalf, (float) heightHalf),
                new ((float)-widthHalf, (float) heightHalf)
            ];

        rotatedCorners = new Point2f[corners.Length];

        for (int i = 0; i < corners.Length; i++)
        {
            // Apply rotation matrix for X-axis rotation, - sin is not needed because Z = 0
            rotatedCorners[i] = transformToMilling
                ? new Point2f(corners[i].X, (float)(cosA * corners[i].Y))
                : new Point2f(corners[i].X, (float)((2 - cosA) * corners[i].Y));
        }

        newImageSize = new Size((int)Math.Round(rotatedCorners[2].X - rotatedCorners[0].X), (int)Math.Round(rotatedCorners[2].Y - rotatedCorners[0].Y));
        return corners;
    }

    /// <summary>
    /// Rotate image 2d point around X axis.
    /// </summary>
    /// <param name="point">Image 2d point.</param>
    /// <param name="tiltMatrix">Tilt matrix.</param>
    /// <returns>Rotated image point.</returns>
    public static Point2d Rotate2dPointAroundX(this Point2d point, Mat tiltMatrix)
    {
        Mat pointM = point.GetHomogenousMatFrom2DPoint();
        pointM = tiltMatrix * pointM;

        var w = pointM.At<double>(2, 0);

        if (w == 0 || Math.Abs(w) < 0.001)
        {
            w = (double)1.0;
        }

        return new Point2d(
            (double)(pointM.At<double>(0, 0) / w),
            (double)(pointM.At<double>(1, 0) / w)
        );
    }
}
