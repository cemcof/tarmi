using Tarmi.Confocal.Implementation;

namespace Tarmi.Confocal;

public interface IImageGraberFactory
{
    IImageGrabber CreateGrabber(bool isEmulated, PythonController pythonController);
}
