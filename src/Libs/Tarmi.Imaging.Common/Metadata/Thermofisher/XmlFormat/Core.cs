using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Core file properties
/// </summary>
[XmlType]
public record Core
{
    /// <summary>
    /// Used internal to FEI systems as unique-id of the Image instance
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// FEI system ID of the image from which this image was derived
    /// </summary>
    public Guid? ParentGuid { get; init; }

    /// <summary>
    /// MD5 hash of binary result data. Used to determine duplicate results (when importing), or modification (checksum)
    /// </summary>
    [XmlElement(ElementName = "Md5Checksum", DataType = "hexBinary")]
    public byte[]? Md5Checksum { get; init; }

    /// <summary>
    /// Vendor-specific checksum used for proprietary purposes such as validating the image was from a particular instrument manufacturer
    /// </summary>
    [XmlElement(ElementName = "PrivateChecksum", DataType = "hexBinary")]
    public byte[]? PrivateChecksum { get; init; }

    /// <summary>
    /// Filename and extension (but not path) that is the pixel content (binary data) described by this metadata
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// DateTime when result file is created, usually soon after the acquisition datetime
    /// </summary>
    public DateTime? FileDatetime { get; init; }

    /// <summary>
    /// The User ID or User Name of the user running the process that acquired the image
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// User provided description or comment
    /// </summary>
    public string? Comment { get; init; }

    /// <summary>
    /// The application the user interacts with to setup the acquisition, application that saves the Image
    /// </summary>
    public required string ApplicationSoftware { get; init; }

    /// <summary>
    /// Up to 4-part version number, e.g. 1.0.0.0
    /// </summary>
    public required string ApplicationSoftwareVersion { get; init; }

    /// <summary>
    /// Represents the computer that the Application Software is running on which could be the same or different from the Acquisition and Control software
    /// </summary>
    public string? ApplicationComputerName { get; init; }
}
