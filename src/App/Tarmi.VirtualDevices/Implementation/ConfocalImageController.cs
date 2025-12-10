using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Confocal;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thorlabs.FilterWheel;
using Tarmi.Devices.Thorlabs.PinHoleWheel;
using Tarmi.Configuration;
using UnitsNet;

namespace Tarmi.VirtualDevices.Implementation;

public class ConfocalImageController
{
    private readonly IPinHoleWheelController _pinHoleWheelController;
    private readonly IFilterWheelController _filterWheelController;
    private readonly IFilterHandler _filterHandler;
    private readonly IConfocalDevice _confocalDevice;
    private readonly Subject<FilterType> _currentFilter = new();
    private FilterType CurrentFilter => _filterHandler.FilterPosition;
    private readonly Dictionary<FilterType, ConfocalConfiguration> _filterConfigurations = [];
    private readonly Duration _filterSwitchDelay;

    public Dictionary<FilterType, ConfocalConfiguration> DefaultLightConfigurations { get; } = new Dictionary<FilterType, ConfocalConfiguration>()
    {
        { FilterType.Reflection, ConfocalConfiguration.DefaultReflection },
        { FilterType.Fluorescence, ConfocalConfiguration.DefaultFluorescence }
    };
    public IObservable<FilterType> CurrentFilterChanges => _currentFilter.AsObservable().DistinctUntilChanged();

    public ConfocalImageController(
        IPinHoleWheelController pinHoleWheelController,
        IFilterWheelController filterWheelController,
        IFilterHandler filterHandler,
        IConfocalDevice confocalDevice,
        ApplicationConfig applicationConfig
        )
    {
        _pinHoleWheelController = pinHoleWheelController;
        _filterWheelController = filterWheelController;
        _filterHandler = filterHandler;
        _confocalDevice = confocalDevice;
        _filterSwitchDelay = applicationConfig.UserPreferences.ConfocalFilterSwitchDelay;
        InitFilterConfigurations();
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await SwitchFilter(FilterType.Reflection, cancellationToken);

        if (!await _pinHoleWheelController.IsDeviceActive(cancellationToken))
        {
            // TODO
            _confocalDevice.PinHolePosition = Length.FromNanometers(_pinHoleWheelController.Position);
        }

        if (!await _filterWheelController.IsDeviceActive(cancellationToken))
        {
            // TODO
            _confocalDevice.FilterPosition = Length.FromNanometers(_filterWheelController.FilterColor);
        }

        _confocalDevice.Intensity = _filterConfigurations[CurrentFilter].Intensity;
    }

    public Length GetPinHoleWheelPosition() => Length.FromNanometers(_pinHoleWheelController.Position);

    public async Task SetPinHolePosition(long position, CancellationToken cancellationToken) =>
        await _pinHoleWheelController.SetPosition(position, cancellationToken);

    public Length GetFilterWheelColor() => Length.FromNanometers(_filterWheelController.FilterColor);

    public async Task SetFilterWheelColor(double filterColor, CancellationToken cancellationToken) =>
        await _filterWheelController.SetFilterColor(filterColor, cancellationToken);

    //public Ratio GetIntensity() => _filterConfigurations[CurrentFilter].Intensity;

    public void SetIntensity(Ratio intensity)
    {
        _confocalDevice.Intensity = intensity;
        _filterConfigurations[CurrentFilter].Intensity = _confocalDevice.Intensity;
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
        _confocalDevice.Intensity = _filterConfigurations[FilterType.Reflection].Intensity;
        _ = await _filterHandler.SwitchFilterAsync(FilterType.Reflection, cancellationToken);
        await Task.Delay(_filterSwitchDelay.ToTimeSpan(), cancellationToken);
        _currentFilter.OnNext(FilterType.Reflection);
    }

    private async Task SwitchToFluorescence(CancellationToken cancellationToken)
    {
        _ = await _filterHandler.SwitchFilterAsync(FilterType.Fluorescence, cancellationToken);
        await Task.Delay(_filterSwitchDelay.ToTimeSpan(), cancellationToken);
        _currentFilter.OnNext(FilterType.Fluorescence);
        _confocalDevice.Intensity = _filterConfigurations[FilterType.Fluorescence].Intensity;
    }

    private void InitFilterConfigurations()
    {
        _filterConfigurations.Add(FilterType.Fluorescence, ConfocalConfiguration.DefaultFluorescence);
        _filterConfigurations.Add(FilterType.Reflection, ConfocalConfiguration.DefaultReflection);
    }
}

public class ConfocalConfiguration
{
    public Ratio Intensity { get; set; }

    public static ConfocalConfiguration DefaultReflection => new() 
    {
        Intensity = Ratio.FromPercent(0),
    };
    
    public static ConfocalConfiguration DefaultFluorescence => new() 
    {
        Intensity = Ratio.FromPercent(0),
    };
}
