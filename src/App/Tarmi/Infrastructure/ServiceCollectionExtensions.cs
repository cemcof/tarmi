﻿using System.Reflection;
using Tarmi.Communication.Common.Serial;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Devices.Thorlabs.Light;
using Tarmi.WPF;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using Tarmi.VirtualDevices.Implementation;
using Tarmi.App.WPF.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.Modes.LM;
using Tarmi.Devices.Thorlabs.PinHoleWheel;
using Tarmi.Devices.Thorlabs.FilterWheel;
using Tarmi.Confocal;

namespace Tarmi.App.Infrastructure;

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
            .AddPinHoleWheelServices()
            .AddFilterWheelServices()
            .AddConfocalServices()
            .AddThermofisherInstrumentServices(simulationEnabled)
            .AddSingleton<ILuminescenceMode, LuminescenceMode>()
            .AddSingleton<IElectronBeamMode, ElectronBeamMode>()
            .AddSingleton<IIonBeamMode, IonBeamMode>()
            .AddSingleton<IConfocalMode, ConfocalMode>()
            .AddSingleton<IProjectManager, ProjectManager>()
            .AddSingleton<IWindowService, WindowService>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IShutdownService, ShutdownService>()
            .AddSingleton<IStartupService, StartupService>()
            .AddSingleton<PersistentImagingSettings>()
            .AddSingleton<ViewModels.Modes.Confocal.PersistentImagingSettings>();
    }
}
