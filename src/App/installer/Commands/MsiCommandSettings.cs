using System.ComponentModel;
using CFlMnavi.Installer.Validators;
using Spectre.Console.Cli;

namespace CFlMnavi.Installer.Commands;

internal interface IMsiCommandSettingsAccessor
{
    DirectoryInfo PublishDirectory { get; }
    Version Version { get; }
    DirectoryInfo OutputDirectory { get; }
}

internal class MsiCommandSettings : CommandSettings, IMsiCommandSettingsAccessor
{
    [CommandOption("-i|--input-dir <APP_PUBLISH_DIRECTORY>")]
    [Description("The app publishing directory path.")]
    public DirectoryInfo PublishDirectory { get; set; }

    [CommandOption("-v|--version <VERSION>")]
    [Description("The app version in dotted format up to four segments, e.g. '1.1.1'.")]
    [VersionValidator("Invalid version format.")]
    [TypeConverter(typeof(VersionConverter))]
    public Version Version { get; set; }

    [CommandOption("-o|--out <OUTPUT_DIRECTORY>")]
    [Description("The output directory path.")]
    public DirectoryInfo OutputDirectory { get; set; }
}
