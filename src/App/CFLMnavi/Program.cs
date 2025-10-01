using Fluxera.Extensions.Hosting;

namespace Betrian.CflmNavi.App;

internal class Program
{
    [STAThread]
    public static async Task Main(string[] args) =>
        await ApplicationHost.RunAsync<CflmNaviApplicationHost>(args);
}
