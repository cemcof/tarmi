using System.Globalization;
using System.Windows.Data;
using UnitsNet;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(IQuantity), typeof(string))]
public class StageUnitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IQuantity)
        {
            throw new ArgumentException("Expected value of type UnitsNet.IQuantity.", nameof(value));
        }

        return value switch
        {
            Length length => Length.FromMillimeters(length.Millimeters).ToString("0.000", culture),
            Angle angle => Angle.FromDegrees(angle.Degrees).ToString("0.00", culture),
            _ => throw new NotSupportedException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
