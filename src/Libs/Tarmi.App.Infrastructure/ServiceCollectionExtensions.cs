using System.Text;
using Tarmi.App.Infrastructure.Options;
using Tarmi.App.Infrastructure.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<AppConfigurationOptions>(configuration.GetSection(nameof(AppConfigurationOptions)));
        _ = services.Configure<LoggingOptions>(configuration.GetSection(nameof(LoggingOptions)));
        _ = services.Configure<TelemetryOptions>(configuration.GetSection(nameof(TelemetryOptions)));

        return services;
    }

    public static IServiceCollection AddFsLockDictionary(this IServiceCollection services)
    {
        return services.AddSingleton<IFsLocksDictionary, FsLocksDictionary>();
    }

    private static void TryEnsureJsonFile(string filePath)
    {
        var folderPath = Path.GetDirectoryName(filePath);
        _ = Directory.CreateDirectory(folderPath!);
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "{\n}\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    public static IServiceCollection ConfigureWritable<T>(
        this IServiceCollection services,
        IConfigurationSection section,
        string fileName
    )
        where T : class, new()
    {
        return services
            .Configure<T>(section)
            .AddSingleton<IWritableOptions<T>>(sp =>
            {
                var sco = sp.GetRequiredService<IOptions<AppConfigurationOptions>>().Value;
                return new JsonWritableOptions<T>(
                    sp.GetRequiredService<IFsLocksDictionary>(),
                    sp.GetRequiredService<IOptions<T>>(),
                    sp.GetRequiredService<IOptionsMonitor<T>>(),
                    (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                    section.Key,
                    Path.Combine(sco.ConfigurationDirectory, fileName)
                );
            });
    }

    public static IServiceCollection ConfigureStateWritable<T>(
        this IServiceCollection services,
        IConfigurationRoot configurationRoot,
        IConfigurationSection section,
        string fileName
    )
        where T : class, new()
    {
        return services
            .Configure<T>(section)
            .AddSingleton<IWritableOptions<T>>(sp =>
            {
                var sco = sp.GetRequiredService<IOptions<AppConfigurationOptions>>().Value;
                var path = Path.Combine(sco.StateDirectory, fileName);
                TryEnsureJsonFile(path);
                return new JsonWritableOptions<T>(
                    sp.GetRequiredService<IFsLocksDictionary>(),
                    sp.GetRequiredService<IOptions<T>>(),
                    sp.GetRequiredService<IOptionsMonitor<T>>(),
                    configurationRoot,
                    section.Key,
                    path
                );
            });
    }
}
