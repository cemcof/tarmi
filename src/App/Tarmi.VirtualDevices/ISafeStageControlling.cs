using Tarmi.Models;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface ISafeStageControlling
{
    StageCameraView ActiveCameraView { get; }
    IObservable<StageCameraView> ActiveCameraViewChanges { get; }
    Task<bool> SwitchStageViewAsync(StageCameraView targetView, CancellationToken cancellationToken = default);
    Task<bool> MoveStageAsync(StagePosition position, CancellationToken cancellationToken = default);
    Task<bool> TiltStageAsync(Angle angle, CancellationToken cancellationToken = default);
}
