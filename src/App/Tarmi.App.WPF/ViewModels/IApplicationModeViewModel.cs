using Tarmi.App.WPF;

namespace Tarmi.App.WPF.ViewModels;

public interface IApplicationModeViewModel
{
    bool IsInitialized { get; }

    Task DeInitialize(ApplicationMode nextMode);
    Task Initialize(ApplicationMode prevMode);
}
