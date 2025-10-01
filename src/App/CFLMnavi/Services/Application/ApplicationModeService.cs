using System.Reactive.Subjects;
using Betrian.CflmNavi.App.Infrastructure;
using CFLMnavi.WPF;

namespace Betrian.CflmNavi.App.Services.Application;

public interface IApplicationModeService : IService
{
    ISubject<ApplicationMode> Mode { get; }
    ApplicationMode GetCurrentMode();
}

internal class ApplicationModeService : IApplicationModeService
{
    private readonly BehaviorSubject<ApplicationMode> _modeSubject = new(ApplicationMode.Viewer);

    public ISubject<ApplicationMode> Mode => _modeSubject;

    public ApplicationMode GetCurrentMode() => _modeSubject.Value;
}
