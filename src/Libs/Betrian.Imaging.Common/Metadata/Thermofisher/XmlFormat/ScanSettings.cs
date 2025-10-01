using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Settings of the Scan Engine
/// </summary>
[XmlType]
public record ScanSettings
{
    /// <summary>
    /// Time to scan one pixel, scan only
    /// </summary>
    public double? DwellTime { get; init; }

    /// <summary>
    /// Total size of the frame (scan only) Width, Height [pixels], total size possible,
    /// also spans the resolution of the pixel series or spectrum series
    /// </summary>
    public required Size ScanSize { get; init; }

    /// <summary>
    /// Rectangular area of the scan, the actual captured area, which must be <= the Scan Size,
    /// expressed in pixel units as defined by the scan size, if the ScanArea is not provided
    /// then it is assumed to be the Scan Size with 0,0 for X, Y.
    /// </summary>
    public Rectangle? ScanArea { get; init; }

    /// <summary>
    /// If on (true), the image line or frame scan frequency is synced with the power supply frequency
    /// </summary>
    public bool? MainsLockOn { get; init; }

    /// <summary>
    /// Time to scan one line including flyback time, scan only
    /// </summary>
    public double? LineTime { get; init; }

    /// <summary>
    /// The number of lines that you average before you proceed to the next line, default value is 1
    /// </summary>
    public int? LineIntegrationCount { get; init; }

    /// <summary>
    /// The number of partial frames to get one total frame, default value is 1
    /// </summary>
    public int? LineInterlacing { get; init; }

    /// <summary>
    /// Time to scan one frame, including flyback times, scan only
    /// </summary>
    public double? FrameTime { get; init; }

    /// <summary>
    /// Total time taken to acquire the image/result, always >= the frame time,
    /// but may be larger due to post processing or multi-frame compositing
    /// </summary>
    public double? AcquisitionTime { get; init; }

    /// <summary>
    /// The rotation angle of the scan
    /// </summary>
    public double? ScanRotation { get; init; }
}
