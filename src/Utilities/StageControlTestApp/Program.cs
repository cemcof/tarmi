using System.Net;
using Betrian.Devices.SmarAct.Stage;
using Betrian.Devices.SmarAct.Stage.Implementation;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UnitsNet;

internal class Program
{
    public static bool IsSimulated => false;

    public static async Task Main(string[] args)
    {
        var serviceProvider = InitializeServiceProvider();
        var stage = serviceProvider.GetRequiredService<ILinearStage>();

        List<(string, Func<ILinearStage, Task<bool>>)> tests =
        [
            (
                "Device connection test",
                TestConnection
            ),
            (
                "Errors test",
                TestError
            ),
            (
                "State test",
                TestState
            ),
            (
                "Temperature test",
                TestTemperature
            ),
            (
                "Position test",
                TestPosition
            ),
            (
                "Protraction test",
                TestProtraction
            ),
            (
                "Retraction test",
                TestRetraction
            ),
            (
                "Movement test",
                TestRelativeMovement
            ),
        ];
        
        try
        {
            foreach (var (testName, testFunction) in tests)
            {
                WriteLineYellow($"{testName} started.");
                var result = await testFunction(stage);
                if (!result)
                {
                    WriteLineRed($"{testName} failed.");
                    WriteLineRed("Aborting all further tests.");
                    return;
                }
                WriteLineGreen($"{testName} passed successfully.");
            }

            WriteLineGreen("Tests finished.");
            WriteLineYellow("Retracting stage.");
            await stage.RetractAsync();
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
            WriteLineRed("Aborting all further tests.");
        }
    }

    #region Auxiliary methods

    private static ServiceProvider InitializeServiceProvider()
    {
        var endPoint = IPEndPoint.Parse("198.211.143.62:55551");
        var applicationConfiguration = new ApplicationConfig();
        applicationConfiguration = applicationConfiguration with
        {
            Microscope = applicationConfiguration.Microscope with
            {
                LinearStage = new LinearStageConfiguration()
                {
                    IPAddress = "198.211.143.62",
                    Port = 55551
                }
            }
        };

        var services = new ServiceCollection();
        
        _ = services
            .AddSingleton(applicationConfiguration)
            .AddLogging()
            .AddSingleton<IMcs2CommunicationFactory, Mcs2CommunicationFactory>()
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IMcs2CommunicationFactory>().CreateCommunication(applicationConfiguration))
            .AddSingleton<ILinearStage>(serviceProvider =>
                new LinearStage(
                    serviceProvider.GetRequiredService<IMcs2Communication>(),
                    serviceProvider.GetRequiredService<ILogger<LinearStage>>(),
                    2
                )
            );

        return services.BuildServiceProvider();
    }

    private static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        WriteLineYellow($"Waiting for {delay.Seconds} seconds...");
        return Task.Delay(delay, cancellationToken);
    }

    private static void WriteLineYellow(string line) => AnsiConsole.MarkupLineInterpolated($"[yellow]{line}[/]");
    private static void WriteLineGreen(string line) => AnsiConsole.MarkupLineInterpolated($"[green]{line}[/]");
    private static void WriteLineRed(string line) => AnsiConsole.MarkupLineInterpolated($"[red]{line}[/]");

    #endregion

    private static async Task<bool> TestTemperature(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var temperature = await stage.GetTemperatureAsync(cts.Token);
        WriteLineYellow($"Measured temperature of {temperature} °C");
        return true;
    }

    private static async Task<bool> TestState(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var states = await stage.GetStateAsync(cts.Token);
        WriteLineYellow("Stage is in the following state");
        foreach (var state in Enum.GetValues<ChannelState>())
        {
            WriteLineYellow($"{(states.HasFlag(state) ? string.Empty : "Not ")}{state}");
        }
        return true;
    }

    private static async Task<bool> TestError(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var errorCount = await stage.GetErrorsCountAsync(cts.Token);
        WriteLineYellow($"Found {errorCount} error(s).");
        for (int i = 0; i < errorCount; i++)
        {
            var error = stage.GetErrorAsync(cts.Token);
            WriteLineYellow($"{i + 1}. {error}");
        }
        return true;
    }

    private static async Task<bool> TestConnection(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await stage.IsConnectedAsync(cts.Token);
        if (!result)
        {
            WriteLineRed("Connection check failed.");
        }
        return result;
    }
    
    private static async Task<bool> TestPosition(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var position = await stage.GetPositionAsync(cts.Token);
        WriteLineYellow($"Stage found at position {position.Micrometers} μm from origin.");
        var result = position >= Length.Zero;
        if (!result)
        {
            WriteLineRed("Expected non-negative position.");
        }
        return result;
    }

    private static async Task<bool> TestProtraction(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        WriteLineYellow("Attempting to protract stage.");
        await stage.ProtractAsync(cts.Token);
        await Delay(TimeSpan.FromSeconds(20), cts.Token);
        var position = await stage.GetPositionAsync(cts.Token);
        WriteLineYellow($"Stage is at position {position.Micrometers} μm from origin after protract.");
        return true;
    }

    private static async Task<bool> TestRetraction(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        WriteLineYellow("Attempting to retract stage.");
        await stage.RetractAsync(cts.Token);
        await Delay(TimeSpan.FromSeconds(20), cts.Token);
        var position = await stage.GetPositionAsync(cts.Token);
        WriteLineYellow($"Stage is at position {position.Micrometers} μm from origin after retract.");
        return true;
    }

    private static async Task<bool> TestRelativeMovement(ILinearStage stage)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var originalPosition = await stage.GetPositionAsync(cts.Token);
        WriteLineYellow($"Stage found at position {originalPosition.Micrometers} μm from origin.");
        var distance = Length.FromMicrometers(5000);
        WriteLineYellow($"Moving 5000 μm from current position.");
        await stage.MoveRelativeAsync(distance, cts.Token);
        await Delay(TimeSpan.FromSeconds(5), cts.Token);
        var newPosition = await stage.GetPositionAsync(cts.Token);
        WriteLineYellow($"Stage is at position {newPosition.Micrometers} μm from origin after move.");
        return true;
    }
}
