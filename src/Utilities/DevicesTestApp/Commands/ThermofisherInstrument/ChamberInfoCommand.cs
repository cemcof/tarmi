using System.ComponentModel;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ThermofisherInstrument;


[Description("Prints chamber pressure.")]
internal sealed class ChamberInfoCommand : Command<ChamberInfoCommand.Settings>
{
    public sealed class Settings : ThermofisherInstrumentSettings
    {
    }

    private readonly Chamber _chamber;

    public ChamberInfoCommand(Chamber chamber)
    {
        _chamber = chamber;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var result = _chamber.GetChamberPressure();
            if (result.IsSuccess)
            {
                var pressure = result.Value;
                AnsiConsole.MarkupLine($"[bold][yellow]Pressure[/]: {pressure.Pascals} Pa, {pressure.Torrs} Torrs[/]");
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
