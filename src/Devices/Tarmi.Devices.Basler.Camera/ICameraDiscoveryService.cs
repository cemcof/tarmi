namespace Tarmi.Devices.Basler.Camera;

public interface ICameraDiscoveryService
{
    void Refresh();
    IReadOnlyList<CameraInformation> GetCameras();
}
