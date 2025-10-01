using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

/// <summary>
/// Image Metadata
/// </summary>
[XmlRoot(ElementName = "Metadata")]
public record Metadata
{
    /// <summary>
    /// Core file properties
    /// </summary>
    public required Core Core { get; init; }

    /// <summary>
    /// Custom name value pair descriptors that conform to the Data Services label types
    /// </summary>
    public LabelCollection? Labels { get; init; }

    /// <summary>
    /// Properties of the overall instrument (system) and software used to obtain the result
    /// </summary>
    public Instrument? Instrument { get; init; }

    /// <summary>
    /// General properties of the acquisition not related to a specific subsystem
    /// </summary>
    public Acquisition? Acquisition { get; init; }

    /// <summary>
    /// Properties of the Optics subsystem in effect during acquisition
    /// </summary>
    public Optics? Optics { get; init; }

    /// <summary>
    /// Properties of the Energy Filter
    /// </summary>
    public EnergyFilterSettings? EnergyFilterSettings { get; init; }

    /// <summary>
    /// Properties of the Stage position and hardware
    /// </summary>
    public StageSettings? StageSettings { get; init; }

    /// <summary>
    /// Settings of the Scan Engine
    /// </summary>
    public ScanSettings? ScanSettings { get; init; }

    /// <summary>
    /// Properties of the vacuum subsystem
    /// </summary>
    public VacuumProperties? VacuumProperties { get; init; }

    /// <summary>
    /// Detector properties, a set of properties per active Detector
    /// </summary>
    [XmlArrayItem("AnalyticalDetector", typeof(AnalyticalDetector), IsNullable = false)]
    [XmlArrayItem("ImagingDetector", typeof(ImagingDetector), IsNullable = false)]
    [XmlArrayItem("ScanningDetector", typeof(ScanningDetector), IsNullable = false)]
    public DetectorCollection? Detectors { get; init; }

    /// <summary>
    /// Gas Injection System (GIS) properties. a set of properties per active GIS port.
    /// </summary>
    public GasInjectionSystemCollection? GasInjectionSystems { get; init; }

    /// <summary>
    /// Properties related to the resulting binary data such as an image
    /// </summary>
    public BinaryResult? BinaryResult { get; init; }

    /// <summary>
    /// The physical item or materials being imaged
    /// </summary>
    public Sample? Sample { get; init; }

    /// <summary>
    /// Tracked properties (name value pairs) specific to an application that are not part of the Metadata standard
    /// </summary>
    public CustomPropertyCollectionCollection? CustomProperties { get; init; }

    /// <summary>
    /// Any Arbitrary application defined xml
    /// </summary>
    public CustomSectionCollection? CustomSections { get; init; }
}
