using Tarmi.Imaging.Common.OpenCvWrapper;
using OpenCvSharp;

namespace Tarmi.Imaging.Common;

public interface IImage : IDisposable
{
    int Height { get; }
    int Width { get; }
    int NumberOfChannels { get; }
    int Depth { get; }
    Size Size { get; }
    Mat Mat { get; }
    InputOutputArray InputOutputArray { get; }
    InputArray InputArray { get; }
    OutputArray OutputArray { get; }
    bool SaveTiff(string fileName);
    void FlipInplace(FlipMode mode);
    IImage Flip(FlipMode mode);
    IImage Resize(int width, int height, InterpolationFlags interpolation);
    IImage Resize(double scale, InterpolationFlags interpolation);
    IImage GetSubRect(Rect rect);
    IImage Clone();
    IImage CopyBlank();
    IImage ToGrayscale();

    IImage Convert(MatType matType)
    {
        if (matType ==  MatType.CV_8UC1)
        {
            return Image<Gray, byte>.ConvertFromImage(this);
        }
        else if (matType == MatType.CV_8UC3)
        {
            return Image<Bgr, byte>.ConvertFromImage(this);
        }
        else if (matType == MatType.CV_16UC4)
        {
            return Image<Bgra, byte>.ConvertFromImage(this);
        }
        throw new NotSupportedException();
    }
}
