namespace Tarmi.Devices.Thorlabs.FilterWheel;

public interface IFilterWheelController : IDisposable
{
    double FilterColor { get; }
    bool FilterWheelIsActive { get; }

    Task<bool> IsDeviceActive(CancellationToken cancellationToken = default);

    Task<double> GetCurrentFilterColor(CancellationToken cancellationToken = default);

    Task<bool> SetFilterColor(double filterColor, CancellationToken cancellationToken = default);
}
