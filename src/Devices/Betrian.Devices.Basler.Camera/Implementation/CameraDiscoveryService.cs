using Basler.Pylon;
using Betrian.Devices.Basler.Camera.Internal;
using Microsoft.Extensions.Logging;

namespace Betrian.Devices.Basler.Camera.Implementation;

internal class CameraDiscoveryService : ICameraDiscoveryService, ICameraInfoLocator
{
    private readonly ILogger _logger;
    private readonly Dictionary<CameraInformation, ICameraInfo> _foundCameras = [];
    private readonly object _lock = new();

    public CameraDiscoveryService(ILogger<CameraDiscoveryService> logger)
    {
        _logger = logger;
        Refresh();
    }

    public void Refresh()
    {
        try
        {
            var cameras = CameraFinder.Enumerate();

            lock (_lock)
            {
                _foundCameras.Clear();
                cameras.ForEach(ci => _foundCameras.Add(ci.ToCameraInformation(), ci));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retrieving Cameras Failed");
            throw;
        }
    }

    public IReadOnlyList<CameraInformation> GetCameras()
    {
        lock (_lock)
        {
            return _foundCameras.Keys.ToList();
        }
    }

    public ICameraInfo GetCameraInfo(CameraInformation cameraInformation)
    {
        lock (_lock)
        {
            return _foundCameras.First(kv => kv.Key == cameraInformation).Value;
        }
    }
}
