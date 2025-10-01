using Betrian.Serializers.Ini;

namespace Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;

public class NamedBeamSection
{
    public string Source { get; set; } = "FEG";
    public string ColumnType { get; set; } = "Elstar";
    public string FinalLens { get; set; } = "Elstar";
    public string Acq { get; set; } = "PIA 4";
    public string Aperture { get; set; } = "AVA";
    public double ApertureDiameter { get; set; }
    public double HV { get; set; }
    public double HFW { get; set; }
    public double VFW { get; set; }
    public double WD { get; set; }
    public double BeamCurrent { get; set; }
    [IniBoolValueFormatter("yes", "no")]
    public bool TiltCorrectionIsOn { get; set; }
    [IniBoolValueFormatter("yes", "no")]
    public bool DynamicFocusIsOn { get; set; }
    [IniBoolValueFormatter("On", "Off")]
    public bool DynamicWDIsOn { get; set; }
    public double ScanRotation { get; set; }
    public string LensMode { get; set; } = "Immersion";
    public string LensModeA { get; set; } = string.Empty;
    public double? ATubeVoltage { get; set; }
    public string UseCase { get; set; } = string.Empty;
    public string SemOpticalMode { get; set; } = string.Empty;
    public string ImageMode { get; set; } = "Normal";
    public double SourceTiltX { get; set; }
    public double SourceTiltY { get; set; }
    public double StageX { get; set; }
    public double StageY { get; set; }
    public double StageZ { get; set; }
    public double StageR { get; set; }
    public double StageTa { get; set; }
    public double StageTb { get; set; }
    public double StigmatorX { get; set; }
    public double StigmatorY { get; set; }
    public double BeamShiftX { get; set; }
    public double BeamShiftY { get; set; }
    public double EucWD { get; set; }
    public double EmissionCurrent { get; set; }
    public double TiltCorrectionAngle { get; set; }
    public double PreTilt { get; set; }
    public string WehneltBias { get; set; } = string.Empty;
    public string BeamMode { get; set; } = "N-Beam";
    [IniBoolValueFormatter("On", "Off")]
    public bool MagnificationCorrection { get; set; }
    public double AngularFieldWidth { get; set; }
    public double AngularPixelWidth { get; set; }
    [IniBoolValueFormatter("On", "Off")]
    public bool ElectronChannelingPatternIsOn { get; set; }
    [IniValue("MagnificationSinglePointCorrection.x")]
    public double MagnificationSinglePointCorrectionX { get; set; }
    [IniValue("MagnificationSinglePointCorrection.y")]
    public double MagnificationSinglePointCorrectionY { get; set; }
    public double OrthogonalitySinglePointCorrection { get; set; }
    public double ScanRotationSinglePointCorrection { get; set; }
    [IniBoolValueFormatter("On", "Off")]
    public bool MagnificationSinglePointCorrectionIsOn { get; set; }
}
