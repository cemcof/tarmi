using OpenCvSharp;

namespace Tarmi.Imaging.Common.OpenCvWrapper;

public interface IColor
{
    Scalar Scalar { get; set; }
    int Dimension { get; }
}

public struct Gray : IColor, IEquatable<Gray>
{
    private Scalar _scalar;

    public Gray()
    {
        _scalar = new Scalar();
    }

    public Gray(double intensity)
    {
        _scalar = new Scalar(intensity);
    }

    public double Intensity { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public readonly int Dimension => 1;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Intensity}]";
    public readonly bool Equals(Gray other) => _scalar.Equals(other.Scalar);
}

public struct Rgb : IColor, IEquatable<Rgb>
{
    private Scalar _scalar;

    public Rgb()
    {
        _scalar = new Scalar();
    }

    public Rgb(double red, double green, double blue)
    {
        _scalar = new Scalar(red, green, blue);
    }

    public Rgb(System.Drawing.Color winColor)
        : this(winColor.R, winColor.G, winColor.B)
    {
    }

    public double Red { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public double Green { readonly get => _scalar.Val1; set => _scalar.Val1 = value; }
    public double Blue { readonly get => _scalar.Val2; set => _scalar.Val2 = value; }
    public readonly int Dimension => 3;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Red},{Green},{Blue}]";
    public readonly bool Equals(Rgb other) => _scalar.Equals(other.Scalar);
}

public struct Rgba : IColor, IEquatable<Rgba>
{
    private Scalar _scalar;

    public Rgba()
    {
        _scalar = new Scalar();
    }

    public Rgba(double blue, double green, double red, double alpha)
    {
        _scalar = new Scalar(blue, green, red, alpha);
    }

    public double Red { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public double Green { readonly get => _scalar.Val1; set => _scalar.Val1 = value; }
    public double Blue { readonly get => _scalar.Val2; set => _scalar.Val2 = value; }
    public double Alpha { readonly get => _scalar.Val3; set => _scalar.Val3 = value; }
    public readonly int Dimension => 4;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Red},{Green},{Blue},{Alpha}]";
    public readonly bool Equals(Rgba other) => _scalar.Equals(other.Scalar);
}

public struct Bgr : IColor, IEquatable<Bgr>
{
    private Scalar _scalar;

    public Bgr()
    {
        _scalar = new Scalar();
    }

    public Bgr(double blue, double green, double red, double alpha)
    {
        _scalar = new Scalar(blue, green, red, alpha);
    }

    public double Blue { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public double Green { readonly get => _scalar.Val1; set => _scalar.Val1 = value; }
    public double Red { readonly get => _scalar.Val2; set => _scalar.Val2 = value; }
    public readonly int Dimension => 3;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Blue},{Green},{Red}]";
    public readonly bool Equals(Bgr other) => _scalar.Equals(other.Scalar);
}

public struct Bgra : IColor, IEquatable<Bgra>
{
    private Scalar _scalar;

    public Bgra()
    {
        _scalar = new Scalar();
    }

    public Bgra(double blue, double green, double red, double alpha)
    {
        _scalar = new Scalar(blue, green, red, alpha);
    }

    public double Blue { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public double Green { readonly get => _scalar.Val1; set => _scalar.Val1 = value; }
    public double Red { readonly get => _scalar.Val2; set => _scalar.Val2 = value; }
    public double Alpha { readonly get => _scalar.Val3; set => _scalar.Val3 = value; }
    public readonly int Dimension => 4;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Blue},{Green},{Red},{Alpha}]";
    public readonly bool Equals(Bgra other) => _scalar.Equals(other.Scalar);
}

public struct Hsv : IColor, IEquatable<Hsv>
{
    private Scalar _scalar;

    public Hsv() { _scalar = new Scalar(); }

    public Hsv(double hue, double saturation, double value)
    {
        _scalar = new Scalar(hue, saturation, value);
    }

    public double Hue { readonly get => _scalar.Val0; set => _scalar.Val0 = value; }
    public double Saturation { readonly get => _scalar.Val1; set => _scalar.Val1 = value; }
    public double Value { readonly get => _scalar.Val2; set => _scalar.Val2 = value; }
    public readonly int Dimension => 3;
    public Scalar Scalar { readonly get => _scalar; set => _scalar = value; }
    public override readonly string ToString() => $"[{Hue},{Saturation},{Value}]";
    public readonly bool Equals(Hsv other) => _scalar.Equals(other.Scalar);
}

