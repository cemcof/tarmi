using OpenCvSharp;

namespace Betrian.Imaging.Algorithms.Utilities
{
    public static class Channels
    {
        private static readonly Dictionary<MatType, MatType> TypeMap = new()
        {
            { MatType.CV_8UC3, MatType.CV_8UC1 },
            { MatType.CV_16UC3, MatType.CV_16UC1 },
        };

        public static Mat ToGrayscale(this Mat image)
        {
            if (!TypeMap.TryGetValue(image.Type(), out var type))
            {
                throw new NotImplementedException("Unexpected MatType found.");
            }
            var grayscale = new Mat(image.Size(), type);
            Cv2.MixChannels([image], [grayscale], [0, 0, 1, 0, 2, 0]);
            return grayscale;
        }
    }
}
