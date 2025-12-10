using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(VirtualChildVM), typeof(Thickness))]
public class RoiChildToExpanderMarginConverter : IValueConverter
{
    private readonly int[] Offsets = [26, 50, 70, 90, 110, 130, 150];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VirtualChildVM vm)
        {
            return new Thickness(Offsets[vm.NestingLevel], 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}


