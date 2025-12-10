namespace Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;

public enum BeamType
{
    Electron,
    Ion,
    Photon,
    Infrared,
    Optical,
    Unknown
}

public enum SourceType
{
    FEG,
    W,
    LaB6,
    XFEG,
    SFEG,
    Monochromator,
    Thermionic,
    Tungsten,
    Undefined,
    Xenon, // IBeam
}

public enum ProbeMode
{
    Nanoprobe,
    Microprobe
}

public enum IlluminationMode
{
    Parallel,
    Probe,
    ProbeLargeAngle,
    C2Off,
    C3Off,
    Free,
    None
}

public enum ObjectiveLensMode
{
    LM,
    HM,
    Lorentz
}

public enum ProjectorMode
{
    Diffraction,
    Imaging
}

public enum TEMOperatingSubMode
{
    BrightField,
    DarkField,
    ACDarkField,
    Holography,
    None
}

public enum OperatingMode
{
    TEM,
    STEM
}

public enum HighMagnificationMode
{
    Mi,
    SA,
    Mh,
    None
}

public enum ApertureType
{
    Slit,
    Cicular, // compatibility typo
    Biprism,
    Retracted,
    None,
    EnergySlit,
    FaradayCup,
    Unknown
}

public enum ApertureMechanismType
{
    Manual,
    Motorized,
    Unknown
}

public enum EntranceApertureType
{
    Circular,
    Mask
}

public enum VacuumMode
{
    HighVacuum,
    LowVacuum,
    ESEM,
    ETEM,
    Vented,
    Unknown,
    Off,
    Evacuating,
    Stopping,
    Ready,
    Venting,
    Error
}

public enum ShutterState
{
    Unknown,
    Opening,
    Closing,
    Opened,
    Closed
}

public enum ShutterType
{
    [Obsolete("Use ShutterPosition instead")]
    PreSpecimen = 1,
    [Obsolete("Use ShutterPosition instead")]
    PostSpecimen,
    [Obsolete("Use ShutterPosition instead")]
    Camera,
    [Obsolete("Use ShutterPosition instead")]
    Filter,
    Rolling,
    Mechanical,
    Electrostatic
}

public enum DarkGainCorrectionType
{
    None,
    Dark,
    DarkGain
}

public enum RawDatatype
{
    None,
    Boolean,
    DateTime,
    Double,
    Int,
    String
}

public enum NeedleState
{
    Offline,
    Inserted,
    Retracted,
    Inserting,
    Retracting,
    Error,
    MaskOnline,
    Venting,
    Unknown
}

public enum AcquisitionUnit
{
    Pixel,
    Spectrum,
    CameraImage
}

public enum CompositionType
{
    Single = 1,
    Series,
    Rectangle
}

public enum Encoding
{
    Unsigned,
    Signed,
    Float,
    RGB,
    RGBA,
    BGR,
    Complex_Float_Half,
    Complex_Float
}
