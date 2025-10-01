using System.Globalization;
using System.Windows.Data;

namespace Betrian.CflmNavi.App.Converters;

/// <summary>
/// Bool inversion converter
/// </summary>
public class BooleanInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !System.Convert.ToBoolean(value, culture);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Convert(value, targetType, parameter, culture);
}
