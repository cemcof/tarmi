using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Models;
using UnitsNet;

namespace Tarmi.VirtualDevices.Implementation;

public abstract class StageControllingModeBase
{
    protected readonly IInstrument _instrument;
    protected readonly ISafeStageControlling _safeStageControlling;

    protected abstract string CreateActivityName([CallerMemberName] string methodName = "");

    protected StageControllingModeBase(IInstrument instrument, ISafeStageControlling safeStageControlling)
    {
        _instrument = instrument;
        _safeStageControlling = safeStageControlling;
    }

    protected StageCameraView ActiveCameraView => _safeStageControlling.ActiveCameraView;

    protected Task<bool> SwitchStageViewAsync(StageCameraView cameraView)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        return _safeStageControlling.SwitchStageViewAsync(cameraView, default);
    }

    public Task<StagePosition> GetStagePositionAsync()
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        return Task.FromResult(_instrument.CurrentStageState.CurrentPosition);
    }

    public async Task<bool> MoveStageAsync(StagePosition position)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        return await _safeStageControlling.MoveStageAsync(position, default);
    }

    public async Task<bool> TiltStageAsync(Angle angle)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        return await _safeStageControlling.TiltStageAsync(angle, default);
    }

    public void StopStageMove()
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        _instrument.StageStopMoving().SyncResult();
    }


    public virtual Task FocusAtAsync(Length focusLength, CancellationToken cancellationToken) => Task.Run(() => _instrument.SetBeamFreeWorkingDistance(focusLength), cancellationToken);
    public virtual Length GetCurrentFocusLength() => _instrument.GetBeamFreeWorkingDistance();
}
