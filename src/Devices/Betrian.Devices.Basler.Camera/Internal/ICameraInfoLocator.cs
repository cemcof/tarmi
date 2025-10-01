using Basler.Pylon;

namespace Betrian.Devices.Basler.Camera.Internal;
internal interface ICameraInfoLocator
{
    ICameraInfo GetCameraInfo(CameraInformation cameraInformation);
}
