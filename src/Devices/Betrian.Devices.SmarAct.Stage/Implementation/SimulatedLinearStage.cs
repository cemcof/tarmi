using System.Reactive.Linq;
using System.Reactive.Subjects;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Alignments;
using CommunityToolkit.Diagnostics;
using UnitsNet;

namespace Betrian.Devices.SmarAct.Stage.Implementation;

public class SimulatedLinearStage : ILinearStage
{
    private ChannelState _state = ChannelState.InPosition;
    private readonly LinearStageAlignment _alignment;

    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(100);

    private bool _stopRequested = true;

    private readonly BehaviorSubject<Length> _positionSubject = new(default);
    public IObservable<Length> Position => _positionSubject.AsObservable();

    public Length CurrentPosition
    {
        get => _positionSubject.Value;
        private set => _positionSubject.OnNext(value);
    }

    public IObservable<bool> IsProtracted => Position
        .Select(IsInProtractedRange)
        .DistinctUntilChanged();

    private bool IsInProtractedRange(Length position)
        => position >= _alignment.RetractPosition + _alignment.PositionTolerance;

    public bool GetIsProtracted() => IsInProtractedRange(_positionSubject.Value);

    public SimulatedLinearStage(ApplicationConfig applicationConfig)
    {
        _alignment = applicationConfig.Microscope.Alignment.LinearStage;
    }

    public async Task<ResponseType> GetErrorAsync(CancellationToken cancellationToken)
        => await GetDelayedValueAsync(ResponseType.NoError, cancellationToken);

    public async Task<int> GetErrorsCountAsync(CancellationToken cancellationToken)
        => await GetDelayedValueAsync(0, cancellationToken);

    public async Task<ChannelState> GetStateAsync(CancellationToken cancellationToken)
        => await GetDelayedValueAsync(_state, cancellationToken);

    public async Task<int> GetTemperatureAsync(CancellationToken cancellationToken)
        => await GetDelayedValueAsync(15, cancellationToken);

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
        => await GetDelayedValueAsync(true, cancellationToken);

    public async Task MoveRelativeAsync(Length distance, CancellationToken cancellationToken)
    {
        var position = UnitMath.Clamp(CurrentPosition + distance, _alignment.FocusMinimum, _alignment.FocusMaximum);
        await MoveAbsoluteAsync(position, cancellationToken);
    }

    public async Task MoveAbsoluteAsync(Length position, CancellationToken cancellationToken)
    {
        Guard.IsBetweenOrEqualTo(position, _alignment.FocusMinimum, _alignment.FocusMaximum);
        await MoveAsync(position, _alignment.LowVelocity, cancellationToken);
    }

    public async Task ProtractAsync(CancellationToken cancellationToken)
        => await MoveAsync(_alignment.ProtractPosition, _alignment.HighVelocity, cancellationToken);

    public async Task RetractAsync(CancellationToken cancellationToken)
        => await MoveAsync(_alignment.RetractPosition, _alignment.HighVelocity, cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken)
        => await StopIfMoving();

    public static async Task<T> GetDelayedValueAsync<T>(T value, CancellationToken cancellationToken)
    {
        await Task.Delay(Delay, cancellationToken);
        return value;
    }

    private async Task MoveAsync(Length targetPosition, Speed velocity, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await StopIfMoving();
        _stopRequested = false;
        _state = ChannelState.ActivelyMoving;
        var step = Math.Sign((targetPosition - CurrentPosition).Value) * (Delay * velocity);
        while (!_stopRequested && (targetPosition - CurrentPosition).Abs() > step.Abs())
        {
            await Task.Delay(Delay, cancellationToken);
            CurrentPosition += step;
        }
        _state = ChannelState.InPosition;
        if (!_stopRequested)
        {
            CurrentPosition = targetPosition;
        }
    }

    private async Task StopIfMoving()
    {
        if (_state.HasFlag(ChannelState.ActivelyMoving))
        {
            await Task.Delay(Delay);
            _state = ChannelState.Idle;
        }
    }
}
