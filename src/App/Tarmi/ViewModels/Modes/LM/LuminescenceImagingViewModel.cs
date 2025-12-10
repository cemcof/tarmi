using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Tarmi.App.Infrastructure;
using Tarmi.App.Infrastructure.Options;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thorlabs.Light;
using Tarmi.Models.Serialization;
using Tarmi.WPF;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes.LM;

public partial class LuminescenceImagingViewModel : ViewModelBase
{
    private readonly ILuminescenceMode _virtualDevice;
    private readonly IOptions<AppConfigurationOptions> _options;
    private readonly SemaphoreSlim _colorSwitchLock = new(1, 1);

    [ObservableProperty]
    private double _intensity;

    [ObservableProperty]
    private double _exposure;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntensityEditEnabled))]
    [NotifyPropertyChangedFor(nameof(ExposureEditEnabled))]
    public partial LightColor? SelectedLightColor { get; private set; }

    public List<LightColor> LightColors { get; } = [];
    public List<LightSettingsViewModel> LightSettings { get; } = [];
    public IObservable<bool> CanAcquireData { get; private set; }
    public bool IntensityEditEnabled => _virtualDevice.ActiveLightColor is not null;
    public bool ExposureEditEnabled => _virtualDevice.ActiveLightColor is not null;

    public LuminescenceImagingViewModel(ILuminescenceMode virtualDevice, IOptions<AppConfigurationOptions> options, ILoggerFactory loggerFactory)
    {
        _virtualDevice = virtualDevice;
        InitializeLightSettings(loggerFactory);
        _options = options;
    }

    protected override Task InitializeCoreAsync()
    {
        InitializeLightColors();
        Exposure = _virtualDevice.ExposureTime.Microseconds;
        Intensity = _virtualDevice.Intensity.Percent;
        _disposables.Add(_virtualDevice.CurrentActiveLightColor.Subscribe(color => SelectedLightColor = color));
        LoadUserSettings();
        return base.InitializeCoreAsync();
    }

    [MemberNotNull(nameof(CanAcquireData))]
    private void InitializeLightSettings(ILoggerFactory loggerFactory)
    {
        var settings = Enum
            .GetValues<LightColor>()
            .Select(color =>
                new LightSettingsViewModel(_virtualDevice, color, loggerFactory.CreateLogger<LightSettingsViewModel>())
            );

        LightSettings.AddRange(settings);
        LightSettings.ForEach(x =>
        {
            _disposables.Add(x.ExposureChanges.Subscribe(async o => await HandleExposureUpdates(o)));
            _disposables.Add(x.IntensityChanges.Subscribe(async o => await HandleIntensityUpdates(o)));
        });

        CanAcquireData = LightSettings
            .Select(setting => setting.IsSelectedChanged)
            .Append(_virtualDevice.CurrentActiveLightColor.Select(color => color.HasValue))
            .Merge()
            .Select(_ => _virtualDevice.ActiveLightColor.HasValue || LightSettings.Any(light => light.IsSelected))
            .CombineLatest(_virtualDevice.IsProtracted, (isLightSelected, isProtracted) => isLightSelected && isProtracted)
            .DistinctUntilChanged();
    }

    [RelayCommand]
    public async Task SetLightColorAsync(LightColor color)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _colorSwitchLock.UseOnceAsync(default);

        if (color == SelectedLightColor)
        {
            await _virtualDevice.TurnLightOff(default);
        }
        else
        {
            _virtualDevice.ExposureTime = Duration.FromMicroseconds(LightSettings.First(ls => ls.Color == color).ImagingSettings.Exposure);
            Exposure = _virtualDevice.ExposureTime.Microseconds;
            await _virtualDevice.SetIntensityAsync(Ratio.FromPercent(LightSettings.First(ls => ls.Color == color).ImagingSettings.Intensity), default);
            Intensity = _virtualDevice.Intensity.Percent;
            await _virtualDevice.TurnLightOn(color, default);
        }
    }

    [RelayCommand]
    public async Task SetExposure()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _colorSwitchLock.UseOnceAsync(default);

        _virtualDevice.ExposureTime = Duration.FromMicroseconds(Exposure);
        Exposure = _virtualDevice.ExposureTime.Microseconds;
        if (_virtualDevice.ActiveLightColor is not null)
        {
            LightSettings.First(ls => ls.Color == _virtualDevice.ActiveLightColor).ImagingSettings.Exposure = Exposure;
        }        
    }

    [RelayCommand]
    public async Task SetIntensity()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _colorSwitchLock.UseOnceAsync(default);

        await _virtualDevice.SetIntensityAsync(Ratio.FromPercent(Intensity), default);
        Intensity = _virtualDevice.Intensity.Percent;
        if (_virtualDevice.ActiveLightColor is not null)
        {
            LightSettings.First(ls => ls.Color == _virtualDevice.ActiveLightColor).ImagingSettings.Intensity = Intensity;
        }
    }

    private async Task HandleExposureUpdates((LightColor, double) item)
    {
        using var guard = await _colorSwitchLock.UseOnceAsync(default);
        try
        {
            if (Exposure != item.Item2 && _virtualDevice.ActiveLightColor == item.Item1)
            {
                _virtualDevice.ExposureTime = Duration.FromMicroseconds(item.Item2);
                Exposure = _virtualDevice.ExposureTime.Microseconds;
            }
            SaveUserSettings();
        }
        catch { }
    }

    private async Task HandleIntensityUpdates((LightColor, double) item)
    {
        using var guard = await _colorSwitchLock.UseOnceAsync(default);
        try
        {
            if (Intensity != item.Item2 && _virtualDevice.ActiveLightColor == item.Item1)
            {
                await _virtualDevice.SetIntensityAsync(Ratio.FromPercent(item.Item2), default);
                Intensity = _virtualDevice.Intensity.Percent;
            }
            SaveUserSettings();
        }
        catch { }
    }

    private void SaveUserSettings()
    {
        string path = System.IO.Path.Combine(_options.Value.StateDirectory, "LM_light_settings.xml");
        using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        Helpers.Save(GetSerializableSettings(), file);
    }

    private LightSettingsSerializable LoadUserSettingsFile()
    {
        string path = System.IO.Path.Combine(_options.Value.StateDirectory, "LM_light_settings.xml");
        using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var serializer = new DataContractSerializer(typeof(LightSettingsSerializable));
        return serializer.ReadObject(file) as LightSettingsSerializable ??
            throw new InvalidDataException($"Failed to deserialize {typeof(LightSettingsSerializable)} from '{path}'.");
    }

    private void LoadUserSettings()
    {
        try
        {
            LightSettingsSerializable deserializedSettings = LoadUserSettingsFile();
            foreach (var settingVM in LightSettings) // TODO: optimize
            {
                settingVM.GetImagingSettingsForFilter(FilterType.Reflection).Exposure = deserializedSettings.ReflectionSettings.First(x => x.Color == settingVM.Color).ImageSettings.Exposure;
                settingVM.GetImagingSettingsForFilter(FilterType.Reflection).Intensity = deserializedSettings.ReflectionSettings.First(x => x.Color == settingVM.Color).ImageSettings.Intensity;
                settingVM.GetImagingSettingsForFilter(FilterType.Fluorescence).Exposure = deserializedSettings.FluorescenceSettings.First(x => x.Color == settingVM.Color).ImageSettings.Exposure;
                settingVM.GetImagingSettingsForFilter(FilterType.Fluorescence).Intensity = deserializedSettings.FluorescenceSettings.First(x => x.Color == settingVM.Color).ImageSettings.Intensity;
            }
        }
        catch { }
    }

    private LightSettingsSerializable GetSerializableSettings()
    {
        return new()
        {
            FluorescenceSettings = [.. LightSettings.Select(x =>
            new LightColorSettingsSerializable()
            {
                Color = x.Color, ImageSettings = new()
                {
                    Exposure = x.GetImagingSettingsForFilter(FilterType.Fluorescence).Exposure,
                    Intensity = x.GetImagingSettingsForFilter(FilterType.Fluorescence).Intensity }
            })],
            ReflectionSettings = [.. LightSettings.Select(x =>
            new LightColorSettingsSerializable()
            {
                Color = x.Color, ImageSettings = new()
                {
                    Exposure = x.GetImagingSettingsForFilter(FilterType.Reflection).Exposure,
                    Intensity = x.GetImagingSettingsForFilter(FilterType.Reflection).Intensity }
            })]
        };
    }

    private string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(VirtualDeviceViewModel)}::{methodName}";

    private void InitializeLightColors()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        LightColors.AddRange(Enum.GetValues<LightColor>());
    }
}
