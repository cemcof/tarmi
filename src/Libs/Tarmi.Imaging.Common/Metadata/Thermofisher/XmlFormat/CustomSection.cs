using System.Xml;
using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Any Arbitrary application defined XML
/// </summary>
[XmlType]
public record CustomSection
{
    /// <summary>
    /// The Application scope of tracked properties, what application do these properties belong too
    /// </summary>
    [XmlAttribute("scope")]
    public required string Scope { get; init; }

    /// <summary>
    /// Any Arbitrary application defined XML
    /// </summary>
    [XmlAnyElement(Order = 1)]
    public required XmlNode AnyXML { get; init; }
}

/// <summary>
/// Any Arbitrary application defined XML
/// </summary>
public class CustomSectionCollection : List<CustomSection> { }

