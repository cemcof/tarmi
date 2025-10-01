using System.Diagnostics;
using Betrian.Imaging.Algorithms.Utilities;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;

namespace Betrian.Imaging.Algorithms.Focus.Sharpness;

public class FastImageSharpness
{
    public static async Task<double> CalculateIndex(Mat mat)
    {
        if (mat.Channels() == 1)
        {
            return await CalculateSingleChannelIndexAsync(mat);
        }
        using var gray = mat.ToGrayscale();
        return await CalculateSingleChannelIndexAsync(gray);
    }

    private static async Task<double> CalculateSingleChannelIndexAsync(Mat image)
    {
        return await Task.Run(() =>
        {
            using var data = new Mat();
            if (image.Type() == MatType.CV_8UC1)
            {
                image.ConvertTo(data, MatType.CV_64FC1, 1.0 / byte.MaxValue);
            }
            else if (image.Type() == MatType.CV_16UC1)
            {
                image.ConvertTo(data, MatType.CV_64FC1, 1.0 / ushort.MaxValue);
            }
            else
            {
                throw new NotImplementedException("8-bit or 16-bit single-channel image expected");
            }

            int levels = 3;
            CDF97.TransformForward(data, levels);
            var logEnergies = ComputeLogEnergies(data, levels);
            return ComputeSharpnessIndex(logEnergies);
        });
    }

    /// <summary>
    /// Empirical coefficient
    /// </summary>
    private const double Alpha = 0.8;

    private static double[] ComputeLogEnergies(Mat wavelet, int levels)
    {
        var result = new double[levels];
        var (width, height) = wavelet.Size();
        using var squaredWavelet = wavelet.Mul(wavelet);

        for (int level = 0; level < levels; level++)
        {
            width /= 2;
            height /= 2;

            var lowHighRoi = new Rect(0, height, width, height);
            using var lowHigh = squaredWavelet.SubMat(lowHighRoi);

            var highLowRoi = new Rect(width, 0, width, height);
            using var highLow = squaredWavelet.SubMat(highLowRoi);

            var highHighRoi = new Rect(width, height, width, height);
            using var highHigh = squaredWavelet.SubMat(highHighRoi);

            result[level] = (1 - Alpha) / 2 * (CalculateLogEnergy(lowHigh) + CalculateLogEnergy(highLow)) + Alpha * CalculateLogEnergy(highHigh);
        }

        return result;
    }

    private static double CalculateLogEnergy(Mat squaredSubBand)
    {
        var sum = squaredSubBand.Sum().ToDouble();
        var count = squaredSubBand.Total();
        // Using Log2 instead of Log10.
        return double.Log2(1 + sum / count);
    }

    private static double ComputeSharpnessIndex(double[] logEnergies)
    {
        Guard.IsEqualTo(logEnergies.Length, 3);
        return 4 * logEnergies[0] + 2 * logEnergies[1] + 1 * logEnergies[2];
    }

    /// <summary>
    /// Cohen-Daubechies-Feauveau 9/7 wavelet transform
    /// </summary>
    private class CDF97
    {
        // Lifting scheme coefficients
        private const double Alpha = -1.586134342;
        private const double Beta = -0.05298011854;
        private const double Gamma = 0.8829110762;
        private const double Delta = 0.4435068522;

        private const double Kappa = 1.149604398;
        private const double KappaInverse = 1 / Kappa;

        internal static void TransformForward(Mat data, int levels)
        {
            Debug.Assert(data.Type() == MatType.CV_64F);

            var (width, height) = data.Size();

            // 2^levels
            var factor = 1 << levels;
            Debug.Assert(width % factor == 0);
            Debug.Assert(height % factor == 0);

            var roi = new Rect()
            {
                Width = width,
                Height = height,
            };

            for (int level = 0; level < levels; level++)
            {
                using var subData = data.SubMat(roi);
                TransformForward(subData);
                roi.Width /= 2;
                roi.Height /= 2;
            }
        }

        private static void TransformForward(Mat data)
        {
            // Horizontal
            LiftForwardHorizontallyEfficient(data);

            using var transposed = data.Transpose();

            // Vertical
            LiftForwardHorizontallyEfficient(transposed);

            using var temp = transposed.Transpose();
            Cv2.CopyTo(temp, data);
        }

        private static void LiftForwardHorizontallyEfficient(Mat data)
        {
            Predict(data, Alpha);
            Update(data, Beta);
            Predict(data, Gamma);
            Update(data, Delta);
            Scale(data);
            Deinterleave(data);
        }

        private static void Predict(Mat data, double coefficient)
        {
            var width = data.Width;
            Guard.IsEqualTo(width % 2, 0);

            // Reuse variable names.
            {
                using var column = data.Col(width - 1);
                using var temp = (column + coefficient * (data.Col(0) + data.Col(width - 2))).ToMat();
                temp.CopyTo(column);
            }
            for (int columnIndex = 1; columnIndex < width - 2; columnIndex += 2)
            {
                using var column = data.Col(columnIndex);
                using var temp = (column + coefficient * (data.Col(columnIndex - 1) + data.Col(columnIndex + 1))).ToMat();
                temp.CopyTo(column);
            }
        }

        private static void Update(Mat data, double coefficient)
        {
            var width = data.Width;
            Guard.IsEqualTo(width % 2, 0);
            // Reuse variable names.
            {
                using var column = data.Col(0);
                using var temp = (column + coefficient * (data.Col(1) + data.Col(width - 1))).ToMat();
                temp.CopyTo(column);
            }
            for (int columnIndex = 2; columnIndex < width; columnIndex += 2)
            {
                using var column = data.Col(columnIndex);
                using var temp = (column + coefficient * (data.Col(columnIndex - 1) + data.Col(columnIndex + 1))).ToMat();
                temp.CopyTo(column);
            }
        }

        private static void Scale(Mat data)
        {
            var width = data.Width;
            Guard.IsEqualTo(width % 2, 0);
            for (int columnIndex = 0; columnIndex < width; columnIndex += 2)
            {
                using var column1 = data.Col(columnIndex);
                using var temp1 = (KappaInverse * column1).ToMat();
                temp1.CopyTo(column1);

                using var column2 = data.Col(columnIndex + 1);
                using var temp2 = (Kappa * column2).ToMat();
                temp2.CopyTo(column2);
            }
        }

        private static void Deinterleave(Mat data)
        {
            var (width, height) = data.Size();
            using var temp = new Mat(height, width, data.Type());

            var index = 0;
            for (int columnIndex = 0; columnIndex < width; columnIndex += 2)
            {
                using var sourceColumn = data.Col(columnIndex);
                using var targetColumn = temp.Col(index++);
                sourceColumn.CopyTo(targetColumn);
            }
            index = width - 1;
            for (int columnIndex = width - 1; columnIndex >= 0; columnIndex -= 2)
            {
                using var sourceColumn = data.Col(columnIndex);
                using var targetColumn = temp.Col(index--);
                sourceColumn.CopyTo(targetColumn);
            }
            temp.CopyTo(data);
        }
    }
}
