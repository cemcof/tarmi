using Tarmi.Imaging.Common.OpenCvWrapper;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static partial class Filters
{
    /// <summary>
    /// Apply gamma on grayscale or bgr image.
    /// </summary>
    /// <param name="image">Origin image. 8 bit or 16 bit (will be changed to 8 bit).</param>
    /// <param name="gamma">Current gamma value. Range 0..10</param>
    /// <returns>Image (8 bit) with applied gamma correction.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throws exception when gamma is not in range 0..10</exception>
    // TODO: update to our LUT for 16bit images if needed
    public static Mat ApplyGamma(this Mat image, double gamma)
    {
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        var conversionNeeded = image.Type().Equals(MatType.CV_16U) || image.Type().Equals(MatType.CV_16UC3);

        using Mat origin = conversionNeeded ? new(image.Size(), MatType.CV_8UC3) : image.Clone();
        if (conversionNeeded)
        {
            image.ConvertTo(origin, MatType.CV_8UC3, 1.0 / 256);
        }

        byte[] lut = new byte[256];

        for (int i = 0; i < lut.Length; i++)
        {
            lut[i] = (byte)Math.Min(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0, 255.0);
        }

        var gammaImage = new Mat(origin.Size(), origin.Type());
        Cv2.LUT(origin, lut, gammaImage);

        return gammaImage;
    }

    public static void ApplyGammaInplace(this Image<Bgr, byte> image, double gamma)
    {
        Guard.IsBetweenOrEqualTo(gamma, 0.0, 10.0);

        byte[] lut = new byte[256];
        for (int i = 0; i < lut.Length; i++)
        {
            lut[i] = (byte)Math.Min(Math.Pow(i / 255.0, 1.0 / gamma) * 255.0, 255.0);
        }

        Cv2.LUT(image.InputArray, lut, image.OutputArray);
    }
}
