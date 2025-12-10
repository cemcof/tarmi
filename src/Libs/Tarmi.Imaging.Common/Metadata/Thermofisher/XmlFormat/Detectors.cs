using System.Xml.Serialization;

namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

[XmlType]
public abstract record Detector
{
    /// <summary>
    /// String indicating the type of detector, UI Name
    /// </summary>
    public required string DetectorName { get; init; }

    /// <summary>
    /// Unique identifier for the detector model that can be used to cross reference
    /// external data such as quantification properties
    /// </summary>
    public string? DetectorType { get; init; }

    /// <summary>
    /// Whether Detector is inserted or not, may or may not contribute to result (see MixContribution)
    /// </summary>
    public bool? Inserted { get; init; }

    /// <summary>
    /// Indicates the detector is switched on, which may influence the optics
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Tracked properties (name value pairs) specific to an application that are not part
    /// of the MetadataNet standard
    /// </summary>
    public CustomPropertyCollectionCollection? CustomProperties { get; init; }

    /// <summary>
    /// Any Arbitrary application defined xml
    /// </summary>
    public CustomSectionCollection? CustomSections { get; init; }
}

/// <summary>
/// Shutter type in effect for detection
/// </summary>
[XmlType]
public readonly record struct Shutter
{
    /// <summary>
    /// The type of shutter
    /// </summary>
    [XmlAttribute("type")]
    public required ShutterType Type { get; init; }

    /// <summary>
    /// The position of the shutter
    /// </summary>
    [XmlAttribute("position")]
    public required string Position { get; init; }
}

/// <summary>
/// List of which shutter types were in effect for detection, can be combination of 1 or more
/// </summary>
public class ShutterCollection : List<Shutter> { }

/// <summary>
/// Properties that apply to analytical detectors
/// </summary>
[XmlType]
public record AnalyticalDetector : Detector
{
    /// <summary>
    /// Amount this detector signal contributes to the final result. -1 means inverted,
    /// can be proportion, if only one signal, would be 1.0
    /// </summary>
    public double? MixContribution { get; init; }

    /// <summary>
    /// he angle between the center of the detector and the horizontal plane
    /// </summary>
    public double? ElevationAngle { get; init; }

    /// <summary>
    /// The Azimuthal angle between the center of the detector and the primary tilt axis
    /// </summary>
    public double? AzimuthAngle { get; init; }

    /// <summary>
    /// The solid angle subtended by the active area of the detector at the sample (in steradians)
    /// </summary>
    public double? CollectionAngle { get; init; }

    /// <summary>
    /// Dispersion setting indicating electron-volts per channel
    /// </summary>
    public double? Dispersion { get; init; }

    /// <summary>
    /// Time in seconds to shape a pulse and during which no other pulses can be accepted
    /// </summary>
    public double? PulseProcessTime { get; init; }

    /// <summary>
    /// Total time in seconds to do the acquisition which includes live time and dead time
    /// </summary>
    public double? RealTime { get; init; }

    /// <summary>
    /// The effective acquisition time, real time minus dead time
    /// </summary>
    public double? LiveTime { get; init; }

    /// <summary>
    /// How many photons per second are detected during input
    /// </summary>
    public double? InputCountRate { get; init; }

    /// <summary>
    /// How many photons per second are counted
    /// </summary>
    public double? OutputCountRate { get; init; }

    /// <summary>
    /// Number of pixels (in X,Y respectively) whose combined signal produces one pixel output
    /// </summary>
    public Point<uint>? Binning { get; init; }

    /// <summary>
    /// List of which shutter types were in effect for detection, can be combination of 1 or more
    /// </summary>
    public ShutterCollection? Shutters { get; init; }

    /// <summary>
    /// The state of the mechanical shutter
    /// </summary>
    public ShutterState? ShutterState { get; init; }

    /// <summary>
    /// The start energy of bin 0, this would be in binary data when acquiring a spectrum,
    /// when collecting spectrum events this value has to be in the Metadata
    /// </summary>
    public double? OffsetEnergy { get; init; }

    /// <summary>
    /// The noise in the spectrum caused by the electronics, defined as the standard deviation,
    /// a typical value is 30 eV
    /// </summary>
    public double? ElectronicsNoise { get; init; }
}

/// <summary>
/// Properties that apply to imaging detectors
/// </summary>
[XmlType]
public record ImagingDetector : Detector
{
    /// <summary>
    /// Gain of the CCD camera in dB
    /// </summary>
    public double? Gain { get; init; }

    /// <summary>
    /// Number of pixels (in X,Y respectively) whose combined signal produces one pixel output
    /// </summary>
    public required Point<uint> Binning { get; init; }

    /// <summary>
    /// The area that is readout on the CCD, Binning included
    /// </summary>
    public required Rectangle ReadoutArea { get; init; }

    /// <summary>
    /// Total time in seconds to expose one frame
    /// </summary>
    public required double ExposureTime { get; init; }

    /// <summary>
    /// 2nd exposure time for dual exposure
    /// </summary>
    public double? ExposureTime2 { get; init; }

    /// <summary>
    /// Pre-exposure time in seconds
    /// </summary>
    public double? PreExposureTime { get; init; }

    /// <summary>
    /// List of which shutter types were in effect for detection, can be combination of 1 or more
    /// </summary>
    public ShutterCollection? Shutters { get; init; }

    /// <summary>
    /// The type of dark gain correction applied
    /// </summary>
    public required DarkGainCorrectionType DarkGainCorrectionType { get; init; }

    /// <summary>
    /// PreExposurePauseTime is the time between pre-exposure and the Exposure,
    /// only applicable if pre-exposure was used
    /// </summary>
    public double? PreExposurePauseTime { get; init; }

    /// <summary>
    /// The size of the ccd pixels at the scintillator
    /// </summary>
    public PointD? ScintillatorPixelSize { get; init; }

    /// <summary>
    /// The post-magnification of the ccd camera with respect to the nominal magnification plane
    /// </summary>
    public double? PostMagnification { get; init; }
}

/// <summary>
/// Properties that apply to scanning detectors
/// </summary>
[XmlType]
public record ScanningDetector : Detector
{
    /// <summary>
    /// Amount this detector signal contributes to the final result, -1 means inverted,
    /// can be proportion, if only one signal, would be 1.0
    /// </summary>
    public double? MixContribution { get; init; }

    /// <summary>
    /// A setting on the detector to to set the signal
    /// </summary>
    public string? Signal { get; init; }

    /// <summary>
    /// Mathematical expression of segment contributions where coefficient is assumed to be 1.0
    /// unless otherwise noted, for example, "+A+B-C-D" means 1.0 contribution from A and B
    /// and -1.0 contribution from C and D
    /// </summary>
    public string? Segments { get; init; }

    /// <summary>
    /// Detector gain in decibels, for ESEM this is enhanced gain, in dB
    /// </summary>
    public double? Gain { get; init; }

    /// <summary>
    /// Detector offset in volts
    /// </summary>
    public double? Offset { get; init; }

    /// <summary>
    /// Additional signal boost after Gain and Offset is applied, in dB
    /// </summary>
    public double? EnhancedGain { get; init; }

    /// <summary>
    /// Voltage of the grid on the ETD/CDEM/ICE
    /// </summary>
    public double? GridVoltage { get; init; }

    /// <summary>
    /// Voltage of the suction tube on the TLD
    /// </summary>
    public double SuctionTubeVoltage { get; init; }

    /// <summary>
    /// Voltage of the ESEM/CDEM detector front end
    /// </summary>
    public double? FrontEndVoltage { get; init; }

    /// <summary>
    /// Voltage of the converter electrode of the ICE detector
    /// </summary>
    public double? ConverterElectrodeVoltage { get; init; }

    /// <summary>
    /// Voltage on the Scintillator for ETD/ICE
    /// </summary>
    public double? ScintillatorVoltage { get; init; }


    /// <summary>
    /// Contrast voltage converted to decibels then normalized to a 0-100 user-facing value
    /// </summary>
    public double? ContrastNormalized { get; init; }

    /// <summary>
    /// Value applied to detector inducing brightness then converted to a 0-100 user-facing value
    /// </summary>
    public double? BrightnessNormalized { get; init; }

    /// <summary>
    /// The CollectionAngleRange is the range (start-end) of semi-angles of the electrons that
    /// the (annular) detector can detect
    /// </summary>
    public AngularRange? CollectionAngleRange { get; init; }
}

/// <summary>
/// Collection of Detector properties, a set of properties per active Detector
/// </summary>
//public class DetectorCollection : List<ScanningDetector> { }
public class DetectorCollection : List<Detector> { }
