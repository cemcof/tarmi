namespace Tarmi.Serializers.Ini;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IniValueFormatterAttribute : Attribute
{
    public string Formatter { get; }

    public IniValueFormatterAttribute(string formatter)
    {
        Formatter = formatter;
    }
}
