using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(ItemCollection), typeof(Visibility))]
public class ItemsSourceToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ItemCollection collection)
        {
            foreach (var item in collection)
            {
                if (item is Control control)
                {
                    if (control.Visibility != Visibility.Collapsed)
                    {
                        return Visibility.Visible;
                    }
                }
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
