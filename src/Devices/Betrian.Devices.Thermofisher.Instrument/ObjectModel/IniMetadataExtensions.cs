using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Betrian.Imaging.Common.Metadata.Thermofisher.IniFormat;
using Fei.XT.Instrument.gen;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel;

internal static class IniMetadataExtensions
{
    public static Metadata ToIniMetadata(this Memento memento, IXtObjectsCollection xtObjectsCollection)
    {
        var beamType =
            memento.Get<string>("View.ActiveBeam")!
                .Equals("Electron", StringComparison.OrdinalIgnoreCase) ? "EBeam" : "IBeam";

        var metadata = new Metadata
        {
            User = memento.CreateUserSection(),
            System = memento.CreateSystemSection(beamType),
            Beam = memento.CreateBeamSection(beamType),
        };

        if (beamType == "EBeam")
        {
            metadata = metadata with
            {
                EBeam = memento.CreateNamedBeamSection(beamType),
                EScan = memento.CreateNamedScanSection(beamType)
            };
        }
        else
        {
            metadata = metadata with
            {
                IBeam = memento.CreateNamedBeamSection(beamType),
                IScan = memento.CreateNamedScanSection(beamType)
            };
        }

        return metadata;
    }

    private static UserSection CreateUserSection(this Memento memento)
    {
        return new UserSection
        {
            Date = DateOnly.FromDateTime(memento.Get<DateTime>("View.Date")),
            Time = TimeOnly.FromDateTime(memento.Get<DateTime>("View.Date")),
            User = memento.Get<string>("View.UserName") ?? Environment.UserName
        };
    }

    private static SystemSection CreateSystemSection(this Memento memento, string beamType)
    {
        return new SystemSection
        {
            Type = memento.Get<string>("System.MachineClassification")!,
            Dnumber = memento.Get<string>("System.DNumber")!,
            Software = new Version(memento.Get<string>("System.InstrumentServerVersion")!),
            BuildNr = (ushort)memento.Get<int>("System.BuildNumber"),
            Source = memento.Get<string>($"{beamType}.Source")!,
            Column = memento.Get<string>($"{beamType}.CondensorOptics")!,
            FinalLens = memento.Get<string>($"{beamType}.FinalLens")!,
            Chamber = memento.Get<string>("Chamber.Type")!,
            Stage = memento.Get<string>("stage.Type")!,
            Pump = memento.Get<string>("Chamber.PumpType")!,
            ESEM = memento.Get<bool>("Chamber.ESEMAvailable") ? "yes" : "no",
            Aperture = memento.Get<string>($"{beamType}.ApertureType")!,
            Scan = memento.Get<string>($"{beamType}.ScanDevice")!,
            Acq = memento.Get<string>($"{beamType}.AcquisitionDevice")!,
            EucWD = memento.Get<double>($"{beamType}.EucWorkingDistance"),
            SystemType = memento.Get<string>("System.Type")!,
            DisplayWidth = memento.Get<double>("System.DisplayWidth"),
            DisplayHeight = memento.Get<double>("System.DisplayHeight")
        };
    }

    private static BeamSection CreateBeamSection(this Memento memento, string beamType)
    {
        return new BeamSection
        {
            HV = memento.Get<double>($"{beamType}.HighVoltage"),
            Spot = (int)memento.Get<double>($"{beamType}.SpotSize"),
            StigmatorX = memento.Get<double>($"{beamType}.Stigmator.x"),
            StigmatorY = memento.Get<double>($"{beamType}.Stigmator.y"),
            BeamShiftX = memento.Get<double>($"{beamType}.Shift.x"),
            BeamShiftY = memento.Get<double>($"{beamType}.Shift.y"),
            ScanRotation = memento.Get<double>($"{beamType}.ScanRotation"),
            ImageMode = "Normal",
            Beam = beamType,
            Scan = beamType == "EBeam" ? "EScan" : "IScan"
        };
    }

    private static NamedBeamSection CreateNamedBeamSection(this Memento memento, string beamType)
    {
        return new NamedBeamSection
        {
            Source = memento.Get<string>($"{beamType}.Source")!,
            ColumnType = memento.Get<string>($"{beamType}.CondensorOptics")!,
            FinalLens = memento.Get<string>($"{beamType}.FinalLens")!,
            Acq = memento.Get<string>($"{beamType}.AcquisitionDevice")!,
            Aperture = memento.Get<string>($"{beamType}.ApertureType")!,
            HV = memento.Get<double>($"{beamType}.HighVoltage"),
            HFW = memento.Get<double>($"{beamType}.ScanField.Width"),
            VFW = memento.Get<double>($"{beamType}.ScanField.Height"),
            WD = memento.Get<double>($"{beamType}.WorkingDistance"),
            BeamCurrent = memento.Get<double>($"{beamType}.Current"),
            TiltCorrectionIsOn = memento.Get<bool>($"{beamType}.TiltCorrection"),
            DynamicFocusIsOn = memento.Get<bool>($"{beamType}.DynamicFocus"),
            DynamicWDIsOn = memento.Get<bool>($"{beamType}.DynamicWD"),
            ScanRotation = memento.Get<double>($"{beamType}.ScanRotation"),
            LensMode = memento.Get<string>($"{beamType}.LensMode")!,
            LensModeA = memento.Get<string>($"{beamType}.LensModeA") ?? string.Empty,
            ATubeVoltage = memento.Get<double>($"{beamType}.ATubeVoltage") == 0 ? null : memento.Get<double>($"{beamType}.ATubeVoltage"),
            UseCase = memento.Get<string>($"{beamType}.UseCase") ?? string.Empty,
            SemOpticalMode = memento.Get<string>($"{beamType}.SemOpticalMode") ?? string.Empty,
            ImageMode = "Normal",
            SourceTiltX = memento.Get<double>($"{beamType}.SourceTilt.x"),
            SourceTiltY = memento.Get<double>($"{beamType}.SourceTilt.y"),
            StageX = memento.Get<double>("Stage.Position.x"),
            StageY = memento.Get<double>("Stage.Position.y"),
            StageZ = memento.Get<double>("Stage.Position.z"),
            StageR = memento.Get<double>("Stage.Rotation"),
            StageTa = memento.Get<double>("Stage.Tilt"),
            StageTb = memento.Get<double>("Stage.BetaTilt"),
            StigmatorX = memento.Get<double>($"{beamType}.Stigmator.x"),
            StigmatorY = memento.Get<double>($"{beamType}.Stigmator.y"),
            BeamShiftX = memento.Get<double>($"{beamType}.Shift.x"),
            BeamShiftY = memento.Get<double>($"{beamType}.Shift.y"),
            EucWD = memento.Get<double>($"{beamType}.EucWorkingDistance"),
            EmissionCurrent = memento.Get<double>($"{beamType}.EmissionCurrent"),
            TiltCorrectionAngle = memento.Get<double>($"{beamType}.TiltCorrectionAngle"),
            PreTilt = memento.Get<double>($"{beamType}.PreTilt"),
            WehneltBias = memento.Get<string>($"{beamType}.WehneltBias") ?? string.Empty,
            BeamMode = memento.Get<string>($"{beamType}.BeamMode")!,
            MagnificationCorrection = memento.Get<bool>($"{beamType}.MagnificationCorrection"),
            AngularFieldWidth = memento.Get<double>($"{beamType}.AngularFieldWidth"),
            AngularPixelWidth = memento.Get<double>($"{beamType}.AngularPixelWidth"),
            ElectronChannelingPatternIsOn = memento.Get<bool>($"{beamType}.ElectronChannelingPatternIsOn"),
            MagnificationSinglePointCorrectionX = memento.Get<double>($"{beamType}.MagnificationSinglePointCorrection.x"),
            MagnificationSinglePointCorrectionY = memento.Get<double>($"{beamType}.MagnificationSinglePointCorrection.y"),
            OrthogonalitySinglePointCorrection = memento.Get<double>($"{beamType}.OrthogonalitySinglePointCorrection"),
            ScanRotationSinglePointCorrection = memento.Get<double>($"{beamType}.ScanRotationSinglePointCorrection"),
            MagnificationSinglePointCorrectionIsOn = memento.Get<bool>($"{beamType}.MagnificationSinglePointCorrectionIsOn")
        };
    }

    private static GisSection CreateGisSection(this Memento memento)
    {
        return new GisSection
        {
            Number = memento.Get<int>("GIS.Number")
        };
    }

    private static ScanSection CreateScanSection(this Memento memento, string beamType)
    {
        var filterType = memento.Get<string>("View.FilterType") ?? "Live";
        var filterCount = memento.Get<int>("View.FilterCount");
        if (filterCount == 0)
        {
            filterCount = 1;
        }

        var dwellTime = memento.Get<double>($"{beamType}.DwellTime");
        var width = memento.Get<double>($"{beamType}.PixelSize.Width");
        var height = memento.Get<double>($"{beamType}.PixelSize.Height");

        return new ScanSection
        {
            InternalScan = true,
            Dwelltime = dwellTime,
            PixelWidth = width,
            PixelHeight = height,
            HorFieldsize = memento.Get<double>($"{beamType}.ScanField.Width"),
            VerFieldsize = memento.Get<double>($"{beamType}.ScanField.Height"),
            Average = filterType == "Average" ? filterCount : 0,
            Integrate = filterType == "Integrate" ? filterCount : 0,
            FrameTime = width * height * dwellTime * filterCount
        };
    }

    private static NamedScanSection CreateNamedScanSection(this Memento memento, string beamType)
    {
        return new NamedScanSection
        {
            Scan = memento.Get<string>($"{beamType}.AcquisitionDevice")!,
            InternalScan = true,
            Dwell = memento.Get<double>($"{beamType}.DwellTime"),
            PixelWidth = memento.Get<double>($"{beamType}.PixelSize.Width"),
            PixelHeight = memento.Get<double>($"{beamType}.PixelSize.Height"),
            HorFieldsize = memento.Get<double>($"{beamType}.ScanField.Width"),
            VerFieldsize = memento.Get<double>($"{beamType}.ScanField.Height"),
            FrameTime = memento.Get<double>($"{beamType}.FrameTime"),
            LineTime = memento.Get<double>($"{beamType}.LineTime"),
            Mainslock = memento.Get<bool>($"{beamType}.MainsLock"),
            LineIntegration = memento.Get<int>($"{beamType}.LineIntegration"),
            ScanInterlacing = memento.Get<int>($"{beamType}.ScanInterlacing"),
        };
    }

    private static StageSection CreateStageSection(this Memento memento, string beamType)
    {
        return new StageSection
        {
            ActiveStage = memento.Get<string>("stage.ActiveStage")!,
            StageX = memento.Get<double>("stage.Position.x"),
            StageY = memento.Get<double>("stage.Position.y"),
            StageZ = memento.Get<double>("stage.Position.z"),
            StageR = memento.Get<double>("stage.Position.Rotation"),
            StageT = memento.Get<double>("stage.Position.Tilt"),
            StageTb = memento.Get<double>("stage.Position.BetaTilt"),
            StageRawX = memento.Get<double>("stage.PositionRaw.x"),
            StageRawY = memento.Get<double>("stage.PositionRaw.y"),
            StageRawZ = memento.Get<double>("stage.PositionRaw.z"),
            StageRawR = memento.Get<double>("stage.PositionRaw.Rotation"),
            StageRawT = memento.Get<double>("stage.PositionRaw.Tilt"),
            StageRawTb = memento.Get<double>("stage.PositionRaw.BetaTilt"),
            SpecTilt = 0,
            WorkingDistance = memento.Get<double>("stage.Position.z")
        };
    }

    //[Image]
    //DigitalContrast=1
    //DigitalBrightness=0
    //DigitalGamma=1
    //Average=0
    //Integrate=0
    //ResolutionX=3072
    //ResolutionY=2048
    //DriftCorrected=Off
    //ZoomFactor=1.0
    //ZoomPanX=
    //ZoomPanY=
    //MagCanvasRealWidth=
    //MagnificationMode=
    //ScreenMagCanvasRealWidth=
    //ScreenMagnificationMode=
    //PostProcessing=
    //Transformation=

    //[Vacuum]
    //ChPressure=0.000289
    //Gas=
    //UserMode=High vacuum
    //Humidity=

    //[Specimen]
    //Temperature=
    //SpecimenCurrent=-2.11707e-10
    //CryoShieldTemperature=
    //CryoStageTemperature=

    //[Detectors]
    //Number=1
    //Name=TLD
    //Mode=BSE

    //[TLD]
    //Contrast=66
    //Brightness=38.4391
    //Signal=BSE
    //ContrastDB=30.0887
    //BrightnessDB=-2.77475
    //SuctionTube=-244.8
    //Mirror=0
    //MinimumDwellTime=1e-07

    //[Accessories]
    //Number=0

    //[PrivateFei]
    //BitShift=0
    //DataBarSelected=
    //DataBarAvailable=
    //TimeOfCreation=09.10.2023 20:04:59
    //DatabarHeight=

    //[HiResIllumination]
    //BrightFieldIsOn=
    //BrightFieldValue=
    //DarkFieldIsOn=
    //DarkFieldValue=
    //[EasyLift]
    //Rotation=0
    //[HotStageMEMS]
    //HeatingCurrent=
    //HeatingVoltage=
    //TargetTemperature=
    //ActualTemperature=
    //HeatingPower=
    //SampleBias=
    //SampleResistance=
    //[HotStage]
    //TargetTemperature=
    //ActualTemperature=
    //SampleBias=
    //ShieldBias=
    //[HotStageHVHS]
    //TargetTemperature=
    //ActualTemperature=
    //SampleBias=
    //ShieldBias=
    //[ColdStage]
    //TargetTemperature=
    //ActualTemperature=
    //Humidity=
    //SampleBias=

}
