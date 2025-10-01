using System.ComponentModel;
using CFlMnavi.Installer.Validators;
using Spectre.Console.Cli;

namespace CFlMnavi.Installer.Commands;

internal interface IBundleCommandSettingsAccessor : IMsiCommandSettingsAccessor
{
    FlagValue<int?> RuntimeVersion { get; }
    FlagValue<FileInfo> RuntimeInstaller { get; }
}

internal class BundleCommandSettings : CommandSettings, IBundleCommandSettingsAccessor
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

    [CommandOption("-r|--runtime-version [RUNTIME_VERSION]")]
    [Description("The .Net runtimes major version to download for including in app bundle.")]
    [RuntimeVersionValidator("Invalid .Net runtime major version provided.")]
    public FlagValue<int?> RuntimeVersion { get; set; } = null;

    [CommandOption("-d|--desktop-runtime-file [DESKTOP_RUNTIME_INSTALLER_PATH]")]
    [Description("The .Net Desktop runtime installer file path. If provided it's not downloaded regardless on the provided major version.")]
    public FlagValue<FileInfo> RuntimeInstaller { get; set; }
}
