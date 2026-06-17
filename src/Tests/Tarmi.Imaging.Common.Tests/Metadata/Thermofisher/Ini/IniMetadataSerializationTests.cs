using Tarmi.Imaging.Common.Metadata.Thermofisher.IniFormat;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.Metadata.Thermofisher.Ini;

public class IniMetadataSerializationTests
{
    [Fact]
    public void Serialization_of_known_ini_data_should_succeed()
    {
        const string testData =
"""
[User]
Date=10/09/2023
Time=08:04:59 PM
User=user
UserText=
UserTextUnicode=

[System]
Type=DualBeam
Dnumber=9954108
Software=17.22.0.19782
BuildNr=19782
Source=FEG
Column=Elstar
FinalLens=Elstar
Chamber=xT-SDB
Stage=110 x 110
Pump=TMP
ESEM=no
Aperture=AVA
Scan=PIA 4
Acq=PIA 4
EucWD=0.00397609
SystemType=Helios 5 Hydra CX
DisplayWidth=0.518
DisplayHeight=0.324

[Beam]
HV=2000
Spot=-2
StigmatorX=-0.252203
StigmatorY=0.0272703
BeamShiftX=1.89054e-07
BeamShiftY=-4.90386e-08
ScanRotation=0
ImageMode=Normal
FineStageBias=
Beam=EBeam
Scan=EScan

[EBeam]
Source=FEG
ColumnType=Elstar
FinalLens=Elstar
Acq=PIA 4
Aperture=AVA
ApertureDiameter=2.26e-05
HV=2000
HFW=2.243E-05
VFW=1.088E-05
WD=0.00396297
BeamCurrent=2e-10
TiltCorrectionIsOn=yes
DynamicFocusIsOn=yes
DynamicWDIsOn=
ScanRotation=0
LensMode=Immersion
LensModeA=
ATubeVoltage=
UseCase=
SemOpticalMode=
ImageMode=Normal
SourceTiltX=0.003125
SourceTiltY=0.00166667
StageX=-0.00446336
StageY=-0.000951034
StageZ=0.00396632
StageR=-3.13742
StageTa=0.907553
StageTb=0
StigmatorX=-0.252203
StigmatorY=0.0272703
BeamShiftX=1.89054e-07
BeamShiftY=-4.90386e-08
EucWD=0.00397609
EmissionCurrent=
TiltCorrectionAngle=-0.663242
PreTilt=0
WehneltBias=
BeamMode=N-Beam
MagnificationCorrection=Off
AngularFieldWidth=
AngularPixelWidth=
ElectronChannelingPatternIsOn=
MagnificationSinglePointCorrection.x=1
MagnificationSinglePointCorrection.y=1
OrthogonalitySinglePointCorrection=0
ScanRotationSinglePointCorrection=0
MagnificationSinglePointCorrectionIsOn=Off

[GIS]
Number=0

[Scan]
InternalScan=true
Dwelltime=3e-05
PixelWidth=1e-08
PixelHeight=1e-08
HorFieldsize=2.243E-05
VerFieldsize=1.088E-05
Average=0
Integrate=0
FrameTime=74.1581

[EScan]
Scan=PIA 4
InternalScan=true
Dwell=3e-05
PixelWidth=1e-08
PixelHeight=1e-08
HorFieldsize=2.243E-05
VerFieldsize=1.088E-05
FrameTime=74.1581
LineTime=0.06816
Mainslock=On
LineIntegration=1
ScanInterlacing=1

[Stage]
StageX=-0.00446317
StageY=-0.000951083
StageZ=0.00396632
StageR=-3.13742
StageT=0.907553
StageTb=0
SpecTilt=0
WorkingDistance=0.00396297
ActiveStage=Bulk
StageRawX=-0.00549358
StageRawY=5.59167e-05
StageRawZ=0.0296644
StageRawR=-1.26537
StageRawT=0.907553
StageRawTb=0

[Image]
DigitalContrast=1
DigitalBrightness=0
DigitalGamma=1
Average=0
Integrate=0
ResolutionX=3072
ResolutionY=2048
DriftCorrected=Off
ZoomFactor=1.0
ZoomPanX=
ZoomPanY=
MagCanvasRealWidth=
MagnificationMode=
ScreenMagCanvasRealWidth=
ScreenMagnificationMode=
PostProcessing=
Transformation=

[Vacuum]
ChPressure=0.000289
Gas=
UserMode=High vacuum
Humidity=

[Specimen]
Temperature=
SpecimenCurrent=-2.11707e-10
CryoShieldTemperature=
CryoStageTemperature=

[Detectors]
Number=1
Name=TLD
Mode=BSE

[TLD]
Contrast=66
Brightness=38.4391
Signal=BSE
ContrastDB=30.0887
BrightnessDB=-2.77475
SuctionTube=-244.8
Mirror=0
MinimumDwellTime=1e-07

[Accessories]
Number=0

[PrivateFei]
BitShift=0
DataBarSelected=
DataBarAvailable=
TimeOfCreation=09.10.2023 20:04:59
DatabarHeight=

[HiResIllumination]
BrightFieldIsOn=
BrightFieldValue=
DarkFieldIsOn=
DarkFieldValue=
[EasyLift]
Rotation=0
[HotStageMEMS]
HeatingCurrent=
HeatingVoltage=
TargetTemperature=
ActualTemperature=
HeatingPower=
SampleBias=
SampleResistance=
[HotStage]
TargetTemperature=
ActualTemperature=
SampleBias=
ShieldBias=
[HotStageHVHS]
TargetTemperature=
ActualTemperature=
SampleBias=
ShieldBias=
[ColdStage]
TargetTemperature=
ActualTemperature=
Humidity=
SampleBias=

""";

        var metadata = MetadataIniSerializer.Deserialize(testData);
        Assert.NotNull(metadata);
    }
}
