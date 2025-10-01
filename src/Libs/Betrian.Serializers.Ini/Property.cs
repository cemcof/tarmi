namespace Betrian.Serializers.Ini;

internal class Property
{
    public string? Section { get; set; }

    public bool IsArraySection { get; set; }

    public string? Name { get; set; }

    public object? Value { get; set; }

    public List<string> Comments { get; set; } = [];
}
