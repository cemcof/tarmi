using Betrian.App.Infrastructure.Logging;
using Fluxera.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Betrian.CflmNavi.App;

internal class CflmNaviApplicationHost : WpfApplicationHost<CflmNaviApplicationModule>
{
    protected override ILoggerFactory CreateBootstrapperLoggerFactory(IConfiguration configuration) =>
        new SerilogLoggerFactory(SerilogConfiguration.CreateBootstrapLogger(configuration));

    protected override void ConfigureHostBuilder(IHostBuilder builder)
    {
        base.ConfigureHostBuilder(builder);

        _ = builder
            .ConfigureAppConfiguration((ctx, configurationBuilder) =>
            {
                _ = configurationBuilder
                    .AddJsonFile($"{ctx.HostingEnvironment.ApplicationName}.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"{ctx.HostingEnvironment.ApplicationName}.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);
            })
            .UseDefaultServiceProvider(options => options.ValidateOnBuild = true)
            .ConfigureHostOptions(options =>
            {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = true;
                options.ShutdownTimeout = TimeSpan.FromSeconds(5);
            })
            .UseWpfApplicationLifetime(System.Windows.ShutdownMode.OnMainWindowClose)
            .UseSerilog((hostingContext, loggerConfiguration) =>
                SerilogConfiguration.ConfigureLogging(loggerConfiguration, hostingContext.Configuration),
                    preserveStaticLogger: true,
                    writeToProviders: false
                );
    }

    protected override void ConfigureApplicationHostEvents(ApplicationHostEvents applicationHostEvents)
    {
        base.ConfigureApplicationHostEvents(applicationHostEvents);
        // here subscribe events if needed
    }
}
