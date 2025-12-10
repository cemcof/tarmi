using System.Xml.Serialization;

namespace Tarmi.Maps.DataFormat.TfsDataModel;

[Serializable]
public record Image
{
    [XmlElement("Guid")]
    public required Guid Guid { get; init; }

    [XmlElement("Index")]
    public required ImageIndex Index { get; init; } = new ImageIndex();

    [XmlElement("Position")]
    public required ImagePosition Position { get; init; } = new ImagePosition();

    [XmlElement("RelativePath")]
    public required string RelativePath { get; init; }

    [XmlElement("Time")]
    public required string Time { get; init; }
}

[Serializable]
public record ImagePosition
{
    [XmlElement("X")]
    public double X { get; init; } = 0;

    [XmlElement("Y")]
    public double Y { get; init; } = 0;

    [XmlElement("Z")]
    public double Z { get; init; } = 0;
}

[Serializable]
public record ImageIndex
{
    [XmlElement("Row")]
    public byte Row { get; init; } = 0;

    [XmlElement("Column")]
    public byte Column { get; init; } = 0;

    [XmlElement("Channel")]
    public byte Channel { get; init; } = 0;

    [XmlElement("Plane")]
    public byte Plane { get; init; } = 0;

    [XmlElement("TimeFrame")]
    public byte TimeFrame { get; init; } = 0;
}
