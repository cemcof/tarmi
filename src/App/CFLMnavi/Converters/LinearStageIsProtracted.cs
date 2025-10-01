using System.Globalization;
using System.Windows.Data;

namespace Betrian.CflmNavi.App.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class LinearStageIsProtracted : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool protracted ? protracted == true ? "Retract" : "Protract" : "Invalid";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException("Cannot convert back");
}
