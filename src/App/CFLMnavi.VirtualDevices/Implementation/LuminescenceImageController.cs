using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Devices.Basler.Camera;
using Betrian.Devices.Thorlabs.Light;
using CFLMnavi.Configuration;
using UnitsNet;

namespace CFLMnavi.VirtualDevices.Implementation;

public class LuminescenceImageController // TODO: a better name?
{
    private readonly ILightController _lightController;
    private readonly IFilterHandler _filterHandler;
    private readonly IImageGrabber _imageGrabber;
    private readonly Subject<FilterType> _currentFilter = new();
    private FilterType CurrentFilter => _filterHandler.FilterPosition;
    private readonly Dictionary<FilterType, LightConfiguration> _filterConfigurations = [];
    private readonly Duration _filterSwitchDelay;

    public Dictionary<FilterType, LightConfiguration> DefaultLightConfigurations { get; } = new Dictionary<FilterType, LightConfiguration>()
    {
        { FilterType.Reflection, LightConfiguration.DefaultReflection },
        { FilterType.Fluorescence, LightConfiguration.DefaultFluorescence }
    };
    public IObservable<FilterType> CurrentFilterChanges => _currentFilter.AsObservable().DistinctUntilChanged();

    public LuminescenceImageController(ILightController lightController, IFilterHandler filterHandler, IImageGrabber imageGrabber, ApplicationConfig applicationConfig)
    {
        _lightController = lightController;
        _filterHandler = filterHandler;
        _imageGrabber = imageGrabber;
        _filterSwitchDelay = applicationConfig.UserPreferences.LuminescenceFilterSwitchDelay;
        InitFilterConfigurations();
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await SwitchFilter(FilterType.Reflection, cancellationToken);
        await _lightController.SetBrightnessAsync(_filterConfigurations[CurrentFilter].Intensity, cancellationToken);
        _imageGrabber.ExposureTime = _filterConfigurations[CurrentFilter].Exposure;
    }

    public async Task SetIntensity(Ratio intensity, CancellationToken cancellationToken)
    {
        await _lightController.SetBrightnessAsync(intensity, cancellationToken);
        _filterConfigurations[CurrentFilter].Intensity = _lightController.Brightness;
    }

    public void SetExposure(Duration exposure)
    {
        _imageGrabber.ExposureTime = exposure;
        _filterConfigurations[CurrentFilter].Exposure = _imageGrabber.ExposureTime;
    }

    public Task SwitchFilter(FilterType filter, CancellationToken cancellationToken)
    {
        return filter != CurrentFilter
            ? filter switch
            {
                FilterType.Reflection => SwitchToReflection(cancellationToken),
                FilterType.Fluorescence => SwitchToFluorescence(cancellationToken),
                _ => throw new InvalidOperationException($"Unknown filter type {filter}")
            }
            : Task.CompletedTask;
    }

    private async Task SwitchToReflection(CancellationToken cancellationToken)
    {
        _imageGrabber.ExposureTime = _filterConfigurations[FilterType.Reflection].Exposure;
        await _lightController.SetBrightnessAsync(_filterConfigurations[FilterType.Reflection].Intensity, cancellationToken);
        _ = await _filterHandler.SwitchFilterAsync(FilterType.Reflection, cancellationToken);
        await Task.Delay(_filterSwitchDelay.ToTimeSpan(), cancellationToken);
        _currentFilter.OnNext(FilterType.Reflection);
    }

    private async Task SwitchToFluorescence(CancellationToken cancellationToken)
    {
        _ = await _filterHandler.SwitchFilterAsync(FilterType.Fluorescence, cancellationToken);
        await Task.Delay(_filterSwitchDelay.ToTimeSpan(), cancellationToken);
        _currentFilter.OnNext(FilterType.Fluorescence);
        await _lightController.SetBrightnessAsync(_filterConfigurations[FilterType.Fluorescence].Intensity, cancellationToken);
        _imageGrabber.ExposureTime = _filterConfigurations[FilterType.Fluorescence].Exposure;
    }

    private void InitFilterConfigurations()
    {
        _filterConfigurations.Add(FilterType.Fluorescence, LightConfiguration.DefaultFluorescence);
        _filterConfigurations.Add(FilterType.Reflection, LightConfiguration.DefaultReflection);
    }
}

public class LightConfiguration
{
    public Duration Exposure { get; set; }
    public Ratio Intensity { get; set; }

    public static LightConfiguration DefaultReflection => new() { Exposure = Duration.FromMicroseconds(2000), Intensity = Ratio.FromPercent(0) };
    public static LightConfiguration DefaultFluorescence => new() { Exposure = Duration.FromMicroseconds(1), Intensity = Ratio.FromPercent(50) };
}
