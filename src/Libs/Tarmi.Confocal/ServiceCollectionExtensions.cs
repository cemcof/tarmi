using Tarmi.Confocal.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.Confocal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfocalServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IImageGraberFactory, ImageGraberFactory>()
            .AddSingleton<IConfocalDevice, ConfocalDevice>();
    }
}
