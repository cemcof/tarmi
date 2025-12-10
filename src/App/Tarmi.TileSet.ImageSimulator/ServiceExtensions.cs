using Tarmi.TileSet.ImageSimulator.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.TileSet.ImageSimulator;

public static class ServiceExtensions
{
    public static IServiceCollection AddTileSetImageSimulator(this IServiceCollection services, bool simulationEnabled)
    {
        if (simulationEnabled)
        {
            return services
                .AddSingleton<ITileSetImageSimulator, TileSetSimulator>();
        }
        return services;
    }
}
