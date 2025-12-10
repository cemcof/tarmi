namespace Tarmi.Devices.Basler.Camera;

public interface ISimulatedImageGrabber : IImageGrabber
{
    SimulationImageMode SimulationMode { get; set; }
    FileInfo ImageFile { get; set; }
}
