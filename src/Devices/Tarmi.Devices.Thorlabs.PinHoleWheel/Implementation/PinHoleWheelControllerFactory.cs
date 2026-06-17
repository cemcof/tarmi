using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Tarmi.Configuration.Devices;
using Microsoft.Extensions.Logging;

namespace Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;

public class PinHoleWheelControllerFactory : IPinHoleWheelControllerFactory
{
    private readonly ISerialCommunicationFactory _serialCommunicationFactory;
    private readonly ILogger _logger;
    private readonly ILogger<PinHoleWheelController> _controllerLogger;
    private readonly bool _simulationEnabled;
    private readonly SerialPort _serialPort;
    private readonly PinHoleWheelAlignments _alignments;

    public PinHoleWheelControllerFactory(
        ISerialCommunicationFactory serialCommunicationFactory,
        ILogger<PinHoleWheelControllerFactory> logger,
#pragma warning disable S6672 // Generic logger injection should match enclosing type
        // logger for instancees creation
        ILogger<PinHoleWheelController> controllerLogger,
#pragma warning restore S6672 // Generic logger injection should match enclosing type
        ApplicationConfig applicationConfig
    )
    {
        _serialCommunicationFactory = serialCommunicationFactory;
        _logger = logger;
        _controllerLogger = controllerLogger;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _serialPort = applicationConfig.Microscope.ThorlabsPinHoleWheel.Port;
        _alignments = applicationConfig.Microscope.ThorlabsPinHoleWheel.PinHoleWheelAlignments;
    }

    public IPinHoleWheelController CreatePinHoleWheelController()
    {
        _logger.LogInformation("Creating pin hole wheel controller with {@Configuration}", _serialPort);

        return _simulationEnabled ?
            new SimulatedPinHoleWheelController(_alignments, _controllerLogger) :
            new PinHoleWheelController(_serialCommunicationFactory.CreateSerialCommunication(_serialPort), _alignments, _controllerLogger);
    }
}
