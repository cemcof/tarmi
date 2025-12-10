using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Properties of the overall instrument (system) and software used to obtain the result
/// </summary>
[XmlType]
public record Instrument
{
    /// <summary>
    /// Name of the software used to perform result acquisition
    /// </summary>
    public string? AcquisitionServer { get; init; }

    /// <summary>
    /// Up to 4-part version number, e.g. 1.0.0.0
    /// </summary>
    public string? AcquisitionServerVersion { get; init; }

    /// <summary>
    /// Name of the instrument control software
    /// </summary>
    public string? ControlSoftware { get; init; }

    /// <summary>
    /// Up to 4-part version number, e.g. 1.0.0.0
    /// </summary>
    public string? ControlSoftwareVersion { get; init; }

    /// <summary>
    /// Instrument manufacturer name
    /// </summary>
    public required string Manufacturer { get; init; }

    /// <summary>
    /// Type (overall classification) of the instrument
    /// </summary>
    public string? InstrumentClass { get; init; }

    /// <summary>
    /// Instrument model name. Usually consists of "Product Line" plus specific "Model"
    /// </summary>
    public string? InstrumentModel { get; init; }

    /// <summary>
    /// Instrument Unique ID. May be unique within all instruments (like serial number) or may be only unique within a product line (like "D-number")
    /// </summary>
    public required string InstrumentID { get; init; }

    /// <summary>
    /// Name of primary instrument computer, used by customers to identify the instrument, unique within one network
    /// </summary>
    public string? ComputerName { get; init; }
}
