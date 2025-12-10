using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Tarmi.WPF.CeitecStyles.Controls;

[TemplatePart(Name = "PART_Canvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_LowerThumb", Type = typeof(Thumb))]
[TemplatePart(Name = "PART_UpperThumb", Type = typeof(Thumb))]
public class DualSlider : Control
{
    public static readonly DependencyProperty DeadBandProperty = DependencyProperty.Register(nameof(DeadBand), typeof(double), typeof(DualSlider), new FrameworkPropertyMetadata(0.05, null, coerceValueCallback: CoerceDeadBand));
    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(nameof(LowerValue), typeof(double), typeof(DualSlider), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnLowerValueChanged, ConstrainToRangeLower));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(DualSlider), new FrameworkPropertyMetadata(1.0, MaximumChanged, CoerceMaximum), IsValidDoubleValue);
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(DualSlider), new FrameworkPropertyMetadata(0.0, MinimumChanged, CoerceMinimum), IsValidDoubleValue);
    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(nameof(UpperValue), typeof(double), typeof(DualSlider), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnUpperValueChanged, ConstrainToRangeUpper));

    private Canvas? _canvas;
    private Thumb? _lowerThumb;
    private Thumb? _upperThumb;

    public double DeadBand
    {
        get => (double)GetValue(DeadBandProperty);
        set => SetValue(DeadBandProperty, value);
    }

    public double LowerValue
    {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double UpperValue
    {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (_lowerThumb != null)
        {
            _lowerThumb.DragDelta -= LowerThumbDragDelta;
        }

        if (_upperThumb != null)
        {
            _upperThumb.DragDelta -= UpperThumbDragDelta;
        }

        _canvas = GetTemplateChild("PART_Canvas") as Canvas;
        _lowerThumb = GetTemplateChild("PART_LowerThumb") as Thumb;
        _upperThumb = GetTemplateChild("PART_UpperThumb") as Thumb;

        if (_upperThumb != null)
        {
            _upperThumb.DragDelta += UpperThumbDragDelta;
        }

        if (_lowerThumb != null)
        {
            _lowerThumb.DragDelta += LowerThumbDragDelta;
        }
    }

    private static object CoerceDeadBand(DependencyObject d, object value)
    {
        if (value is double val)
        {
            return Math.Clamp(val, 0, 1);
        }
        return value;
    }

    private static object CoerceMaximum(DependencyObject d, object value)
    {
        if (d is DualSlider ctrl && value is double val)
        {
            double min = ctrl.Minimum;
            if (val < min)
            {
                return min;
            }
        }
        return value;
    }

    private static object CoerceMinimum(DependencyObject d, object value)
    {
        if (d is DualSlider ctrl && value is double v)
        {
            double max = ctrl.Maximum;
            if (v > max)
            {
                return max;
            }
        }
        return value;
    }

    private static object ConstrainToRangeLower(DependencyObject d, object value)
    {
        if (d is DualSlider ctrl && value is double v)
        {
            double min = ctrl.Minimum;
            if (v < min)
            {
                return min;
            }

            double upper = ctrl.UpperValue - ctrl.GetDeadBand();
            if (!double.IsNaN(upper) && v > upper)
            {
                return upper;
            }
        }
        return value;
    }

    private static object ConstrainToRangeUpper(DependencyObject d, object value)
    {
        if (d is DualSlider ctrl && value is double v)
        {
            double lower = ctrl.LowerValue + ctrl.GetDeadBand();
            if (!double.IsNaN(lower) && v < lower)
            {
                return lower;
            }

            double max = ctrl.Maximum;
            if (v > max)
            {
                return max;
            }
        }
        return value;
    }

    private static bool IsValidDoubleValue(object value) => value is double d && !(double.IsNaN(d) || double.IsInfinity(d));

    private static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DualSlider ctrl)
        {
            ctrl.CoerceValue(MinimumProperty);
            ctrl.CoerceValue(UpperValueProperty);
            ctrl.CoerceValue(LowerValueProperty);
            ctrl.UpdateLowerThumb();
            ctrl.UpdateUpperThumb();
        }
    }

    private static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DualSlider ctrl)
        {
            ctrl.CoerceValue(MaximumProperty);
            ctrl.CoerceValue(UpperValueProperty);
            ctrl.CoerceValue(LowerValueProperty);
            ctrl.UpdateLowerThumb();
            ctrl.UpdateUpperThumb();
        }
    }

    private static void OnLowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DualSlider ctrl)
        {
            ctrl.UpdateLowerThumb();
        }
    }

    private static void OnUpperValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DualSlider ctrl)
        {
            ctrl.UpdateUpperThumb();
        }
    }

    private double GetDeadBand() => (Maximum - Minimum) * DeadBand;

    private void LowerThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_canvas != null)
        {
            double deltaPerPixel = (Maximum - Minimum) / _canvas.ActualWidth;
            double delta = deltaPerPixel * e.HorizontalChange;
            double newValue = LowerValue + delta;
            LowerValue = Math.Clamp(newValue, Minimum, UpperValue - GetDeadBand());
        }
    }

    private void UpdateLowerThumb()
    {
        double pixelsPerUnit = _canvas != null ? _canvas.ActualWidth / (Maximum - Minimum) : 1;
        if (_lowerThumb != null)
        {
            Canvas.SetLeft(_lowerThumb, LowerValue * pixelsPerUnit);
        }
    }

    private void UpdateUpperThumb()
    {
        double pixelsPerUnit = _canvas != null ? _canvas.ActualWidth / (Maximum - Minimum) : 1;
        if (_upperThumb != null)
        {
            Canvas.SetLeft(_upperThumb, UpperValue * pixelsPerUnit);
        }
    }

    private void UpperThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_canvas != null)
        {
            double deltaPerPixel = (Maximum - Minimum) / _canvas.ActualWidth;
            double delta = deltaPerPixel * e.HorizontalChange;
            double newValue = UpperValue + delta;
            UpperValue = Math.Clamp(newValue, LowerValue + GetDeadBand(), Maximum);
        }
    }
}
