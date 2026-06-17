using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Tarmi.Models.Serialization;

public static class Helpers
{
    public const string LegacyAppNamespace = "http://schemas.datacontract.org/2004/07/CFLMNavi";
    public const string AppNamespace = "http://schemas.datacontract.org/2004/07/Tarmi";

    private static readonly Encoding Utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly XmlWriterSettings XmlWriterSettings = new()
    {
        Encoding = Utf8NoBOM,
        Indent = true,
        NewLineOnAttributes = true
    };

    public static string NormalizeXmlString(string xml)
    {
        return xml.Contains(LegacyAppNamespace, StringComparison.OrdinalIgnoreCase)
            ? xml.Replace(LegacyAppNamespace, AppNamespace, StringComparison.OrdinalIgnoreCase)
            : xml;
    }

    public static void Save(object obj, Stream stream)
    {
        var namespaceAttributes = obj
            .GetType()
            .GetCustomAttributes(typeof(NamespaceAttribute), true)
            .Cast<NamespaceAttribute>()
            .DistinctBy(nsa => nsa.Uri)
            .ToArray();

        using var xmlWriter = XmlWriter.Create(stream, XmlWriterSettings);
        var dataContractSerializer = new DataContractSerializer(obj.GetType());

        dataContractSerializer.WriteStartObject(xmlWriter, obj);
        foreach (var ns in namespaceAttributes)
        {
            xmlWriter.WriteAttributeString("xmlns", ns.Prefix, null, ns.Uri);
        }

        dataContractSerializer.WriteObjectContent(xmlWriter, obj);
        dataContractSerializer.WriteEndObject(xmlWriter);
    }

    public static string SerializeToString(object obj)
    {
        using var stream = new MemoryStream();
        Save(obj, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream, Utf8NoBOM);
        return reader.ReadToEnd();
    }
}
