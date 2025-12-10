using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(ImageChildVM), typeof(Thickness))]
public class RoiChildToImageAttributesMarginConverter : IValueConverter
{
    private readonly int[] Offsets = [ 4, 27, 49, 71, 93, 115, 137 ];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ImageChildVM vm)
        {
            return new Thickness(Offsets[vm.NestingLevel], 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
