using System.Reflection;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.CflmNavi.App.ViewModels.Modes.LM;
using Betrian.Communication.Common.Serial;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Thermofisher.Instrument;
using Betrian.Devices.Thorlabs.Light;
using Betrian.WPF;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CFLMnavi.VirtualDevices.Implementation;
using CFLMnavi.WPF.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Betrian.CflmNavi.App.Infrastructure;

internal static class ServiceCollectionExtensions
{
    public static readonly Type[] AssemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

    internal static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        IEnumerable<Type> viewModels = AssemblyTypes
            .Where(
                t => t.BaseType != null &&
                (t.IsAssignableTo(typeof(ViewModelBase)) || t.IsAssignableTo(typeof(ApplicationModeViewModelBase))) &&
                !t.IsAbstract);
        foreach (Type viewModel in viewModels)
        {
            _ = services.AddSingleton(viewModel); // TODO: use scoped?
        }
        return services;
    }

    internal static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration, bool simulationEnabled)
    {
        return services
            .AddApplicationOptions(configuration)
            .AddFsLockDictionary()
            .AddSingleton<IApplicationModeService, ApplicationModeService>()
            .AddBaslerServices()
            .AddSerialCommunicationServices()
            .AddThorlabsLightServices()
            .AddFilterHandlerServices()
            .AddThermofisherInstrumentServices(simulationEnabled)
            .AddSingleton<ILuminescenceMode, LuminescenceMode>()
            .AddSingleton<IElectronBeamMode, ElectronBeamMode>()
            .AddSingleton<IIonBeamMode, IonBeamMode>()
            .AddSingleton<IProjectManager, ProjectManager>()
            .AddSingleton<IWindowService, WindowService>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IShutdownService, ShutdownService>()
            .AddSingleton<IStartupService, StartupService>()
            .AddSingleton<PersistentImagingSettings>();
    }
}
