namespace Tarmi.Devices.Basler.Camera;

public interface IImageGraberFactory
{
    IImageGrabber CreateGrabber(CameraInformation cameraInformation);
}
