using OpenCvSharp;

namespace Tarmi.Imaging.Common.OpenCvWrapper;

#pragma warning disable S4050 // Operators should be overloaded consistently
public unsafe partial class Image<TColor, TDepth> : IImage
#pragma warning restore S4050 // Operators should be overloaded consistently
        where TColor : struct, IColor
        where TDepth : unmanaged
{
    private readonly TColor _color = default;

    public int Height => Mat.Height;
    public int Width => Mat.Width;
    public int NumberOfChannels => _color.Dimension;
    public Size Size => Mat.Size();
    public int Depth => sizeof(TDepth) * 8;
    public Mat Mat { get; }
    public InputOutputArray InputOutputArray => Mat;
    public InputArray InputArray => Mat;
    public OutputArray OutputArray => Mat;

    private Image(Image<TColor, TDepth> src)
    {
        Mat = src.Mat.Clone();
    }

    internal Image(Mat src)
    {
        if (GetMatType() != src.Type())
        {
            throw new ArgumentException("Incompatible Mat type with the specified template constraints", nameof(src));
        }
        Mat = src;
    }

    public Image(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new ArgumentException("File does not exist", nameof(fileName));
        }

        var mat = Cv2.ImRead(fileName, ImreadModes.AnyColor | ImreadModes.AnyDepth)
            ?? throw new InvalidOperationException("Failed to load image from file");
        Mat = CreateImageMatFromLoadedMat(mat);
    }

    public static Image<TColor, TDepth> FromMat(Mat mat)
    {
        return new Image<TColor, TDepth>(mat);
    }

    public static Image<TColor, TDepth> ConvertFromMat(Mat mat)
    {
        var result = CreateImageMatFromLoadedMat(mat);
        return new Image<TColor, TDepth>(result);
    }

    public static Image<TColor, TDepth> ConvertFromImage(IImage image)
    {
        var result = CreateImageMatFromLoadedMat(image.Mat);
        return new Image<TColor, TDepth>(result);
    }

    internal static Mat CreateImageMatFromLoadedMat(Mat src)
    {
        var matType = src.Type();
        if (matType == MatType.CV_8UC1)
        {
            using var tmpImage = new Image<Gray, byte>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_16UC1)
        {
            using var tmpImage = new Image<Gray, ushort>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_32FC1)
        {
            using var tmpImage = new Image<Gray, float>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_64FC1)
        {
            using var tmpImage = new Image<Gray, double>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_8UC3)
        {
            using var tmpImage = new Image<Bgr, byte>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_8UC4)
        {
            using var tmpImage = new Image<Bgra, byte>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_16UC3)
        {
            using var tmpImage = new Image<Bgr, ushort>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else if (matType == MatType.CV_16UC4)
        {
            using var tmpImage = new Image<Bgra, ushort>(src);
            return ConvertToMatFrom(tmpImage);
        }
        else
        {
            throw new NotSupportedException("Unsupported image format for conversion");
        }
    }

    public Image(int width, int height)
    {
        Mat = Mat.Zeros(height, width, GetMatType());
    }

    public Image(int width, int height, TColor color)
    {
        Mat = new Mat(height, width, GetMatType(), color.Scalar);
    }

    public Image(Size size)
       : this(size.Width, size.Height)
    {
    }

    public Image(Size size, TColor color)
       : this(size.Width, size.Height, color)
    {
    }

    public Image<TColor, TDepth> CopyBlank()
    {
        return new Image<TColor, TDepth>(Size);
    }

    IImage IImage.CopyBlank()
        => CopyBlank();

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
    // intentional
    private MatType GetMatType()
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
    {
        return GetMatType<TColor, TDepth>();
    }

    private static MatType GetMatType<TQueryColor, TQueryDepth>()
        where TQueryColor : struct, IColor
        where TQueryDepth : notnull
    {
        var depth = default(TQueryDepth);
        var color = default(TQueryColor);
        return (color, depth) switch
        {
            (Gray, byte) => MatType.CV_8UC1,
            (Gray, ushort) => MatType.CV_16UC1,
            (Gray, float) => MatType.CV_32FC1,
            (Gray, double) => MatType.CV_64FC1,
            (Rgb or Bgr or Hsv, byte) => MatType.CV_8UC3,
            (Rgb or Bgr or Hsv, ushort) => MatType.CV_16UC3,
            (Rgba or Bgra, byte) => MatType.CV_8UC4,
            (Rgba or Bgra, ushort) => MatType.CV_16UC4,
            _ => throw new NotSupportedException("Invalid combination of color type and depth")
        };
    }

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
    // intentional
    private ColorConversionCodes GetColorConversionCode<TDstColor>()
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
        where TDstColor : struct, IColor
    {
        return GetColorConversionCode<TColor, TDstColor>();
    }

    private static ColorConversionCodes GetColorConversionCode<TSrcColor, TDstColor>()
        where TSrcColor : struct, IColor
        where TDstColor : struct, IColor
    {
        var srcColor = default(TSrcColor);
        var dstColor = default(TDstColor);
        return (srcColor, dstColor) switch
        {
            (Rgb, Bgr) => ColorConversionCodes.RGB2BGR,
            (Rgba, Bgra) => ColorConversionCodes.RGBA2BGRA,
            (Bgr, Gray) => ColorConversionCodes.BGR2GRAY,
            (Bgra, Gray) => ColorConversionCodes.BGRA2GRAY,
            (Bgr, Rgb) => ColorConversionCodes.BGR2RGB,
            (Bgr, Bgra) => ColorConversionCodes.BGR2BGRA,
            (Bgra, Bgr) => ColorConversionCodes.BGRA2BGR,
            (Bgra, Rgba) => ColorConversionCodes.BGRA2RGBA,
            (Rgb, Gray) => ColorConversionCodes.RGB2GRAY,
            (Rgba, Gray) => ColorConversionCodes.RGBA2GRAY,
            (Gray, Bgr) => ColorConversionCodes.GRAY2BGR,
            (Gray, Bgra) => ColorConversionCodes.GRAY2BGRA,
            (Gray, Rgb) => ColorConversionCodes.GRAY2RGB,
            (Gray, Rgba) => ColorConversionCodes.GRAY2RGBA,
            (Hsv, Rgb) => ColorConversionCodes.HSV2BGR,
            (Hsv, Bgr) => ColorConversionCodes.HSV2BGR,
            _ => throw new NotSupportedException("Invalid color conversion")
        };
    }

    public TDepth this[int row, int col, int channelIdx]
    {
        get => Get(row, col, channelIdx);
        set => Set(row, col, channelIdx, value);
    }

    public TDepth Get(int row, int col, int colorIdx)
    {
        return Mat.At<TDepth>(row, col, colorIdx);
    }

    public void Set(int row, int col, int colorIdx, TDepth value)
    {
        Mat.At<TDepth>(row, col, colorIdx) = value;
    }

    public Image<TColor, TDepth> GetSubRect(Rect rect)
    {
        var subMat = Mat.SubMat(rect);
        return new Image<TColor, TDepth>(subMat);
    }

    IImage IImage.GetSubRect(Rect rect)
        => GetSubRect(rect);

    public Image<TColor, TDepth> AbsDiff(Image<TColor, TDepth> image)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Absdiff(InputArray, image.InputArray, result.OutputArray);
        return result;
    }

    public void SetValue(TColor color, Image<Gray, byte>? mask = null)
    {
        _ = Mat.SetTo(color.Scalar, mask?.Mat);
    }

    public void SetZero(Image<Gray, byte>? mask = null)
    {
        SetValue(default, mask);
    }

    public TColor GetSum()
    {
        return new TColor { Scalar = Cv2.Sum(Mat) };
    }

    public TColor GetAverage(Image<Gray, byte>? mask = null)
    {
        return new TColor
        {
            Scalar = Cv2.Mean(InputArray, mask?.InputArray)
        };
    }

    public int[] CountNonzero()
    {
        if (NumberOfChannels == 1)
        {
            return [Cv2.CountNonZero(Mat)];
        }
        else
        {
            int[] result = new int[NumberOfChannels];
            using var tmp = new Mat();
            for (int i = 0; i < result.Length; i++)
            {
                Cv2.ExtractChannel(InputArray, tmp, i);
                result[i] = Cv2.CountNonZero(tmp);
            }
            return result;
        }
    }

    public void MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations)
    {
        minValues = new double[NumberOfChannels];
        maxValues = new double[NumberOfChannels];
        minLocations = new Point[NumberOfChannels];
        maxLocations = new Point[NumberOfChannels];

        if (NumberOfChannels == 1)
        {
            Cv2.MinMaxLoc(InputArray, out var minVal, out var maxVal, out var minLoc, out var maxLoc);

            minValues[0] = minVal;
            maxValues[0] = maxVal;
            minLocations[0] = minLoc;
            maxLocations[0] = maxLoc;
        }
        else
        {
            using var channel = new Mat();
            for (int i = 0; i < NumberOfChannels; i++)
            {
                Cv2.ExtractChannel(InputArray, channel, i);
                Cv2.MinMaxLoc(channel, out var minVal, out var maxVal, out var minLoc, out var maxLoc);
                minValues[i] = minVal;
                maxValues[i] = maxVal;
                minLocations[i] = minLoc;
                maxLocations[i] = maxLoc;
            }
        }
    }

    public Moments GetMoments(bool binary)
    {
        return Cv2.Moments(InputArray, binary);
    }

    public Image<TColor, TDepth> Resize(int width, int height, InterpolationFlags interpolation)
    {
        var result = new Image<TColor, TDepth>(width, height);
        Cv2.Resize(InputArray, result.OutputArray, new Size(width, height), 0, 0, interpolation);
        return result;
    }

    public Image<TColor, TDepth> Resize(double scale, InterpolationFlags interpolation)
    {
        return Resize((int)(Width * scale), (int)(Height * scale), interpolation);
    }

    IImage IImage.Resize(int width, int height, InterpolationFlags interpolation) => Resize(width, height, interpolation);
    IImage IImage.Resize(double scale, InterpolationFlags interpolation) => Resize(scale, interpolation);

    public Image<TColor, TDepth> Flip(FlipMode mode)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Flip(Mat, result.Mat, mode);
        return result;
    }

    IImage IImage.Flip(FlipMode mode) => Flip(mode);

    public void FlipInplace(FlipMode mode)
    {
        Cv2.Flip(Mat, Mat, mode);
    }

    void IImage.FlipInplace(FlipMode mode) => FlipInplace(mode);

    private void ForEachChannel(Image<TColor, TDepth> result, Action<InputArray, OutputArray, int> channelAction)
    {
        if (NumberOfChannels == 1)
        {
            channelAction(InputArray, result.OutputArray, 0);
        }
        else
        {
            using var tmpChannel = new Mat();
            for (int i = 0; i < NumberOfChannels; i++)
            {
                Cv2.ExtractChannel(InputArray, tmpChannel, i);
                channelAction(tmpChannel, tmpChannel, i);
                Cv2.InsertChannel(tmpChannel, result.InputOutputArray, i);
            }
        }
    }

    public Image<TColor, TDepth> ThresholdAdaptive(TColor maxValue, AdaptiveThresholdTypes adaptiveType, ThresholdTypes thresholdType, int blockSize, TColor param1)
    {
        double[] max = maxValue.Scalar.ToArray();
        double[] p1 = param1.Scalar.ToArray();
        var result = new Image<TColor, TDepth>(Width, Height);
        ForEachChannel(result, (input, output, channel) =>
        {
            Cv2.AdaptiveThreshold(input, output, max[channel], adaptiveType, thresholdType, blockSize, p1[channel]);
        });
        return result;
    }

    private void ThresholdBase(Image<TColor, TDepth> dest, TColor threshold, TColor maxValue, ThresholdTypes thresholdType)
    {
        //double[] t = threshold.Scalar.ToArray();
        //double[] m = maxValue.Scalar.ToArray();
        ForEachChannel(dest, (input, output, channel) =>
        {
            _ = Cv2.Threshold(input, output, threshold.Scalar[channel], maxValue.Scalar[channel], thresholdType);
        });
    }

    public Image<TColor, TDepth> ThresholdToZero(TColor threshold)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        ThresholdBase(result, threshold, new TColor(), ThresholdTypes.Tozero);
        return result;
    }

    public Image<TColor, TDepth> ThresholdToZeroInv(TColor threshold)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        ThresholdBase(result, threshold, new TColor(), ThresholdTypes.TozeroInv);
        return result;
    }

    public Image<TColor, TDepth> ThresholdTrunc(TColor threshold)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        ThresholdBase(result, threshold, new TColor(), ThresholdTypes.Trunc);
        return result;
    }

    public Image<TColor, TDepth> ThresholdBinary(TColor threshold, TColor maxValue)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        ThresholdBase(result, threshold, maxValue, ThresholdTypes.Binary);
        return result;
    }

    public Image<TColor, TDepth> ThresholdBinaryInv(TColor threshold, TColor maxValue)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        ThresholdBase(result, threshold, maxValue, ThresholdTypes.BinaryInv);
        return result;
    }

    public void ThresholdToZeroInplace(TColor threshold)
    {
        ThresholdBase(this, threshold, new TColor(), ThresholdTypes.Tozero);
    }

    public void ThresholdToZeroInvInplace(TColor threshold)
    {
        ThresholdBase(this, threshold, new TColor(), ThresholdTypes.TozeroInv);
    }

    public void ThresholdTruncInplace(TColor threshold)
    {
        ThresholdBase(this, threshold, new TColor(), ThresholdTypes.Trunc);
    }

    public void ThresholdBinaryInplace(TColor threshold, TColor maxValue)
    {
        ThresholdBase(this, threshold, maxValue, ThresholdTypes.Binary);
    }

    public void ThresholdBinaryInvInplace(TColor threshold, TColor maxValue)
    {
        ThresholdBase(this, threshold, maxValue, ThresholdTypes.BinaryInv);
    }

    public void ThresholdInplace(TColor threshold, TColor maxValue, ThresholdTypes thresholdType)
    {
        ThresholdBase(this, threshold, maxValue, thresholdType);
    }

    public Image<TColor, TDepth> SmoothMedian(int size)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.MedianBlur(InputArray, result.OutputArray, size);
        return result;
    }

    public Image<TColor, TDepth> SmoothGaussian(int kernelSize)
    {
        return SmoothGaussian(kernelSize, kernelSize, 0, 0);
    }

    public Image<TColor, TDepth> SmoothGaussian(int kernelWidth, int kernelHeight, double sigma1, double sigma2)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.GaussianBlur(InputArray, result.OutputArray, new Size(kernelWidth, kernelHeight), sigma1, sigma2);
        return result;
    }

    public void SmoothGaussianInplace(int kernelSize)
    {
        SmoothGaussianInplace(kernelSize, kernelSize, 0, 0);
    }

    public void SmoothGaussianInplace(int kernelWidth, int kernelHeight, double sigma1, double sigma2)
    {
        Cv2.GaussianBlur(InputArray, OutputArray, new Size(kernelWidth, kernelHeight), sigma1, sigma2);
    }

    public Image<TColor, TDepth> Erode(int iterations)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Erode(InputArray, result.OutputArray, null, new Point(-1, -1), iterations, BorderTypes.Constant, Cv2.MorphologyDefaultBorderValue());
        return result;
    }

    public void ErodeInplace(int iterations)
    {
        Cv2.Erode(InputArray, OutputArray, null, new Point(-1, -1), iterations, BorderTypes.Constant, Cv2.MorphologyDefaultBorderValue());
    }

    public Image<TColor, TDepth> Dilate(int iterations)
    {
        var result = new Image<TColor, TDepth>(Width, Height);
        Cv2.Dilate(InputArray, result.OutputArray, null, new Point(-1, -1), iterations, BorderTypes.Constant, Cv2.MorphologyDefaultBorderValue());
        return result;
    }

    public void DilateInplace(int iterations)
    {
        Cv2.Dilate(InputArray, OutputArray, null, new Point(-1, -1), iterations, BorderTypes.Constant, Cv2.MorphologyDefaultBorderValue());
    }

    public bool Save(string fileName)
    {
        return SaveImplementation(mat =>
        {
            return Cv2.ImWrite(fileName, mat);
        });
    }

    public bool SaveTiff(string fileName)
    {
        return SaveImplementation(mat =>
        {
            return Cv2.ImWrite(fileName, mat, new ImageEncodingParam(ImwriteFlags.TiffCompression, 1));
        });
    }

    private bool SaveImplementation(Func<Mat, bool> saveRoutine)
    {
        if (NumberOfChannels == 3 && typeof(TColor) != typeof(Bgr))
        {
            using var tmp = new Mat();
            Cv2.CvtColor(InputArray, tmp, GetColorConversionCode<Bgr>());
            return saveRoutine(tmp);
        }
        else if (NumberOfChannels == 4 && typeof(TColor) != typeof(Bgra))
        {
            using var tmp = new Mat();
            Cv2.CvtColor(InputArray, tmp, GetColorConversionCode<Bgra>());
            return saveRoutine(tmp);
        }
        else
        {
            return saveRoutine(Mat);
        }
    }

    public void EqualizeHistInplace()
    {
        if (NumberOfChannels == 1)
        {
            //Gray scale image
            Cv2.EqualizeHist(InputArray, OutputArray);
        }
        else
        {
            Image<Hsv, TDepth> hsv = this as Image<Hsv, TDepth> ?? Convert<Hsv, TDepth>();

            using var v = new Image<Gray, TDepth>(Width, Height);
            Cv2.MixChannels([hsv.Mat], [v.Mat], [1, 0]);
            v.EqualizeHistInplace();
            Cv2.MixChannels([v.Mat], [hsv.Mat], [0, 1]);

            if (!object.ReferenceEquals(this, hsv))
            {
                ConvertFrom(hsv);
                hsv.Dispose();
            }
        }
    }

    public Image<TColor, float> Convolution(ConvolutionKernelF kernel, double delta = 0, BorderTypes borderType = BorderTypes.Default)
    {
        Image<TColor, float> floatImage = (typeof(TDepth) == typeof(float)) ?
            (this as Image<TColor, float>)! :
            Convert<TColor, float>();

        try
        {
            var result = new Image<TColor, float>(Width, Height);
            int numberOfChannels = NumberOfChannels;
            if (numberOfChannels == 1)
            {
                Cv2.Filter2D(floatImage.InputArray, result.OutputArray, result.Mat.Type(), kernel.InputArray, kernel.Center, delta, borderType);
            }
            else
            {
                using var m1 = new Mat(Height, Width, MatType.CV_32FC1);
                using var m2 = new Mat(Height, Width, MatType.CV_32FC1);
                for (int i = 0; i < numberOfChannels; i++)
                {
                    Cv2.ExtractChannel(floatImage.InputArray, m1, i);
                    Cv2.Filter2D(m1, m2, m2.Type(), kernel.InputArray, kernel.Center, delta, borderType);
                    Cv2.InsertChannel(m2, result.InputOutputArray, i);
                }
            }
            return result;
        }
        finally
        {

            if (!object.ReferenceEquals(floatImage, this))
            {
                floatImage.Dispose();
            }
        }
    }

    public Image<Gray, float> MatchTemplate(Image<TColor, TDepth> template, TemplateMatchModes method)
    {
        var result = new Image<Gray, float>(Width - template.Width + 1, Height - template.Height + 1);
        Cv2.MatchTemplate(InputArray, template.InputArray, result.OutputArray, method);
        return result;
    }

    public Image<TDestColor, TDestDepth> Convert<TDestColor, TDestDepth>()
        where TDestColor : struct, IColor
        where TDestDepth : unmanaged
    {
        var res = new Image<TDestColor, TDestDepth>(Width, Height);
        res.ConvertFrom(this);
        return res;
    }

    private static Mat ConvertToMatFrom<TSrcColor, TSrcDepth>(Image<TSrcColor, TSrcDepth> srcImage)
           where TSrcColor : struct, IColor
           where TSrcDepth : unmanaged
    {
        var result = new Mat(srcImage.Height, srcImage.Width, GetMatType<TColor, TDepth>());

        if (typeof(TColor) == typeof(TSrcColor))
        {
            if (typeof(TDepth) == typeof(TSrcDepth))
            {
                srcImage.Mat.CopyTo(result);
            }
            else
            {
                if (typeof(TDepth) == typeof(byte) && typeof(TSrcDepth) != typeof(byte))
                {
                    srcImage.MinMax(out var minVal, out var maxVal, out var _, out var _);
                    double min = minVal[0];
                    double max = maxVal[0];
                    for (int i = 1; i < minVal.Length; i++)
                    {
                        min = Math.Min(min, minVal[i]);
                        max = Math.Max(max, maxVal[i]);
                    }
                    double scale = 1.0, shift = 0.0;
                    if (max > 255.0 || min < 0)
                    {
                        scale = (max.Equals(min)) ? 0.0 : 255.0 / (max - min);
                        shift = (scale.Equals(0)) ? min : -min * scale;
                    }

                    Cv2.ConvertScaleAbs(srcImage.InputArray, result, scale, shift);
                }
                else
                {
                    srcImage.Mat.ConvertTo(result, result.Type(), 1.0, 0.0);
                }
            }
        }
        else
        {
            if (typeof(TDepth) == typeof(TSrcDepth))
            {
                Cv2.CvtColor(srcImage.InputArray, result, GetColorConversionCode<TSrcColor, TColor>());
            }
            else
            {
                if (typeof(TSrcDepth) == typeof(byte))
                {
                    using var tmp = srcImage.Convert<TColor, TSrcDepth>();
                    result.Dispose();
                    result = ConvertToMatFrom(tmp);
                }
                else
                {
                    using var tmp = srcImage.Convert<TSrcColor, TDepth>();
                    Cv2.CvtColor(tmp.InputArray, result, GetColorConversionCode<TSrcColor, TColor>());
                }
            }
        }
        return result;
    }

    public void ConvertFrom<TSrcColor, TSrcDepth>(Image<TSrcColor, TSrcDepth> srcImage)
           where TSrcColor : struct, IColor
           where TSrcDepth : unmanaged
    {
        if (!Size.Equals(srcImage.Size))
        {
            using var tmp = new Image<TSrcColor, TSrcDepth>(Size);
            Cv2.Resize(srcImage.InputArray, tmp.OutputArray, Size);
            ConvertFrom(tmp);
            return;
        }

        if (typeof(TColor) == typeof(TSrcColor))
        {
            if (typeof(TDepth) == typeof(TSrcDepth))
            {
                srcImage.Mat.CopyTo(OutputArray);
            }
            else
            {
                if (typeof(TDepth) == typeof(byte) && typeof(TSrcDepth) != typeof(byte))
                {
                    srcImage.MinMax(out var minVal, out var maxVal, out var _, out var _);
                    double min = minVal[0];
                    double max = maxVal[0];
                    for (int i = 1; i < minVal.Length; i++)
                    {
                        min = Math.Min(min, minVal[i]);
                        max = Math.Max(max, maxVal[i]);
                    }
                    double scale = 1.0, shift = 0.0;
                    if (max > 255.0 || min < 0)
                    {
                        scale = (max.Equals(min)) ? 0.0 : 255.0 / (max - min);
                        shift = (scale.Equals(0)) ? min : -min * scale;
                    }

                    Cv2.ConvertScaleAbs(srcImage.InputArray, OutputArray, scale, shift);
                }
                else
                {
                    srcImage.Mat.ConvertTo(OutputArray, Mat.Type(), 1.0, 0.0);
                }
            }
        }
        else
        {
            if (typeof(TDepth) == typeof(TSrcDepth))
            {
                Cv2.CvtColor(srcImage.InputArray, OutputArray, srcImage.GetColorConversionCode<TColor>());
            }
            else
            {
                if (typeof(TSrcDepth) == typeof(byte))
                {
                    using var tmp = srcImage.Convert<TColor, TSrcDepth>();
                    ConvertFrom(tmp);
                }
                else
                {
                    using var tmp = srcImage.Convert<TSrcColor, TDepth>();
                    Cv2.CvtColor(tmp.InputArray, OutputArray, tmp.GetColorConversionCode<TColor>());
                }
            }
        }
    }

    IImage IImage.ToGrayscale() => Convert<Gray, TDepth>();

    public Image<TColor, TDepth> Clone() => new(this);

    IImage IImage.Clone() => Clone();

    public void Dispose()
    {
        Mat.Dispose();
        GC.SuppressFinalize(this);
    }
}
