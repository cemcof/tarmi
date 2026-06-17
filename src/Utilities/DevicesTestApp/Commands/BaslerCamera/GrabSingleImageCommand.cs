using System.ComponentModel;
using Tarmi.Devices.Basler.Camera;
using Dumpify;
using OpenCvSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.BaslerCamera;

[Description("Grab one image and show it.")]
internal sealed class GrabSingleImageCommand : Command<GrabSingleImageCommand.Settings>
{
    public sealed class Settings : BaslerCameraSettings
    {
        [Description("Numeric id of camera")]
        [CommandOption("-i")]
        public int? CameraId { get; set; }
    }

    private readonly ICameraDiscoveryService _cameraDiscoveryService;
    private readonly IImageGraberFactory _imageGraberFactory;

    public GrabSingleImageCommand(ICameraDiscoveryService cameraDiscoveryService, IImageGraberFactory imageGraberFactory)
    {
        _cameraDiscoveryService = cameraDiscoveryService;
        _imageGraberFactory = imageGraberFactory;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var cameras = _cameraDiscoveryService.GetCameras();
            var cameraId = settings.CameraId ?? 0;
            var cameraInfo = cameras[cameraId];
            _ = cameraInfo.Dump("Selected Camera Info");

            using var grabber = _imageGraberFactory.CreateGrabber(cameraInfo);
            grabber.Open(TimeSpan.FromSeconds(1));

            grabber.Width = 512;
            grabber.Height = 512;
            grabber.PixelFormat = ImagePixelFormat.Mono12;
            if (grabber is ISimulatedImageGrabber simulated)
            {
                simulated.SimulationMode = SimulationImageMode.StaticPattern;
            }

            using var result = grabber.GrabImage(TimeSpan.FromSeconds(1));
            using var win = new Window("Grabbed image", result.Image.Mat, WindowFlags.AutoSize);
            _ = Cv2.WaitKey();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}
