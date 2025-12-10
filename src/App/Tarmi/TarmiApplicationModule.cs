using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Tarmi.App.Infrastructure;
using Tarmi.App.Infrastructure.Options;
using Tarmi.TileSet.ImageSimulator;
using Tarmi.TileSet.ImageSimulator.Abstractions;
using Tarmi.WPF;
using Tarmi.Configuration;
using Fluxera.Extensions.Hosting;
using Fluxera.Extensions.Hosting.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Tarmi.App;

internal class TarmiApplicationModule : ConfigureServicesModule, IPostConfigureApplication
{
    public override void ConfigureServices(IServiceConfigurationContext context)
    {
        var appConfig = ConfigSerialization.LoadApplicationConfig(context.Environment);

        _ = context.Services
            .AddSingleton(appConfig)
            .AddLogging()
            .AddTransient<IWpfApplicationInitializer, WpfApplicationInitializer>()
            .AddSingleton<IMainWindow, Views.MainWindow>()
            .AddServices(context.Configuration, appConfig.Simulation.Enabled)
            .AddViewModels();

#if DEBUG
        _ = context.Services
            .AddTileSetImageSimulator(appConfig.Simulation.Enabled);
#endif // DEBUG

        AddDevicesServices(context.Services, appConfig, appConfig.Simulation.Enabled);

        var telemetryOptions = context.Configuration.GetTelemetryOptions();
        ConfigureTelemetry(context, telemetryOptions);
    }

    private static void AddDevicesServices(IServiceCollection services, ApplicationConfig configuration, bool simulationEnabled)
    {
        _ = services
            .AddVirtualDevices(configuration, simulationEnabled);
    }

    public void PostConfigure(IApplicationInitializationContext context)
    {
        WPFDependencyInjection.ServiceProvider = context.ServiceProvider;

        _ = context.ServiceProvider.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
        {
            RegisterUnhandledExceptionHandlers(context.ServiceProvider.GetRequiredService<ILogger<TarmiApplicationModule>>(), Application.Current.Dispatcher);
        });
    }

    public override void PostConfigureServices(IServiceConfigurationContext context)
    {
        _ = context;
        const string AppCulture = "en-US";
        Thread.CurrentThread.CurrentCulture = new CultureInfo(AppCulture);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(AppCulture);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(AppCulture)));
    }

    private static void RegisterUnhandledExceptionHandlers(ILogger logger, Dispatcher dispatcher)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => logger.Log(args.IsTerminating ? LogLevel.Critical : LogLevel.Error, (Exception)args.ExceptionObject, "Uncaught severe exception");
        TaskScheduler.UnobservedTaskException += (sender, e) => logger.LogWarning(e.Exception, "Unobserved task exception");
        dispatcher.UnhandledException += (sender, e) => logger.LogWarning(e.Exception, "Unhandled Dispatcher exception");
    }

    private static void ConfigureTelemetry(IServiceConfigurationContext context, TelemetryOptions telemetryOptions)
    {
        if (telemetryOptions.TracingEnabled)
        {
            _ = context.Services
                .AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    var uri = new Uri(telemetryOptions.TracingOtlpUri);

                    _ = builder
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(AppDomain.CurrentDomain.FriendlyName)
                        )
                        .SetErrorStatusOnException(true)
                        .SetSampler(new AlwaysOnSampler())
                        .AddSource(AppTelemetry.SourceNames)
                        .AddOtlpExporter(options =>
                        {
                            try
                            {
                                options.Endpoint = uri;
                                options.Protocol = uri.Scheme == "http" ? OtlpExportProtocol.HttpProtobuf : OtlpExportProtocol.Grpc;
                                if (!string.IsNullOrWhiteSpace(telemetryOptions.TracingOtlpHeaders))
                                {
                                    options.Headers = telemetryOptions.TracingOtlpHeaders;
                                }
                            }
                            catch (Exception ex)
                            {
                                context.Logger.LogWarning(ex, "Tracing configuration");
                            }
                        });
                });
        }
    }
}
