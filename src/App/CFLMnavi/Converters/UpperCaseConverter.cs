using System.Globalization;
using System.Windows.Data;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(string), typeof(string))]
public class UpperCaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string str ? str.ToUpper(culture) : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value;
}
