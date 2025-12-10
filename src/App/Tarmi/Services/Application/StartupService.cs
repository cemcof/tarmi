using Tarmi.VirtualDevices;

namespace Tarmi.App.Services.Application;

public interface IStartupService
{
    Task PerformPreStartProcedure(CancellationToken cancellationToken);
}

public class StartupService : IStartupService
{
    private readonly ILuminescenceMode _luminescenceMode;

    public StartupService(ILuminescenceMode luminescenceMode)
    {
        _luminescenceMode = luminescenceMode;
    }

    public Task PerformPreStartProcedure(CancellationToken cancellationToken)
    {
        return _luminescenceMode.RetractAsync(cancellationToken);
    }
}
