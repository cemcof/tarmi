using Betrian.Devices.Thorlabs.Light.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Betrian.Devices.Thorlabs.Light;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThorlabsLightServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ILightControllerFactory, LightControllerFactory>();
    }
}
