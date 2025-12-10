using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Properties of the vacuum subsystem
/// </summary>
[XmlType("VacuumProperties")]
public record VacuumProperties
{
    /// <summary>
    /// The Pressure near the sample
    /// </summary>
    public double? SamplePressure { get; init; }

    /// <summary>
    /// Mode the vacuum is in
    /// </summary>
    public VacuumMode? VacuumMode { get; init; }

    /// <summary>
    /// The Pressure near the Electron source
    /// </summary>
    public double? ElectronSourcePressure { get; init; }

    /// <summary>
    /// The Pressure near the Ion source
    /// </summary>
    public double? IonSourcePressure { get; init; }

    /// <summary>
    /// The Pressure inside the Electron column
    /// </summary>
    public double? ElectronColumnPressure { get; init; }

    /// <summary>
    /// The Pressure inside the Ion column
    /// </summary>
    public double? IonColumnPressure { get; init; }

    /// <summary>
    /// The Pressure in the Projection chamber
    /// </summary>
    public double? ProjectionChamberPressure { get; init; }
}
