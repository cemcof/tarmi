using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

public static class MetadataXmlSerializer
{
    private static XmlSerializerNamespaces CreateSerializerNamespaces()
    {
        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("nil", "http://schemas.fei.com/Metadata/v1/2013/07");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        return namespaces;
    }

    private static readonly XmlSerializerNamespaces SerializerNamespaces = CreateSerializerNamespaces();
    private static readonly XmlSerializer Serializer = new(typeof(Metadata));
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Indent = true,
        IndentChars = "  ",
        OmitXmlDeclaration = false,
        NamespaceHandling = NamespaceHandling.OmitDuplicates
    };

    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        IgnoreWhitespace = true,
        IgnoreComments = true
    };

    public static Metadata Deserialize(string xml)
    {
        using var reader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(reader, ReaderSettings);
        return (Metadata)Serializer.Deserialize(xmlReader)!;
    }

    public static string Serialize(Metadata metadata)
    {
        var doc = XmlSerialize(metadata);
        return doc.ToString();
    }

    private static XElement XmlSerialize(Metadata metadata)
    {
        var doc = new XDocument();
        {
            using var writer = doc.CreateWriter();
            Serializer.Serialize(writer, metadata);
        }

        // remove nullable elements
        doc
            .Descendants()
            .Where(x => (bool?)x.Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")) == true)
            .Remove();

        // remove empty elements
        doc
            .Descendants()
            .Where(x => x.IsEmpty && !x.HasAttributes)
            .Remove();

        XNamespace nil = "http://schemas.fei.com/Metadata/v1/2013/07";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        var newRoot = new XElement("Metadata",
          new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
          new XAttribute(XNamespace.Xmlns + "nil", nil.NamespaceName)
        );

        newRoot.Add(doc.Element("Metadata")!.Descendants());

        var finalDoc = new XDocument();
        finalDoc.Add(newRoot);

        return finalDoc.Root!;
    }
}
