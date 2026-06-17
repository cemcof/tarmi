namespace Tarmi.Serializers.Ini;

public static class IniSerializer
{
    public static T Deserialize<T>(string content)
        where T : class, new()
    {
        return IniDeserialization.Deserialize<T>(content);
    }
}
