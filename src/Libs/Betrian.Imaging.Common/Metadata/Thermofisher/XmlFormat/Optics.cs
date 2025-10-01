using System.Xml.Serialization;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.XmlFormat;

[XmlType]
public record Optics
{
    /// <summary>
    /// Collection of Aperture Properties
    /// </summary>
    public List<Aperture>? Apertures { get; init; }

    /// <summary>
    /// A user selected value that affects the voltage of the gun lens
    /// </summary>
    public double? GunLensSetting { get; init; }

    /// <summary>
    /// Bias Voltage of the Wehnelt, thermionic only
    /// </summary>
    public double? WehneltBias { get; init; }

    /// <summary>
    /// Voltage applied to the extractor electrode to extract electrons from the tip, FEG only
    /// </summary>
    public double? ExtractorVoltage { get; init; }

    /// <summary>
    /// Voltage applied to L0 which contributes to BeamCurrent and Focus, applies only to some FIB systems
    /// </summary>
    public double? FibL0Voltage { get; init; }

    /// <summary>
    /// Voltage applied to L1 which contributes to BeamCurrent and Focus
    /// </summary>
    public double? FibL1Voltage { get; init; }

    /// <summary>
    /// Voltage applied to L2 which contributes to BeamCurrent and Focus
    /// </summary>
    public double? FibL2Voltage { get; init; }

    /// <summary>
    /// Voltage applied to steering deflector in the column X [or Y] axis
    /// </summary>
    public Point<double>? FibSteering { get; init; }

    /// <summary>
    /// Shift of the source. (0, 0) is no shift
    /// </summary>
    public PointD? GunShiftRaw { get; init; }

    /// <summary>
    /// Tilt of the source. (0, 0) is no tilt
    /// </summary>
    public PointD? GunTiltRaw { get; init; }

    /// <summary>
    /// Accelerating voltage from the beam for the charged particle
    /// </summary>
    public double? AccelerationVoltage { get; init; }

    /// <summary>
    /// The deceleration voltage
    /// </summary>
    public double? DecelerationVoltage { get; init; }

    /// <summary>
    /// Setting that affects (L1) Beam Current and Spot Size. But not precise measurement
    /// </summary>
    public int? SpotIndex { get; init; }

    /// <summary>
    /// Illumination intensity normalized to 0-100 where 100 is the maximum possibly intensity
    /// for the illumination source in question, 0 is none
    /// </summary>
    public double? IlluminationIntensityNormalized { get; init; }

    /// <summary>
    /// Whether optical camera illumination source is active during image capture
    /// </summary>
    public bool? IlluminationOn { get; init; }

    /// <summary>
    /// Name of the type of illumination
    /// </summary>
    public string? IlluminationType { get; init; }

    /// <summary>
    /// C1 lens setting ([0-1] interval)
    /// </summary>
    public double? C1LensIntensity { get; init; }

    /// <summary>
    /// C2 lens setting ([0-1] interval)
    /// </summary>
    public double? C2LensIntensity { get; init; }

    /// <summary>
    /// C3 lens setting ([0-1] interval)
    /// </summary>
    public double? C3LensIntensity { get; init; }

    /// <summary>
    /// Objective lens setting ([0-1] interval)
    /// </summary>
    public double? ObjectiveLensIntensity { get; init; }

    /// <summary>
    /// Intermediate lens setting ([0-1] interval)
    /// </summary>
    public double? IntermediateLensIntensity { get; init; }

    /// <summary>
    /// Diffraction lens setting ([0-1] interval)
    /// </summary>
    public double? DiffractionLensIntensity { get; init; }

    /// <summary>
    /// Projector1 lens setting ([0-1] interval)
    /// </summary>
    public double? Projector1LensIntensity { get; init; }

    /// <summary>
    /// Projector2 lens setting ([0-1] interval)
    /// </summary>
    public double? Projector2LensIntensity { get; init; }

    /// <summary>
    /// Lorentz lens setting ([0-1] interval)
    /// </summary>
    public double? LorentzLensIntensity { get; init; }

    /// <summary>
    /// MiniCondenser lens setting ([0-1] interval)
    /// </summary>
    public double? MiniCondenserLensIntensity { get; init; }

    /// <summary>
    /// Spot diameter
    /// </summary>
    public double? SpotSize { get; init; }

    /// <summary>
    /// Source emission current
    /// </summary>
    public double? EmissionCurrent { get; init; }

    /// <summary>
    /// Actual current induced into the sample (cf. Spot), output from the Beam Limiting Aperture
    /// </summary>
    public double? BeamCurrent { get; init; }

    public double? BeamCurrentSelected { get; init; }

    /// <summary>
    /// Diameter of the beam (m) at the sample
    /// </summary>
    public double? BeamDiameter { get; init; }

    /// <summary>
    /// Convergence semi-angle of the beam
    /// </summary>
    public double? BeamConvergence { get; init; }

    /// <summary>
    /// Current as measured on the flu screen
    /// </summary>
    public double? ScreenCurrent { get; init; }

    /// <summary>
    /// The last measured flu screen current when the screen was inserted
    /// </summary>
    public double? LastMeasuredScreenCurrent { get; init; }

    /// <summary>
    /// The size of the full scan
    /// </summary>
    public PointD? FullScanFieldOfView { get; init; }

    /// <summary>
    /// The size of the image
    /// </summary>
    public PointD? ScanFieldOfView { get; init; }

    /// <summary>
    /// Distance from the pole tip to the focal point of the beam
    /// </summary>
    public double? WorkingDistance { get; init; }

    /// <summary>
    /// The working distance of the primary beam to the beam intersection
    /// </summary>
    public double? EucentricWorkingDistance { get; init; }

    /// <summary>
    /// Tilt of the beam above the sample
    /// </summary>
    public PointD? BeamTilt { get; init; }

    /// <summary>
    /// Shift of the Image from the optical axis, TEM only
    /// </summary>
    public PointD? ImageShift { get; init; }

    /// <summary>
    /// Shift of the beam from the optical axis [x,y: m] as applied by the user
    /// </summary>
    public PointD? BeamShift { get; init; }

    /// <summary>
    /// Whether tilt-correction is turned on
    /// </summary>
    public bool? SampleTiltCorrectionOn { get; init; }

    /// <summary>
    /// Inherent Sample Tilt, the combined sample tilt and stage tilt will be sent to optics for tilt correction
    /// </summary>
    public double? SamplePreTiltAngle { get; init; }

    /// <summary>
    /// The stigmator strength for SEM or FIB., electron column [-1, 1], ion column [-3, 3]
    /// </summary>
    public PointD? StigmatorRaw { get; init; }

    /// <summary>
    /// Gun stigmator Strength, optional on Titan
    /// </summary>
    public double? GunStigmatorRaw { get; init; }

    /// <summary>
    /// Stigmator of the beam shape
    /// </summary>
    public double? CondenserStigmatorRaw { get; init; }

    /// <summary>
    /// Stigmator of the objective lens (TEM image stigmation)
    /// </summary>
    public double? ObjectiveStigmatorRaw { get; init; }

    /// <summary>
    /// Diffraction lens stigmator (Diffraction pattern stigmation)
    /// </summary>
    public double? DiffractionStigmatorRaw { get; init; }

    /// <summary>
    /// Spherical aberration of the objective lens (constant) or the spherical aberration
    /// set by the aberration corrector, TEM only
    /// </summary>
    public double? SphericalAberration { get; init; }

    /// <summary>
    /// Distance from the focus plane to the eucentric focus plane, controlled by the objective lens, TEM only
    /// </summary>
    public double? Focus { get; init; }

    /// <summary>
    /// Distance from the focus plane to the eucentric focus plane, controlled by changing intensity, TEM only
    /// </summary>
    public double? STEMFocus { get; init; }

    /// <summary>
    /// Distance from the focus plane to a plane the user chooses, TEM only
    /// </summary>
    public double? Defocus { get; init; }

    /// <summary>
    /// Magnification to the Plate camera plane, only for image mode
    /// </summary>
    public double? NominalMagnification { get; init; }

    /// <summary>
    /// Property of the magnification
    /// </summary>
    public HighMagnificationMode? HighMagnificationMode { get; init; }

    /// <summary>
    /// Primary operating mode of the column
    /// </summary>
    public OperatingMode? OperatingMode { get; init; }

    /// <summary>
    /// TEM specific operating mode of the column
    /// </summary>
    public TEMOperatingSubMode? TEMOperatingSubMode { get; init; }

    /// <summary>
    /// Name of the optical preset
    /// </summary>
    public string? OpticalMode { get; init; }

    /// <summary>
    /// Mode of the projection system
    /// </summary>
    public ProjectorMode? ProjectorMode { get; init; }

    /// <summary>
    /// Whether the magnifications are adapted to the energy filter
    /// </summary>
    public bool? EFTEMOn { get; init; }

    /// <summary>
    /// Mode of the objective lens. S/TEM only
    /// </summary>
    public ObjectiveLensMode? ObjectiveLensMode { get; init; }

    /// <summary>
    /// Illumination/Condenser mode
    /// </summary>
    public IlluminationMode? IlluminationMode { get; init; }

    /// <summary>
    /// Mode of the minicondenser (type of probe)
    /// </summary>
    public ProbeMode? ProbeMode { get; init; }

    /// <summary>
    /// Length of a virtual camera system (distance between film plate/ccd and the sample), only for diffraction
    /// </summary>
    public double? CameraLength { get; init; }

    /// <summary>
    /// Focus value used in diffraction
    /// </summary>
    public double? DiffractionFocus { get; init; }

    /// <summary>
    /// Whether cross over mode is on
    /// </summary>
    public bool? CrossOverOn { get; init; }

    /// <summary>
    /// The setting of the filament heating current
    /// </summary>
    public double? GunFilamentSetting { get; init; }
}
