using System.ComponentModel;
using Tarmi.Devices.Arduino.FilterHandler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ArduinoFilterHandler;

[Description("Get currently selected filter.")]
internal class GetFilterCommand : AsyncCommand<FilterHandlerSettings>
{
    private readonly IFilterHandlerFactory _filterHandlerFactory;

    public GetFilterCommand(IFilterHandlerFactory filterHandlerFactory)
    {
        _filterHandlerFactory = filterHandlerFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, FilterHandlerSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // TODO: fixme
            var filterHandler = _filterHandlerFactory.CreateFilterHandler();
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            var filter = await filterHandler.ReadFilterPositionAsync(cts.Token);
            AnsiConsole.MarkupLineInterpolated($"{filter} filter is currently selected");
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
