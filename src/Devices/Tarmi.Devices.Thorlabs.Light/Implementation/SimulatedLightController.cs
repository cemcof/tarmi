using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;
public sealed class SimulatedLightController : ILightController
{
    private readonly ILogger _logger;
    private readonly TimeSpan AnswerDelay = TimeSpan.FromMilliseconds(300);

    private readonly BehaviorSubject<LightColor?> _activeLightSubject = new(null);
    private readonly BehaviorSubject<Ratio> _brightnessSubject = new(Ratio.FromPercent(0));

    public IObservable<LightColor?> CurrentSelectedLight => _activeLightSubject.DistinctUntilChanged();

    public LightColor? SelectedLight
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

    private readonly BehaviorSubject<bool> _isActiveLight = new(false);
    public bool IsLightActive
    {
        get => _isActiveLight.Value;
        set => _logger.Swallow(() => _isActiveLight.OnNext(value));
    }

    public IObservable<bool> CurrentIsLightActive => _isActiveLight.DistinctUntilChanged();

    public SimulatedLightController(ILogger<SimulatedLightController> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken)
    {
        Guard.IsBetweenOrEqualTo(brightness.Percent, 0, 100);

        await Task.Delay(AnswerDelay, cancellationToken);

        _logger.LogTrace("Simulator brightness set to {Brightness}.", brightness);
        Brightness = brightness;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await SelectLightAsync(null, cancellationToken);
        await SetBrightnessAsync(Brightness, cancellationToken);
    }

    public async Task Deinitialize(CancellationToken cancellationToken)
    {
        await SelectLightAsync(null, cancellationToken);
    }

    public async Task SelectLightAsync(LightColor? color, CancellationToken cancellationToken)
    {
        await Task.Delay(AnswerDelay, cancellationToken);
        if (color.HasValue)
        {
            _logger.LogTrace("Simulator {Light} set on with {Brightness}.", color.Value, Brightness);
        }
        else if (SelectedLight.HasValue)
        {
            _logger.LogTrace("Simulator {Light} set off.", SelectedLight);
        }
        SelectedLight = color;
    }

    public async Task TurnLightOnAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(AnswerDelay, cancellationToken);
        IsLightActive = true;
    }

    public async Task TurnLightOffAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(AnswerDelay, cancellationToken);
        IsLightActive = false; 
    }
}
