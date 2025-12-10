using System.Xml.Serialization;

namespace Tarmi.Maps.DataFormat.TfsDataModel;

[Serializable]
[XmlRoot(ElementName = "TfsData")]
public record TfsData
{
    [XmlElement("ImageMatrix", typeof(ImageMatrix))]
    public required List<ImageMatrix> Items { get; init; }
}
