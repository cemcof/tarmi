using System.Windows;
using System.Windows.Controls;

namespace Betrian.CflmNavi.App.Controls;

public class ScaleBar : Control
{
    public static readonly DependencyProperty BarPartsProperty = DependencyProperty.Register(nameof(BarParts), typeof(int), typeof(ScaleBar), new PropertyMetadata(5, UpdateBars));
    public static readonly DependencyProperty BarsProperty = DependencyProperty.Register(nameof(Bars), typeof(List<ScaleSegment>), typeof(ScaleBar), new PropertyMetadata());
    public static readonly DependencyProperty MaxBarsPixelLengthProperty = DependencyProperty.Register(nameof(MaxBarsPixelLength), typeof(double), typeof(ScaleBar), new PropertyMetadata(300.0, UpdateBars));
    public static readonly DependencyProperty PixelSizeProperty = DependencyProperty.Register(nameof(PixelSize), typeof(double), typeof(ScaleBar), new PropertyMetadata(1.0, UpdateBars));
    public static readonly DependencyProperty PixelSizeUnitProperty = DependencyProperty.Register(nameof(PixelSizeUnit), typeof(string), typeof(ScaleBar), new PropertyMetadata("px"));
    public static readonly DependencyProperty RealLengthProperty = DependencyProperty.Register(nameof(RealLength), typeof(double), typeof(ScaleBar), new PropertyMetadata());
    public static readonly DependencyProperty SegmentHalfLengthProperty = DependencyProperty.Register(nameof(SegmentHalfLength), typeof(double), typeof(ScaleBar), new PropertyMetadata(0.0));
    public static readonly DependencyProperty SizePrefixProperty = DependencyProperty.Register(nameof(SizePrefix), typeof(string), typeof(ScaleBar), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ZoomScaleProperty = DependencyProperty.Register(nameof(ZoomScale), typeof(double), typeof(ScaleBar), new PropertyMetadata(1.0, UpdateBars));

    private static readonly List<double> _125AvailableValues = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1_000, 2_000, 5_000, 10_000];

    public int BarParts
    {
        get => (int)GetValue(BarPartsProperty);
        set => SetValue(BarPartsProperty, value);
    }

    public List<ScaleSegment> Bars
    {
        get => (List<ScaleSegment>)GetValue(BarsProperty);
        set => SetValue(BarsProperty, value);
    }

    public double MaxBarsPixelLength
    {
        get => (double)GetValue(MaxBarsPixelLengthProperty);
        set => SetValue(MaxBarsPixelLengthProperty, value);
    }

    public double PixelSize
    {
        get => (double)GetValue(PixelSizeProperty);
        set => SetValue(PixelSizeProperty, value);
    }

    public string PixelSizeUnit
    {
        get => (string)GetValue(PixelSizeUnitProperty);
        set => SetValue(PixelSizeUnitProperty, value);
    }

    public double RealLength
    {
        get => (double)GetValue(RealLengthProperty);
        set => SetValue(RealLengthProperty, value);
    }

    public double SegmentHalfLength
    {
        get => (double)GetValue(SegmentHalfLengthProperty);
        set => SetValue(SegmentHalfLengthProperty, value);
    }

    public string SizePrefix
    {
        get => (string)GetValue(SizePrefixProperty);
        set => SetValue(SizePrefixProperty, value);
    }

    public double ZoomScale
    {
        get => (double)GetValue(ZoomScaleProperty);
        set => SetValue(ZoomScaleProperty, value);
    }

    public override void OnApplyTemplate()
    {
        UpdateBars();
        base.OnApplyTemplate();
    }

    private static void UpdateBars(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScaleBar scaleBar)
        {
            scaleBar.UpdateBars();
        }
    }

    private void UpdateBars()
    {
        double scaledPixelSize = PixelSize / ZoomScale;
        double maxBarRealLength = MaxBarsPixelLength * scaledPixelSize;

        int exponent = maxBarRealLength == 0 ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(maxBarRealLength)));

        (int exponent, string prefix) unit = exponent switch
        {
            >= 3 and < 6 => (3, "k"),
            >= -3 and < 0 => (-3, "m"),
            >= -6 and < -3 => (-6, "μ"),
            >= -9 and < -6 => (-9, "n"),
            >= -12 and < -9 => (-12, "p"),
            _ => (0, string.Empty),
        };

        double mantissa = maxBarRealLength / Math.Pow(10, unit.exponent);
        double largest125Value = _125AvailableValues.LastOrDefault(x => x < mantissa, 0.5);
        double barSegmentPixelLength = largest125Value * Math.Pow(10, unit.exponent) / scaledPixelSize / BarParts;
        double segmentDisplayLength = largest125Value / BarParts;

        List<ScaleSegment> parts = [];
        for (int i = 0; i <= BarParts; i++)
        {
            parts.Add(new ScaleSegment() { Length = barSegmentPixelLength, IsEven = i % 2 == 0, Label = i * segmentDisplayLength });
        }

        Bars = parts;
        RealLength = largest125Value;
        SizePrefix = unit.prefix;
        SegmentHalfLength = barSegmentPixelLength / 2;
    }
}

public class ScaleSegment
{
    public bool IsEven { get; set; }
    public double Label { get; set; }
    public double Length { get; set; }
}
