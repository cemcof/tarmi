namespace Tarmi.Serializers.Ini;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IniBoolValueFormatterAttribute : Attribute
{
    public string TrueValue { get; }
    public string FalseValue { get; }

    public IniBoolValueFormatterAttribute(string trueValue, string falseValue)
    {
        TrueValue = trueValue;
        FalseValue = falseValue;
    }
}
