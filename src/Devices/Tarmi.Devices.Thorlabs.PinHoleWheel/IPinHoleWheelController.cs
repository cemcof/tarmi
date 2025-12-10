namespace Tarmi.Devices.Thorlabs.PinHoleWheel;

public interface IPinHoleWheelController : IDisposable
{
    long Position { get; }
    bool PinHoleWheelIsActive { get; }
    int Address { get; }

    Task<bool> IsDeviceActive(CancellationToken cancellationToken = default);
    Task<long> GetCurrentPosition(CancellationToken cancellationToken = default);
    Task<bool> SetPosition(long position, CancellationToken cancellationToken = default);
}
