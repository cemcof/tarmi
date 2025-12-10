using System.Globalization;
using System.Windows.Data;
using Tarmi.Configuration.Holders;

namespace Tarmi.App.Converters;

public class TilesetControlEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is IEnumerable<AreaOfInterest> grids && grids.Any();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
