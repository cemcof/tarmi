using System.Net;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Alignments;
using CFLMnavi.Configuration.Application;
using CFLMnavi.Configuration.Devices;
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
                LuminescenceFilterSwitchDelay = Duration.FromSeconds(3)
            },
            Microscope = CreateMicroscope(),
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
            }
        };
    }
}
