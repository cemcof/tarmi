using System.Reactive.Disposables;
using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration;
using Tarmi.Configuration.Devices;
using Ivi.Visa.Interop;
using Microsoft.Extensions.Logging;
using Thorlabs.DC4100_64.Interop;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;

internal class LightControllerFactory : ILightControllerFactory
{
    private readonly ISerialCommunicationFactory _serialCommunicationFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly bool _simulationEnabled;
    private readonly Thorlabs4100 _thorlabsConfig;

    public LightControllerFactory(
        ISerialCommunicationFactory serialCommunicationFactory,
        ILoggerFactory loggerFactory,
        ILogger<LightControllerFactory> logger,
        ApplicationConfig applicationConfig
    )
    {
        _serialCommunicationFactory = serialCommunicationFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _thorlabsConfig = applicationConfig.Microscope.Thorlabs4100;
    }

    public ILightController CreateLightController()
    {
        _logger.LogInformation("Creating light controller with {@Configuration}", _thorlabsConfig.Port);
        bool useThorlabsDriver = true;
        if (_simulationEnabled)
        {
            return new SimulatedLightController(_loggerFactory.CreateLogger<SimulatedLightController>());
        }
        else if (useThorlabsDriver)
        {
            return new ThorlabsLightController(InitializeThorlabsController(), _loggerFactory.CreateLogger<ThorlabsLightController>());
        }
        return new LightController(_serialCommunicationFactory, _thorlabsConfig.Port,  _loggerFactory.CreateLogger<LightController>());
    }

    private TLDC4100 InitializeThorlabsController()
    {
        try
        {
            _logger.LogInformation("Initializing resource manager.");
            IResourceManager3 resourceManager = new ResourceManager();

            _logger.LogInformation("Detecting devices.");
            var resources = resourceManager
                .FindRsrc("ASRL?*");

            _logger.LogInformation("Looking for the device DC4100.");
            var resourceName = resources
                .First(resourceName =>
                {
                    try
                    {
                        _logger.LogInformation("Opening device {Name}.", resourceName);
                        IVisaSession session = resourceManager.Open(resourceName);
                        using var disposable = Disposable.Create(() =>
                        {
                            _logger.LogInformation("Closing device {Name}", resourceName);
                            session.Close();
                        });
                        return session.HardwareInterfaceName.Contains("DC4100", StringComparison.InvariantCulture)
                            || session.HardwareInterfaceName.Contains("DC4104", StringComparison.InvariantCulture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "");
                        return false;
                    }
                });
            _logger.LogInformation("Initializing Thorlabs driver using {Name} device", resourceName);
            var tldc4100 = new TLDC4100(resourceName, false, false);
            _logger.LogInformation("Driver initialization succesful.");
            return tldc4100;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured during initializization!");
            throw;
        }
    }
}
