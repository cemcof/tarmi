using Microsoft.Extensions.Logging;

namespace Tarmi.Confocal.Implementation;

internal sealed class SimulatedImageGrabber : ImageGrabber, ISimulatedImageGrabber
{
    public SimulatedImageGrabber(ILogger logger, PythonController pythonController)
        : base(logger, pythonController)
    {
    }

    public FileInfo ImageFile
    {
        get
        {
            return new FileInfo(ImagePath);
        }
        set
        {
            if (!value.Exists)
            {
                throw new FileNotFoundException("Existing file path must be provided")
                    .AddData("path", value.FullName);
            }
            ThrowIfGrabbingInProgress();
            ImagePath = value.FullName;
        }
    }
}
