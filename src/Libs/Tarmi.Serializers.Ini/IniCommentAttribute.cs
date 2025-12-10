namespace Tarmi.Serializers.Ini;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class IniCommentAttribute : Attribute
{
    public string Comment { get; }

    public IniCommentAttribute(string comment) => Comment = comment;
}
