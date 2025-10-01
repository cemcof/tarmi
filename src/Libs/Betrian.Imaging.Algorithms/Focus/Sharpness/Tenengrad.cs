using Betrian.Imaging.Algorithms.Utilities;
using OpenCvSharp;

namespace Betrian.Imaging.Algorithms.Focus.Sharpness
{
    public class Tenengrad
    {
        public static double CalculateIndex(Mat image)
        {
            if (image.Channels() == 1)
            {
                return CalculateSingleChannelIndex(image);
            }
            using var gray = image.ToGrayscale();
            return CalculateSingleChannelIndex(gray);
        }

        private static double CalculateSingleChannelIndex(Mat mat)
        {
            using var blurred = mat.GaussianBlur(new() { Width = 5, Height = 5 }, 0);

            using var gradX = blurred.Sobel(MatType.CV_64F, 1, 0);
            using var gradY = blurred.Sobel(MatType.CV_64F, 0, 1);

            using var magnitudes = new Mat();
            Cv2.Magnitude(gradX, gradY, magnitudes);

            return magnitudes.Mean().Val0;
        }
    }
}
