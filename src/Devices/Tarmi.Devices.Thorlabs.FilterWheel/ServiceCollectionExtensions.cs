using Tarmi.Devices.Thorlabs.FilterWheel.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.Devices.Thorlabs.FilterWheel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilterWheelServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IFilterWheelControllerFactory, FilterWheelControllerFactory>();
    }
}
