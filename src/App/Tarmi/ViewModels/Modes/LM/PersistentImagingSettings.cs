using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels.Modes.LM;
public partial class PersistentImagingSettings : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImagingSettings))]
    private FilterType _filterType;

    private readonly Dictionary<FilterType, ImagingSettings> _imagingSettings = [];

    public PersistentImagingSettings(ILoggerFactory loggerFactory)
    {
        foreach (var filter in Enum.GetValues<FilterType>())
        {
            ImagingSettings imagingSettings = new();
            _imagingSettings.Add(filter, imagingSettings);
        }
    }

    public ImagingSettings ImagingSettings => _imagingSettings[FilterType];

    internal void Initialize(ILuminescenceMode virtualDevice)
    {
        foreach (var item in _imagingSettings)
        {
            item.Value.Initialize(virtualDevice);
        }
    }
}

public partial class ImagingSettings : ObservableObject
{
    [ObservableProperty]
    private double _exposure;

    [ObservableProperty]
    private double _intensity;

    [ObservableProperty]
    private double _gamma;

    [ObservableProperty]
    private double _gain;

    internal void Initialize(ILuminescenceMode virtualDevice)
    {
        Exposure = virtualDevice.ExposureTime.Microseconds;
        Intensity = virtualDevice.Intensity.Percent;
        Gamma = virtualDevice.Gamma;
        Gain = virtualDevice.Gain.Decibels;
    }
}
