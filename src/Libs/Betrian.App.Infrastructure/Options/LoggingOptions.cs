using Microsoft.Extensions.Logging;

namespace Betrian.App.Infrastructure.Options;

public class LoggingOptions
{
    public LogLevel DefaultMinimumLevel { get; set; } = LogLevel.Information;

    public Dictionary<string, LogLevel> OverrideMinimumLevel { get; set; } = new Dictionary<string, LogLevel>
    {
        ["Microsoft"] = LogLevel.Warning,
        ["System"] = LogLevel.Warning,
        ["Microsoft.Hosting.Lifetime"] = LogLevel.Information
    };

    public string LoggingDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Betrian",
            AppDomain.CurrentDomain.FriendlyName,
            "logs"
        );

    public SeqOptions SeqOptions { get; set; } = new();
}
