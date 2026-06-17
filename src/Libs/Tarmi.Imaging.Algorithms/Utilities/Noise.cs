using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static class Noise
{
    public static Mat ApplyGaussianNoise(this Mat image, double mean = 35, double stdDev = 10)
    {
        var result = image.Clone();
        ApplyGaussianNoiseInplace(result, mean, stdDev);
        return result;
    }

    public static void ApplyGaussianNoiseInplace(this Mat image, double mean = 35, double stdDev = 10)
    {
        var imageType = image.Type();
        using var gaussianNoise = new Mat(image.Size(), imageType);
        if (imageType.Depth != MatType.CV_8U)
        {
            mean *= 257;
            stdDev *= 257;
        }

        Cv2.Randn(gaussianNoise, mean, stdDev);
        Cv2.Add(image, gaussianNoise, image);
        //image += gaussianNoise;
    }

    public static Mat ApplySaltAndPepperNoise(this Mat image, double saltProbability = 0.5, double pepperProbability = 0.5)
    {
        var result = image.Clone();
        ApplySaltAndPepperNoiseInplace(result, saltProbability, pepperProbability);
        return result;
    }

    public static void ApplySaltAndPepperNoiseInplace(this Mat image, double saltProbability = 0.5, double pepperProbability = 0.5)
    {
        const double SaltMin = 235;
        const double SaltMax = 255;
        const double PepperMin = 0;
        const double PepperMax = 20;

        using var saltPepperNoise = new Mat(image.Size(), MatType.CV_8UC1);
        Cv2.Randu(saltPepperNoise, 0, 255);

        using var white = new Mat();

        var saltThreshold = saltProbability * (SaltMax - SaltMin) + SaltMin;
        _ = Cv2.Threshold(saltPepperNoise, white, saltThreshold, 255, ThresholdTypes.Triangle);

        using var black = new Mat();

        var pepperThreshold = pepperProbability * (PepperMax - PepperMin);
        pepperThreshold = pepperThreshold == 0 ? PepperMin : pepperThreshold;
        _ = Cv2.Threshold(saltPepperNoise, black, pepperThreshold, 255, ThresholdTypes.Triangle);
        _ = image.SetTo(0, black).SetTo(255, white);
    }
}
