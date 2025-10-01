using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Properties of the Stage position and hardware
/// </summary>
[XmlType("StageSettings")]
public record class StageSettings
{
    /// <summary>
    /// The position as reported by the stage (X, Y and Z),
    /// the Tilt of the stage as reported and rotation of the stage
    /// </summary>
    public required StagePosition StagePosition { get; init; }

    /// <summary>
    /// Identification of the type of specimen holder used
    /// </summary>
    public string? HolderType { get; init; }

    /// <summary>
    /// Only applies to some holder types (cryo, heating)
    /// </summary>
    public double? HolderTemperature { get; init; }

    /// <summary>
    /// The type of sample loader mounted on the stage
    /// </summary>
    public string? SampleLoader { get; init; }
}
