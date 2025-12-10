using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;
using UnitsNet;

namespace Tarmi.App.Controls;

[TemplatePart(Name = "PART_Canvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_LowerThumb", Type = typeof(Thumb))]
[TemplatePart(Name = "PART_UpperThumb", Type = typeof(Thumb))]
[TemplatePart(Name = "PART_ValueThumb", Type = typeof(Thumb))]

public class ZStackSlider : Control
{
    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(nameof(LowerValue), typeof(double), typeof(ZStackSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnLowerValueChanged, ConstrainToRangeLower));
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ZStackSlider), new FrameworkPropertyMetadata(0.0, MaximumChanged, CoerceMaximum), IsValidDoubleValue);
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ZStackSlider), new FrameworkPropertyMetadata(0.0, MinimumChanged, CoerceMinimum), IsValidDoubleValue);
    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(nameof(UpperValue), typeof(double), typeof(ZStackSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnUpperValueChanged, ConstrainToRangeUpper));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(ZStackSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.Journal, OnValueChanged, ConstrainToRange));

    private Canvas? _canvas;
    private Thumb? _lowerThumb;
    private Thumb? _upperThumb;
    private Thumb? _valueThumb;
    private Rectangle? _stripedRectangle;

    public ZStackSlider()
    {
        this.SizeChanged += ZStackSlider_SizeChanged;
    }

    private void ZStackSlider_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        UpdateUpperThumb();
        UpdateLowerThumb();
        UpdateValueThumb();
        UpdateStripedRectangle();
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

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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
        _valueThumb = GetTemplateChild("PART_ValueThumb") as Thumb;
        _stripedRectangle = GetTemplateChild("PART_StripedRectangle") as Rectangle;

        if (_upperThumb != null)
        {
            _upperThumb.DragDelta += UpperThumbDragDelta;
        }

        if (_lowerThumb != null)
        {
            _lowerThumb.DragDelta += LowerThumbDragDelta;
        }
    }

    private static object CoerceMaximum(DependencyObject d, object value)
    {
        if (d is ZStackSlider ctrl && value is double val)
        {
            return Math.Max(val, ctrl.Minimum);
        }
        return value;
    }

    private static object CoerceMinimum(DependencyObject d, object value)
    {
        if (d is ZStackSlider ctrl && value is double val)
        {
            return Math.Min(val, ctrl.Maximum);
        }
        return value;
    }

    private static object ConstrainToRange(DependencyObject d, object value)
    {
        if (d is ZStackSlider ctrl && value is double val)
        {
            return Math.Clamp(val, ctrl.Minimum, ctrl.Maximum);
        }
        return value;
    }

    private static object ConstrainToRangeLower(DependencyObject d, object value)
    {
        if (d is ZStackSlider ctrl && value is double val)
        {
            return Math.Clamp(val, ctrl.Minimum, ctrl.Maximum);
        }
        return value;
    }

    private static object ConstrainToRangeUpper(DependencyObject d, object value)
    {
        if (d is ZStackSlider ctrl && value is double val)
        {
            return Math.Clamp(val, ctrl.Minimum, ctrl.Maximum);
        }
        return value;
    }

    private static bool IsValidDoubleValue(object value) => value is double;

    private static void MaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZStackSlider ctrl)
        {
            ctrl.CoerceValue(MinimumProperty);
            ctrl.CoerceValue(UpperValueProperty);
            ctrl.CoerceValue(LowerValueProperty);
            ctrl.UpdateSliders();
        }
    }

    private static void MinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZStackSlider ctrl)
        {
            ctrl.CoerceValue(MaximumProperty);
            ctrl.CoerceValue(UpperValueProperty);
            ctrl.CoerceValue(LowerValueProperty);
            ctrl.UpdateSliders();
        }
    }

    private static void OnLowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZStackSlider ctrl)
        {
            ctrl.UpdateSliders();
        }
    }

    private static void OnUpperValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZStackSlider ctrl)
        {
            ctrl.UpdateSliders();
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZStackSlider ctrl)
        {
            ctrl.UpdateSliders();
        }
    }

    private void UpdateLowerThumb()
    {
        if (_lowerThumb != null && _canvas != null)
        {
            double pixelsPerUnit = _canvas.ActualHeight / (Maximum - Minimum);
            Canvas.SetBottom(_lowerThumb, (LowerValue - Minimum) * pixelsPerUnit);
        }
    }

    private void UpdateUpperThumb()
    {
        if (_upperThumb != null && _canvas != null)
        {
            double pixelsPerUnit = _canvas.ActualHeight / (Maximum - Minimum);
            Canvas.SetBottom(_upperThumb, (UpperValue - Minimum) * pixelsPerUnit);
        }
    }

    private void UpdateValueThumb()
    {
        if (_valueThumb != null && _canvas != null)
        {
            double pixelsPerUnit = _canvas.ActualHeight / (Maximum - Minimum);
            Canvas.SetBottom(_valueThumb, (Value - Minimum) * pixelsPerUnit);
        }
    }

    private void UpdateStripedRectangle()
    {
        if (_stripedRectangle != null && _canvas != null)
        {
            double deltaPerPixel = (Maximum - Minimum) / _canvas.ActualHeight;
            double height = (Maximum - LowerValue) / deltaPerPixel;
           // _stripedRectangle.Height = height;
        }
    }

    private void LowerThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_canvas != null)
        {
            double deltaPerPixel = (Maximum - Minimum) / _canvas.ActualHeight;
            double delta = deltaPerPixel * e.VerticalChange;
            double newValue = LowerValue - delta;
            LowerValue = Math.Clamp(newValue, Minimum, UpperValue);
        }
    }

    private void UpperThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_canvas != null)
        {
            double deltaPerPixel = (Maximum - Minimum) / _canvas.ActualHeight;
            double delta = deltaPerPixel * e.VerticalChange;
            double newValue = UpperValue - delta;
            UpperValue = Math.Clamp(newValue, LowerValue, Maximum);
        }
    }
}
