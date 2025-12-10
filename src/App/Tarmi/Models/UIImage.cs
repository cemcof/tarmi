using System.Windows.Media.Imaging;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;

using OpenCvSharp.WpfExtensions;

namespace Tarmi.App.Models;

public class UIImage
{
    public SortedDictionary<int, double> Histogram { get; private set; }

    public BitmapSource Image { get; private set; }

    public UIImage(ImageWithMetadata image)
    {
        Image = image.Image.Mat.ToBitmapSource();
        Histogram = image.Image.GetNormalizedHistogram();
    }
}
