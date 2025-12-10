using System.Runtime.Serialization;
using Microsoft.Extensions.Hosting;
using Tarmi.Models.Serialization;

namespace Tarmi.Configuration;

public static class ConfigSerialization
{
    public const string ApplicationDevConfigFileName = "config.dev.xml";
    public const string ApplicationProdConfigFileName = "config.prod.xml";
    public const string HoldersConfigFileName = "holders.xml";

    public static ApplicationConfig LoadApplicationConfig(IHostEnvironment hostEnvironment)
    {
        var filename = hostEnvironment.IsDevelopment() ? ApplicationDevConfigFileName : ApplicationProdConfigFileName;
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(directory, filename);
        var serializer = new DataContractSerializer(typeof(ApplicationConfig));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var config = serializer.ReadObject(stream) as ApplicationConfig;
        return config!;
    }

    public static void SaveApplicationConfig(ApplicationConfig config, string fileName)
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(directory, fileName);
        using var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        Helpers.Save(config, fileStream);
    }

    public static HoldersConfig LoadHoldersConfig()
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(directory, HoldersConfigFileName);
        var serializer = new DataContractSerializer(typeof(HoldersConfig));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var config = serializer.ReadObject(stream) as HoldersConfig;
        return config ?? new HoldersConfig();
    }

    public static void SaveHoldersConfig(HoldersConfig config, string fileName)
    {
        var directory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(directory, fileName);
        using var fileStream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        Helpers.Save(config, fileStream);
    }
}
