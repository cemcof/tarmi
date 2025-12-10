using Tarmi.App.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Templates;

namespace Tarmi.App.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static LoggerConfiguration ConfigureLogging(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggerConfiguration);
        ArgumentNullException.ThrowIfNull(configuration);

        var loggingOptions = configuration.GetLoggingOptions();

        var logDirPath = GetLogDirectoryPath(loggingOptions);
        var level = GetLogEventLevel(loggingOptions.DefaultMinimumLevel);

        return loggerConfiguration
            .SetupLogLevels(logLevelSwitch: level, loggingOptions)
            .AddEnrichers()
            .AddAsyncFile(logDirPath, logLevelSwitch: level)
            .AddAsyncSeq(logLevelSwitch: level, loggingOptions.SeqOptions);
    }

    public static Serilog.ILogger CreateBootstrapLogger(IConfiguration configuration)
    {
        var loggingOptions = configuration?.GetLoggingOptions() ?? new LoggingOptions();
        var logDirPath = GetLogDirectoryPath(loggingOptions);
        var level = GetLogEventLevel(LogLevel.Debug);

        return new LoggerConfiguration()
            .SetupLogLevels(logLevelSwitch: level, new LoggingOptions())
            .AddEnrichers()
            .AddAsyncFile(logDirPath, logLevelSwitch: level, logFileSuffix: "bootstrap")
            .AddAsyncSeq(logLevelSwitch: level, loggingOptions.SeqOptions)
            .CreateBootstrapLogger();
    }

    private static string GetLogDirectoryPath(LoggingOptions loggingOptions)
    {
        try
        {
            return Path.GetFullPath(loggingOptions.LoggingDirectory);
        }
        catch
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Betrian", AppDomain.CurrentDomain.FriendlyName, "logs");
        }
    }

    private static LogEventLevel GetLogEventLevel(LogLevel msLogLevel)
    {
        return msLogLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Fatal
        };
    }

    private static LoggerConfiguration AddAsyncFile(this LoggerConfiguration loggerConfiguration, string logDirPath, LogEventLevel logLevelSwitch = LogEventLevel.Information, string logFileSuffix = "")
    {
        var path = Path.Combine(
            logDirPath,
            AppDomain.CurrentDomain.FriendlyName + (logFileSuffix is { Length: > 0 } ? "-" : "") + logFileSuffix + "-.log"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory);

        return loggerConfiguration
            .WriteTo.Logger(logger =>
                logger
                    .WriteTo.Async(configure =>
                        configure.File(
                            path: path,
                            restrictedToMinimumLevel: logLevelSwitch,
                            rollingInterval: RollingInterval.Day,
                            retainedFileTimeLimit: TimeSpan.FromDays(31),
                            formatter: new ExpressionTemplate("{@t:yyyy-MM-ddTHH:mm:ss.fffffffK} [{@l:u3} {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n{@x}")
                        )
                    )
                    .WriteTo.Async(configure =>
                        configure.File(
                            path: Path.ChangeExtension(path, ".jlog"),
                            restrictedToMinimumLevel: logLevelSwitch,
                            rollingInterval: RollingInterval.Day,
                            retainedFileTimeLimit: TimeSpan.FromDays(31),
                            formatter: new CompactJsonFormatter()
                        )
                    )
            );
    }

    private static LoggerConfiguration AddAsyncSeq(this LoggerConfiguration loggerConfiguration, LogEventLevel logLevelSwitch, SeqOptions seqOptions)
    {
        if (seqOptions.Enabled)
        {
            return loggerConfiguration.WriteTo.Async(configure =>
                configure.Seq(
                    serverUrl: seqOptions.Uri.ToString(),
                    restrictedToMinimumLevel: logLevelSwitch,
                    apiKey: seqOptions.ApiKey
                )
            );
        }

        return loggerConfiguration;
    }

    private static LoggerConfiguration AddEnrichers(this LoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithDemystifiedStackTraces()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            .Enrich.WithAssemblyName()
            .Enrich.WithAssemblyVersion()
            .Enrich.WithOsInfo()
            .Enrich.WithFrameworkVersion();
    }

    public static LoggerConfiguration SetupLogLevels(this LoggerConfiguration configuration, LogEventLevel logLevelSwitch, LoggingOptions loggingOptions)
    {
        configuration.MinimumLevel.Is(logLevelSwitch);

        foreach (var kv in loggingOptions.OverrideMinimumLevel)
        {
            configuration.MinimumLevel.Override(kv.Key, GetLogEventLevel(kv.Value));
        }

        return configuration;
    }
}
