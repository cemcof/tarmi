using Tarmi.Configuration.Devices;
using Tarmi.Confocal;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Models;
using Tarmi.VirtualDevices.Implementation;
using UnitsNet;

namespace Tarmi.VirtualDevices;

public interface IConfocalMode : IVirtualDevice, IZStackGrabbingMode
{
    Task<FilterType> GetModeAsync();
    Task ChangeModeAsync(FilterType mode);
    IObservable<FilterType> ModeChanges { get; }
    //IObservable<LightColor?> CurrentActiveLightColor { get; }
    ConfocalLight LaserColor { get; set; }
    Ratio Intensity { get; set; }
    RangeDescriptor<Ratio> IntensityRange { get; }
    //Task SetIntensityAsync(Ratio intensity, CancellationToken cancellationToken);
    Length FieldWidth { get; set; }
    Length FieldHeight { get; set; }
    string Resolution { get; set; }
    Duration Dwell { get; set; }
    IEnumerable<Duration> DwellRanges { get; }
    Length PinHolePosition { get; }
    Length PinHoleWheelPosition { get; set; }
    Task SetPinHolePositionAsync(Length pinhole, CancellationToken cancellationToken);
    Length FilterWheelColor { get; }
    Task SetFilterWheelColorAsync(Length laserColor, CancellationToken cancellationToken);
    void FieldSelection();
    Level Gain { get; set; }
    RangeDescriptor<Level> GainRange { get; }
    //RangeDescriptor<ElectricPotential> ADCRange { get; }
    Dictionary<FilterType, ConfocalConfiguration> DefaultLightConfigurations { get; }
    Length PixelSize { get; set; }
    IEnumerable<Length> PixelSizes { get; }
    ElectricPotential ADC { get; set; }
    IEnumerable<ElectricPotential> ADCRanges { get; }
    IEnumerable<Length> PinHoleSizes { get; }
    IList<ConfocalLight> ConfocalLights { get; }
    Task SetComponentsBeforeGrabbing();
    IConfocalDevice ConfocalData { get; }
}
