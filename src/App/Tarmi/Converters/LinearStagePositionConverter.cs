using System.Globalization;
using System.Windows.Data;
using UnitsNet;

namespace Tarmi.App.Converters;


[ValueConversion(typeof(Length), typeof(string))]
public class LinearStagePositionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Length len)
        {
            throw new ArgumentException("Expected value of type UnitsNet.Length.", nameof(value));
        }

        return Length.FromMicrometers(len.Micrometers).ToString("0.0", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
