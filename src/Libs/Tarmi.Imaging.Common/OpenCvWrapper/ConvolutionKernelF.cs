using OpenCvSharp;

namespace Tarmi.Imaging.Common.OpenCvWrapper;

public class ConvolutionKernelF : IDisposable
{
    private readonly Mat _mat;

    public Point Center { get; }
    public InputArray InputArray => _mat;

    public int Width => _mat.Width;
    public int Height => _mat.Height;

    public ConvolutionKernelF(int rows, int cols)
    {
        _mat = new Mat(rows, cols, MatType.CV_32FC1);
        Center = new Point(-1, -1);
    }

    private ConvolutionKernelF(Mat kernel, Point center)
    {
        _mat = kernel;
        Center = center;
    }

    public ConvolutionKernelF(Image<Gray, float> kernel, Point center)
    {
        _mat = kernel.Mat.Clone();
        Center = center;
    }

    public float Get(int row, int col)
    {
        return _mat.At<float>(row, col, 0);
    }

    public void Set(int row, int col, float value)
    {
        _mat.At<float>(row, col, 0) = value;
    }

    public ConvolutionKernelF Flip(FlipMode flipType)
    {
#pragma warning disable S3358 // Ternary operators should not be nested
        var flippedCenter = new Point
        {
            X = Center.X == -1 ? -1 : flipType.IsOneOf(FlipMode.Y, FlipMode.XY) ? _mat.Width - Center.X - 1 : Center.X,
            Y = Center.Y == -1 ? -1 : flipType.IsOneOf(FlipMode.X, FlipMode.XY) ? _mat.Height - Center.Y - 1 : Center.Y
        };
#pragma warning restore S3358 // Ternary operators should not be nested
        return new ConvolutionKernelF(_mat.Flip(flipType), flippedCenter);
    }

    public ConvolutionKernelF Transpose()
    {
        return new ConvolutionKernelF(_mat.Transpose(), Center);
    }

    public void Dispose()
    {
        _mat.Dispose();
        GC.SuppressFinalize(this);
    }
}
