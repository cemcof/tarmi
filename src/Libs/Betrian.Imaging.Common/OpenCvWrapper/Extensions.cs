using OpenCvSharp;

namespace Betrian.Imaging.Common.OpenCvWrapper;

public static class Extensions
{
    public static double[] ToArray(this Scalar scalar)
        => [scalar.Val0, scalar.Val1, scalar.Val2, scalar.Val3];

    public static Point2d GetGravityCenter(this Moments moments)
        => new(moments.M10 / moments.M00, moments.M01 / moments.M00);

    public static void ForEachDuplicateChannel(this InputArray image, Action<InputArray, int> action)
    {
        int channels = image.Channels();
        if (channels == 1)
        {
            action(image, 0);
        }
        else
        {
            using var tmp = new Mat();
            for (int i = 0; i < channels; i++)
            {
                Cv2.ExtractChannel(image, tmp, i);
                action(tmp, i);
            }
        }
    }
}
