using Tarmi.Imaging.Common;
using OpenCvSharp;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static class Disposables
{
    public static void Dispose(this IEnumerable<Mat> mats)
    {
        ArgumentNullException.ThrowIfNull(mats);

        foreach (var mat in mats)
        {
            mat?.Dispose();
        }
    }

    public static void Dispose(this IEnumerable<IImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        foreach (var image in images)
        {
            image?.Dispose();
        }
    }

    public static void Dispose(this IEnumerable<IEnumerable<Mat>> mats)
    {
        ArgumentNullException.ThrowIfNull(mats);

        foreach (var subMats in mats)
        {
            subMats?.Dispose();
        }
    }
}
