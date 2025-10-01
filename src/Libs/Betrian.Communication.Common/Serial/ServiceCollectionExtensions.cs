using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Betrian.Communication.Common.Serial.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Betrian.Communication.Common.Serial;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSerialCommunicationServices(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton<ISerialCommunicationFactory, SerialCommunicationFactory>();
    }
}
