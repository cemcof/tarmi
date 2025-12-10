using Fluxera.Extensions.Hosting;

namespace Tarmi.App;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args) =>
        await ApplicationHost.RunAsync<TarmiNaviApplicationHost>(args);
}
