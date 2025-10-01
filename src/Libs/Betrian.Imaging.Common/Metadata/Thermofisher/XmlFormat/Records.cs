using System.Reactive;
using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Represents a 2D vector
/// </summary>
[XmlType]
public record Point<TValue>
    where TValue: struct
{
    /// <summary>
    /// Gets or sets the x-coordinate of this Point
    /// </summary>
    public required TValue X { get; init; }

    /// <summary>
    /// Gets or sets the y-coordinate of this Point
    /// </summary>
    public required TValue Y { get; set; }
}

[XmlType]
public record PointD : Point<double> { }

/// <summary>
/// Stores a pair of integers that specify a Height and Width
/// </summary>
[XmlType]
public record Size
{
    /// <summary>
    /// The horizontal component
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// The vertical component
    /// </summary>
    public required int Height { get; set; }
}

/// <summary>
/// Represents a rectangle
/// </summary>
[XmlType]
public record Rectangle : Point<int>
{
    /// <summary>
    /// The horizontal component
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// The vertical component
    /// </summary>
    public required int Height { get; set; }
}

/// <summary>
/// Represents a rectangle
/// </summary>
[XmlType(TypeName = "RectangleR")]
public record RatioRectangle : Point<double>
{
    /// <summary>
    /// The horizontal component
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// The vertical component
    /// </summary>
    public required double Height { get; set; }
}

/// <summary>
/// A Range expressed in Angles (Radians)
/// </summary>
[XmlType]
public record AngularRange
{
    /// <summary>
    /// The beginning of the range
    /// </summary>
    public required double Begin { get; init; }

    /// <summary>
    /// The end of the range
    /// </summary>
    public required double End { get; init; }
}

public record ReferenceTransformation
{
    public double? M11 { get; init; }
    public double? M12 { get; init; }
    public double? M21 { get; init; }
    public double? M22 { get; init; }
    public double? OffsetX { get; init; }
    public double? OffsetY { get; init; }
    public Unit? Unit { get; init; }
}

public readonly record struct Quantity
{
    /// <summary>
    /// Gets the unit
    /// </summary>
    [XmlAttribute("unit")]
    public required string Unit { get; init; }

    /// <summary>
    /// Gets the unit
    /// </summary>
    [XmlAttribute("unitPrefixPower")]
    public required string Exponent { get; init; }

    /// <summary>
    /// The numeric value
    /// </summary>
    [XmlText]
    public required double Value { get; init; }
}
