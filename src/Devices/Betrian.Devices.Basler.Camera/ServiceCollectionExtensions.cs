using Betrian.Devices.Basler.Camera;
using Betrian.Devices.Basler.Camera.Implementation;
using Betrian.Devices.Basler.Camera.Internal;

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
