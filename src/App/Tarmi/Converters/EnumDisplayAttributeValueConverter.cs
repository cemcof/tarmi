using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Tarmi.App.Converters;

[ValueConversion(typeof(Enum), typeof(string))]
public class EnumDisplayAttributeValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            FieldInfo? info = enumValue.GetType().GetField(enumValue.ToString());
            Attribute? attribute = info?.GetCustomAttribute<DisplayAttribute>();
            return attribute is DisplayAttribute displayAttribute ? displayAttribute.Name ?? "Unset enum name" : enumValue.ToString();
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Cannot convert back");
    }
}
