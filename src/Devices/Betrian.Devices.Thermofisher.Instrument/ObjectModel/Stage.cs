using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Betrian.Models;
using Fei.XT.Instrument.gen;
using Microsoft.Extensions.Logging;
using UnitsNet;
using StagePosition = Betrian.Models.StagePosition;

namespace Betrian.Devices.Thermofisher.Instrument.ObjectModel;

internal sealed class Stage
{
    private readonly ILogger _logger;
    private readonly IXtObjectHandle<BulkStageNavigation> _bulkStageNavigation;
    private readonly IXtObjectHandle<NavigationRestrictions> _stageRestrictions;
    private readonly IXtObjectHandle<Navigation> _stageNavigation;
    private readonly IXtObjectHandle<BulkStageDevice> _bulkStageDevice;

    private readonly BehaviorSubject<bool> _isLinkedSubject = new(false);
    private readonly BehaviorSubject<bool> _isMovingSubject = new(false);
    private readonly BehaviorSubject<bool> _isInErrorSubject = new(false);
    private readonly BehaviorSubject<StagePosition> _currentPositionSubject = new(StagePosition.Zero);

    private CompositeDisposable _disposables = [];

    public Stage(ILogger<Stage> logger, IXtObjectsCollection xtObjectsCollection)
    {
        _logger = logger;
        _bulkStageNavigation = xtObjectsCollection.GetObject<BulkStageNavigation>(PathLiterals.Instrument.Positioning.BulkStage.AsString);
        _stageRestrictions = xtObjectsCollection.GetObject<NavigationRestrictions>(PathLiterals.Instrument.Positioning.BulkStage.AsString);
        _stageNavigation = xtObjectsCollection.GetObject<Navigation>(PathLiterals.Instrument.Positioning.BulkStage.AsString);
        _bulkStageDevice = xtObjectsCollection.GetObject<BulkStageDevice>(PathLiterals.Instrument.Positioning.BulkStage.AsString);

        _stageNavigation.Connected += (obj, args) => Connect();
        _stageNavigation.Disconnecting += (obj, args) => Disconnect();

        if (_stageNavigation.IsConnected)
        {
            Connect();
        }
        else
        {
            xtObjectsCollection.ConnectObjects();
        }

    }

    public IObservable<bool> IsLinked => _isLinkedSubject.AsObservable().DistinctUntilChanged();
    public IObservable<bool> IsMoving => _isMovingSubject.AsObservable().DistinctUntilChanged();
    public IObservable<bool> IsInError => _isInErrorSubject.AsObservable().DistinctUntilChanged();
    public IObservable<StagePosition> CurrentPosition => _currentPositionSubject.AsObservable().DistinctUntilChanged();

    private void InitializeStates()
    {
        _logger.Swallow(() => _isLinkedSubject.OnNext(_bulkStageNavigation.Object.GetLinkingState() == LinkingZtoWorkingDistanceState.LinkingZtoWorkingDistanceState_Linked));
        var state = GetState();
        _logger.Swallow(() => _isMovingSubject.OnNext(state == BulkStageState.BulkStageState_Moving));
        _logger.Swallow(() => _isInErrorSubject.OnNext(state == BulkStageState.BulkStageState_Moving));
        _logger.Swallow(() => _currentPositionSubject.OnNext(GetCurrentPosition().Value!));
    }

    private void Connect()
    {
        _disposables.Add(
            Observable.FromEvent(
                h => _bulkStageNavigation.Object.LinkingStateChanged += new IBulkStageNavigationEvents_LinkingStateChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(unit => {
                var isStageLinked = _bulkStageNavigation.Object.GetLinkingState() == LinkingZtoWorkingDistanceState.LinkingZtoWorkingDistanceState_Linked;
                _logger.Swallow(() => _isLinkedSubject.OnNext(isStageLinked));
                if (isStageLinked)
                {
                    _logger.Swallow(() => Unlink());
                }
            })
        );
        Unlink();

        _disposables.Add(
            Observable.FromEvent<BulkStageState>(
                h => _bulkStageDevice.Object.StateChanged += new IBulkStageDeviceEvents_StateChangedEventHandler(h),
                h => _ = h // not necessary when COM object is disconnected
            ).Subscribe(state =>
            {
                //_logger.Swallow(() => _isLinkedSubject.OnNext(_bulkStageNavigation.Object.GetLinkingState() == LinkingZtoWorkingDistanceState.LinkingZtoWorkingDistanceState_Linked))
                _logger.Swallow(() => _isMovingSubject.OnNext(state == BulkStageState.BulkStageState_Moving));
                _logger.Swallow(() => _isInErrorSubject.OnNext(state == BulkStageState.BulkStageState_Error));
            })
        );

        _stageNavigation.Object.CurrentPositionChanged +=
            (viewType, coordinates) =>
            {
                var result = GetCurrentPosition();
                if (result.IsSuccess)
                {
                    _logger.Swallow(() => _currentPositionSubject.OnNext(result.Value!));
                }
                else
                {
                    _logger.LogWarning(result.Exception, "Failed to get current position on position changed event");
                }
            };

        InitializeStates();
    }

    private void Disconnect()
    {
        _disposables.Dispose();
        _disposables = [];
    }

    private BulkStageState GetState()
    {
        try
        {
            return _bulkStageDevice.Object.State;
        }
        catch
        {
            return BulkStageState.BulkStageState_Error;
        }
    }

    public Result Stop()
    {
        try
        {
            _bulkStageDevice.Object.Stop();
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop stage");
            return ex.MapToResult();
        }
    }

    public Result<bool> AreAllAxesHomed()
    {
        try
        {
            AllowedPositionRange allowedPositions = _stageRestrictions.Object.GetLimitsForCurrentPosition();

            var allHomed = _bulkStageDevice.Object.GetHomedAxes() == (CoordinateMask.axis_x | CoordinateMask.axis_y | CoordinateMask.axis_z | CoordinateMask.axis_rx | CoordinateMask.axis_rz);
            return new Result<bool>(allHomed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if all axes are homed");
            return ex.MapToResult<bool>();
        }
    }

    public Result<bool> GetIsLinked()
    {
        try
        {
            return new Result<bool>(_bulkStageNavigation.Object.GetLinkingState() == LinkingZtoWorkingDistanceState.LinkingZtoWorkingDistanceState_Linked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if Z is linked");
            return ex.MapToResult<bool>();
        }
    }

    private void Unlink()
    {
        if (_bulkStageNavigation.Object.GetLinkingState() == LinkingZtoWorkingDistanceState.LinkingZtoWorkingDistanceState_Linked)
        {
            _bulkStageNavigation.Object.Unlink();
        }
    }

    public Result<StagePosition> GetCurrentPosition()
    {
        try
        {
            var position = _stageNavigation.Object.GetCurrentPosition(NavigationViewType.NavigationViewType_ElectronBeam, CoordinateSystemType.CoordinateSystemType_Integrated);
            var axesPos = new StagePosition
            {
                X = Length.FromMeters(position.x),
                Y = Length.FromMeters(position.y),
                Z = Length.FromMeters(position.z),
                Rotation = Angle.FromRadians(position.rz),
                Tilt = Angle.FromRadians(position.rx)
            };

            return new Result<StagePosition>(axesPos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current position");
            return ex.MapToResult<StagePosition>();
        }
    }

    public Result Move(StagePosition axesPositions)
    {
        try
        {
            var mp = new MoveParameters
            {
                Coordinates = CoordinateSystemType.CoordinateSystemType_Integrated,
                forView = NavigationViewType.NavigationViewType_ElectronBeam,
                toPosition = new Coordinates
                {
                    x = axesPositions.X.Meters,
                    y = axesPositions.Y.Meters,
                    z = axesPositions.Z.Meters,
                    rx = axesPositions.Tilt.Radians,
                    rz = axesPositions.Rotation.Radians
                },
                mask = CoordinateMask.axis_x | CoordinateMask.axis_y | CoordinateMask.axis_z | CoordinateMask.axis_rx | CoordinateMask.axis_rz
            };

            var strategy = _stageNavigation.Object.CreateDefaultMoveStrategy(NavigationViewType.NavigationViewType_ElectronBeam);
            _stageNavigation.Object.Move(ref mp, strategy);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move to {Position}", axesPositions);
            return ex.MapToResult();
        }
    }

    public Result MoveBy(StagePosition axesOffsets)
    {
        try
        {
            var coordinates = new Coordinates
            {
                x = axesOffsets.X.Meters,
                y = axesOffsets.Y.Meters,
                z = axesOffsets.Z.Meters,
                rx = axesOffsets.Tilt.Radians,
                rz = axesOffsets.Rotation.Radians
            };
            CoordinateMask mask = 0;
            if (axesOffsets.X.Meters != 0) { mask |= CoordinateMask.axis_x; }
            if (axesOffsets.Y.Meters != 0) { mask |= CoordinateMask.axis_y; }
            if (axesOffsets.Z.Meters != 0) { mask |= CoordinateMask.axis_z; }
            if (axesOffsets.Tilt.Radians != 0) { mask |= CoordinateMask.axis_rx; }
            if (axesOffsets.Rotation.Radians != 0) { mask |= CoordinateMask.axis_rz; }

            var mp = new MoveParameters
            {
                Coordinates = CoordinateSystemType.CoordinateSystemType_Integrated,
                forView = NavigationViewType.NavigationViewType_ElectronBeam,
                toPosition = coordinates,
                mask = mask
            };

            var strategy = _stageNavigation.Object.CreateDefaultMoveStrategy(NavigationViewType.NavigationViewType_ElectronBeam);
            _stageNavigation.Object.Move(ref mp, strategy);
            _stageNavigation.Object.MoveByOffset(ref mp, strategy);

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move to {@Position}", axesOffsets);
            return ex.MapToResult();
        }
    }

    public Result<StageLimits> GetStageLimits()
    {
        try
        {
            var details = _bulkStageNavigation.Object.GetStageDetails();
            return new Result<StageLimits>(new StageLimits
            {
                X = new LengthRangeDescriptor() { Min = Length.FromMeters(details.LowerLimits.x), Max = Length.FromMeters(details.UpperLimits.x) },
                Y = new LengthRangeDescriptor() { Min = Length.FromMeters(details.LowerLimits.y), Max = Length.FromMeters(details.UpperLimits.y) },
                Z = new LengthRangeDescriptor() { Min = Length.FromMeters(details.LowerLimits.z), Max = Length.FromMeters(details.UpperLimits.z) },
                Rotation = new AngleRangeDescriptor() { Min = Angle.FromRadians(details.LowerLimits.r), Max = Angle.FromRadians(details.UpperLimits.r) },
                Tilt = new AngleRangeDescriptor() { Min = Angle.FromRadians(details.LowerLimits.t), Max = Angle.FromRadians(details.UpperLimits.t) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stage limits");
            return ex.MapToResult<StageLimits>();
        }
    }
}
