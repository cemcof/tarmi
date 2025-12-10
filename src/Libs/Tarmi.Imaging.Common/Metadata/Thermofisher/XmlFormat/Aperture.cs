using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

[XmlType]
public record Aperture
{
    /// <summary>
    /// The sequence of the aperture mechanisms starting at 1 being the top most aperture mechanism in the column
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// The name of the Aperture
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The type of aperture mechanism
    /// </summary>
    public ApertureMechanismType? MechanismType { get; init; }

    /// <summary>
    /// The type of Aperture
    /// </summary>
    public ApertureType? Type { get; init; }

    /// <summary>
    /// The diameter, only for circular apertures
    /// </summary>
    public double? Diameter { get; init; }

    /// <summary>
    /// Numeric indicator of selected aperture
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// Position offset from the align position
    /// </summary>
    public PointD? PositionOffset { get; init; }
}
