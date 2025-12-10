using System.Xml.Serialization;
using UnitsNet;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

[XmlType]
public record Tilt
{
    /// <summary>
    /// The Alpha tilt of the stage as reported
    /// </summary>
    public required double Alpha { get; init; }

    /// <summary>
    /// The Beta tilt of the stage as reported
    /// </summary>
    public required double Beta { get; init; }
}

[XmlType]
public record StagePosition
{
    public static implicit operator Models.StagePosition(StagePosition other)
    {
        return new Models.StagePosition
        {
            X = Length.FromMeters(other.X),
            Y = Length.FromMeters(other.Y),
            Z = Length.FromMeters(other.Z),
            Rotation = Angle.FromRadians(other.Rotation ?? 0),
            Tilt = Angle.FromRadians(other.Tilt?.Alpha ?? 0)
        };
    }

    /// <summary>
    /// The X position as reported by the stage
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// The Y position as reported by the stage
    /// </summary>
    public required double Y { get; set; }

    /// <summary>
    /// The Z position as reported by the stage
    /// </summary>
    public required double Z { get; set; }

    /// <summary>
    /// Rotation of the stage
    /// </summary>
    public required double? Rotation { get; set; }

    /// <summary>
    /// The Tilt of the stage as reported
    /// </summary>
    public Tilt? Tilt { get; set; }
}
