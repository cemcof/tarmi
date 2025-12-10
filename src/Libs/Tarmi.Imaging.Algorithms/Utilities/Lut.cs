using Tarmi.Imaging.Common.OpenCvWrapper;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static class Lut
{
    /// <summary>
    /// Fill Mat for LUT with exp. LUT Mat could be only 8 bit.
    /// </summary>
    /// <param name="lookupTable">Current Mat.</param>
    /// <param name="exp">Exp to add into Mat.</param>
    private static void FillLookupTable(this Mat lookupTable, double exp)
    {
        for (int i = 0; i <= byte.MaxValue; i++)
        {
            lookupTable.At<byte>(0, i) = (byte)Math.Min((int)Math.Pow(i, exp), byte.MaxValue);
        }
    }

    /// <summary>
    /// Fill array for LUT with exp. 
    /// Method is used only for 16 bit (do not use for Cv2.LUT method, use ApplyLUT() instead)
    /// </summary>
    /// <param name="exp">Exp to add into Mat.</param>
    private static ushort[] FillLookupTable(double exp)
    {
        var maxValue = ushort.MaxValue;
        ushort[] lut = new ushort[maxValue + 1];

        for (int j = 0; j <= maxValue; j++)
        {
            lut[j] = (ushort)Math.Min((int)Math.Pow(j, exp), maxValue);
        }

        return lut;
    }

    /// <summary>
    /// Get color LUT filter Mat array from light frequency.
    /// </summary>
    /// <param name="lightWavelength">Light frequency. Range 380..780</param>
    /// <param name="binCount">Bin count for 8-bit is 255. For 16-bit should be 65 535.</param>
    /// <returns>Array with Mat for BGR. Using as filter for LUT function.</returns>
    public static Array GetLutFilterFromWavelength(this double lightWavelength, int bitDepth = 8)
    {
        if (bitDepth == 8)
        {
            byte[] bgr = (byte[])ColorSpace.ChangeLightWavelengthToBGR(lightWavelength, bitDepth);
            return GetLutFilterFromBgr(bgr);
        }
        else
        {
            ushort[] bgr = (ushort[])ColorSpace.ChangeLightWavelengthToBGR(lightWavelength, bitDepth);
            return GetLutFilterFromBgr(bgr);
        }
    }

    public static Array GetLutFilterFromBgr(byte[] bgr)
    {
        Mat[] bgrLookupTable = new Mat[3];
        for (int i = 0; i < 3; i++)
        {
            bgrLookupTable[i] = new Mat(1, 256, MatType.CV_8U);
            bgrLookupTable[i].FillLookupTable(bgr[i] / ColorSpace.ColorTo02Range);
        }
        return bgrLookupTable;
    }

    public static Array GetLutFilterFromBgr(ushort[] bgr)
    {
        ushort[][] bgrLookupTable =
        [
            FillLookupTable(bgr[0] / ColorSpace.Color16To02Range),
            FillLookupTable(bgr[1] / ColorSpace.Color16To02Range),
            FillLookupTable(bgr[2] / ColorSpace.Color16To02Range)
        ];
        return bgrLookupTable;
    }

    public static Mat LutWithBgrFilter(this Mat bgrImage, Array bgrFilter)
    {
        Mat result = bgrImage.Type() != MatType.CV_8UC1 && bgrImage.Type() != MatType.CV_16UC1
            ? bgrImage.CvtColor(ColorConversionCodes.GRAY2BGR)
            : bgrImage.Clone();
        LutWithBgrFilterInplace(result, bgrFilter);
        return result;
    }

    public static void LutWithBgrFilterInplace(this Image<Bgr, byte> bgrImage, Array bgrFilter)
    {
        LutWithBgrFilterInplace(bgrImage.Mat, bgrFilter);
    }

    /// <summary>
    /// LUT application on image by defined BGR color filter.
    /// </summary>
    /// <param name="bgrImage">Image in BGR format.</param>
    /// <param name="bgrFilter">BGR filter color. Array of blue, green and red.</param>
    /// <returns>Image proceed by color filter.</returns>
    public static void LutWithBgrFilterInplace(this Mat bgrImage, Array bgrFilter)
    {
        Guard.IsGreaterThanOrEqualTo(bgrFilter.Length, 3);

        if (bgrImage.Type() != MatType.CV_8UC3)
        {
            throw new ArgumentException("Image must be in BGR format (CV_8UC3).", nameof(bgrImage));
        }

        Cv2.Split(bgrImage, out var channels);
        Mat[] newChannels = new Mat[channels.Length];

        if (bgrImage.Type().Depth == MatType.CV_8U)
        {
            Mat[] bgr = (Mat[])bgrFilter;

            for (int i = 0; i < 3; i++)
            {
                newChannels[i] = new Mat();
                Cv2.LUT(channels[i], bgr[i], newChannels[i]);
            }
        }
        else
        {
            ushort[][] bgr = (ushort[][])bgrFilter;

            for (int i = 0; i < 3; i++)
            {
                newChannels[i] = channels[i].ApplyLut(bgr[i]);
            }
        }

        Cv2.Merge(newChannels, bgrImage);
        newChannels.Dispose();
    }

    public static Mat ApplyLut(this Mat image, Array lut)
    {
        var result = image.Clone();
        switch (lut)
        {
            case byte[] bytes:
                result.ApplyLutInplace(bytes);
                break;
            case ushort[] ushorts:
                result.ApplyLutInplace(ushorts);
                break;
            default:
                throw new InvalidOperationException();
        }
        return result;
    }

    
    public static void ApplyLutInplace(this Mat image, byte[] lut)
    {
        Guard.IsEqualTo(image.Depth(), MatType.CV_8U);
        Guard.HasSizeEqualTo(lut, byte.MaxValue + 1);
        Cv2.LUT(image, lut, image);
    }

    public static void ApplyLutInplace(this Mat image, ushort[] lut)
    {
        Guard.IsEqualTo(image.Depth(), MatType.CV_16U);
        Guard.HasSizeEqualTo(lut, ushort.MaxValue + 1);

        int rows = image.Rows;
        int cols = image.Cols;

        var indexer = image.GetUnsafeGenericIndexer<ushort>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var pixelValue = indexer[row, col, 0];
                indexer[row, col, 0] = lut[pixelValue];
            }
        }
    }
}
