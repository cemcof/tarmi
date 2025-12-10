using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Tarmi.Imaging.Common.Metadata.Thermofisher.XmlFormat;
using Fei.XT.Instrument.gen;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;
internal static class XmlMetadataExtensions
{
    private static readonly Guid CoreGuid = new("e2725362-40be-4551-ad2f-81be4131f500");

    public static Metadata ToXmlMetadata(this Memento memento, IXtObjectsCollection xtObjectsCollection)
    {
        var beamType =
            memento.Get<string>("View.ActiveBeam")!.Equals("Electron", StringComparison.OrdinalIgnoreCase) ?
                BeamType.Electron :
                BeamType.Ion;
        var isEBeam = beamType == BeamType.Electron;

        return new Metadata
        {
            Core = memento.CreateCoreSection(),
            Instrument = memento.CreateInstrumentSection(),
            Acquisition = memento.CreateAcquisitionSection(beamType, isEBeam),
            Optics = memento.CreateOpticsSection(isEBeam),
            StageSettings = memento.CreateStageSettingsSection(),
            ScanSettings = memento.CreateScanSettingsSection(isEBeam),
            VacuumProperties = memento.CreateVacuumPropertiesSection(),
            Detectors = memento.CreateDetectorsSection(xtObjectsCollection),
            GasInjectionSystems = [], // TODO: implement
            BinaryResult = memento.CreateBinaryResultSection(isEBeam),
            CustomProperties = [], // TODO: implement
        };
    }

    private static Core CreateCoreSection(this Memento memento)
    {
        return new Core
        {
            ApplicationSoftware = "xT",
            ApplicationSoftwareVersion = memento.Get<string>("System.InstrumentServerVersion")!,
            UserId = memento.Get<string>("View.UserName") ?? Environment.UserName,
            Guid = CoreGuid,
        };
    }

    private static Imaging.Common.Metadata.Thermofisher.XmlFormat.Instrument CreateInstrumentSection(this Memento memento)
    {
        return new Imaging.Common.Metadata.Thermofisher.XmlFormat.Instrument
        {
            ControlSoftwareVersion = memento.Get<string>("System.InstrumentServerVersion"),
            Manufacturer = "FEI Company",
            InstrumentClass = memento.Get<string>("System.Type")!,
            InstrumentID = memento.Get<string>("System.DNumber")!,
            ComputerName = Environment.MachineName,
        };
    }

    private static Acquisition CreateAcquisitionSection(this Memento memento, BeamType beamType, bool isEBeam)
    {
        var beam = isEBeam ? "EBeam" : "IBeam";
        return new Acquisition
        {
            AcquisitionDatetime = memento.Get<DateTime>("View.Date"),
            BeamType = beamType,
            ColumnType = memento.Get<string>($"{beam}.CondensorOptics"),
            SourceType = isEBeam ? SourceType.FEG : SourceType.Xenon,
        };
    }

    private static List<Imaging.Common.Metadata.Thermofisher.XmlFormat.Aperture> CreateOpticsAperturesSection(
        this Memento memento,
        string beam
    )
    {
        return
        [
            new Imaging.Common.Metadata.Thermofisher.XmlFormat.Aperture
            {
                Number = memento.Get<int>($"{beam}.Aperture.Index"),
                Diameter = memento.Get<double>($"{beam}.Aperture.Diameter")
            }
        ];
    }

    private static Optics CreateOpticsSection(this Memento memento, bool isEBeam)
    {
        var beam = isEBeam ? "EBeam" : "IBeam";
        return new Optics
        {
            Apertures = memento.CreateOpticsAperturesSection(beam),
            GunTiltRaw = new PointD
            {
                // where to get?
                X = 0.0,
                Y = 0
            },
            AccelerationVoltage = memento.Get<double>($"{beam}.HighVoltage"),
            SpotSize = memento.Get<double>($"{beam}.SpotSize"),
            BeamCurrent = memento.Get<double>($"{beam}.Current"),
            FullScanFieldOfView = new PointD
            {
                X = memento.Get<double>($"{beam}.ScanField.Width"),
                Y = memento.Get<double>($"{beam}.ScanField.Height"),
            },
            ScanFieldOfView = new PointD
            {
                // reduce in case of reduced area, spot or line if needed
                X = memento.Get<double>($"{beam}.ScanField.Width"),
                Y = memento.Get<double>($"{beam}.ScanField.Height"),
            },
            WorkingDistance = memento.Get<double>($"{beam}.WorkingDistance"),
            EucentricWorkingDistance = memento.Get<double>($"{beam}.EucWorkingDistance"),
            BeamShift = new PointD
            {
                X = memento.Get<double>($"{beam}.Shift.x"),
                Y = memento.Get<double>($"{beam}.Shift.y"),
            },
            SampleTiltCorrectionOn = memento.Get<bool>($"{beam}.TiltCorrection"),
            SamplePreTiltAngle = memento.Get<double>($"{beam}.TiltCorrectionAngle"),
            StigmatorRaw = new PointD
            {
                X = memento.Get<double>($"{beam}.Stigmator.x"),
                Y = memento.Get<double>($"{beam}.Stigmator.y"),
            },
            OpticalMode = "Field-Free",
            CrossOverOn = memento.Get<bool>($"{beam}.CrossOver")
        };
    }

    private static StageSettings CreateStageSettingsSection(this Memento memento)
    {
        return new StageSettings
        {
            StagePosition = new Imaging.Common.Metadata.Thermofisher.XmlFormat.StagePosition
            {
                X = memento.Get<double>("stage.Position.x"),
                Y = memento.Get<double>("stage.Position.y"),
                Z = memento.Get<double>("stage.Position.z"),
                Rotation = memento.Get<double>("stage.Position.Rotation"),
                Tilt = new Tilt
                {
                    Alpha = memento.Get<double>("stage.Position.Tilt"),
                    Beta = memento.Get<double>("stage.Position.BetaTilt"),
                },
            }
        };
    }

    private static ScanSettings CreateScanSettingsSection(this Memento memento, bool isEBeam)
    {
        var beam = isEBeam ? "EBeam" : "IBeam";
        var width = memento.Get<int>($"{beam}.Resolution.x");
        var height = memento.Get<int>($"{beam}.Resolution.y");

        return new ScanSettings
        {
            DwellTime = memento.Get<double>($"{beam}.DwellTime"),
            ScanSize = new Size
            {
                Width = width,
                Height = height,
            },
            ScanArea = new Rectangle
            {
                // adjust for other mode than full frame if needed
                X = 0,
                Y = 0,
                Width = width,
                Height = height
            },
            MainsLockOn = memento.Get<bool>($"{beam}.MainsLock"),
            LineTime = memento.Get<double>($"{beam}.LineTime"),
            LineIntegrationCount = memento.Get<int>($"{beam}.LineIntegration"),
            LineInterlacing = memento.Get<int>($"{beam}.ScanInterlacing"),
            FrameTime = memento.Get<double>($"{beam}.FrameTime"),
            ScanRotation = memento.Get<double>($"{beam}.ScanRotation")
        };
    }

    private static VacuumProperties CreateVacuumPropertiesSection(this Memento memento)
    {
        return new VacuumProperties
        {
            SamplePressure = memento.Get<double>("Chamber.Pressure")
        };
    }

    //private static readonly string[] DetectorNames =
    //[
    //    "ABS", "BottomPixelatedDetector", "CBS", "CDEM", "CLD", "CRD", "DualBSD", "ECD", "ETD",
    //    "GAD", "GBSD", "GSED", "HiResOptical", "HiResOpticalLowMag", "ICE", "ILBSED", "ILSED",
    //    "InColumnBSD", "IR", "LFD", "LmABS", "LmABS2", "LmCBS", "LmCBS2", "LVD", "LVSED",
    //    "MirrorDetector", "Mix", "OM", "PMT", "QuadBSD", "SICD", "SingleBSD", "STEM", "STEM3",
    //    "TLD", "TrinityT1", "TrinityT2", "TrinityT3", "VsABS", "VsCBS", "VsGAD"
    //];

    private static double NormalizeDetectorValue(double value, double min, double max) => (value - min) / (max - min);

    private static ScanningDetector CreateDetectorSection(this Memento memento, string detectorName, double mixContribution, IXtObjectsCollection xtObjectsCollection)
    {
        // ETD, TLD, ICE are ok, otherwise crate names mappings
        // only applicable for scanning detectors here (our scenarios)
        //detectorName = detectorName;

        var detector = xtObjectsCollection.GetObject<IDetector>($"Instrument.Detectors.{detectorName}").Object;

        detector.Offset.GetLogicalLimits(out var brightnessMin, out var brightnessMax);
        detector.Gain.GetLogicalLimits(out var contrastMin, out var contrastMax);

        var brightnessNormalized =
            NormalizeDetectorValue(memento.Get<double>($"Detectors.{detectorName}.Brightness"), brightnessMin, brightnessMax);
        var contrastNormalized =
            NormalizeDetectorValue(memento.Get<double>($"Detectors.{detectorName}.Contrast"), contrastMin, contrastMax);

        var result = new ScanningDetector
        {
            DetectorName = detectorName,
            DetectorType = detectorName,
            Gain = memento.Get<double>($"Detectors.{detectorName}.Gain"),
            ContrastNormalized = contrastNormalized,
            BrightnessNormalized = brightnessNormalized,
            Offset = memento.Get<double>($"Detectors.{detectorName}.Offset"),
            MixContribution = mixContribution,
        };

        // special cases for our scenarios
        // ETD
        // TLD

        // TODO: add specific values for detectors

        return result;
    }

    private static DetectorCollection CreateDetectorsSection(this Memento memento, IXtObjectsCollection xtObjectsCollection)
    {
        DetectorCollection result = [];

        List<(string, double)> activeDetectorNames = [];
        var activeDetectorName = memento.Get<string>("View.Detector");
        if (activeDetectorName!.Equals("Mix", StringComparison.OrdinalIgnoreCase))
        {
            var mixDetectorName = memento.Get<string>("Detectors.Mix.Source1.Detector");
            if (mixDetectorName is not null)
            {
                var mixContribution = memento.Get<double>("Detectors.Mix.Source1.Ratio");
                activeDetectorNames.Add((mixDetectorName, mixContribution));
            }
            mixDetectorName = memento.Get<string>("Detectors.Mix.Source2.Detector");
            if (mixDetectorName is not null)
            {
                var mixContribution = memento.Get<double>("Detectors.Mix.Source2.Ratio");
                activeDetectorNames.Add((mixDetectorName, mixContribution));
            }
            mixDetectorName = memento.Get<string>("Detectors.Mix.Source3.Detector");
            if (mixDetectorName is not null)
            {
                var mixContribution = memento.Get<double>("Detectors.Mix.Source3.Ratio");
                activeDetectorNames.Add((mixDetectorName, mixContribution));
            }
        }
        else
        {
            activeDetectorNames.Add((activeDetectorName, 0.0));
        }

        foreach (var (detectorName, mixContribution) in activeDetectorNames)
        {
            result.Add(memento.CreateDetectorSection(detectorName, mixContribution, xtObjectsCollection));
        }

        return result;
    }

    private static BinaryResult CreateBinaryResultSection(this Memento memento, bool isEBeam)
    {
        var beam = isEBeam ? "EBeam" : "IBeam";

        return new BinaryResult
        {
            AcquisitionUnit = AcquisitionUnit.Pixel,
            CompositionType = CompositionType.Single,
            ImageSize = new Point<uint>
            {
                X = (uint)memento.Get<int>($"{beam}.Resolution.x"),
                Y = (uint)memento.Get<int>($"{beam}.Resolution.y"),
            },
            FilterType = memento.Get<string>("View.FilterType"),
            FilterFrameCount = memento.Get<int>("View.FilterCount"),
            PixelSize = new Point<Quantity>
            {
                X = new Quantity
                {
                    Value = memento.Get<double>($"{beam}.PixelSize.Width"),
                    Exponent = "1",
                    Unit = "m",
                },
                Y = new Quantity
                {
                    Value = memento.Get<double>($"{beam}.PixelSize.Height"),
                    Exponent = "1",
                    Unit = "m",
                }
            },
            IntensityScale = 1,
            IntensityOffset = 0,
            Gamma = memento.Get<double>("View.LUT.Gamma"),
            AcquisitionArea = new RatioRectangle
            {
                // adjust for other mode than full frame if needed
                X = 0,
                Y = 0,
                Width = 1,
                Height = 1,
            }
        };
    }
}
