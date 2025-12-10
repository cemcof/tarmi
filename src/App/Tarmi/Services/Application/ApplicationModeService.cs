﻿using System.Reactive.Subjects;
using Tarmi.App.Infrastructure;
using Tarmi.App.WPF;

namespace Tarmi.App.Services.Application;

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
