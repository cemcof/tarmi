using Tarmi.Installer.Commands;
using WixSharp;

namespace Tarmi.Installer.Helpers;

internal static class MsiBuilder
{

    // TODO: desktop shortcut

    public static string Build(IMsiCommandSettingsAccessor settings)
    {
        string msiFileNameWithoutExt = $"{SharedData.ProductName}-{settings.Version}";
        string iconPath = Path.Combine(settings.PublishDirectory.FullName, "icon", "tarmi.ico");

        var project = CreateProject(settings.PublishDirectory.FullName)
            .SetProjectInfo(settings.Version)
            .SetControlPanelInfo(iconPath)
            .SetOutputPath(new DirectoryInfo(settings.OutputDirectory.FullName), msiFileNameWithoutExt)
            .AddFiles()
            .SetMinimalUI()
            .SetDowngradeRules()
            .SetControlPanelInfo(iconPath);

        return project.BuildMsi();
    }

    private static ManagedProject CreateProject(string basePublishDirPath)
    {
        return new ManagedProject(SharedData.ProductName)
        {
            SourceBaseDir = basePublishDirPath,
            InstallerVersion = 500,
            Platform = Platform.x64,
            ReinstallMode = "a",
        };
    }

    private static Project AddFiles(this Project project)
    {
        return project.AddDirectories(
            new Dir(Path.Combine("%ProgramFiles%", SharedData.Manufacturer, SharedData.ProductName),
                new Files("*.*")
                )
            );
    }

    private static Project SetOutputPath(this Project project, DirectoryInfo outputDir, string outputFileName)
    {
        project.OutDir = outputDir?.FullName;
        project.OutFileName = outputFileName;
        return project;
    }

    private static Project AddDirectories(this Project project, params Dir[] directories)
    {
        if (directories.Length > 0)
        {
            project.Dirs = project.Dirs?.Concat(directories).ToArray() ?? directories;
        }
        return project;
    }

    private static Project SetProjectInfo(this Project project, Version version)
    {
        project.GUID = Guid.NewGuid();
        project.UpgradeCode = SharedData.ProductUpgradeCode;
        project.Name = SharedData.ProductName;
        project.Description = SharedData.ProductDescription;
        project.Scope = InstallScope.perMachine;
        project.Version = version;
        return project;
    }

    private static Project SetMinimalUI(this Project project)
    {
        project.UI = WUI.WixUI_Minimal;
        return project;
    }

    public static Project SetDowngradeRules(this Project project)
    {
        project.MajorUpgrade ??= new MajorUpgrade();
        project.MajorUpgrade.AllowDowngrades = true;
        project.MajorUpgrade.Schedule = UpgradeSchedule.afterInstallInitialize;
        project.MajorUpgrade.MigrateFeatures = true;
        project.MajorUpgrade.IgnoreRemoveFailure = true;
        return project;
    }

    private static Project SetControlPanelInfo(this Project project, string productIconFilePath)
    {
        project.ControlPanelInfo = new ProductInfo()
        {
            Name = SharedData.ProductName,
            Manufacturer = SharedData.Manufacturer,
            Readme = null,
            Comments = null,
            Contact = SharedData.ManufacturerContact,
            HelpLink = null,
            UrlInfoAbout = null,
            ProductIcon = productIconFilePath,
            HelpTelephone = null
        };
        return project;
    }
}
