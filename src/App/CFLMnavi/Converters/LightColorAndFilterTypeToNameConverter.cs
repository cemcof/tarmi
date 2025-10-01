using System.Globalization;
using System.Windows.Data;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Thorlabs.Light;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(LightColor), typeof(string))]
public class LightColorAndFilterTypeToNameConverter : IMultiValueConverter
{
    private const string Ultraviolet = "Ultraviolet";
    private const string Red = "Red";
    private const string Green = "Green";
    private const string Blue = "Blue";

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is LightColor lightColor && values[1] is FilterType filter)
        {
            return (lightColor, filter) switch
            {
                (LightColor.UltraViolet, FilterType.Fluorescence) => Blue,
                (LightColor.Blue, FilterType.Fluorescence) => Green,
                (LightColor.Green, FilterType.Fluorescence) => Red,
                (LightColor.Red, FilterType.Fluorescence) => Ultraviolet,
                (LightColor.UltraViolet, FilterType.Reflection) => Ultraviolet,
                (LightColor.Green, FilterType.Reflection) => Green,
                (LightColor.Blue, FilterType.Reflection) => Blue,
                (LightColor.Red, FilterType.Reflection) => Red,
                _ => Binding.DoNothing,
            };
        }
        return Binding.DoNothing;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
