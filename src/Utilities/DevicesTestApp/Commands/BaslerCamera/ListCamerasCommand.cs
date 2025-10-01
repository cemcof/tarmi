using System.ComponentModel;
using Betrian.Devices.Basler.Camera;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.BaslerCamera;

[Description("List available cameras.")]
internal sealed class ListCamerasCommand : Command<ListCamerasCommand.Settings>
{
    public sealed class Settings : BaslerCameraSettings
    {
    }

    private readonly ICameraDiscoveryService _cameraDiscoveryService;

    public ListCamerasCommand(ICameraDiscoveryService cameraDiscoveryService)
    {
        _cameraDiscoveryService = cameraDiscoveryService;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var cameras = _cameraDiscoveryService.GetCameras().ToList();
            for (var i = 0; i < cameras.Count; ++i)
            {
                var cameraInfo = cameras[i];
                AnsiConsole.MarkupLine($"[bold][yellow]{i}[/]: {cameraInfo.FullName}[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}

