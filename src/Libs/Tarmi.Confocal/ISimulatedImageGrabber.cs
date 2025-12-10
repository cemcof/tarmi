namespace Tarmi.Confocal;

public interface ISimulatedImageGrabber : IImageGrabber
{
    //SimulationImageMode SimulationMode { get; set; }
    FileInfo ImageFile { get; set; }
}
