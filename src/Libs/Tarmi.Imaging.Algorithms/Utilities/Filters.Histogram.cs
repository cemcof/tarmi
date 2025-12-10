using CommunityToolkit.Diagnostics;
using OpenCvSharp;
using UnitsNet;
using Tarmi.Imaging.Common;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static partial class Filters
{
    public static SortedDictionary<int, double> GetNormalizedHistogram(this IImage image, int binCount = 256)
    {
        ArgumentNullException.ThrowIfNull(image);

        using Mat histogram = image.CalculateHistogram(binCount);
        Cv2.Normalize(histogram, histogram);
        var histogramSpan = histogram.AsSpan<float>();

        SortedDictionary<int, double> result = [];
        for (int i = 0; i < histogramSpan.Length; i++)
        {
            result.Add(i, histogramSpan[i]);
        }
        return result;
    }

    public static (IImage NewImage, SortedDictionary<int, double> Histogram) UpdateImageByAutoExposure(
        this IImage bgrImage, double clipLimit = 20.0, Size? tileGridSize = null
    )
    {
        var result = bgrImage.Clone();
        var histogram = UpdateImageByAutoExposureInplace(result, clipLimit, tileGridSize);
        return (result, histogram);
    }

    public static SortedDictionary<int, double> UpdateImageByAutoExposureInplace(
        this IImage bgrImage, double clipLimit = 20.0, Size? tileGridSize = null
    )
    {
        tileGridSize ??= new Size(16, 16);
        Mat[] channels = bgrImage.Mat.Split();
        var clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
        using var equalizedImage = new Mat();
        clahe.Apply(channels[0], equalizedImage);
        Mat[] exposedImages = new Mat[channels.Length];

        for (int i = 0; i < channels.Length; i++)
        {
            exposedImages[i] = new Mat();
            equalizedImage.CopyTo(exposedImages[i]);
        }

        Cv2.Merge(exposedImages, bgrImage.Mat);
        channels.Dispose();
        exposedImages.Dispose();

        return bgrImage.GetNormalizedHistogram();
    }

    private const ushort ByteToUnsignedShortCoefficient = ushort.MaxValue / byte.MaxValue;

    public static void UpdateImageByMinAndMaxHistogramValuesUsingLutInplace(this IImage image, int min, int max)
    {
        Guard.IsBetweenOrEqualTo(min, byte.MinValue, byte.MaxValue);
        Guard.IsBetweenOrEqualTo(max, byte.MinValue, byte.MaxValue);
        Guard.IsLessThan(min, max);

        if (min == byte.MinValue && max == byte.MaxValue)
        {
            return;
        }

        using Mat channel = image.Mat.ExtractChannel(0);

        double valuesRange = max - min;
        if (image.Depth == 8)
        {
            byte[] lookUpTable = new byte[byte.MaxValue + 1];

            lookUpTable[0] = 0;
            lookUpTable[byte.MaxValue] = byte.MaxValue;

            for (int i = 1; i < byte.MaxValue; i++)
            {
                var minMaxScale = (i - min) / valuesRange;
                lookUpTable[i] = (byte)Math.Clamp(minMaxScale * byte.MaxValue, byte.MinValue, byte.MaxValue);
            }

            channel.ApplyLutInplace(lookUpTable);
        }
        else
        {
            min *= ByteToUnsignedShortCoefficient;
            valuesRange *= ByteToUnsignedShortCoefficient;

            var lookUpTable = new ushort[ushort.MaxValue + 1];

            lookUpTable[0] = 0;
            lookUpTable[ushort.MaxValue] = ushort.MaxValue;

            for (int i = 1; i < ushort.MaxValue; i++)
            {
                var minMaxScale = (i - min) / valuesRange;
                lookUpTable[i] = (ushort)Math.Clamp(minMaxScale * ushort.MaxValue, ushort.MinValue, ushort.MaxValue);
            }
            channel.ApplyLutInplace(lookUpTable);
        }

        var channelCount = image.Mat.Channels();
        for (int i = 0; i < channelCount; i++)
        {
            channel.InsertChannel(image.Mat, i);
        }
    }

    /// <summary>
    /// Calculate histogram with default binCount 256 and range 0..255
    /// </summary>
    /// <param name="image">Grayscale image.</param>
    /// <param name="binCount">Bin count. Default is 256.</param>
    /// <returns>Histogram mat.</returns>
    public static Mat CalculateHistogram(this IImage image, int binCount = 256)
    {
        Mat histogram = new();
        Rangef[] ranges = [new(0, image.Depth == 8 ? byte.MaxValue : ushort.MaxValue)];
        Cv2.CalcHist([image.Mat], [0], null, histogram, 1, [binCount], ranges);
        return histogram;
    }

    public static (int HistogramMin, int HistogramMax) GetMinMaxHistogramValues(this IImage image, Ratio minRatio, Ratio maxRatio)
    {
        int histogramMin = 0;
        int histogramMax = 255;

        var totalPixels = image.Mat.Rows * image.Mat.Cols;
        var min = minRatio.DecimalFractions * totalPixels;
        var max = maxRatio.DecimalFractions * totalPixels;

        using Mat histogram = image.CalculateHistogram(256);
        var histogramSpan = histogram.AsSpan<float>();

        for (int i = 0; i < histogramSpan.Length; i++)
        {
            if (histogramSpan[i] >= min)
            {
                histogramMin = i;
                break;
            }
        }

        for (int i = histogramSpan.Length - 1; i >= 0; i--)
        {
            if (histogramSpan[i] >= max)
            {
                histogramMax = i;
                break;
            }
        }

        if (histogramMin >= histogramMax)
        {
            histogramMin = 0;
            histogramMax = 255;
        }

        return (histogramMin, histogramMax);
    }
}
