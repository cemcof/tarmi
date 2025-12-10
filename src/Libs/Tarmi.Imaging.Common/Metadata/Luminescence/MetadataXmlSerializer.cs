using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Tarmi.Models.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Luminescence;
public static class MetadataXmlSerializer
{
    public static string Serialize(Metadata metadata) => Helpers.SerializeToString(metadata);

    public static Metadata Deserialize(string xml)
    {
        xml = Helpers.NormalizeXmlString(xml);
        using var reader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(reader);
        var serializer = new DataContractSerializer(typeof(Metadata));
        return serializer.ReadObject(xmlReader) as Metadata ?? throw new FormatException("Invalid XML Luminescence metadata format").AddData("xml", xml);
    }
}
