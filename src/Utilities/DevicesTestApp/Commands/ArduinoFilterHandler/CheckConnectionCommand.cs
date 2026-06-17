using System.ComponentModel;
using Tarmi.Devices.Arduino.FilterHandler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ArduinoFilterHandler;

[Description("Check whether connection to filter handler is working as expected.")]
internal class CheckConnectionCommand : AsyncCommand<FilterHandlerSettings>
{
    private readonly IFilterHandlerFactory _filterHandlerFactory;

    public CheckConnectionCommand(IFilterHandlerFactory filterHandlerFactory)
    {
        _filterHandlerFactory = filterHandlerFactory;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, FilterHandlerSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: fixme
            var filterHandler = _filterHandlerFactory.CreateFilterHandler();
            if (await filterHandler.IsConnectedAsync())
            {
                AnsiConsole.MarkupLine("[green]Filter connected.[/]");
                return 0;
            }
            AnsiConsole.MarkupLine("[red]Filter not connected.[/]");
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
