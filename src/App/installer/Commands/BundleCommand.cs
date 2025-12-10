using Tarmi.Installer.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Tarmi.Installer.Commands;

internal sealed class BundleCommand : AsyncCommand<BundleCommandSettings>
{
    private readonly MsiCommand _msiCommand = new();

    public override ValidationResult Validate(CommandContext context, BundleCommandSettings settings)
    {
        if (settings.RuntimeVersion is null && settings.RuntimeInstaller is null)
        {
            return ValidationResult.Error("Either runtime version or runtime installer path must be provided.");
        }

        if (settings.RuntimeInstaller.Value?.Exists ?? false || settings.RuntimeVersion.Value is not null)
        {
            return ValidationResult.Success();
        }

        return ValidationResult.Error("Runtime installer file not found.");
    }

    public override async Task<int> ExecuteAsync(CommandContext context, BundleCommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            _ = await BuildBundle(settings!);
            return 0;

        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return -1;
        }
    }

    public async Task<string> BuildBundle(BundleCommandSettings settings)
    {
        var msiFilePath = _msiCommand.BuildMsi(settings);
        return await BundleBuilder.Build(settings, msiFilePath);
    }
}
