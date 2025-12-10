using System.Xml.Serialization;

namespace Tarmi.Maps.DataFormat.TfsDataModel;

[Serializable]
public record Channel
{
    [XmlElement("Name")]
    public string? Name { get; init; }

    [XmlElement("Guid")]
    public Guid? Guid { get; init; }

    [XmlElement("Index")]
    public byte? Index { get; init; }

    [XmlElement("Color")]
    public ChannelColor? Color { get; init; }

    [XmlElement("CameraBits")]
    public double? CameraBits { get; init; }

    [XmlElement("Additive")]
    public bool? Additive { get; init; }
}

[Serializable]
public record ChannelColor
{
    [XmlElement("A")]
    public byte? A { get; init; }

    [XmlElement("R")]
    public byte? R { get; init; }

    [XmlElement("G")]
    public byte? G { get; init; }

    [XmlElement("B")]
    public byte? B { get; init; }
}
