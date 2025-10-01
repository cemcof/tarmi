using System.Windows.Media.Imaging;
using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;

using OpenCvSharp.WpfExtensions;

namespace Betrian.CflmNavi.App.Models;

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
