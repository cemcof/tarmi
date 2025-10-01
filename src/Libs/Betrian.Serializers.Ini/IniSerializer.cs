namespace Betrian.Serializers.Ini;

public class IniSerializer
{
    //public static string Serialize(object obj)
    //{
    //    return IniSerialization.Serialize(obj);
    //}

    public static T Deserialize<T>(string content)
        where T : class, new()
    {
        return IniDeserialization.Deserialize<T>(content);
    }
}
