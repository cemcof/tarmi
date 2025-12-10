using System.Net;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Tarmi.Configuration.Application;
using Tarmi.Configuration.Devices;
using Tarmi.Models;
using UnitsNet;

internal static class ProdConfigGenerator
{
    public static void Generate(string filename = "config.prod.xml")
    {
        var config = new ApplicationConfig()
        {
            Simulation = new Simulation()
            {
                Enabled = false
            },
            UserPreferences = new UserPreferences()
            {
                ImageColoring = CreateDefaultImageColoring(),
                Algorithms = CreateDefaultAlgorithms(),
                ProjectsDirectory = """d:\Betrian\Projects""",
                LinearStageFocus = CreateDefaultLinearStageFocus(),
                LuminescenceAberrations = CreateDefaultAberrationSettings(),
                LuminescenceFilterSwitchDelay = Duration.FromSeconds(3),
                ConfocalAberrations = CreateDefaultConfocalAberrationSettings(),
                ConfocalFilterSwitchDelay = Duration.FromSeconds(3),
                ConfocalSettings = CreateDefaultConfocalSettings(),
                PythonConfig = CreateDefaultPythonConfiguration(),
            },
            Microscope = CreateMicroscope(),
            Features = new Features()
            {
                EnableConfocalMode = false,
            }
        };

        ConfigSerialization.SaveApplicationConfig(config, filename);
    }

    public static LuminescenceAberrations CreateDefaultAberrationSettings()
    {
        return new LuminescenceAberrations()
        {
            FluorescenceAberrations = new()
            {
                { "Red", Length.FromMicrometers(0) },
                { "Green", Length.FromMicrometers(0) },
                { "Blue", Length.FromMicrometers(0) },
                { "UltraViolet", Length.FromMicrometers(0) },
            },
            ReflectionAberrations = new()
            {
                { "Red", Length.FromMicrometers(0) },
                { "Green", Length.FromMicrometers(0) },
                { "Blue", Length.FromMicrometers(0) },
                { "UltraViolet", Length.FromMicrometers(0) },
            }
        };
    }

    public static LuminescenceAberrations CreateDefaultConfocalAberrationSettings()
    {
        return new LuminescenceAberrations()
        {
            FluorescenceAberrations = new()
            {
                { "COLOR1", Length.FromMicrometers(0) },
                { "COLOR2", Length.FromMicrometers(0) },
                { "COLOR3", Length.FromMicrometers(0) },
                { "COLOR4", Length.FromMicrometers(0) },
            },
            ReflectionAberrations = new()
            {
                { "COLOR1", Length.FromMicrometers(0) },
                { "COLOR2", Length.FromMicrometers(0) },
                { "COLOR3", Length.FromMicrometers(0) },
                { "COLOR4", Length.FromMicrometers(0) },
            }
        };
    }

    public static LinearStageFocus CreateDefaultLinearStageFocus()
    {
        return new LinearStageFocus
        {
            FocusSteps = [
                Length.FromMicrometers(0.25),
                Length.FromMicrometers(0.5),
                Length.FromMicrometers(1),
                Length.FromMicrometers(1.5),
                Length.FromMicrometers(2.5),
                Length.FromMicrometers(5),
                Length.FromMicrometers(10),
                Length.FromMicrometers(25),
                Length.FromMicrometers(50),
                Length.FromMicrometers(100),
                Length.FromMicrometers(200)
            ]
        };
    }

    public static ConfocalSettings CreateDefaultConfocalSettings()
    {
        return new ConfocalSettings
        {
            PixelSizes = [
                Length.FromNanometers(25),
                Length.FromNanometers(30),
                Length.FromNanometers(35),
                Length.FromNanometers(40),
                Length.FromNanometers(45),
                Length.FromNanometers(50),
                Length.FromNanometers(60),
                Length.FromNanometers(70),
                Length.FromNanometers(80),
                Length.FromNanometers(90),
                Length.FromNanometers(100),
                Length.FromNanometers(125),
                Length.FromNanometers(200),
                Length.FromNanometers(300),
                Length.FromNanometers(1000),
                Length.FromNanometers(2000),
            ],
            ADCRanges = [
                ElectricPotential.FromVolts(0.1),
                ElectricPotential.FromVolts(0.2),
                ElectricPotential.FromVolts(0.5),
                ElectricPotential.FromVolts(1.0),
            ],
            DwellRanges = [
                Duration.FromNanoseconds(0.1),
                Duration.FromNanoseconds(0.2),
                Duration.FromNanoseconds(0.3)
            ],
            GainRanges = new RangeDescriptor<Level>() { Min = Level.Zero, Max = Level.FromDecibels(48) },
        };
    }

    public static PythonConfig CreateDefaultPythonConfiguration()
    {
        return new PythonConfig
        {
            ExecutablePath = @"C:\Program Files\Python\Python313\python.exe",
            ScriptPath = "confocalScript.py",
            ScriptTuningPath = "confocalScriptTuning.py",
        };
    }

    public static Algorithms CreateDefaultAlgorithms()
    {
        return new()
        {
            TiltingFunctions = new()
            {
                ManualTiltStep = Angle.FromDegrees(0.1),
            },
            FocusFunctions = new()
            {
                CenterFocusAreaSize = Ratio.FromPercent(25),
                FocusRangeRatio = Ratio.FromPercent(1),
                FIBFocusRange = Length.FromMillimeters(5),
                SEMFocusRange = Length.FromMillimeters(3),
                LMFocusRange = Length.FromMicrometers(100)
            },
            TileSetPreferences = new()
            {
                FixedHfwImageOverlap = Ratio.FromPercent(5),
                VariableHfwImageOverlap = Ratio.FromPercent(10),
            },
            AutoEqualize = new()
            {
                Min = Ratio.FromPercent(2),
                Max = Ratio.FromPercent(0.17),
            }
        };
    }

    public static ImageColoring CreateDefaultImageColoring()
    {
        return new()
        {
            Fluorescence = new()
            {
                LightMappings = [
                    new() { WaveLength = Length.FromNanometers(625), Color = new() { Red = 127, Green = 0, Blue = 127 } },
                    new() { WaveLength = Length.FromNanometers(565), Color = new() { Red = 127, Green = 0, Blue = 0 } },
                    new() { WaveLength = Length.FromNanometers(470), Color = new() { Red = 0, Green = 127, Blue = 0 } },
                    new() { WaveLength = Length.FromNanometers(385), Color = new() { Red = 0, Green = 0, Blue = 127 } }
                ]
            },
            Reflection = new()
            {
                LightMappings = []
            }
        };
    }

    public static PinHoleWheelAlignments CreateDefaultPinHoleWheelAlignments()
    {
        return new()
        {
            PinHoleAlignments =
            [
                new() { PinHoleSize = Length.FromNanometers(25), Alignment = Length.FromNanometers(6732) },
                new() { PinHoleSize = Length.FromNanometers(30), Alignment = Length.FromNanometers(7232) },
                new() { PinHoleSize = Length.FromNanometers(35), Alignment = Length.FromNanometers(7732) },
                new() { PinHoleSize = Length.FromNanometers(40), Alignment = Length.FromNanometers(8232) },
                new() { PinHoleSize = Length.FromNanometers(45), Alignment = Length.FromNanometers(9232) },
                new() { PinHoleSize = Length.FromNanometers(50), Alignment = Length.FromNanometers(9732) },
                new() { PinHoleSize = Length.FromNanometers(60), Alignment = Length.FromNanometers(232) },
                new() { PinHoleSize = Length.FromNanometers(70), Alignment = Length.FromNanometers(732) },
                new() { PinHoleSize = Length.FromNanometers(80), Alignment = Length.FromNanometers(1732) },
                new() { PinHoleSize = Length.FromNanometers(90), Alignment = Length.FromNanometers(2232) },
                new() { PinHoleSize = Length.FromNanometers(100), Alignment = Length.FromNanometers(2732) },
                new() { PinHoleSize = Length.FromNanometers(125), Alignment = Length.FromNanometers(3232) },
                new() { PinHoleSize = Length.FromNanometers(200), Alignment = Length.FromNanometers(4232) },
                new() { PinHoleSize = Length.FromNanometers(300), Alignment = Length.FromNanometers(4732) },
                new() { PinHoleSize = Length.FromMillimeters(1), Alignment = Length.FromNanometers(5232) },
                new() { PinHoleSize = Length.FromMillimeters(2), Alignment = Length.FromNanometers(5732) },
            ]
        };
    }

    public static EmissionFilters CreateDefaultEmissionFilters()
    {
        return new()
        {
            Filter1 = new EmissionFilter { FilterColor = Length.FromNanometers(460), LaserColor = Length.FromNanometers(405) },
            Filter2 = new EmissionFilter { FilterColor = Length.FromNanometers(535), LaserColor = Length.FromNanometers(488) },
            Filter3 = new EmissionFilter { FilterColor = Length.FromNanometers(600), LaserColor = Length.FromNanometers(561) },
            Filter4 = new EmissionFilter { FilterColor = Length.FromNanometers(705), LaserColor = Length.FromNanometers(640) }
        };
    }

    public static ConfocalLights CreateDefaultConfocalLights()
    {
        return new()
        {
            ConfocalLight1 = new ConfocalLight { Wavelength = Length.FromNanometers(405), LightColor = ConfocalLightColor.COLOR1 },
            ConfocalLight2 = new ConfocalLight { Wavelength = Length.FromNanometers(488), LightColor = ConfocalLightColor.COLOR2 },
            ConfocalLight3 = new ConfocalLight { Wavelength = Length.FromNanometers(561), LightColor = ConfocalLightColor.COLOR3 },
            ConfocalLight4 = new ConfocalLight { Wavelength = Length.FromNanometers(640), LightColor = ConfocalLightColor.COLOR4 }
        };
    }

    public static InstrumentAlignment CreateDefaultInstrumentAlignment()
    {
        return new()
        {
            Sem = new()
            {
                OffsetX = Length.FromMeters(0),
                OffsetY = Length.FromMeters(0),
                OffsetRotation = Angle.FromDegrees(180),
                // Hydra 2
                // OffsetRotation = Angle.FromDegrees(-70),
                OffsetTilt = Angle.FromDegrees(0)
            },
            FibMilling = new()
            {
                OffsetX = Length.FromMillimeters(-0.003),
                OffsetY = Length.FromMeters(0),
                OffsetRotation = Angle.FromDegrees(180),
                // Hydra 2
                // OffsetRotation = Angle.FromDegrees(-70),
                OffsetTilt = Angle.FromDegrees(0)
            },
            Lm = new()
            {
                OffsetX = Length.FromMillimeters(51.880),
                OffsetY = Length.FromMeters(0),
                OffsetRotation = Angle.FromDegrees(0),
                // Hydra 2
                // OffsetRotation = Angle.FromDegrees(110),
                OffsetTilt = Angle.FromDegrees(0.4),
            },
            FibRightAngle = new()
            {
                OffsetX = Length.FromMillimeters(0.088),
                OffsetY = Length.FromMeters(0),
                OffsetRotation = Angle.FromDegrees(0),
                // OffsetRotation = Angle.FromDegrees(110),
                OffsetTilt = Angle.FromDegrees(0)
            },
            Confocal = new()
            {
                OffsetX = Length.FromMillimeters(51.880),
                OffsetY = Length.FromMeters(0),
                OffsetRotation = Angle.FromDegrees(0),
                // Hydra 2
                // OffsetRotation = Angle.FromDegrees(110),
                OffsetTilt = Angle.FromDegrees(0.4),
            },
            LinearStage = new()
            {
                Acceleration = Acceleration.FromNanometersPerSecondSquared(1e7),
                HighVelocity = Speed.FromNanometersPerSecond(4e6),
                LowVelocity = Speed.FromNanometersPerSecond(4e5),
                PositionTolerance = Length.FromMicrometers(2),

                RetractPosition = Length.FromNanometers(2_510),
                ProtractPosition = Length.FromMicrometers(10_000),

                // refl green z - 10820um

                // 50um +-
                FocusMinimum = Length.FromMicrometers(9_800),
                FocusMaximum = Length.FromMicrometers(11_250),

                // Currently used as the epsilon in lm autofocus.
                FocusStep = Length.FromNanometers(150)
            },
            FilterHandler = new()
            {
                ReflectionFilterPosition = 15,
                FluorescenceFilterPosition = 119,
            }
        };
    }

    private static Microscope CreateMicroscope()
    {
        return new Microscope()
        {
            Alignment = CreateDefaultInstrumentAlignment(),
            FilterHandler = new()
            {
                Port = new() { DeviceName = "COM15", BaudRate = 9600 }
            },
            ThermofisherInstrument = new()
            {
                SemQuad = 1,
                IonQuad = 2,
            },
            BaslerCamera = new()
            {
                CameraName = "Basler acA3088-57um (23713667)",
                Width = 3088,
                Height = 2064,
                FieldWidth = Length.FromNanometers(105.5 * 3088),
                FieldHeight = Length.FromNanometers(105.5 * 2064),
                FlipImageOnX = true,
                FlipImageOnY = false
            },
            Thorlabs4100 = new()
            {
                Lights = new(),
                Port = new() { DeviceName = "COM4", BaudRate = 115200 }
            },
            LinearStage = new()
            {
                IPAddress = IPAddress.Parse("198.211.143.62").ToString(),
                Port = 55551,
                Timeout = Duration.FromSeconds(5),
                Channel = 2
            },
            ThorlabsPinHoleWheel = new()
            {
                Port = new() { DeviceName = "COM2", BaudRate = 9600 },
                PinHoleWheelAlignments = CreateDefaultPinHoleWheelAlignments()
            },
            ThorlabsFilterWheel = new()
            {
                Port = new() { DeviceName = "COM3", BaudRate = 9600 },
                EmissionFilters = CreateDefaultEmissionFilters()
            },
            ConfocalConfig = new()
            {
                ConfocalCamera = new()
                {
                    CameraName = "Name",
                    FieldWidth = Length.FromNanometers(80 * 4096),
                    FieldHeight = Length.FromNanometers(80 * 4096),
                    Width = 4096,
                    Height = 4096
                },
                ConfocalLights = CreateDefaultConfocalLights()
            }
        };
    }
}
