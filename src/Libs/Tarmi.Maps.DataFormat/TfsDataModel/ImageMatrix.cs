using System.Xml.Serialization;

namespace Tarmi.Maps.DataFormat.TfsDataModel;

[Serializable]
public record ImageMatrix
{
    [XmlElement("Name")]
    public required string Name { get; init; }

    [XmlElement("Guid")]
    public required Guid Guid { get; init; }

    [XmlElement("TileWidth")]
    public double TileWidth { get; init; }

    [XmlElement("TileHeight")]
    public double TileHeight { get; init; }

    [XmlElement("TilePixelWidth")]
    public int TilePixelWidth { get; init; }

    [XmlElement("TilePixelHeight")]
    public int TilePixelHeight { get; init; }

    [XmlElement("Channels")]
    public required Channels Channels { get; init; }

    [XmlElement("Images", IsNullable = false)]
    public required Images Images { get; init; }
}

public record Images
{
    [XmlElement("Image", IsNullable = false)]
    public required List<Image> Items { get; init; }
}

public record Channels
{
    [XmlElement("Channel")]
    public List<Channel> Items { get; init; } = [];
}
