using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels.Modes.Confocal;

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

    internal void Initialize(IConfocalMode virtualDevice)
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
    private double _intensity;

    internal void Initialize(IConfocalMode virtualDevice) => Intensity = virtualDevice.Intensity.Percent;
}
