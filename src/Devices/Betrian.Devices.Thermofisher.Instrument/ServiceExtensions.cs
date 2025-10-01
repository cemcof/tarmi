using Betrian.Devices.Thermofisher.Instrument.ObjectModel;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Betrian.Devices.Thermofisher.Instrument.ObjectModel.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Betrian.Devices.Thermofisher.Instrument;

public static class ServiceExtensions
{
    public static IServiceCollection AddThermofisherInstrumentServices(this IServiceCollection services, bool simulationEnabled)
    {
        if (simulationEnabled)
        {
            return services
                .AddSingleton<IInstrument, Implementation.SimulatedInstrument>();
        }
        else
        {
            return services
                .AddSingleton<IBrickConnector, BrickConnector>()
                .AddSingleton<IXtObjectsCollection, XtObjectsCollection>()
                .AddSingleton<XtConnectionService>()
                .AddHostedService(sp => sp.GetRequiredService<XtConnectionService>())
                .AddSingleton<Chamber>()
                .AddSingleton<Stage>()
                .AddSingleton<ElectronBeam>()
                .AddSingleton<IonBeam>()
                .AddSingleton<SemImageSource>()
                .AddSingleton<IonImageSource>()
                .AddSingleton<IInstrument, Implementation.Instrument>();
        }
    }
}
