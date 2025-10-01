using Betrian.Communication.Common.Serial;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Thermofisher.Instrument;
using DevicesTestApp.Commands.ArduinoFilterHandler;
using DevicesTestApp.Commands.BaslerCamera;
using DevicesTestApp.Commands.ThermofisherInstrument;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace DevicesTestApp;

public static class Program
{
    private static IConfiguration GetProgramConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        return builder.Build();
    }

    public static int Main(string[] args)
    {
        var configuration = GetProgramConfiguration();

        var services = new ServiceCollection();
        services
            .AddSerialCommunicationServices()
            .AddFilterHandlerServices()
            .AddBaslerServices()
            .AddThermofisherInstrumentServices(false)
            .AddLogging();

        var registrar = new DependencyInjectionRegistrar(services);

        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
#if DEBUG
            config
                .PropagateExceptions()
                .ValidateExamples();
#endif

            _ = config
                .SetApplicationName("DevicesTestApp");

            _ = config
                .AddBranch<BaslerCameraSettings>("baslercam", check =>
                {
                    check.SetDescription("Basler Camera Device Operations");
                    _ = check
                        .AddCommand<ListCamerasCommand>("ls")
                        .WithExample("baslercam", "ls");
                    _ = check
                        .AddCommand<GrabSingleImageCommand>("grabone")
                        .WithExample("baslercam", "grabone");
                    _ = check
                        .AddCommand<GrabContinuousImageCommand>("grab")
                        .WithExample("baslercam", "grab");
                });

            _ = config
                .AddBranch<ThermofisherInstrumentSettings>("tfs", check =>
                {
                    check.SetDescription("Thermofisher Instrument Operations");
                    _ = check
                        .AddCommand<ChamberInfoCommand>("chamber")
                        .WithExample("tfs", "chamber");
                    _ = check
                        .AddCommand<SemInfoCommand>("sem")
                        .WithExample("tfs", "sem");

                    _ = check
                        .AddCommand<GrabSemImageCommand>("grabsem")
                        .WithExample("tfs", "grabsem");

                    _ = check
                        .AddCommand<StageInfoCommand>("stage")
                        .WithExample("tfs", "stage");

                });

            _ = config.AddBranch("filter", check =>
            {
                check.SetDescription("Arduino Filter Handler Operations");
                _ = check
                    .AddCommand<CheckConnectionCommand>("check")
                    .WithExample("filter", "check");
                _ = check
                    .AddCommand<SelectFilterCommand>("select")
                    .WithExample("filter", "select", "fluorescence");
                _ = check
                    .AddCommand<GetFilterCommand>("get")
                    .WithExample("filter", "get");
            });
        });

        return app.Run(args);
    }
}
