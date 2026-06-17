using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thorlabs.Light;
using Tarmi.Models;
using Tarmi.VirtualDevices.Implementation;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface ILuminescenceMode : IVirtualDevice, IZStackGrabbingMode
{
    IObservable<FilterType> ModeChanges {  get; }
    Duration ExposureTime { get; set; }
    RangeDescriptor<Duration> ExposureTimeRange { get; }
    IObservable<LightColor?> CurrentSelectedLightColor { get; }
    LightColor? SelectedLightColor { get; }
    Ratio Intensity { get; }
    RangeDescriptor<Ratio> IntensityRange { get; }
    double Gamma { get; set; }
    RangeDescriptor<double> GammaRange { get; }
    Level Gain { get; set; }
    RangeDescriptor<Level> GainRange { get; }
    BinningSize Binning { get; set; }
    Dictionary<FilterType, LightConfiguration> DefaultLightConfigurations { get; }
    IObservable<bool> CurrentIsLightActive { get; }

    Task<FilterType> GetModeAsync();
    Task ChangeModeAsync(FilterType mode);

    Task SelectLightAsync(LightColor? color, CancellationToken cancellationToken);
    Task SetIntensityAsync(Ratio intensity, CancellationToken cancellationToken);
}
