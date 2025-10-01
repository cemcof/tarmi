using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Custom name value pair descriptors that conform to the Data Services label types
/// </summary>
[XmlType]
public record Label
{
    [XmlAttribute("type")]
    public required string Type { get; set; }

    [XmlAttribute("value")]
    public required string Value { get; init; }
}

/// <summary>
/// Collection of custom name value pair descriptors that conform to the Data Services label types
/// </summary>
public class LabelCollection : List<Label> { }

