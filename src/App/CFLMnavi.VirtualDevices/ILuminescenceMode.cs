using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Thorlabs.Light;
using Betrian.Models;
using CFLMnavi.VirtualDevices.Implementation;
using UnitsNet;

namespace CFLMnavi.VirtualDevices;

public interface ILuminescenceMode : IVirtualDevice, IZStackGrabbingMode, ILuminescenceTubeControllingMode
{
    Task<FilterType> GetModeAsync();
    Task ChangeModeAsync(FilterType mode);
    IObservable<FilterType> ModeChanges {  get; }
    Duration ExposureTime { get; set; }
    RangeDescriptor<Duration> ExposureTimeRange { get; }
    IObservable<LightColor?> CurrentActiveLightColor { get; }
    LightColor? ActiveLightColor { get; }
    Task TurnLightOn(LightColor color, CancellationToken cancellationToken);
    Task TurnLightOff(CancellationToken cancellationToken);
    Ratio Intensity { get; }
    RangeDescriptor<Ratio> IntensityRange { get; }
    Task SetIntensityAsync(Ratio intensity, CancellationToken cancellationToken);
    double Gamma { get; set; }
    RangeDescriptor<double> GammaRange { get; }
    Level Gain { get; set; }
    RangeDescriptor<Level> GainRange { get; }
    BinningSize Binning { get; set; }
    Dictionary<FilterType, LightConfiguration> DefaultLightConfigurations { get; }
}
