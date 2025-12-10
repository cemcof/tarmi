using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.VirtualDevices.Implementation;

internal class SafeStageControlling : ISafeStageControlling, IDisposable
{
    private readonly ILogger _logger;
    private readonly IInstrument _instrument;
    private readonly IStageNavigation _stageNavigation;
    private readonly StageLimits _stageLimits;
    private readonly CompositeDisposable _subscriptions = [];
    private readonly ManualResetEventSlim _stageIdleEvent = new(false);
    private readonly SemaphoreSlim _moveLock = new(1, 1);
    private readonly BehaviorSubject<StageCameraView> _stageCameraViewSubject = new(StageCameraView.Unknown);

    public StageCameraView ActiveCameraView => _stageCameraViewSubject.Value;
    public IObservable<StageCameraView> ActiveCameraViewChanges => _stageCameraViewSubject.DistinctUntilChanged();

    public SafeStageControlling(ILogger<SafeStageControlling> logger, IInstrument instrument, IStageNavigation stageNavigation)
    {
        _logger = logger;
        _instrument = instrument;
        _stageNavigation = stageNavigation;
        _stageLimits = _instrument.GetStageLimits();
        _subscriptions.Add(
            _instrument.Stage
                .Select(s => s.IsMoving)
                .DistinctUntilChanged()
                .Subscribe(isMoving =>
                {
                    if (isMoving)
                    {
                        _stageIdleEvent.Reset();
                    }
                    else
                    {
                        _stageIdleEvent.Set();
                    }
                    _logger.LogDebug("Stage move flag changed to {IsStageMoving}", isMoving);
                })
        );
    }

    private StagePosition GetValidStagePosition(StagePosition stagePosition)
    {
        return new StagePosition
        {
            X = UnitMath.Max(_stageLimits.X.Min, UnitMath.Min(_stageLimits.X.Max, stagePosition.X)),
            Y = UnitMath.Max(_stageLimits.Y.Min, UnitMath.Min(_stageLimits.Y.Max, stagePosition.Y)),
            Z = UnitMath.Max(_stageLimits.Z.Min, UnitMath.Min(_stageLimits.Z.Max, stagePosition.Z)),
            Tilt = UnitMath.Max(_stageLimits.Tilt.Min, UnitMath.Min(_stageLimits.Tilt.Max, stagePosition.Tilt)),
            Rotation = stagePosition.Rotation.NormalizeAngle()
        };
    }

    public async Task<bool> SwitchStageViewAsync(StageCameraView targetView, CancellationToken cancellationToken)
    {
        if (ActiveCameraView == targetView)
        {
            return true;
        }

        using var lockGuard = await _moveLock.UseOnceAsync(cancellationToken);

        await _stageIdleEvent.WaitAsync(cancellationToken);
        var newPosition = _stageNavigation.TransformPosition(_instrument.CurrentStageState.CurrentPosition, ActiveCameraView, targetView);
        _logger.Swallow(() => _stageCameraViewSubject.OnNext(targetView));
        return await Task.Run(async () => await MoveStageInModeSwitchSafeWayAsync(newPosition, cancellationToken));
    }

    public async Task<bool> MoveStageAsync(StagePosition position, CancellationToken cancellationToken)
    {
        using var lockGuard = await _moveLock.UseOnceAsync(cancellationToken);

        return await Task.Run(async () => await MoveStageInPlaneSafeWayAsync(position, cancellationToken));
    }

    private async Task<bool> MoveStageInPlaneSafeWayAsync(StagePosition position, CancellationToken cancellationToken)
    {
        var currentPosition = _instrument.CurrentStageState.CurrentPosition;
        _logger.LogInformation("MoveStageInPlaneSafeWayAsync requested from {CurrentPosition} to {Position}", currentPosition, position);
        bool result;

        // finer check to allow move on small distances
        if (currentPosition.Equals(position, 1E-6,1E-3))
        {
            result = true;
        }
        else if (currentPosition.Z.Equals(position.Z, Length.FromMeters(1E-6)))
        {
            result = await MoveStageCoreAsync(position, cancellationToken);
        }
        else if (currentPosition.Z > position.Z)
        {
            var midPosition = currentPosition with { Z = position.Z };
            result = await MoveStageCoreAsync(midPosition, cancellationToken);
            if (result)
            {
                result = await MoveStageCoreAsync(position, cancellationToken);
            }
        }
        else
        {
            var midPosition = position with { Z = currentPosition.Z };
            result = await MoveStageCoreAsync(midPosition, cancellationToken);
            if (result)
            {
                result = await MoveStageCoreAsync(position, cancellationToken);
            }
        }
        _logger.LogInformation("MoveStageInPlaneSafeWayAsync result: {Result}", result);
        return result;
    }

    private async Task<bool> MoveStageInModeSwitchSafeWayAsync(StagePosition position, CancellationToken cancellationToken)
    {
        var currentPosition = _instrument.CurrentStageState.CurrentPosition;
        _logger.LogInformation("MoveStageInModeSwitchSafeWayAsync requested from {CurrentPosition} to {Position}", currentPosition, position);

        // finer check to allow move on small distances
        if (currentPosition.Equals(position, 1E-6, 1E-3))
        {
            return true;
        }

        var midPosition = currentPosition with { Z = _stageNavigation.SafeUnknownMoveZ };
        var result = await MoveStageCoreAsync(midPosition, cancellationToken);
        if (result)
        {
            midPosition = position with { Z = _stageNavigation.SafeUnknownMoveZ };
            result = await MoveStageCoreAsync(midPosition, cancellationToken);
            if (result)
            {
                result = await MoveStageCoreAsync(position, cancellationToken);
            }
        }
        _logger.LogInformation("MoveStageInModeSwitchSafeWayAsync result: {Result}", result);
        return result;
    }

    private async Task<bool> MoveStageCoreAsync(StagePosition position, CancellationToken cancellationToken)
    {
        _logger.LogDebug("MoveStageCoreAsync moving to {Position}", position);

        if (position.Equals(StagePosition.Zero))
        {
            // for safety reasons
            return false;
        }

        // wait till stage is idle
        await _stageIdleEvent.WaitAsync(cancellationToken);

        var inLimitsPosition = GetValidStagePosition(position);
        await _logger.SwallowAsync(() => _instrument.StageMove(inLimitsPosition));
        // let the stage move begin
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        StagePosition currentPosition = _instrument.CurrentStageState.CurrentPosition;

        // HACK: add some nice retry logic here
        for (var i = 5; i != 0; --i)
        {
            await _stageIdleEvent.WaitAsync(cancellationToken);
            currentPosition = _instrument.CurrentStageState.CurrentPosition;
            if (currentPosition.Equals(position))
            {
                return true;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        _logger.LogInformation("MoveStageCoreAsync failed to reach demanded position");
        _logger.LogInformation("MoveStageCoreAsync current position: {CurrentPosition}", currentPosition);
        _logger.LogInformation("MoveStageCoreAsync demanded position: {DemandedPosition}", position);

        return false;
    }

    public async Task<bool> TiltStageAsync(Angle angle, CancellationToken cancellationToken)
    {
        // wait till stage is idle
        await _stageIdleEvent.WaitAsync(cancellationToken);

        var stage = _instrument.CurrentStageState;
        var newPosition = stage.CurrentPosition with
        {
            Tilt = stage.CurrentPosition.Tilt + angle
        };

        return await Task.Run(async () => await MoveStageAsync(newPosition, cancellationToken));
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _stageIdleEvent.Dispose();
        _moveLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
