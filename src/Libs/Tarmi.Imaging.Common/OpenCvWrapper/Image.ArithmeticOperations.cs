using OpenCvSharp;

namespace Tarmi.Imaging.Common.OpenCvWrapper;

public partial class Image<TColor, TDepth>
{
    public Image<TColor, TDepth> Add(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Add(InputArray, img2.InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }

    public Image<TColor, TDepth> Add(TColor val)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.Add(InputArray, ia, result.OutputArray);
        return result;
    }

    public Image<TColor, TDepth> Mul(Image<TColor, TDepth> img2, double scale)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Multiply(InputArray, img2.InputArray, result.OutputArray, scale);
        return result;
    }

    public Image<TColor, TDepth> Mul(Image<TColor, TDepth> img2)
        => Mul(img2, 1.0);

    public Image<TColor, TDepth> Mul(double scale)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        if (_color is Gray && typeof(TDepth) == typeof(byte))
        {
            Cv2.ConvertScaleAbs(InputArray, result.OutputArray, scale, 0.0);
        }
        else
        {
            Cv2.AddWeighted(InputArray, scale, InputArray, 0.0, 0.0, result.OutputArray);
        }
        return result;
    }

    public Image<TColor, TDepth> Sub(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Subtract(InputArray, img2.InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }

    public Image<TColor, TDepth> Sub(TColor val)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.Subtract(InputArray, ia, result.OutputArray);
        return result;
    }

    public Image<TColor, TDepth> SubR(TColor val, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.Subtract(ia, InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }


    public static Image<TColor, TDepth> operator +(Image<TColor, TDepth> img1, Image<TColor, TDepth> img2)
        => img1.Add(img2);

    public static Image<TColor, TDepth> operator +(double val, Image<TColor, TDepth> img1)
        => img1 + val;

    public static Image<TColor, TDepth> operator +(Image<TColor, TDepth> image, double value)
    {
        var color = new TColor
        {
            Scalar = new Scalar(value, value, value, value)
        };
        return image.Add(color);
    }

    public static Image<TColor, TDepth> operator +(Image<TColor, TDepth> image, TColor value)
        => image.Add(value);

    public static Image<TColor, TDepth> operator +(TColor value, Image<TColor, TDepth> image)
        => image.Add(value);

    public static Image<TColor, TDepth> operator -(Image<TColor, TDepth> image1, Image<TColor, TDepth> image2)
        => image1.Sub(image2);

    public static Image<TColor, TDepth> operator -(Image<TColor, TDepth> image, TColor value)
        => image.Sub(value);

    public static Image<TColor, TDepth> operator -(TColor value, Image<TColor, TDepth> image)
        => image.SubR(value);

    public static Image<TColor, TDepth> operator -(double value, Image<TColor, TDepth> image)
    {
        var color = new TColor
        {
            Scalar = new Scalar(value, value, value, value)
        };
        return image.SubR(color);
    }

    public static Image<TColor, TDepth> operator -(Image<TColor, TDepth> image, double value)
    {
        var color = new TColor
        {
            Scalar = new Scalar(value, value, value, value)
        };
        return image.Sub(color);
    }

    public static Image<TColor, TDepth> operator *(Image<TColor, TDepth> image, double scale)
        => image.Mul(scale);

    public static Image<TColor, TDepth> operator *(double scale, Image<TColor, TDepth> image)
        => image.Mul(scale);

    public static Image<TColor, float> operator *(Image<TColor, TDepth> image, ConvolutionKernelF kernel)
        => image.Convolution(kernel);

    public static Image<TColor, TDepth> operator /(Image<TColor, TDepth> image, double scale)
        => image.Mul(1.0 / scale);

    public static Image<TColor, TDepth> operator /(double scale, Image<TColor, TDepth> image)
    {
        var result = new Image<TColor, TDepth>(image.Width, image.Height);
        using var ia = InputArray.Create(scale);
        Cv2.Divide(ia, image.InputArray, result.OutputArray, 1.0);
        return result;
    }
}
