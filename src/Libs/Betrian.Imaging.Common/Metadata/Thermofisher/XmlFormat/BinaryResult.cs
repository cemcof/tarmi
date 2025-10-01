using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Properties related to the resulting binary data such as an image
/// </summary>
[XmlType]
public record BinaryResult
{
    /// <summary>
    /// The basic unit of acquisition
    /// </summary>
    public AcquisitionUnit? AcquisitionUnit { get; init; }

    /// <summary>
    /// The method used to compose the acquisition units
    /// </summary>
    public CompositionType? CompositionType { get; init; }

    /// <summary>
    /// The number of pixels in the X and Y Dimension
    /// </summary>
    public Point<uint>? ImageSize { get; init; }

    /// <summary>
    /// Defines which detector is the main detector that has been used to form the result
    /// </summary>
    public string? Detector { get; init; }

    /// <summary>
    /// Type of filter used, e.g. integrate or average filter, etc.
    /// </summary>
    public string? FilterType { get; init; }

    /// <summary>
    /// User-Selected value that describes the filtering,
    /// for RecursiveAverage filter, (Averaging: alpha).
    /// </summary>
    public double? RecursiveFilterCoefficient { get; init; }

    /// <summary>
    /// Number of source frames combined using the averaging or integration filter
    /// </summary>
    public int? FilterFrameCount { get; init; }

    /// <summary>
    /// Physical size of the image pixel. (In X,Y dimension. m, eV, m-1, rad)
    /// </summary>
    public Point<Quantity>? PixelSize { get; init; }

    /// <summary>
    /// Relative distance the top-left (0,0) pixel represents from the geometric center
    /// of the scanned area (the 0,0 x,y coordinate)
    /// </summary>
    public PointD? Offset { get; init; }

    /// <summary>
    /// Multiplication factor to convert intensity values to other physical measurement
    /// in the event they do not represent intensity, default intensity scale = 1.0
    /// </summary>
    public double? IntensityScale { get; init; }

    /// <summary>
    /// The value added to the scaled input value to determine actual intensity,
    /// default IntensityOffset = 0.0
    /// </summary>
    public double? IntensityOffset { get; init; }

    /// <summary>
    /// Luminosity value in the image to display as the "black" (darkest) level,
    /// e.g. for histogram "stretching"
    /// </summary>
    public double? BlackLevel { get; init; }

    /// <summary>
    /// Luminosity value in the image to display as the "white" (brightest) level,
    /// e.g. for histogram "stretching"
    /// </summary>
    public double? WhiteLevel { get; init; }

    /// <summary>
    /// Computed maximum numeric value of pixel values of the unprocessed image,
    /// for grayscale images corresponds to the white-most value
    /// </summary>
    public double? PixelValueMaximum { get; init; }

    /// <summary>
    /// Computed minimum numeric value of pixel values of the unprocessed image,
    /// for grayscale images corresponds to the black-most value
    /// </summary>
    public double? PixelValueMinimum { get; init; }

    /// <summary>
    /// Computed average (mean) numeric value of pixel values of the unprocessed image
    /// </summary>
    public double? PixelValueMean { get; init; }

    /// <summary>
    /// Computed standard deviation numeric value of pixel values of the unprocessed image
    /// </summary>
    public double? PixelValueStandardDeviation { get; init; }

    /// <summary>
    /// Sharpness as computed from the image pixel data
    /// </summary>
    public double? Sharpness { get; init; }

    /// <summary>
    /// The algorithm used to calculate the sharpness value, default: Unknown
    /// </summary>
    public string? SharpnessAlgorithm { get; init; }

    /// <summary>
    /// Contrast which is applied to the image after acquisition and prior to saving
    /// </summary>
    public double? DigitalContrast { get; init; }

    /// <summary>
    /// Brightness which is applied to the image after acquisition and prior to saving
    /// </summary>
    public double? DigitalBrightness { get; init; }

    /// <summary>
    /// Image adjustment gamma setting, default of 1.0 = no adjustment
    /// </summary>
    public double? Gamma { get; init; }

    /// <summary>
    /// Sigma adjustment
    /// </summary>
    public double? Sigma { get; init; }

    /// <summary>
    /// The type of color channel
    /// </summary>
    public Encoding? Encoding { get; init; }

    /// <summary>
    /// The number of bits per pixel
    /// </summary>
    public int? BitsPerPixel { get; init; }

    /// <summary>
    /// The reference transformation matrix translates pixel coordinates to the coordinate system
    /// at the sample, the unit can be Meter or Radian
    /// </summary>
    public ReferenceTransformation? ReferenceTransformation { get; init; }

    /// <summary>
    /// The relative area from the full potential area used for acquisition to arrive at ImageSize
    /// and Offset
    /// </summary>
    public RatioRectangle? AcquisitionArea { get; init; }

    /// <summary>
    /// The index of the detector in the list of detectors,
    /// other detectors are listed as well because they may influence the detector
    /// </summary>
    public int? DetectorIndex { get; init; }
}
