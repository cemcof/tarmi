using System.ComponentModel;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel;
using Dumpify;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ThermofisherInstrument;

[Description("Prints SEM Electron beam info.")]
internal sealed class SemInfoCommand : Command<SemInfoCommand.Settings>
{
    public sealed class Settings : ThermofisherInstrumentSettings
    {
    }

    private readonly ElectronBeam _beam;

    public SemInfoCommand(ElectronBeam beam)
    {
        _beam = beam;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var result = _beam.GetIsBeamOn();
            AnsiConsole.MarkupLine($"[bold]Beam On[/]: {result.Value}");
            result = _beam.GetIsBeamBlank();
            AnsiConsole.MarkupLine($"[bold]Beam Blank[/]: {result.Value}");
            var dwell = _beam.GetDwellTime();
            AnsiConsole.MarkupLine($"[bold]Dwell[/]: {dwell.Value.Nanoseconds} ns");
            var voltage = _beam.GetHighTensionVoltage();
            AnsiConsole.MarkupLine($"[bold]HV[/]: {voltage.Value.Volts} eV");
            var ecurrent = _beam.GetEmissionCurrent();
            AnsiConsole.MarkupLine($"[bold]Emission Current[/]: {ecurrent.Value.Milliamperes} mA");
            var hfw = _beam.GetHorizontalFieldWidth();
            AnsiConsole.MarkupLine($"[bold]HFW[/]: {hfw.Value.Micrometers} um");
            var fs = _beam.GetFieldSize();
            AnsiConsole.MarkupLine($"[bold]Field Size[/]: {fs.Value!.Width.Micrometers}x{fs.Value.Height.Micrometers} um");
            var res = _beam.GetResolution();
            AnsiConsole.MarkupLine($"[bold]Resolution[/]: {res.Value!.Width}x{res.Value.Height}x{res.Value.Depth}bit");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
        return 0;
    }
}
