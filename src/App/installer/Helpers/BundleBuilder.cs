using Tarmi.Installer.Commands;
using WixSharp.Bootstrapper;

namespace Tarmi.Installer.Helpers;

internal static class BundleBuilder
{
    public static async Task<string> Build(IBundleCommandSettingsAccessor settings, string msiFilePath)
    {
        var desktopRuntimeInstallerPath = await GetDesktopRuntime(settings);

        var bundleFilePathWithoutExt = Path.Combine(settings.OutputDirectory.FullName, $"{SharedData.ProductName}-{settings.Version}");
        string iconPath = Path.Combine(settings.PublishDirectory.FullName, "icon", "tarmi.ico");

        var bootStrapper = new Bundle()
        {
            Version = settings.Version,
            UpgradeCode = SharedData.BundleUpgradeCode,
            Name = SharedData.BundleName,
            IconFile = iconPath,
            Manufacturer = SharedData.Manufacturer,
            PreserveDbgFiles = false,
            PreserveTempFiles = false,
            Application = new LicenseBootstrapperApplication()
            {
                Name = SharedData.BundleName,
                LogoFile = iconPath,
                SuppressOptionsUI = true,
                ShowVersion = true
            }
        };

        bootStrapper.Chain.Add(new ExePackage()
        {
            SourceFile = desktopRuntimeInstallerPath,
            Vital = true,
            Permanent = true,
            PerMachine = true,
            InstallArguments = "/install /quiet /norestart",
            Compressed = true
        });

        bootStrapper.Chain.Add(
            new MsiPackage()
            {
                SourceFile = msiFilePath,
                Vital = true,
                Visible = true,
                DisplayInternalUI = false
            }
        );

        return bootStrapper.Build(bundleFilePathWithoutExt);
    }

    private static async ValueTask<string> GetDesktopRuntime(IBundleCommandSettingsAccessor settings)
    {
        if (settings.RuntimeInstaller.Value?.Exists ?? false)
        {
            return settings.RuntimeInstaller.Value.FullName;
        }

        return await RuntimesDownloader.DownloadDesktopRuntime(settings.OutputDirectory.FullName, (short)settings.RuntimeVersion.Value);
    }
}
