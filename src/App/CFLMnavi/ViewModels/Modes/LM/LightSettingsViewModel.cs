using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Thorlabs.Light;
using Betrian.Models;
using Betrian.Models.Serialization;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes.LM;

[DataContract(Namespace = Helpers.AppNamespace)]
public class LightSettingsSerializable
{
    [DataMember]
    public List<LightColorSettingsSerializable> FluorescenceSettings { get; init; } = [];

    [DataMember]
    public List<LightColorSettingsSerializable> ReflectionSettings { get; init; } = [];
}

[DataContract(Namespace = Helpers.AppNamespace)]
public class LightColorSettingsSerializable
{
    [DataMember]
    public LightColor Color { get; init; }

    [DataMember]
    public ImageSettingsSerializable ImageSettings { get; init; } = new();
}

[DataContract(Namespace = Helpers.AppNamespace)]
public class ImageSettingsSerializable
{
    [DataMember]
    public double Exposure { get; init; }

    [DataMember]
    public double Intensity { get; init; }
}

public partial class LightSettingsViewModel : ObservableObject, IDisposable
{
    private readonly ILogger _logger;
    private readonly ILuminescenceMode _virtualDevice;
    private readonly BehaviorSubject<bool> _isSelectedChanged = new(true);
    private readonly Dictionary<FilterType, ImagingSettings> _imagingSettings = [];
    private readonly IDisposable _filterChangesSubscription;
    private FilterType _activeFilter;
    private readonly Subject<(LightColor, double)> _exposureChanges = new();
    private readonly Subject<(LightColor, double)> _intensityChanges = new();

    public LightColor Color { get; }
    public IObservable<bool> IsSelectedChanged => _isSelectedChanged;
    public IObservable<(LightColor, double)> ExposureChanges => _exposureChanges.AsObservable().DistinctUntilChanged();
    public IObservable<(LightColor, double)> IntensityChanges => _intensityChanges.AsObservable().DistinctUntilChanged();
    public ImagingSettings ImagingSettings => _imagingSettings[_activeFilter];

    public LightSettingsViewModel(ILuminescenceMode virtualDevice, LightColor color, ILogger<LightSettingsViewModel> logger)
    {
        _virtualDevice = virtualDevice;
        Color = color;
        _logger = logger;
        _filterChangesSubscription = _virtualDevice.ModeChanges.Subscribe(HandleFilterChanged);
        InitImagingSettings();
        // TODO: Camera does not allow reading limits before opening.
    }

    public ImagingSettings GetImagingSettingsForFilter(FilterType filter) => _imagingSettings[filter];

    private void InitImagingSettings()
    {
        foreach (var filter in Enum.GetValues<FilterType>())
        {
            ImagingSettings imagingSettings = new();
            _imagingSettings.Add(filter, imagingSettings);
        }
        foreach (var setting in _imagingSettings)
        {
            setting.Value.Intensity = _virtualDevice.DefaultLightConfigurations[setting.Key].Intensity.Percent;
            setting.Value.Exposure = _virtualDevice.DefaultLightConfigurations[setting.Key].Exposure.Microseconds;
        }
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _logger.Swallow(() => _isSelectedChanged.OnNext(value));
    }

    private void HandleFilterChanged(FilterType filter)
    {
        _activeFilter = filter;
        OnPropertyChanged(nameof(ImagingSettings));
    }

    [ObservableProperty]
    private bool _isSelected;

    public double IntensityStep { get; } = 1;

    public double ExposureStep { get; } = 10;

    public RangeDescriptor<Ratio> IntensityRange => _virtualDevice.IntensityRange;

    public RangeDescriptor<Duration> ExposureTimeRange => _virtualDevice.ExposureTimeRange;

    [RelayCommand]
    public void NotifyIntensityUpdate() => _intensityChanges.OnNext((Color, ImagingSettings.Intensity));

    [RelayCommand]
    public void NotifyExposureUpdate() => _exposureChanges.OnNext((Color, ImagingSettings.Exposure));

    public void Dispose()
    {
        _filterChangesSubscription.Dispose();
        GC.SuppressFinalize(this);
    }
}
