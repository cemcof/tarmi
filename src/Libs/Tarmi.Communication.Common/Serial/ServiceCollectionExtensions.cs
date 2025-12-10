using Microsoft.Extensions.DependencyInjection;
using Tarmi.Communication.Common.Serial.Implementation;

namespace Tarmi.Communication.Common.Serial;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSerialCommunicationServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISerialCommunicationFactory, SerialCommunicationFactory>();
    }
}
