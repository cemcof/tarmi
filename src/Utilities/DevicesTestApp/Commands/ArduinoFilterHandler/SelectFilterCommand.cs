using System.ComponentModel;
using Betrian.Devices.Arduino.FilterHandler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ArduinoFilterHandler;

[Description("Switch to specified filter.")]
internal class SelectFilterCommand : AsyncCommand<SelectFilterCommand.Settings>
{
    public class Settings : FilterHandlerSettings
    {
        [Description("Name of the filter to switch to [fluorescence/reflection]")]
        [CommandArgument(0, "<filterName>")]
        public string FilterName { get; set; } = FilterType.Fluorescence.ToString();

        [Description("Timeout in seconds")]
        [CommandOption("-t|--timeout")]
        public int Timeout { get; set; } = 10;

    }
    private readonly IFilterHandlerFactory _filterHandlerFactory;

    public SelectFilterCommand(IFilterHandlerFactory filterHandlerFactory)
    {
        _filterHandlerFactory = filterHandlerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            if (!Enum.TryParse(settings.FilterName, true, out FilterType filter) || !Enum.IsDefined(filter))
            {
                AnsiConsole.MarkupLine("[red]Filter type parse error.[/]");
                return 1;
            }

            // TODO: fixme
            var filterHandler = _filterHandlerFactory.CreateFilterHandler();
            using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(settings.Timeout));

            if (!await filterHandler.SwitchFilterAsync(filter, tokenSource.Token))
            {
                AnsiConsole.MarkupLine("[red]Failed to switch filter.[/]");
                return 1;
            }
            AnsiConsole.MarkupLineInterpolated($"[green]Switched to {filter}.[/]");
            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[red]Connection timeout.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
        return 1;
    }
}
