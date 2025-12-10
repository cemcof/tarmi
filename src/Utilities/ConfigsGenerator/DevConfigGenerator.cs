using System.Net;
using Tarmi.Configuration;
using Tarmi.Configuration.Application;
using Tarmi.Configuration.Devices;
using UnitsNet;

namespace ConfigsGenerator;

internal static class DevConfigGenerator
{
    public static void Generate(string filename = "config.dev.xml")
    {
        var config = new ApplicationConfig()
        {
            Simulation = new Simulation()
            {
                Enabled = true
            },
            UserPreferences = new UserPreferences()
            {
                ImageColoring = ProdConfigGenerator.CreateDefaultImageColoring(),
                Algorithms = ProdConfigGenerator.CreateDefaultAlgorithms(),
                ProjectsDirectory = """c:\ProgramData\Betrian\CFLMnavi""",
                LinearStageFocus = ProdConfigGenerator.CreateDefaultLinearStageFocus(),
                LuminescenceAberrations = ProdConfigGenerator.CreateDefaultAberrationSettings(),
                LuminescenceFilterSwitchDelay = Duration.FromSeconds(3),
                ConfocalAberrations = ProdConfigGenerator.CreateDefaultConfocalAberrationSettings(),
                ConfocalFilterSwitchDelay = Duration.FromSeconds(3),
                ConfocalSettings = ProdConfigGenerator.CreateDefaultConfocalSettings(),
                PythonConfig = ProdConfigGenerator.CreateDefaultPythonConfiguration(),
            },
            Microscope = CreateMicroscope(),
            Features = new Features()
            {
                EnableConfocalMode = false,
            }
        };

        ConfigSerialization.SaveApplicationConfig(config, filename);
    }

    private static Microscope CreateMicroscope()
    {
        return new Microscope()
        {
            Alignment = ProdConfigGenerator.CreateDefaultInstrumentAlignment(),
            FilterHandler = new()
            {
                Port = new() { DeviceName = "COM1", BaudRate = 115200 }
            },
            ThermofisherInstrument = new()
            {
                SemQuad = 1,
                IonQuad = 2,
            },
            BaslerCamera = new()
            {
                CameraName = "Basler Emulation (0815-0000)",
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
                PinHoleWheelAlignments = ProdConfigGenerator.CreateDefaultPinHoleWheelAlignments()
            },
            ThorlabsFilterWheel = new()
            {
                Port = new() { DeviceName = "COM3", BaudRate = 9600 },
                EmissionFilters = ProdConfigGenerator.CreateDefaultEmissionFilters()
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
                ConfocalLights = ProdConfigGenerator.CreateDefaultConfocalLights()
            }
        };
    }
}
