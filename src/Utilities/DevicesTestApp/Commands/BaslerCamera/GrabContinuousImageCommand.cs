using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Betrian.Devices.Basler.Camera;
using Dumpify;
using OpenCvSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevicesTestApp.Commands.BaslerCamera;

[Description("Grab image continuously for one minute.")]
internal sealed class GrabContinuousImageCommand : AsyncCommand<GrabContinuousImageCommand.Settings>
{
    public sealed class Settings : BaslerCameraSettings
    {
        [Description("Numeric id of camera")]
        [CommandOption("-i")]
        public int? CameraId { get; set; }

        [Description("Number of seconds to perform image grabbing. Default is 30s if not set.")]
        [CommandOption("-t")]
        public int TimeoutSeconds { get; set; } = 30;
    }

    private readonly ICameraDiscoveryService _cameraDiscoveryService;
    private readonly IImageGraberFactory _imageGraberFactory;

    public GrabContinuousImageCommand(ICameraDiscoveryService cameraDiscoveryService, IImageGraberFactory imageGraberFactory)
    {
        _cameraDiscoveryService = cameraDiscoveryService;
        _imageGraberFactory = imageGraberFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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
            grabber.PixelFormat = ImagePixelFormat.Mono8;
            if (grabber is ISimulatedImageGrabber simulated)
            {
                simulated.SimulationMode = SimulationImageMode.DynamicPattern;
            }
            grabber.FrameRate = UnitsNet.Frequency.FromHertz(100);

            using var mat = new Mat(new OpenCvSharp.Size { Width = 512, Height = 512 }, MatType.CV_8UC1, Scalar.Aqua);
            using var disposable =
                grabber.GrabbedImage
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Subscribe(
                    onCompleted: () => AnsiConsole.MarkupLine("[green]Completed[/]"),
                    onError: ex => AnsiConsole.WriteException(ex),
                    onNext: gi =>
                    {
                        gi.Image.Mat.CopyTo(mat);
                        Cv2.ImShow("Grabbed image", mat);
                        _ = Cv2.WaitKey(1); // win loop for image change
                    }
                );

            grabber.StartContinuousGrabbing();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            await Task.Delay(-1, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        return 0;
    }
}
