namespace Tarmi.Serializers.Ini;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IniSectionAttribute : Attribute
{
    public string Name { get; }

    public IniSectionAttribute(string name) => Name = name;
}
