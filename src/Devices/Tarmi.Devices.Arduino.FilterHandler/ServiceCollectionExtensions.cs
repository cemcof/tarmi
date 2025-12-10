using Tarmi.Devices.Arduino.FilterHandler.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.Devices.Arduino.FilterHandler;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFilterHandlerServices(this IServiceCollection services)
    {
        return services.AddSingleton<IFilterHandlerFactory, FilterHandlerFactory>();
    }
}
