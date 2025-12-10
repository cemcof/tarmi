using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Hidden;
}

