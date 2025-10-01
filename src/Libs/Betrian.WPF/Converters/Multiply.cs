using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Betrian.WPF.Converters;

[ValueConversion(typeof(double), typeof(double), ParameterType = typeof(double))]
[ValueConversion(typeof(double[]), typeof(double), ParameterType = typeof(double))]
public class Multiply : IValueConverter, IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double a && (parameter is double b || double.TryParse(parameter.ToString(), out b)))
        {
            return a * b;
        }
        return DependencyProperty.UnsetValue;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double number = (parameter is double value) ? value : 1.0;
        return values.OfType<double>().Aggregate(number, (a, b) => a * b);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double number1 && parameter is double number2)
        {
            return number1 / number2;
        }
        return DependencyProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
