using Tarmi.Imaging.Common;
using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static partial class Filters
{
    // TODO: normalize output encodings usage
    // https://stackoverflow.com/questions/11456565/opencv-mean-sd-filter

    public static Mat StandardDeviation(this Mat image, Size kSize)
    {
        var result = new Mat();
        image.ConvertTo(result, MatType.CV_32F);
        using var mu = new Mat();
        Cv2.Blur(result, mu, kSize);
        using var mu2 = new Mat();
        Cv2.Blur(result.Mul(result), mu2, kSize);
        Cv2.Sqrt(mu2 - mu.Mul(mu), result);
        return result;
    }

    public static Mat LocalRegionEnergy(Mat image, Size kSize)
    {
        var lre = new Mat();
        image.ConvertTo(lre, MatType.CV_32F);
        Cv2.Multiply(lre, lre, lre);
        Cv2.GaussianBlur(lre, lre, kSize, 0);
        return lre;
    }

    public static IImage UpdateBrightnessContrast(this IImage image, int brightness, int contrast)
    {
        var result = image.Clone();
        UpdateBrightnessContrastInplace(image, brightness, contrast);
        return result;
    }

    public static void UpdateBrightnessContrastInplace(this IImage image, int brightness, int contrast)
    {
        brightness -= 100;
        contrast -= 100;

        double alpha;
        double beta;

        if (contrast > 0)
        {
            double delta = 127f * contrast / 100f;
            alpha = 255f / (255f - delta * 2);
            beta = alpha * (brightness - delta);
        }
        else
        {
            double delta = -128f * contrast / 100;
            alpha = (256f - delta * 2) / 255f;
            beta = alpha * brightness + delta;
        }

        image.Mat.ConvertTo(image.OutputArray, MatType.CV_8UC3, alpha, beta);
    }
}
