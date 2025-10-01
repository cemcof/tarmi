using System.Globalization;
using System.Windows.Data;
using CFLMnavi.Configuration.Holders;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(AreaOfInterest), typeof(string))]
public class GridNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values[0] is IEnumerable<AreaOfInterest> availableGrids && values[1] is AreaOfInterest grid && availableGrids.Contains(grid) ?
            grid.Name :
            "?";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Cannot convert back");
    }
}

