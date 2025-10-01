namespace Betrian.Devices.Basler.Camera;

public interface IImageGraberFactory
{
    IImageGrabber CreateGrabber(CameraInformation cameraInformation);
}
