using Betrian.Devices.Arduino.FilterHandler.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Betrian.Devices.Arduino.FilterHandler;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilterHandlerServices(this IServiceCollection services)
    {
        return services.AddSingleton<IFilterHandlerFactory, FilterHandlerFactory>();
    }
}
