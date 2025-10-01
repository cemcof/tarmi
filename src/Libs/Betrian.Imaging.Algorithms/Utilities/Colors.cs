using OpenCvSharp;

namespace Betrian.Imaging.Algorithms.Utilities;

public static class Colors
{
    /// <summary>
    /// Transparent defined color in image.
    /// </summary>
    /// <param name="image">Image for set transparent.</param>
    /// <param name="color">Scalar color minimum values - this and less will be set as transparent.</param>
    /// <param name="transparency">0 - fully transparent, 255 - no transparent</param>
    /// <param name="makeItBlack">Make colors, which are darker than scalar color - Val0, black.</param>
    /// <returns>Image BGRA with transparency.</returns>
    public static Mat SetTransparentColor(this Mat image, Scalar color, byte transparency = 0, bool makeItBlack = false)
    {
        using var bgraImage = new Mat();

        if (makeItBlack)
        {
            using var tempImage = new Mat();
            _ = Cv2.Threshold(image, tempImage, color.Val0, 255, ThresholdTypes.Binary | ThresholdTypes.Tozero);
            Cv2.CvtColor(tempImage, bgraImage, ColorConversionCodes.BGR2BGRA);
        }
        else
        {
            Cv2.CvtColor(image, bgraImage, ColorConversionCodes.BGR2BGRA);
        }

        Cv2.Split(bgraImage, out Mat[] channels);

        using Mat blueChannel = channels[0];
        var blueIndexer = blueChannel.GetGenericIndexer<byte>();
        using Mat greenChannel = channels[1];
        var greenIndexer = greenChannel.GetGenericIndexer<byte>();
        using Mat redChannel = channels[2];
        var redIndexer = redChannel.GetGenericIndexer<byte>();
        using Mat alphaChannel = channels[3];

        for (int i = 0; i < blueChannel.Height; i++)
        {
            for (int j = 0; j < blueChannel.Width; j++)
            {
                if (blueIndexer[i, j] <= color.Val0 && greenIndexer[i, j] <= color.Val1 && redIndexer[i, j] <= color.Val2)
                {
                    alphaChannel.At<byte>(i, j) = transparency;
                }
            }
        }

        var transparent = new Mat();
        Mat[] newChannels = [blueChannel, greenChannel, redChannel, alphaChannel];
        Cv2.Merge(newChannels, transparent);

        return transparent;
    }

    /// <summary>
    /// Transparent black color in BGR image.
    /// </summary>
    /// <param name="image">BGR image for set black transparency.</param>
    /// <param name="transparency">0 - fully transparent, 255 - no transparent. Default is 0.</param>
    /// <returns>Image bgra with black transparency.</returns>
    public static Mat SetTransparentBlack(this Mat image, byte transparency = 0)
    {
        using var bgraImage = new Mat();
        Cv2.CvtColor(image, bgraImage, ColorConversionCodes.BGR2BGRA);

        Cv2.Split(bgraImage, out var channels);

        using Mat blueChannel = channels[0];
        var blueIndexer = blueChannel.GetGenericIndexer<byte>();
        using Mat greenChannel = channels[1];
        var greenIndexer = greenChannel.GetGenericIndexer<byte>();
        using Mat redChannel = channels[2];
        var redIndexer = redChannel.GetGenericIndexer<byte>();
        using Mat alphaChannel = channels[3];

        for (int i = 0; i < blueChannel.Height; i++)
        {
            for (int j = 0; j < blueChannel.Width; j++)
            {
                if (blueIndexer[i, j] <= 0 && greenIndexer[i, j] <= 0 && redIndexer[i, j] <= 0)
                {
                    alphaChannel.At<byte>(i, j) = transparency;
                }
            }
        }

        var transparent = new Mat(image.Height, image.Width, MatType.CV_8UC4);
        Mat[] newChannels = [blueChannel, greenChannel, redChannel, alphaChannel];
        Cv2.Merge(newChannels, transparent);

        return transparent;
    }
}
