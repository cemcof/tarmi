using System.Windows;

namespace Betrian.CflmNavi.App.Controls;

public class LogarithmicSlider : EnhancedSlider
{
    public static readonly DependencyProperty LogMinimumProperty = DependencyProperty.Register(nameof(LogMinimum), typeof(double), typeof(LogarithmicSlider), new FrameworkPropertyMetadata(1.0, OnLogRangeChanged), IsValidLogValue);
    public static readonly DependencyProperty LogMaximumProperty = DependencyProperty.Register(nameof(LogMaximum), typeof(double), typeof(LogarithmicSlider), new FrameworkPropertyMetadata(10000.0, OnLogRangeChanged), IsValidLogValue);
    public static readonly DependencyProperty LogValueProperty = DependencyProperty.Register(nameof(LogValue), typeof(double), typeof(LogarithmicSlider), new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLogValueChanged, CoerceLogValue), IsValidLogValue);
    public static readonly DependencyProperty RoundLogValueProperty = DependencyProperty.Register(nameof(RoundLogValue), typeof(bool), typeof(LogarithmicSlider), new FrameworkPropertyMetadata(false));

    public LogarithmicSlider()
    {
        Minimum = 0;
        Maximum = 1;
        ValueChanged += OnInternalValueChanged;
    }

    public double LogMinimum
    {
        get => (double)GetValue(LogMinimumProperty);
        set => SetValue(LogMinimumProperty, value);
    }

    public double LogMaximum
    {
        get => (double)GetValue(LogMaximumProperty);
        set => SetValue(LogMaximumProperty, value);
    }

    public double LogValue
    {
        get => (double)GetValue(LogValueProperty);
        set => SetValue(LogValueProperty, value);
    }

    public bool RoundLogValue
    {
        get => (bool)GetValue(RoundLogValueProperty);
        set => SetValue(RoundLogValueProperty, value);
    }

    protected override void OnIncreaseSmall()
    {
        if (LogValue + SmallChange <= LogMaximum)
        {
            LogValue += SmallChange;
        }
    }

    protected override void OnDecreaseSmall()
    {
        if (LogValue - SmallChange >= LogMinimum)
        {
            LogValue -= SmallChange;
        }
    }

    protected override void OnValueChanged(double oldValue, double newValue) { }

    private static object CoerceLogValue(DependencyObject d, object baseValue)
    {
        var control = (LogarithmicSlider)d;
        double val = (double)baseValue;
        return Math.Max(control.LogMinimum, Math.Min(val, control.LogMaximum));
    }

    private static void OnLogRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LogarithmicSlider)d;
        control.CoerceValue(LogValueProperty);
        control.UpdateLinearValueFromLogValue(control.LogValue);
    }

    private void OnInternalValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        double linearVal = Value;
        double logVal = LinearToLog(linearVal, LogMinimum, LogMaximum);
        LogValue = RoundLogValue ? Math.Round(logVal) : logVal;
        HandleValueChanged();
    }

    private static void OnLogValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (LogarithmicSlider)d;
        double newLogVal = (double)e.NewValue;
        control.UpdateLinearValueFromLogValue(newLogVal);
    }

    private void UpdateLinearValueFromLogValue(double logVal)
    {
        double linearVal = LogToLinear(logVal, LogMinimum, LogMaximum);
        if (Math.Abs(Value - linearVal) > double.Epsilon)
        {
            Value = linearVal;
        }
    }

    private static double LinearToLog(double linearVal, double logMin, double logMax)
    {
        double ratio = linearVal;
        return logMin * Math.Pow(logMax / logMin, ratio);
    }

    private static double LogToLinear(double logVal, double logMin, double logMax)
    {
        double ratio = Math.Log(logVal / logMin) / Math.Log(logMax / logMin);
        return Math.Max(0, Math.Min(1, ratio));
    }

    private static bool IsValidLogValue(object value)
    {
        return value is double d && d > 0;
    }
}
