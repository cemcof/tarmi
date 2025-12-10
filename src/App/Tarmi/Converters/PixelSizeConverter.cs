using System.Globalization;
using System.Windows.Data;
using Tarmi.Models;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(PixelSize), typeof(string))]
public class PixelSizeConverter : IValueConverter
{
    private const int _digits = 3;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is PixelSize pixelSize ? $"{Math.Round(pixelSize.X.Nanometers, _digits)} x {Math.Round(pixelSize.Y.Nanometers, _digits)}" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
