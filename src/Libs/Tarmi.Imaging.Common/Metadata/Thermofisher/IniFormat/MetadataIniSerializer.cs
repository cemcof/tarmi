using Tarmi.Serializers.Ini;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;

public static class MetadataIniSerializer
{
    public static Metadata Deserialize(string ini)
    {
        return IniSerializer.Deserialize<Metadata>(ini);
    }

    //public static string Serialize(Metadata metadata)
    //{
    //    return IniSerializer.Serialize(metadata);
    //}
}
