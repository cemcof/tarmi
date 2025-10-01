using System.Reactive.Linq;
using System.Reactive.Subjects;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Alignments;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

using static Betrian.Devices.SmarAct.Stage.Implementation.Commands;
using static Betrian.Devices.SmarAct.Stage.Implementation.Responses;

namespace Betrian.Devices.SmarAct.Stage.Implementation;

public class LinearStage : ILinearStage
{
    private readonly int _channel;
    private readonly IMcs2Communication _communication;
    private readonly LinearStageAlignment _alignment;
    private readonly ILogger _logger;

    private static readonly TimeSpan UpdatePeriod = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MovementTimeout = TimeSpan.FromSeconds(5);

    // TODO: Init with actual position possibly by retraction call.
    private readonly BehaviorSubject<Length> _positionSubject = new(default);
    
    public IObservable<Length> Position => _positionSubject.AsObservable();

    public IObservable<bool> IsProtracted => Position
        .Select(IsInProtractedRange)
        .DistinctUntilChanged();

    private bool IsInProtractedRange(Length position)
        => position >= _alignment.RetractPosition + _alignment.PositionTolerance;

    public bool GetIsProtracted() => IsInProtractedRange(_positionSubject.Value);

    public LinearStage(IMcs2Communication communication, ApplicationConfig applicationConfig, ILogger<LinearStage> logger)
    {
        _communication = communication;
        _alignment = applicationConfig.Microscope.Alignment.LinearStage;
        _logger = logger;
        _channel = applicationConfig.Microscope.LinearStage.Channel;
    }

    public Length CurrentPosition
    {
        get => _positionSubject.Value;
        private set => _logger.Swallow(() => _positionSubject.OnNext(value));
    }

    public async Task UpdatePositionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var position = await _communication.SendCommandAsync<Property.Channel.Position, long>(_channel, cancellationToken);
            CurrentPosition = Length.FromPicometers(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stage position.");
            throw;
        }
    }


    public async Task<int> GetErrorsCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _communication.SendCommandAsync<Property.System.Error.Count, int>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error count.");
            throw;
        }
    }

    public async Task<ResponseType> GetErrorAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _communication.SendCommandAsync<Property.System.Error, ErrorResponse>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error.");
            throw;
        }
    }

    public async Task<ChannelState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _communication.SendCommandAsync<Property.Channel.State, EnumResponse<ChannelState>>(_channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get state.");
            throw;
        }
    }

    public async Task<int> GetTemperatureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _communication.SendCommandAsync<Property.Channel.Temperature, int>(_channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get temperature.");
            throw;
        }
    }

    public async Task ProtractAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await MoveAsync(MovementMode.ClosedLoopAbsolute, _alignment.ProtractPosition, _alignment.HighVelocity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to protract stage.");
            throw;
        }
    }

    public async Task RetractAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await MoveAsync(MovementMode.ClosedLoopAbsolute, _alignment.RetractPosition, _alignment.HighVelocity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retract stage.");
            throw;
        }
    }

    public async Task MoveRelativeAsync(Length distance, CancellationToken cancellationToken = default)
    {
        var targetPosition = UnitMath.Clamp(CurrentPosition + distance, _alignment.FocusMinimum, _alignment.FocusMaximum);
        await MoveAbsoluteAsync(targetPosition, cancellationToken);
    }

    public async Task MoveAbsoluteAsync(Length position, CancellationToken cancellationToken = default)
    {
        Guard.IsBetweenOrEqualTo(position, _alignment.FocusMinimum, _alignment.FocusMaximum);

        try
        {
            await MoveAsync(MovementMode.ClosedLoopAbsolute, position, _alignment.LowVelocity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move stage to position {Position}.", position);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _communication.SendCommandAsync<Movement.Stop>(_channel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop.");
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _communication.SendCommandAsync<Common.Test, int>(cancellationToken) == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check connection.");
            throw;
        }
    }

    private async Task MoveAsync(MovementMode mode, Length position, Speed velocity, CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        cancellationTokenSource.CancelAfter(MovementTimeout);

        await SetMovementModeAsync(mode, cancellationTokenSource.Token);
        await SetVelocityAsync(velocity, cancellationTokenSource.Token);
        await SetAccelerationAsync(_alignment.Acceleration, cancellationTokenSource.Token);
        await _communication.SendCommandAsync<Movement.Move>(_channel, (long)position.Picometers, cancellationTokenSource.Token);
        await WaitForMovementToFinish(cancellationTokenSource.Token);
    }

    private async Task WaitForMovementToFinish(CancellationToken cancellationToken)
    {
        ChannelState state;
        do
        {
            await Task.Delay(UpdatePeriod, cancellationToken);
            await UpdatePositionAsync(cancellationToken);
            state = await GetStateAsync(cancellationToken);
        } while (state.HasFlag(ChannelState.ActivelyMoving));
    }

    private Task SetMovementModeAsync(MovementMode mode, CancellationToken cancellationToken) =>
        _communication.SendCommandAsync<Property.Channel.MovementMode>(_channel, (int)mode, cancellationToken);

    private Task SetVelocityAsync(Speed velocity, CancellationToken cancellationToken) =>
        _communication.SendCommandAsync<Property.Channel.Velocity>(_channel, (long)velocity.ToPicometersPerSecond(), cancellationToken);

    private Task SetAccelerationAsync(Acceleration acceleration, CancellationToken cancellationToken) =>
        _communication.SendCommandAsync<Property.Channel.Acceleration>(_channel, (long)acceleration.ToPicometersPerSecondSquared(), cancellationToken);
}
