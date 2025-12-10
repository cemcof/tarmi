using System.Globalization;
using System.Windows.Data;
using Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(Size), typeof(string))]
public class SizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Size size ? $"{size.Width} x {size.Height}" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
