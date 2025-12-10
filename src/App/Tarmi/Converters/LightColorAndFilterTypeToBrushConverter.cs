using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thorlabs.Light;

namespace Tarmi.App.Converters;

internal class LightColorAndFilterTypeToBrushConverter : IMultiValueConverter
{
    private static readonly Brush FluorescenceRedBrush = (Brush)Application.Current.FindResource("FluorescenceRedBrush");
    private static readonly Brush FluorescenceGreenBrush = (Brush)Application.Current.FindResource("FluorescenceGreenBrush");
    private static readonly Brush FluorescenceBlueBrush = (Brush)Application.Current.FindResource("FluorescenceBlueBrush");
    private static readonly Brush FluorescenceUltravioletBrush = (Brush)Application.Current.FindResource("FluorescenceUltravioletBrush");
    private static readonly Brush ReflectionRedBrush = (Brush)Application.Current.FindResource("ReflectionRedBrush");
    private static readonly Brush ReflectionGreenBrush = (Brush)Application.Current.FindResource("ReflectionGreenBrush");
    private static readonly Brush ReflectionBlueBrush = (Brush)Application.Current.FindResource("ReflectionBlueBrush");
    private static readonly Brush ReflectionUltravioletBrush = (Brush)Application.Current.FindResource("ReflectionUltravioletBrush");

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is LightColor lightColor && values[1] is FilterType filter)
        {
            return (lightColor, filter) switch
            {
                (LightColor.UltraViolet, FilterType.Fluorescence) => FluorescenceUltravioletBrush,
                (LightColor.Blue, FilterType.Fluorescence) => FluorescenceBlueBrush,
                (LightColor.Green, FilterType.Fluorescence) => FluorescenceGreenBrush,
                (LightColor.Red, FilterType.Fluorescence) => FluorescenceRedBrush,
                (LightColor.UltraViolet, FilterType.Reflection) => ReflectionUltravioletBrush,
                (LightColor.Green, FilterType.Reflection) => ReflectionGreenBrush,
                (LightColor.Blue, FilterType.Reflection) => ReflectionBlueBrush,
                (LightColor.Red, FilterType.Reflection) => ReflectionRedBrush,
                _ => Binding.DoNothing,
            };
        }
        return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Cannot convert back.");
    }
}
