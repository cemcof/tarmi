namespace Betrian.WPF;

public interface IViewModel
{
    bool IsInitialized { get; }

    Task DeInitialize();
    Task Initialize();
}
