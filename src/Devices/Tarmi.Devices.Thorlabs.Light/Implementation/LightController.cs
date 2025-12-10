using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Communication.Common.Serial;
using Tarmi.Configuration.Devices;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;


public sealed class LightController : ILightController
{
    private readonly ILogger _logger;
    private readonly ISerialCommunicationFactory _serialCommunicationFactory;
    private readonly SerialPort _portConfig;

    private ISerialCommunication? _serialCommunication;

    private readonly BehaviorSubject<LightColor?> _activeLightSubject = new(null);
    private readonly BehaviorSubject<Ratio> _brightnessSubject = new(Ratio.FromPercent(0));

    public IObservable<LightColor?> CurrentActiveLight => _activeLightSubject.DistinctUntilChanged();

    public LightColor? ActiveLight
    {
        get => _activeLightSubject.Value;
        private set => _logger.Swallow(() => _activeLightSubject.OnNext(value));
    }

    public IObservable<Ratio> CurrentBrightness => _brightnessSubject;
    public Ratio Brightness
    {
        get => _brightnessSubject.Value;
        private set => _logger.Swallow(() => _brightnessSubject.OnNext(value));
    }

    public LightController(ISerialCommunicationFactory serialCommunicationFactory, SerialPort portConfig, ILogger<LightController> logger)
    {
        _serialCommunicationFactory = serialCommunicationFactory;
        _portConfig = portConfig;
        _logger = logger;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        _serialCommunication = _serialCommunicationFactory.CreateSerialCommunication(_portConfig);

        await SetBrightnessModeStateAsync(true, cancellationToken);
        await SetSingleSelectionModeStateAsync(true, cancellationToken);
        await DisableLights(cancellationToken);
    }

    public async Task Deinitialize(CancellationToken cancellationToken)
    {
        await DisableLights(cancellationToken);

        _serialCommunication?.Dispose();
        _serialCommunication = null;
    }

    public void Dispose()
    {
        _serialCommunication?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task DisableLights(CancellationToken cancellationToken)
    {
        foreach (var lightColor in Enum.GetValues<LightColor>())
        {
            await SetLightStateAsync(lightColor, false, cancellationToken);
            await SetLightBrightnessAsync(lightColor, Brightness, cancellationToken);
        }

        ActiveLight = null;
    }

    private async Task SetBrightnessModeStateAsync(bool state, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting brightness mode to {State}.", state);
        var command = Commands.SetBrightnessModeCommand(state);
        await _serialCommunication!.SendCommandAsync(command, cancellationToken);

        ActiveLight = null;
    }

    private async Task SetSingleSelectionModeStateAsync(bool state, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting single selection mode to {State}.", state);
        var command = Commands.SetSingleLightModeCommand(state);
        await _serialCommunication!.SendCommandAsync(command, cancellationToken);

        ActiveLight = null;
    }

    public async Task SetActiveLightAsync(LightColor? color, CancellationToken cancellationToken)
    {
        // Set light on
        if (color.HasValue)
        {
            await SetLightBrightnessAsync(color.Value, Brightness, cancellationToken);
            await SetLightStateAsync(color.Value, true, cancellationToken);

        }
        // Set light off
        else if (ActiveLight.HasValue)
        {
            await SetLightStateAsync(ActiveLight.Value, false, cancellationToken);
        }
        ActiveLight = color;
    }

    private async Task SetLightStateAsync(LightColor color, bool isOn, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting {Color} light state to {State}.", color, isOn);
        var command = Commands.SetLightStateCommand(color, isOn);
        await _serialCommunication!.SendCommandAsync(command, cancellationToken);
    }

    public async Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken)
    {
        Guard.IsBetweenOrEqualTo(brightness.Percent, 0, 100, nameof(brightness));

        if (ActiveLight.HasValue)
        {
            await SetLightBrightnessAsync(ActiveLight.Value, brightness, cancellationToken);
        }

        Brightness = brightness;
    }

    private async Task SetLightBrightnessAsync(LightColor color, Ratio brightness, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting {Color} light brightness to {Brightness}.", color, brightness);
        var command = Commands.SetLightBrightnessCommand(color, brightness);
        await _serialCommunication!.SendCommandAsync(command, cancellationToken);
    }
}
