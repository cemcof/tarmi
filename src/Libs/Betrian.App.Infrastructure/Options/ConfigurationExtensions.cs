using Microsoft.Extensions.Configuration;

namespace Betrian.App.Infrastructure.Options;

public static class ConfigurationExtensions
{
    public static AppConfigurationOptions GetServiceConfigurationOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(nameof(AppConfigurationOptions)).Get<AppConfigurationOptions>() ?? new AppConfigurationOptions();
    }

    public static LoggingOptions GetLoggingOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(nameof(LoggingOptions)).Get<LoggingOptions>() ?? new LoggingOptions();
    }

    public static TelemetryOptions GetTelemetryOptions(this IConfiguration configuration)
    {
        return configuration.GetSection(nameof(TelemetryOptions)).Get<TelemetryOptions>() ?? new TelemetryOptions();
    }
}

