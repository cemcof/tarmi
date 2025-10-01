using OpenCvSharp;

namespace Betrian.Imaging.Common.OpenCvWrapper;

public partial class Image<TColor, TDepth>
{
    public Image<TColor, TDepth> And(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.BitwiseAnd(InputArray, img2.InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }

    public Image<TColor, TDepth> And(TColor val, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.BitwiseAnd(InputArray, ia, result.OutputArray, mask?.InputArray);
        return result;
    }

    public void AndInplace(Image<TColor, TDepth> img2)
    {
        Cv2.BitwiseAnd(InputArray, img2.InputArray, OutputArray, null);
    }

    public void AndInplace(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        Cv2.BitwiseAnd(InputArray, img2.InputArray, OutputArray, mask?.InputArray);
    }

    public Image<TColor, TDepth> Or(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.BitwiseOr(InputArray, img2.InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }

    public void OrInplace(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        Cv2.BitwiseOr(InputArray, img2.InputArray, OutputArray, mask?.InputArray);
    }

    public Image<TColor, TDepth> Or(TColor val, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.BitwiseOr(InputArray, ia, result.OutputArray, mask?.InputArray);
        return result;
    }
    public Image<TColor, TDepth> Xor(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.BitwiseXor(InputArray, img2.InputArray, result.OutputArray, mask?.InputArray);
        return result;
    }

    public void XorInplace(Image<TColor, TDepth> img2, Image<Gray, byte>? mask = null)
    {
        Cv2.BitwiseXor(InputArray, img2.InputArray, OutputArray, mask?.InputArray);
    }

    public Image<TColor, TDepth> Xor(TColor val, Image<Gray, byte>? mask = null)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        using var ia = InputArray.Create(val.Scalar);
        Cv2.BitwiseXor(InputArray, ia, result.OutputArray, mask?.InputArray);
        return result;
    }

    public Image<TColor, TDepth> Not()
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.BitwiseNot(InputArray, result.OutputArray, null);
        return result;
    }

    public void NotInplace()
    {
        Cv2.BitwiseNot(InputArray, OutputArray, null);
    }

    public static Image<TColor, TDepth> operator &(Image<TColor, TDepth> img1, Image<TColor, TDepth> img2)
        => img1.And(img2);

    public static Image<TColor, TDepth> operator &(Image<TColor, TDepth> img1, double val)
    {
        var color = new TColor
        {
            Scalar = new Scalar(val, val, val, val)
        };
        return img1.And(color);
    }

    public static Image<TColor, TDepth> operator &(double val, Image<TColor, TDepth> img1)
    {
        var color = new TColor
        {
            Scalar = new Scalar(val, val, val, val)
        };
        return img1.And(color);
    }

    public static Image<TColor, TDepth> operator &(Image<TColor, TDepth> img1, TColor val)
        => img1.And(val);

    public static Image<TColor, TDepth> operator &(TColor val, Image<TColor, TDepth> img1)
        => img1.And(val);

    public static Image<TColor, TDepth> operator |(Image<TColor, TDepth> img1, Image<TColor, TDepth> img2)
        => img1.Or(img2);

    public static Image<TColor, TDepth> operator |(Image<TColor, TDepth> img1, double val)
    {
        var color = new TColor
        {
            Scalar = new Scalar(val, val, val, val)
        };
        return img1.Or(color);
    }

    public static Image<TColor, TDepth> operator |(double val, Image<TColor, TDepth> img1)
        => img1 | val;

    public static Image<TColor, TDepth> operator |(Image<TColor, TDepth> img1, TColor val)
        => img1.Or(val);

    public static Image<TColor, TDepth> operator |(TColor val, Image<TColor, TDepth> img1)
        => img1.Or(val);

    public static Image<TColor, TDepth> operator ~(Image<TColor, TDepth> image)
        => image.Not();
}
