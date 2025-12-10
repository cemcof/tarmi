using Tarmi.Communication.Common.Serial;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Thorlabs.FilterWheel.Implementation;

public class FilterWheelController : IFilterWheelController
{
    public double FilterColor => _filterColor;
    public bool FilterWheelIsActive => _filterWheelIsActive;
    private readonly int AnswerDelay = 300;

    private readonly ILogger _logger;
    private readonly ISerialCommunication _serialCommunication;

    private bool _filterWheelIsActive = false;
    private double _filterColor;

    public FilterWheelController(ISerialCommunication serialCommunication, ILogger<FilterWheelController> logger)
    {
        _serialCommunication = serialCommunication;
        _logger = logger;
    }

    public void Dispose() => _serialCommunication.Dispose();

    public async Task<bool> IsDeviceActive(CancellationToken cancellationToken = default)
    {
        // TODO - need to implement by real communication protocol
        var result = "ok";
        _filterWheelIsActive = string.Equals(result, "ok", StringComparison.InvariantCultureIgnoreCase);
        await Task.Delay(AnswerDelay, cancellationToken);
        return _filterWheelIsActive;
    }

    public async Task<double> GetCurrentFilterColor(CancellationToken cancellationToken = default)
    {
        // TODO - need to implement by real communication protocol
        _logger.LogInformation("Getting current emission filter color.");
        await Task.Delay(AnswerDelay, cancellationToken);
        return FilterColor;
    }

    public async Task<bool> SetFilterColor(double filterColor, CancellationToken cancellationToken = default)
    {
        // TODO - need to implement by real communication protocol
        _logger.LogInformation("Setting new emission filter color to {FilterColor}.", filterColor);
        var response = "ok";
        await Task.Delay(AnswerDelay, cancellationToken);

        if (string.Equals(response, "ok", StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogTrace("New emission filter color was successfully set.");
            _filterColor = filterColor;
            return true;
        }
        else
        {
            _logger.LogError("New emission filter color wasn't set. Response was {Response}.", response);
            return false;
        }
    }
}
