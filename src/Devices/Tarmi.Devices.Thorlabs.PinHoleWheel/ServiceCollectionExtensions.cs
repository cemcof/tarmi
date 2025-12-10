using Tarmi.Devices.Thorlabs.PinHoleWheel.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.Devices.Thorlabs.PinHoleWheel;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPinHoleWheelServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<IPinHoleWheelControllerFactory, PinHoleWheelControllerFactory>();
    }
}
