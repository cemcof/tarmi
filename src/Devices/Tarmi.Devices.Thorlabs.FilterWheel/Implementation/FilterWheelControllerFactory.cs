using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration;
using Tarmi.Configuration.Devices;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Thorlabs.FilterWheel.Implementation;

public class FilterWheelControllerFactory : IFilterWheelControllerFactory
{
    private readonly ISerialCommunicationFactory _serialCommunicationFactory;
    private readonly ILogger _logger;
    private readonly ILogger _controllerLogger;
    private readonly bool _simulationEnabled;
    private readonly SerialPort _serialPort;

    public FilterWheelControllerFactory(
        ISerialCommunicationFactory serialCommunicationFactory,
        ILogger<FilterWheelControllerFactory> logger,
#pragma warning disable S6672 // Generic logger injection should match enclosing type
        // logger for instancees creation
        ILogger<FilterWheelController> controllerLogger,
#pragma warning restore S6672 // Generic logger injection should match enclosing type
        ApplicationConfig applicationConfig
    )
    {
        _serialCommunicationFactory = serialCommunicationFactory;
        _logger = logger;
        _controllerLogger = controllerLogger;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _serialPort = applicationConfig.Microscope.ThorlabsFilterWheel.Port;
    }

    public IFilterWheelController CreateFilterWheelController()
    {
        _logger.LogInformation("Creating filter wheel controller with {@Configuration}", _serialPort);

        return _simulationEnabled ?
            new SimulatedFilterWheelController(_controllerLogger) :
            new FilterWheelController(_serialCommunicationFactory.CreateSerialCommunication(_serialPort), _controllerLogger);
    }
}
