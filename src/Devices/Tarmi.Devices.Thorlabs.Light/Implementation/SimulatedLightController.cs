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
    public SimulatedLightController(ILogger<SimulatedLightController> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken = default)
    {
        Guard.IsBetweenOrEqualTo(brightness.Percent, 0, 100, nameof(brightness));

        await Task.Delay(AnswerDelay, cancellationToken);

        _logger.LogTrace("Simulator brightness set to {Brightness}.", brightness);
        Brightness = brightness;
    }

    public async Task Initialize(CancellationToken cancellationToken)
    {
        await SetActiveLightAsync(null, cancellationToken);
        await SetBrightnessAsync(Brightness, cancellationToken);
    }

    public async Task Deinitialize(CancellationToken cancellationToken)
    {
        await SetActiveLightAsync(null, cancellationToken);
    }

    public async Task SetActiveLightAsync(LightColor? color, CancellationToken cancellationToken = default)
    {
        await Task.Delay(AnswerDelay, cancellationToken);
        if (color.HasValue)
        {
            _logger.LogTrace("Simulator {Light} set on with {Brightness}.", color.Value, Brightness);
        }
        else if (ActiveLight.HasValue)
        {
            _logger.LogTrace("Simulator {Light} set off.", ActiveLight);
        }
        ActiveLight = color;
    }
}
