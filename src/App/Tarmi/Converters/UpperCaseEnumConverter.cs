using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Diagnostics;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public class UpperCaseEnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Enum e ? e.ToString().ToUpper(culture) : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Guard.IsAssignableToType(targetType, typeof(Enum));

        if (value is not string str)
        {
            throw new ValidationException("Value must be a string");
        }

        return Enum.Parse(targetType, str, ignoreCase: true);
    }
}
