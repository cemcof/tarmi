using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(int), typeof(int))]
[ValueConversion(typeof(double), typeof(double))]
public class HumanIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i + 1,
            double d => d + 1,
            _ => DependencyProperty.UnsetValue
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => i - 1,
            double d => d - 1,
            _ => DependencyProperty.UnsetValue
        };
    }
}

