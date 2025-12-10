using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Arduino.FilterHandler.Implementation;

internal class SimulatedFilterHandler : IFilterHandler
{
    private readonly ILogger _logger;
    private readonly TimeSpan AnswerDelay = TimeSpan.FromMilliseconds(300);

    public FilterType FilterPosition { get; private set; } = FilterType.Fluorescence;

    public SimulatedFilterHandler(ILogger<SimulatedFilterHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Simulator testing filter handler connection.");
        await Task.Delay(AnswerDelay, cancellationToken);
        return true;
    }

    public async Task<bool> SwitchFilterAsync(FilterType filter, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Simulator attempting to switch to {FilterType} filter.", filter);
        await Task.Delay(AnswerDelay, cancellationToken);

        FilterPosition = filter switch
        {
            FilterType.Reflection => FilterType.Reflection,
            FilterType.Fluorescence => FilterType.Fluorescence,
            _ => throw new NotImplementedException()
        };

        _logger.LogTrace("Simulator filter switch was successful.");
        return true;
    }

    public async Task<FilterType> ReadFilterPositionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Simulator attempting to get current filter.");
        await Task.Delay(AnswerDelay, cancellationToken);

        if (FilterPosition == FilterType.Fluorescence || FilterPosition == FilterType.Reflection)
        {
            _logger.LogTrace("Simulator filter switch was successful.");
            return FilterPosition;
        }

        _logger.LogError("Simulator failed to get filter.");
        throw new NotSupportedException("Simulator unexpected response.");
    }
}
