using CommunityToolkit.Diagnostics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using UnitsNet;
using Betrian.Imaging.Common;

namespace Betrian.Imaging.Algorithms.Utilities;

public static partial class Filters
{
    public static SortedDictionary<int, double> GetNormalizedHistogram(this IImage image, int binCount = 256)
    {
        ArgumentNullException.ThrowIfNull(image);

        using Mat histogram = image.CalculateHistogram(binCount);
        Cv2.Normalize(histogram, histogram);

        SortedDictionary<int, double> hist = [];
        for (int i = 0; i < binCount; i++)
        {
            hist.Add(i, histogram.Get<float>(i));
        }
        return hist;
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

        var histogram = bgrImage.GetNormalizedHistogram();
        return histogram;
    }

    /// <summary>
    /// Update image by min and max values.
    /// </summary>
    /// <param name="image">Image to update.</param>
    /// <param name="min">Minimum histogram value in range 0..255.</param>
    /// <param name="max">Maximum histogram value in range 0..255.</param>
    /// <returns>Updated image.</returns>
    public static Mat UpdateImageByMinAndMaxHistogramValues(this Mat image, int min, int max)
    {
        Guard.IsInRange(min, -1, 256);
        Guard.IsInRange(max, -1, 256);
        Guard.IsLessThan(min, max);

        var maxValue = 255;
        int bytesPerPixel = 1;
        int itemsCount = max - min;
        if (image.Type().Depth != MatType.CV_8U)
        {
            bytesPerPixel = 2;
            maxValue = ushort.MaxValue;
            min *= 257;
            itemsCount *= 257;
        }

        Mat[] channels = image.Split();
        Mat[] updatedChannels = new Mat[channels.Length];

        int rows = channels[0].Rows;
        int cols = channels[0].Cols;
        byte[] imageData = new byte[rows * cols * bytesPerPixel];
        Marshal.Copy(channels[0].Data, imageData, 0, imageData.Length);

        if (image.Type().Depth != MatType.CV_8U)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int pixelIndex = row * cols * bytesPerPixel + col * bytesPerPixel;
                    ushort pixelValue = (ushort)(imageData[pixelIndex] | (imageData[pixelIndex + 1] << 8));
                    pixelValue = pixelValue > 0 && pixelValue < maxValue
                        ? (ushort)Math.Min(Math.Round((double)((double)Math.Abs(pixelValue - min) / itemsCount) * maxValue), maxValue)
                        : pixelValue;
                    imageData[pixelIndex] = (byte)(pixelValue & 0xFF);
                    imageData[pixelIndex + 1] = (byte)(pixelValue >> 8);
                }
            }
        }
        else
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int pixelIndex = row * cols * bytesPerPixel + col * bytesPerPixel;
                    byte pixelValue = imageData[pixelIndex];
                    imageData[pixelIndex] = pixelValue > 0 && pixelValue < maxValue
                        ? (byte)Math.Min(Math.Round((double)((double)Math.Abs(pixelValue - min) / itemsCount) * maxValue), maxValue)
                        : pixelValue;
                }
            }
        }

        var imageNew = new Mat(channels[0].Size(), channels[0].Type());
        Marshal.Copy(imageData, 0, imageNew.Data, imageData.Length);

        for (int i = 0; i < channels.Length; i++)
        {
            updatedChannels[i] = new Mat();
            imageNew.CopyTo(updatedChannels[i]);
        }

        Mat updatedImage = new();
        Cv2.Merge(updatedChannels, updatedImage);
        channels.Dispose();
        updatedChannels.Dispose();
        return updatedImage;
    }

    /// <summary>
    /// Update image by min and max values. Using LUT.
    /// </summary>
    /// <param name="image">Image to update.</param>
    /// <param name="min">Minimum histogram value in range 0..255.</param>
    /// <param name="max">Maximum histogram value in range 0..255.</param>
    /// <returns>Updated image.</returns>
    public static IImage UpdateImageByMinAndMaxHistogramValuesUsingLut(this IImage image, int min, int max)
    {
        var result = image.Clone();
        UpdateBrightnessContrastInplace(result, min, max);
        return result;
    }

    public static void UpdateImageByMinAndMaxHistogramValuesUsingLutInplace(this IImage image, int min, int max)
    {
        Guard.IsInRange(min, -1, 256);
        Guard.IsInRange(max, -1, 256);
        Guard.IsLessThan(min, max);

        if (min == 0 && max == 255)
        {
            return;
        }

        var maxValue = 255;
        int itemsCount = max - min;
        if (image.Depth != 8)
        {
            maxValue = ushort.MaxValue;
            min *= 257;
            itemsCount *= 257;
        }

        Mat[] channels = image.Mat.Split();
        Mat[] updatedChannels = new Mat[channels.Length];
        Mat tempMat;

        if (image.Depth != 8)
        {
            ushort[] lut = new ushort[maxValue + 1];

            for (int i = 0; i <= maxValue; i++)
            {
                var helpValue = i - min;
                helpValue = helpValue <= 0 ? 0 : helpValue;

                lut[i] = i > 0 && i < maxValue
                    ? (ushort)Math.Min(Math.Round((double)((double)helpValue / itemsCount) * maxValue), maxValue)
                    : (ushort)i;
            }

            tempMat = channels[0].ApplyLut(lut);
        }
        else
        {
            byte[] lut = new byte[maxValue + 1];

            for (int i = 0; i <= maxValue; i++)
            {
                var helpValue = i - min;
                helpValue = helpValue <= 0 ? 0 : helpValue;

                lut[i] = i > 0 && i < maxValue
                    ? (byte)Math.Min(Math.Round((double)((double)helpValue / itemsCount) * maxValue), maxValue)
                    : (byte)i;
            }

            tempMat = channels[0].ApplyLut(lut);
        }

        for (int i = 0; i < channels.Length; i++)
        {
            updatedChannels[i] = new Mat();
            tempMat.CopyTo(updatedChannels[i]);
        }

        Cv2.Merge(updatedChannels, image.Mat);
        if (tempMat is { IsDisposed: false })
        {
            tempMat.Dispose();
        }
        channels.Dispose();
        updatedChannels.Dispose();
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
        Rangef[] ranges = [new Rangef(0f, image.Depth == 8 ? 255f : 65535f)];
        Cv2.CalcHist([image.Mat], [0], null, histogram, 1, [binCount], ranges);
        return histogram;
    }

    public static (int HistogramMin, int HistogramMax) GetMinMaxHistogramValues(this IImage image, Ratio minPercent, Ratio maxPercent)
    {
        int histogramMin = 0;
        int histogramMax = 255;

        var totalPixels = image.Mat.Rows * image.Mat.Cols;
        var min = minPercent.DecimalFractions * totalPixels;
        var max = maxPercent.DecimalFractions * totalPixels;

        using Mat histogram = image.CalculateHistogram(256);

        for (int i = 0; i <= histogram.Rows; i++)
        {
            var value = histogram.At<float>(i);

            if (value >= min)
            {
                histogramMin = i;
                break;
            }
        }

        for (int i = histogram.Rows - 1; i >= 0; i--)
        {
            var value = histogram.At<float>(i);

            if (value >= max)
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

    ///// <summary>
    ///// Get minimum and maximum from histogram.
    ///// </summary>
    ///// <param name="histogram">Image histogram.</param>
    ///// <param name="min">Output - minimum histogram value in range 0..255 or 0..65535.</param>
    ///// <param name="max">Output - maximum histogram value in range 0..255 or 0..65535.</param>
    //private static (int HistogramMin, int HistogramMax) GetMinMaxHistogramValues(this IImage histogram)
    //{
    //    var indexer = histogram.GetGenericIndexer<float>();
    //    int min = -1;
    //    int max = 0;

    //    for (int i = 0; i < histogram.Rows; i++)
    //    {
    //        if (indexer[i] > 0)
    //        {
    //            if (min < 0)
    //            {
    //                min = i;
    //            }
    //            else
    //            {
    //                max = i;
    //            }
    //        }
    //    }

    //    return (min, max);
    //}

    ///// <summary>
    ///// Get minimum and maximum from two images comparison.
    ///// </summary>
    ///// <param name="image">Updated image.</param>
    ///// <param name="originImage">Origin image.</param>
    ///// <param name="min">Minimum updated image histogram value in range 0..255.</param>
    ///// <param name="max">Maximum updated image histogram value in range 0..255.</param>
    ///// <param name="minValue">Output - minimum value in origin image on max position. Range 0..255.</param>
    ///// <param name="maxValue">Output - maximum value in origin image on min position. Range 0..255.</param>
    //private static (int CalculatedMin, int CalculatedMax) GetMinMaxValues(this Mat image, Mat originImage, int min, int max)
    //{
    //    using Mat exposedChannel = image.Split()[0];
    //    using Mat originChannel = originImage.Split()[0];
    //    var maxValue = 255;
    //    var minValue = 0;
    //    int bytesPerPixel = 1;

    //    if (image.Type().Depth != MatType.CV_8U)
    //    {
    //        bytesPerPixel = 2;
    //        maxValue = ushort.MaxValue;
    //    }

    //    int rows = exposedChannel.Rows;
    //    int cols = exposedChannel.Cols;
    //    byte[] imageOriginData = new byte[rows * cols * bytesPerPixel];
    //    Marshal.Copy(originChannel.Data, imageOriginData, 0, imageOriginData.Length);
    //    byte[] imageExposedData = new byte[rows * cols * bytesPerPixel];
    //    Marshal.Copy(exposedChannel.Data, imageExposedData, 0, imageExposedData.Length);

    //    if (originChannel.Type().Depth != MatType.CV_8U)
    //    {
    //        for (int row = 0; row < rows; row++)
    //        {
    //            for (int col = 0; col < cols; col++)
    //            {
    //                int pixelIndex = row * cols * bytesPerPixel + col * bytesPerPixel;

    //                ushort exposedValue = (ushort)(imageExposedData[pixelIndex] | (imageExposedData[pixelIndex + 1] << 8));
    //                ushort originValue = (ushort)(imageOriginData[pixelIndex] | (imageOriginData[pixelIndex + 1] << 8));

    //                if (exposedValue == min)
    //                {
    //                    minValue = originValue > minValue ? originValue : minValue;
    //                }
    //                else if (exposedValue == max)
    //                {
    //                    maxValue = originValue < maxValue ? originValue : maxValue;
    //                }
    //            }
    //        }

    //        minValue /= 257;
    //        maxValue /= 257;
    //    }
    //    else
    //    {
    //        for (int row = 0; row < rows; row++)
    //        {
    //            for (int col = 0; col < cols; col++)
    //            {
    //                int pixelIndex = row * cols * bytesPerPixel + col * bytesPerPixel;
    //                byte exposedValue = imageExposedData[pixelIndex];
    //                byte originValue = imageOriginData[pixelIndex];

    //                if (exposedValue == min)
    //                {
    //                    minValue = originValue > minValue ? originValue : minValue;
    //                }
    //                else if (exposedValue == max)
    //                {
    //                    maxValue = originValue < maxValue ? originValue : maxValue;
    //                }
    //            }
    //        }
    //    }

    //    return (minValue, maxValue);
    //}
}
