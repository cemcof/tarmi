using CFLMnavi.VirtualDevices.Implementation;

namespace CFLMnavi.VirtualDevices;

public interface IVirtualDevice : IImageGrabbingMode, IInstrumentStageControllingMode, IInstrumentStageObserver
{
    Task EnableAsync(CancellationToken cancellationToken);
    Task DisableAsync(CancellationToken cancellationToken);
    Task StopMovementsAsync(CancellationToken cancellationToken);
}
