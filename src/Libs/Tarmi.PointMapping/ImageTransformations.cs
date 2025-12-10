using OpenCvSharp;
using UnitsNet;
using Tarmi.Models;
using Tarmi.Imaging.Common;

namespace Tarmi.PointMapping;

public static class ImageTransformations
{
    /// <summary>
    /// Flip image by Z axis when rotation angle is near 180. Tilt by X axis. Using image center point.
    /// Image size will stay the same - there could be some border image data lost.
    /// </summary>
    /// <param name="image">Origin image.</param>
    /// <param name="rotationAngle">Rotation angle around Z.</param>
    /// <param name="tiltAngle">Tilt angle around X.</param>
    /// <returns>Flipped and rotated image.</returns>
    public static IImage FlipImageByZAndTiltByX(
        this IImage image,
        Angle rotationAngle,
        Angle tiltAngle,
        bool transformToMilling,
        out Mat tiltMatrix)
    {
        IImage imageCopy = rotationAngle.IsInTolerance(Angle.FromDegrees(180.0)) ?
            image.Flip(FlipMode.XY) :
            image.Clone();

        if (tiltAngle.IsInTolerance(Angle.Zero))
        {
            tiltMatrix = Mat.Eye(3, 3, MatType.CV_64FC1);
            return imageCopy;
        }

        IImage tiltImage = imageCopy.CopyBlank();
        Point2f[] corners = image.RotateCornersAroundX(tiltAngle, transformToMilling, out Point2f[] rotatedCorners, out Size newImageSize);
        tiltMatrix = Cv2.GetPerspectiveTransform(corners, rotatedCorners);
        Cv2.WarpPerspective(imageCopy.InputArray, tiltImage.OutputArray, tiltMatrix, newImageSize, InterpolationFlags.Cubic);

        imageCopy.Dispose();
        return tiltImage;
    }

    /// <summary>
    /// Update image by another scaling.
    /// </summary>
    /// <param name="image">Image with source pixel size.</param>
    /// <param name="sourcePixelSize">Source pixel size.</param>
    /// <param name="targetPixelSize">Target pixel size.</param>
    /// <param name="percentX">Output X percent scale.</param>
    /// <param name="percentY">Output Y percent scale.</param>
    /// <returns>Image with target pixel size.</returns>
    public static IImage GetScaledImage(this IImage image, PixelSize sourcePixelSize, PixelSize targetPixelSize, out double percentX, out double percentY)
    {
        percentX = 1.0;
        percentY = 1.0;

        if (!Equals(targetPixelSize.X, sourcePixelSize.X) || !Equals(targetPixelSize.Y, sourcePixelSize.Y))
        {
            percentX = sourcePixelSize.X.Nanometers / targetPixelSize.X.Nanometers;
            percentY = sourcePixelSize.Y.Nanometers / targetPixelSize.Y.Nanometers;
            var rotatedSize = image.Size;
            rotatedSize.Width = (int)(rotatedSize.Width * percentX);
            rotatedSize.Height = (int)(rotatedSize.Height * percentY);
            return image.Resize(rotatedSize.Width, rotatedSize.Height, InterpolationFlags.Linear);
        }
        else
        {
            return image.Clone();
        }
    }
}
