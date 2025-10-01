using System.Globalization;
using System.Windows.Data;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(object), typeof(bool))]
public class EqualityConverter : IMultiValueConverter, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value.Equals(parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException("Cannot convert back");

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values[0].Equals(values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException("Cannot convert back");
}
