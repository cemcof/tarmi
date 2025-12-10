using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// The physical item or materials being imaged
/// </summary>
[XmlType]
public record Sample
{
    /// <summary>
    /// Used internal to FEI systems as unique-id of the sample instance
    /// </summary>
    public Guid? SampleGuid { get; init; }

    /// <summary>
    /// Customer identifier for sample, this may be a system identifier for a customer
    /// </summary>
    public string? SampleID { get; init; }

    /// <summary>
    /// Human understandable description of the sample
    /// </summary>
    public string? SampleDescription { get; init; }

    /// <summary>
    /// Unique ID for a session with a sample
    /// </summary>
    public string? SampleSessionID { get; init; }

    /// <summary>
    /// Unique ID for a session with a sample
    /// </summary>
    public string? RegionOfInterestID { get; init; }
}
