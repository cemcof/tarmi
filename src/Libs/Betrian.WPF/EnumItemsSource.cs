using System.ComponentModel;
using System.Windows.Markup;

namespace Betrian.WPF;

public class EnumItemsSource : MarkupExtension
{
    private readonly Type _enumType;

    public EnumItemsSource(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        if (enumType.IsEnum == false)
        {
            throw new ArgumentException("Type must be an Enum.");
        }

        _enumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Enum.GetValues(_enumType);
    }

    private string GetDescription(object enumValue)
    {
        if (_enumType.GetField(enumValue.ToString() ?? string.Empty)?
                     .GetCustomAttributes(typeof(DescriptionAttribute), false)
                     .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
        {
            return descriptionAttribute.Description;
        }
        else
        {
            return enumValue.ToString() ?? string.Empty;
        }
    }
}
