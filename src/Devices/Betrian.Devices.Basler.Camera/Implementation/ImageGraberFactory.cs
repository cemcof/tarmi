using Betrian.Devices.Basler.Camera.Internal;
using Microsoft.Extensions.Logging;

namespace Betrian.Devices.Basler.Camera.Implementation;

internal class ImageGraberFactory : IImageGraberFactory
{

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ICameraInfoLocator _cameraInfoLocator;

    public ImageGraberFactory(ILoggerFactory loggerFactory, ICameraInfoLocator cameraInfoLocator)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ImageGraberFactory>();
        _cameraInfoLocator = cameraInfoLocator;
    }

    public IImageGrabber CreateGrabber(CameraInformation cameraInformation)
    {
        _logger.LogInformation("Creating Grabber for {@CameraInformation}", cameraInformation);
        try
        {
            var camInfo = _cameraInfoLocator.GetCameraInfo(cameraInformation);
            if (cameraInformation.IsEmulated)
            {
                return new SimulatedImageGrabber(_loggerFactory.CreateLogger<ImageGrabber>(), camInfo);
            }
            return new ImageGrabber(_loggerFactory.CreateLogger<ImageGrabber>(), camInfo);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed creating grabber for {@CameraInformation}", cameraInformation);
            throw;
        }
    }
}
