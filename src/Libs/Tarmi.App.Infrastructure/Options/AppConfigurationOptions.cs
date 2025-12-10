namespace Tarmi.App.Infrastructure.Options;

public class AppConfigurationOptions
{
    public string ConfigurationDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Betrian",
            AppDomain.CurrentDomain.FriendlyName,
            "configuration"
        );

    public string StateDirectory { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Betrian",
            AppDomain.CurrentDomain.FriendlyName,
            "state"
        );
}
