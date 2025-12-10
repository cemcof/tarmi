using System.ComponentModel;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ThermofisherInstrument;


[Description("Prints stage information.")]
internal sealed class StageInfoCommand : Command<StageInfoCommand.Settings>
{
    public sealed class Settings : ThermofisherInstrumentSettings
    {
    }

    private readonly Stage _stage;

    public StageInfoCommand(Stage stage)
    {
        _stage = stage;
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var result = _stage.GetCurrentPosition();
            if (result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[bold][yellow]X[/]: {result.Value!.X.Millimeters:0.0000} mm[/]");
                AnsiConsole.MarkupLine($"[bold][yellow]Y[/]: {result.Value!.Y.Millimeters:0.0000} mm[/]");
                AnsiConsole.MarkupLine($"[bold][yellow]Z[/]: {result.Value!.Z.Millimeters:0.0000} mm[/]");
                AnsiConsole.MarkupLine($"[bold][yellow]Rotation[/]: {result.Value!.Rotation.Degrees:0.0} °[/]");
                AnsiConsole.MarkupLine($"[bold][yellow]Tilt[/]: {result.Value!.Tilt.Degrees:0.0} °[/]");
            }
            else
            {
                AnsiConsole.WriteException(result.Exception!);
            }

        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}
