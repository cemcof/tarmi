using Microsoft.Extensions.Logging;

namespace Tarmi.Confocal.Implementation;

internal class ImageGraberFactory : IImageGraberFactory
{

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public ImageGraberFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ImageGraberFactory>();
    }

    public IImageGrabber CreateGrabber(bool isEmulated, PythonController pythonController)
    {
        _logger.LogInformation("Creating Grabber for Confocal");
        try
        {
            return isEmulated
                ? new SimulatedImageGrabber(_loggerFactory.CreateLogger<ImageGrabber>(), pythonController)
                : (IImageGrabber)new ImageGrabber(_loggerFactory.CreateLogger<ImageGrabber>(), pythonController);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed creating grabber for Confocal");
            throw;
        }
    }
}
