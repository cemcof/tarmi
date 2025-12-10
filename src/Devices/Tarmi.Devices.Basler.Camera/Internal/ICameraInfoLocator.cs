using Basler.Pylon;

namespace Tarmi.Devices.Basler.Camera.Internal;
internal interface ICameraInfoLocator
{
    ICameraInfo GetCameraInfo(CameraInformation cameraInformation);
}
