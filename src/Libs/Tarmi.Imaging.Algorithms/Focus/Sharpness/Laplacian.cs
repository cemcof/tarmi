using Tarmi.Imaging.Algorithms.Utilities;
using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Focus.Sharpness
{
    public class Laplacian
    {
        public static double CalculateIndex(Mat mat)
        {
            if (mat.Channels() == 1)
            {
                return CalculateSingleChannelIndex(mat);
            }
            using var gray = mat.ToGrayscale();
            return CalculateSingleChannelIndex(gray);
        }

        private static double CalculateSingleChannelIndex(Mat mat)
        {
            using var blurred = mat.GaussianBlur(new() { Width = 5, Height = 5 }, 0);
            using var laplacian = blurred.Laplacian(MatType.CV_64F);
            
            Cv2.MeanStdDev(laplacian, out _, out var scalar);
            
            var standardDeviation = scalar.ToDouble();
            return standardDeviation * standardDeviation;
        }
    }
}
