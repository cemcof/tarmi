using Tarmi.Devices.Basler.Camera;
using Tarmi.Devices.Basler.Camera.Implementation;
using Tarmi.Devices.Basler.Camera.Internal;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaslerServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<ICameraDiscoveryService, ICameraInfoLocator, CameraDiscoveryService>()
            .AddSingleton<IImageGraberFactory, ImageGraberFactory>();
    }
}
