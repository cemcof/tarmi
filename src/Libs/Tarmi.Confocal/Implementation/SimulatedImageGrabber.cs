using CommunityToolkit.Diagnostics;
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
            Guard.IsTrue(value.Exists, "Existing file path must be provided");
            ThrowIfGrabbingInProgress();
            ImagePath = value.FullName;
        }
    }
}
