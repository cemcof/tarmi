namespace Betrian.Serializers.Ini;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class IniValueAttribute : Attribute
{
    public string Name { get; }

    public IniValueAttribute(string name) => Name = name;
}
