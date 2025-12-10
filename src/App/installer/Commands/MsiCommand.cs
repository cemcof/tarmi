using Tarmi.Installer.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Tarmi.Installer.Commands;

internal sealed class MsiCommand : Command<MsiCommandSettings>
{
    public override int Execute(CommandContext context, MsiCommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            _ = BuildMsi(settings!);
            return 0;

        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }
    }

    public string BuildMsi(IMsiCommandSettingsAccessor settings)
    {
        return MsiBuilder.Build(settings);
    }
}
