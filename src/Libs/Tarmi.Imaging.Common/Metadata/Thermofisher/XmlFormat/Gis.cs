using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Gas Injection System (GIS) properties
/// </summary>
[XmlType]
public record Gis
{
    /// <summary>
    /// Ordinal number of GIS port, optional if PortName supplied
    /// </summary>
    public int? PortNumber { get; init; }

    /// <summary>
    /// The configured GIS port name, optional if PortNumber supplied
    /// </summary>
    public string? PortName { get; init; }

    /// <summary>
    /// Whether heater is on
    /// </summary>
    public bool? HeaterOn { get; init; }

    /// <summary>
    /// Whether gas is flowing
    /// </summary>
    public bool? GasFlowOn { get; init; }

    /// <summary>
    /// The inserted state of the needle
    /// </summary>
    public required NeedleState NeedleState { get; init; }

    /// <summary>
    /// Temperature of heated needle at time of execution start
    /// </summary>
    public double? NeedleTemperature { get; init; }

    /// <summary>
    /// Collection of Gas properties, one set per Gas type
    /// </summary>
    public GasCollection? Gases { get; init; }

    /// <summary>
    /// Tracked properties (name value pairs) specific to an application
    /// that are not part of the MetadataNet standard
    /// </summary>
    [XmlArray("CustomPropertyGroup")]
	[XmlArrayItem("CustomProperties")]
    public CustomPropertyCollectionCollection? CustomProperties { get; init; }

    /// <summary>
    /// Any Arbitrary application defined XML
    /// </summary>
    [XmlArray("CustomSectionGroup")]
    [XmlArrayItem("CustomSection")]
    public CustomSectionCollection? CustomSections { get; init; }
}

/// <summary>
/// Collection of Gas Injection System (GIS) properties, one set of properties per active GIS port
/// </summary>
public class GasInjectionSystemCollection : List<Gis> { }

/// <summary>
/// Gas Properties
/// </summary>
[XmlType]
public record Gas
{
    /// <summary>
    /// Description of gas-type
    /// </summary>
    public required string GasType { get; init; }

    /// <summary>
    /// Temperature of crucible at the time of GIS open
    /// </summary>
    public double CrucibleTemperature { get; init; }
}

/// <summary>
/// Collection of Gas properties, one set per Gas Type
/// </summary>
public class GasCollection : List<Gas> { }
