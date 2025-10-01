namespace Betrian.Devices.Arduino.FilterHandler;

public interface IFilterHandler
{
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
    Task<bool> SwitchFilterAsync(FilterType filter, CancellationToken cancellationToken = default);
    Task<FilterType> ReadFilterPositionAsync(CancellationToken cancellationToken = default);
    FilterType FilterPosition { get; }
}
