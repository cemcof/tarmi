using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// General properties of the acquisition not related to a specific subsystem
/// </summary>
[XmlType]
public record Acquisition
{
    /// <summary>
    /// The UTC time when the acquisition is started
    /// </summary>
    public DateTime? AcquisitionStartDatetime { get; init; }

    /// <summary>
    /// Time when result was acquired, should be closest time also to when all the acquisition
    /// properties were read from the system, this is the Time the Acquisition completes
    /// </summary>
    public required DateTime AcquisitionDatetime { get; init; }

    /// <summary>
    /// Identifier of the acquisition event which may have captured more than one result,
    /// multiple results with the same AcquisitionID can be assumed to have been "hardware synced" during capture
    /// </summary>
    public Guid? AcquisitionID { get; init; }

    /// <summary>
    /// The type of particle used as primary acquisition of the image
    /// </summary>
    public BeamType? BeamType { get; init; }

    /// <summary>
    /// The type or model of the physical column used for acquisition
    /// </summary>
    public string? ColumnType { get; init; }

    /// <summary>
    /// Type of source of the microscope
    /// </summary>
    public SourceType? SourceType { get; init; }
}
