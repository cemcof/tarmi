using Betrian.Communication.Common.Serial;
using Betrian.Devices.Thorlabs.Light;
using CFLMnavi.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using UnitsNet;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceProvider = InitializeServiceProvider();
        var lightControllerFactory = serviceProvider.GetRequiredService<ILightControllerFactory>();
        var lightController = lightControllerFactory.CreateLightController();

        (string, Func<ILightController, Task<bool>>)[] tests = [
            (
                "Red simple test",
                (lightController) => SimpleSingleColorTest(lightController, LightColor.Red)
            ),
            (
                "Red brightness test",
                (lightController) => BrightnessSingleColorTest(lightController, LightColor.Red)
            ),
            (
                "Green simple test",
                (lightController) => SimpleSingleColorTest(lightController, LightColor.Green)
            ),
            (
                "Green brightness test",
                (lightController) => BrightnessSingleColorTest(lightController, LightColor.Green)
            ),
            (
                "Blue simple test",
                (lightController) => SimpleSingleColorTest(lightController, LightColor.Blue)
            ),
            (
                "Blue brightness test",
                (lightController) => BrightnessSingleColorTest(lightController, LightColor.Blue)
            ),
            (
                "UV simple test",
                (lightController) => SimpleSingleColorTest(lightController, LightColor.UltraViolet)
            ),
            (
                "UV brightness test",
                (lightController) => BrightnessSingleColorTest(lightController, LightColor.UltraViolet)
            ),
            (
                "All colors test",
                AllColorsTest
            )
        ];

        foreach (var (testName, test) in tests)
        {
            WriteLineYellow($"{testName} started.");
            var isResetSuccessful = await ResetState(lightController);
            if (!isResetSuccessful)
            {
                WriteLineRed("Aborting all further tests.");
                return;
            }
            var result = await test(lightController);
            if (result)
            {
                WriteLineGreen($"{testName} passed successfully.");
            }
            else
            {
                WriteLineRed($"{testName} failed.");
            }
        }
    }

    private static ServiceProvider InitializeServiceProvider()
    {
        var services = new ServiceCollection();

        var appConfig = new ApplicationConfig();
        appConfig = appConfig with
        {
            Microscope = appConfig.Microscope with
            {
                Thorlabs4100 = new CFLMnavi.Configuration.Devices.Thorlabs4100
                {
                    Port = new CFLMnavi.Configuration.Devices.SerialPort
                    {
                        DeviceName = "COM4",
                        BaudRate = 115200
                    }
                }
            }
        };

        return services
            .AddSingleton(appConfig)
            .AddLogging()
            .AddSerialCommunicationServices()
            .AddThorlabsLightServices()
            .BuildServiceProvider();
    }

    private static async Task<bool> ResetState(ILightController lightController)
    {
        WriteLineYellow("Resetting the state of the light controller");
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
        try
        {
            await lightController.SetBrightnessModeStateAsync(false, cts.Token);
            await lightController.SetSingleSelectionModeStateAsync(false, cts.Token);
            foreach (var color in Enum.GetValues<LightColor>())
            {
                await lightController.SetLightStateAsync(color, false, cts.Token);
            }
            return AnsiConsole.Confirm("Are all lights off?");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            WriteLineRed("Reset failed.");
            return false;
        }
    }

    private static async Task<bool> SimpleSingleColorTest(ILightController lightController, LightColor color)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
        try
        {
            await lightController.SetSingleSelectionModeStateAsync(true, cts.Token);
            await lightController.SetLightStateAsync(color, true, cts.Token);
            return AnsiConsole.Confirm($"Is {color} LED on?");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

    private static async Task<bool> BrightnessSingleColorTest(ILightController lightController, LightColor color)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
        try
        {
            await lightController.SetBrightnessModeStateAsync(true, cts.Token);
            await lightController.SetLightStateAsync(color, true, cts.Token);
            await lightController.SetLightBrightnessAsync(color, Ratio.FromPercent(25), cts.Token);
            return AnsiConsole.Confirm($"Is {color} LED at 25 % brightness?");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

    // TODO: Decide whether we need this
    private static async Task<bool> AllColorsTest(ILightController lightController)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
        try
        {
            foreach (var color in Enum.GetValues<LightColor>())
            {
                await lightController.SetLightStateAsync(color, true, cts.Token);
            }
            return AnsiConsole.Confirm($"Are all LEDs on?");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return false;
        }
    }

    private static void WriteLineYellow(string line) => AnsiConsole.MarkupLineInterpolated($"[yellow]{line}[/]");
    private static void WriteLineGreen(string line) => AnsiConsole.MarkupLineInterpolated($"[green]{line}[/]");
    private static void WriteLineRed(string line) => AnsiConsole.MarkupLineInterpolated($"[red]{line}[/]");
}
