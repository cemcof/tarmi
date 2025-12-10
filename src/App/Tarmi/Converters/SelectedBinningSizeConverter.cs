using System.Globalization;
using System.Windows.Data;
using Tarmi.VirtualDevices;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(BinningSize), typeof(string))]
public class SelectedBinningSizeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values[0] is BinningSize selectedBinningSize && values[1] is int currentBinningItem && (int)selectedBinningSize == currentBinningItem;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
