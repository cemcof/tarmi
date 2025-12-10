using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Tracked property (name value pairs) specific to an application
/// that is not part of the Metadata standard
/// </summary>
[XmlType]
public record CustomProperty
{
    /// <summary>
    /// The name of the property
    /// </summary>
    [XmlAttribute("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Application provided value for the property
    /// </summary>
    [XmlAttribute("value")]
    public required string Value { get; init; }

    /// <summary>
    /// An optional type that can represent the type of property or the unit string of a property
    /// </summary>
    [XmlAttribute("type")]
    public required string Type { get; init; }

    /// <summary>
    /// An optional raw data type that identifies the underlying raw type to assist in search ability
    /// </summary>
    [XmlAttribute("rawdatatype")]
    public required RawDatatype RawDatatype { get; init; }
}

[XmlType(TypeName = "CustomProperties")]
public record CustomPropertiesCollection
{
    /// <summary>
    /// The Application scope of tracked properties, what application do these properties belong too
    /// </summary>
    [XmlAttribute("scope")]
    public required string Scope { get; init; }

    /// <summary>
    /// Collection of custom Properties
    /// </summary>
    [XmlElement("CustomProperty")]
    public required List<CustomProperty> CustomProperties { get; init; }
}

/// <summary>
/// Collection of custom name value pair descriptors that conform to the Data Services label types
/// </summary>
public class CustomPropertyCollectionCollection : List<CustomPropertiesCollection> { }
