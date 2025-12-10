using Tarmi.Devices.SmarAct.Stage;
using Tarmi.Devices.SmarAct.Stage.Implementation;
using Tarmi.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmarActStage(this IServiceCollection services, ApplicationConfig applicationConfiguration)
    {
        if (applicationConfiguration.Simulation.Enabled)
        {
            return services
                .AddSingleton<ILinearStage, SimulatedLinearStage>();
        }

        return services
            .AddSingleton<IMcs2CommunicationFactory, Mcs2CommunicationFactory>()
            .AddSingleton(sp => sp.GetRequiredService<IMcs2CommunicationFactory>().CreateCommunication(applicationConfiguration))
            .AddSingleton<ILinearStage>(sp =>
                new LinearStage(
                    sp.GetRequiredService<IMcs2Communication>(),
                    applicationConfiguration,
                    sp.GetRequiredService<ILogger<LinearStage>>()
                )
            );
    }
}
