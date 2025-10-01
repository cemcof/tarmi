using System.ComponentModel;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.ArduinoFilterHandler;

internal class FilterHandlerSettings : CommandSettings
{
    [Description("Name of the COM port")]
    [CommandOption("-p|--port")]
    public string PortName { get; set; } = "COM10";
}
