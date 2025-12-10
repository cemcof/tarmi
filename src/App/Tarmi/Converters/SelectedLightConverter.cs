using System.Globalization;
using System.Windows.Data;
using Tarmi.Devices.Thorlabs.Light;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(LightColor), typeof(bool))]
public class SelectedLightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values[0] is LightColor selected && values[1] is LightColor current && selected == current;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
