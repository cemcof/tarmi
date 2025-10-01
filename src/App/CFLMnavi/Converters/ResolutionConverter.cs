using System.Globalization;
using System.Windows.Data;
using Betrian.Devices.Thermofisher.Instrument.Types;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(Resolution), typeof(string))]
public class ResolutionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Resolution resolution ? $"{resolution.Width} x {resolution.Height}" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
