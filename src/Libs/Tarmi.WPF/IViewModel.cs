namespace Tarmi.WPF;

public interface IViewModel
{
    bool IsInitialized { get; }

    Task DeInitialize();
    Task Initialize();
}
