using Tarmi.Installer.Commands;
using Spectre.Console.Cli;

namespace Tarmi.Installer;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        var app = new CommandApp();
        app.Configure(config =>
        {
            _ = config
                .PropagateExceptions()
                .ValidateExamples();

            _ = config
                .AddBranch<MsiCommandSettings>("msi", check =>
                {
                    check.SetDescription("Build MSI package");
                    _ = check
                        .AddCommand<MsiCommand>("build")
                        .WithExample("msi", "build", "-v=0.7.26", """-i="c:\app\publish\directory" """, """-o="c:\app\output\directory" """);
                });

            _ = config
                .AddBranch<BundleCommandSettings>("bundle", check =>
                {
                    check.SetDescription("Build MSI package and bundle with runtime");
                    _ = check
                        .AddCommand<BundleCommand>("build")
                        .WithExample("bundle", "build", "-v=0.7.26", "-r=8", """-i="c:\app\publish\directory" """, """-o="c:\app\output\directory" """);
                });
        });

        return app.RunAsync(args);
    }
}
