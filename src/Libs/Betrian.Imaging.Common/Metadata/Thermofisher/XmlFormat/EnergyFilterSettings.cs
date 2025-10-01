using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Properties of the Energy Filter
/// </summary>
[XmlType]
public record EnergyFilterSettings
{
    /// <summary>
    /// The offset on the acceleration voltage for Energy Filtered Imaging
    /// </summary>
    public double? AccelerationVoltageOffset { get; init; }

    /// <summary>
    /// The voltage that is applied to the Drift Tube
    /// </summary>
    public double? DriftTubeVoltage { get; init; }

    /// <summary>
    /// The shift of the image or the Spectrum energy by changing the prism current
    /// </summary>
    public double? EnergyShift { get; init; }

    /// <summary>
    /// The width of the energy selection slit
    /// </summary>
    public double? EnergySelectionSlitWidth { get; init; }

    /// <summary>
    /// Indicates whether the slit is inserted or not
    /// </summary>
    public bool? EnergySelectionSlitInserted { get; init; }

    /// <summary>
    /// The diameter of the entrance aperture used
    /// </summary>
    public double? EntranceApertureDiameter { get; init; }

    /// <summary>
    /// The type of entrance aperture
    /// </summary>
    public EntranceApertureType? EntranceApertureType { get; init; }
}
