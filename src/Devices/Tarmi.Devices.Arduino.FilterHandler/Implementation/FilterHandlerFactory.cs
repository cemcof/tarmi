using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Arduino.FilterHandler.Implementation;

internal class FilterHandlerFactory : IFilterHandlerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISerialCommunicationFactory _serialCommunicationFactory;
    private readonly ILogger<FilterHandlerFactory> _logger;
    private readonly bool _simulationEnabled;
    private readonly Tarmi.Configuration.Devices.FilterHandler _filterHandlerConfig;
    private readonly FilterHandlerAlignment _filterHandlerAlignment;

    public FilterHandlerFactory(
        ISerialCommunicationFactory serialCommunicationFactory,
        ILoggerFactory loggerFactory,
        ILogger<FilterHandlerFactory> logger,
        ApplicationConfig applicationConfig
    )
    {
        _serialCommunicationFactory = serialCommunicationFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _filterHandlerConfig = applicationConfig.Microscope.FilterHandler;
        _filterHandlerAlignment = applicationConfig.Microscope.Alignment.FilterHandler;
    }

    public IFilterHandler CreateFilterHandler()
    {
        _logger.LogInformation("Creating serial communication with {@Configuration}", _filterHandlerConfig.Port);

        return _simulationEnabled ?
            new SimulatedFilterHandler(_loggerFactory.CreateLogger<SimulatedFilterHandler>()) :
            new FilterHandler(_serialCommunicationFactory.CreateSerialCommunication(_filterHandlerConfig.Port), _filterHandlerAlignment, _loggerFactory.CreateLogger<FilterHandler>());
    }
}
