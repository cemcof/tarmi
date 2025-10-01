using Betrian.Communication.Common.Serial;
using CFLMnavi.Configuration.Alignments;
using Microsoft.Extensions.Logging;

namespace Betrian.Devices.Arduino.FilterHandler.Implementation;

public class FilterHandler : IFilterHandler
{
    private readonly ISerialCommunication _serialCommunication;
    private readonly ILogger _logger;
    private readonly FilterHandlerAlignment _filterHandlerAlignment;

    public FilterType FilterPosition { get; private set; }

    public FilterHandler(ISerialCommunication serialCommunication, FilterHandlerAlignment filterHandlerAlignment, ILogger<FilterHandler> logger)
    {
        _serialCommunication = serialCommunication;
        _logger = logger;
        _filterHandlerAlignment = filterHandlerAlignment;
    }

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing filter handler connection.");
        var command = "hello";
        var response = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        _logger.LogInformation("Filter handler connection test result: {Response}", response);
        return true;
    }

    public async Task<bool> SwitchFilterAsync(FilterType filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to switch to {FilterType} filter.", filter);
        var position = filter switch
        {
            FilterType.Reflection => _filterHandlerAlignment.ReflectionFilterPosition,
            FilterType.Fluorescence => _filterHandlerAlignment.FluorescenceFilterPosition,
            _ => throw new NotImplementedException()
        };
        var command = position.ToString();
        var response = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        if (!string.Equals($"Set {command}", response, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogError("Filter switch has failed.");
            return false;
        }
        _logger.LogInformation("Filter switch was successful.");
        FilterPosition = filter;
        return true;
    }

    public async Task<FilterType> ReadFilterPositionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to get current filter.");
        var command = "position";
        var response = await _serialCommunication.SendCommandWithResponseAsync(command, cancellationToken);
        _logger.LogInformation("ReadFilterPosition: '{Response}'", response);
        if (!response.StartsWith("Pos ", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogError("Failed to get filter.");
            _ = await SwitchFilterAsync(FilterType.Fluorescence, cancellationToken);
            return FilterType.Fluorescence;
        }
        var position = int.Parse(response.AsSpan()[4..]);
        if (position == _filterHandlerAlignment.FluorescenceFilterPosition)
        {
            _logger.LogInformation("Filter switch was successful.");
            FilterPosition = FilterType.Fluorescence;
            return FilterType.Fluorescence;
            
        }
        if (position == _filterHandlerAlignment.ReflectionFilterPosition)
        {
            _logger.LogInformation("Filter switch was successful.");
            FilterPosition = FilterType.Reflection;
            return FilterType.Reflection;
        }
        else
        {
            _logger.LogInformation("Filter in undefined position, switching to fluorescence.");
            _ = await SwitchFilterAsync(FilterType.Fluorescence, cancellationToken);
            FilterPosition = FilterType.Reflection;
            return FilterType.Fluorescence;
        }
    }
}
