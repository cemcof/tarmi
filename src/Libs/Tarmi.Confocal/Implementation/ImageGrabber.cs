using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Tarmi.Imaging.Common;

namespace Tarmi.Confocal.Implementation;

internal class ImageGrabber : IImageGrabber
{
    protected readonly ILogger _logger;
    private readonly Subject<ImageWithMetadata> _grabbedImageSubject = new();
    private string _imagePath;
    private readonly PythonController _pythonController;
    public string DefaultImagePath { get; }

    public ImageGrabber(ILogger logger, PythonController pythonController)
    {
        _logger = logger;
        _imagePath = string.Empty;
        _pythonController = pythonController;
        DefaultImagePath = Path.Combine(Path.GetTempPath(), "confocalImage.tif");
    }

    public bool IsGrabbing { get; private set; } = false;

    protected void ThrowIfGrabbingInProgress()
    {
        if (IsGrabbing)
        {
            throw new InvalidOperationException("Image grabbing in progress");
        }
    }

    public IObservable<ImageWithMetadata> GrabbedImage => _grabbedImageSubject.AsObservable();

    public string ImagePath
    {
        get => _imagePath;
        set => _imagePath = value;
    }

    public async Task<ImageWithMetadata> GrabImage(IConfocalDevice confocalDevice)
    {
        ThrowIfGrabbingInProgress();

        if (ImagePath.IsNotNullOrWhiteSpace())
        {
            ImagePath = DefaultImagePath;
        }

        // Grab image
        var args = PythonController.GeneratePythonArgs(confocalDevice, ImagePath);
        _logger.LogDebug("Confocal, {name}, python args \n{args}", nameof(GrabImage), args);
        var (result, error) = await _pythonController.ExecuteScriptWithArgs(args);

        if (!result)
        {
            throw new InvalidOperationException($"Grabbing of image failed with result {error}");
        }

        if (!new FileInfo(ImagePath).Exists)
        {
            throw new ArgumentNullException($"Grabbing of image failed, image not found {ImagePath}");
        }

        var imageWithMetadata = TiffImage.Load(ImagePath);
        File.Delete(ImagePath);
        return imageWithMetadata with { ConfocalMetadata = confocalDevice.ConvertToMetadata(imageWithMetadata.Image.Mat) };
    }

    public async Task StartContinuousGrabbing(IConfocalDevice confocalDevice)
    {
        ThrowIfGrabbingInProgress();

        // start grabbing
        IsGrabbing = true;
        _logger.LogDebug("Confocal, start continuous grabbing.");

        var args = PythonController.GeneratePythonArgs(confocalDevice, ImagePath);
        _logger.LogDebug("Confocal, {name}, python args \n{args}", nameof(StartContinuousGrabbing), args);
        var (result, error) = await _pythonController.StartTuningScriptWithArgs(args);

        _grabbedImageSubject.OnNext(ImageWithMetadata.Empty);
    }

    public void StopContinuousGrabbing()
    {
        // stop grabbing
        _pythonController.EndTuning();
        IsGrabbing = false;
        _logger.LogDebug("Confocal, stop continuous grabbing.");
    }

    public void Dispose()
    {
        _grabbedImageSubject.Dispose();
        GC.SuppressFinalize(this);
    }
}
