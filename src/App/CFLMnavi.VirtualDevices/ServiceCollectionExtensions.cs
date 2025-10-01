using Betrian.Devices.Thermofisher.Instrument;
using CFLMnavi.Configuration;
using CFLMnavi.VirtualDevices;
using CFLMnavi.VirtualDevices.Implementation;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVirtualDevices(this IServiceCollection services, ApplicationConfig applicationConfig, bool simulationEnabled)
    {
        return services
            .AddBaslerServices()
            .AddThermofisherInstrumentServices(simulationEnabled)
            .AddSmarActStage(applicationConfig)
            .AddSingleton<IStageNavigation, StageNavigation>()
            .AddSingleton<ILimits, Limits>()
            .AddSingleton<ISafeStageControlling, SafeStageControlling>()
            .AddSingleton<ILuminescenceMode, LuminescenceMode>()
            .AddSingleton<IElectronBeamMode, ElectronBeamMode>()
            .AddSingleton<IIonBeamMode, IonBeamMode>()
            .AddSingleton<IViewerMode, ViewerMode>();
    }
}
