using UnitsNet;

namespace Tarmi.Devices.SmarAct.Stage;

public interface ILinearStage
{
    IObservable<Length> Position { get; }
    Length CurrentPosition { get; }
    IObservable<bool> IsProtracted { get; }
    bool GetIsProtracted();
    Task<ResponseType> GetErrorAsync(CancellationToken cancellationToken = default);
    Task<int> GetErrorsCountAsync(CancellationToken cancellationToken = default);
    Task<ChannelState> GetStateAsync(CancellationToken cancellationToken = default);
    Task<int> GetTemperatureAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
    Task MoveRelativeAsync(Length distance, CancellationToken cancellationToken = default);
    Task MoveAbsoluteAsync(Length position, CancellationToken cancellationToken = default);
    Task ProtractAsync(CancellationToken cancellationToken = default);
    Task RetractAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
