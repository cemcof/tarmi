using Tarmi.Imaging.Common;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface ILuminescenceTubeControllingMode
{
    Length FocusStep { get; set; }
    IEnumerable<Length> FocusStepSizes { get; }
    IObservable<Length> LinearStagePosition { get; }
    IObservable<bool> IsProtracted { get; }
    Length CurrentLinearStagePosition { get; }
    Task ProtractAsync(CancellationToken cancellationToken);
    Task RetractAsync(CancellationToken cancellationToken);
    Task MoveLinearStageToAsync(Length position, CancellationToken cancellation);
    Task MoveLinearStageRelativeAsync(Length position, CancellationToken cancellation);
    Task RestoreImageState(ImageMetadata imageMetadata, CancellationToken cancellation);
}
