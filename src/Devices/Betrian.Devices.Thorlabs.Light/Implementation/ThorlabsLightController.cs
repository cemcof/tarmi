using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Thorlabs.DC4100_64.Interop;
using UnitsNet;

namespace Betrian.Devices.Thorlabs.Light.Implementation;
public class ThorlabsLightController : ILightController
{
    private readonly TLDC4100 _lightDriver;
    private readonly ILogger<ThorlabsLightController> _logger;
    private readonly SemaphoreSlim _semaphore = new(1);

    public ThorlabsLightController(TLDC4100 thorlabsController, ILogger<ThorlabsLightController> logger)
    {
        _lightDriver = thorlabsController;
        _logger = logger;
    }

    public LightColor? ActiveLight => _activeLightSubject.Value;

    public Ratio Brightness => _brightnessSubject.Value;

    private readonly BehaviorSubject<LightColor?> _activeLightSubject = new(null);
    public IObservable<LightColor?> CurrentActiveLight => _activeLightSubject.DistinctUntilChanged();

    private readonly BehaviorSubject<Ratio> _brightnessSubject = new(Ratio.Zero);
    public IObservable<Ratio> CurrentBrightness => _brightnessSubject.DistinctUntilChanged();

    public async Task Deinitialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deinitializing Thorlabs light controller.");
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        TurnLightsOff();
        _logger.LogInformation("Thorlabs light controller deinitialized.");
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _lightDriver.Dispose();
        GC.SuppressFinalize(this);
    }

    private static readonly LightColor[] LightColors = Enum.GetValues<LightColor>();

    public async Task Initialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Thorlabs light controller.");
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);

        SetOperationMode(OperationMode.Brightness);
        TurnLightsOff();
        SetSelectionMode(SelectionMode.Single);

        _logger.LogInformation("Thorlabs light controller initialized.");
    }

    public async Task SetActiveLightAsync(LightColor? color, CancellationToken cancellationToken)
    {
        if (color == ActiveLight)
        {
            _logger.LogInformation("Ignored light setting as it was already active.");
            return;
        }
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        if (ActiveLight.HasValue)
        {
            SetLightState(ActiveLight.Value, false);
        }
        if (color.HasValue)
        {
            SetBrightness(color.Value, Brightness);
            SetLightState(color.Value, true);
        }
        _logger.Swallow(() => _activeLightSubject.OnNext(color));
    }

    public async Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken)
    {
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        if (ActiveLight.HasValue)
        {
            SetBrightness(ActiveLight.Value, brightness);
        }
        _logger.Swallow(() => _brightnessSubject.OnNext(brightness));
    }
    
    private void TurnLightsOff()
    {
        foreach (var color in LightColors)
        {
            SetLightState(color, false);
            SetBrightness(color, Ratio.Zero);
        }
    }

    private void SetLightState(LightColor color, bool on)
    {
        _logger.LogInformation("Setting light {Light} to state {State}.", color, on);
        var status = _lightDriver.setLedOnOff(color.ToChannel(), on);
        _logger.LogInformation("Setting light {Light} to state {State} returned status {Status}.", color, on, status);
    }

    private void SetBrightness(LightColor color, Ratio brightness)
    {
        Guard.IsBetweenOrEqualTo(brightness, Ratio.Zero, Ratio.FromPercent(100));

        _logger.LogInformation("Setting light {Light} brightness to {Brightness}.", color, brightness);
        var status = _lightDriver.setPercentalBrightness(color.ToChannel(), (float)brightness.Percent);
        _logger.LogInformation("Setting light {Light} brightness to {Brightness} returned status {Status}.", color, brightness, status);
    }

    private void SetOperationMode(OperationMode operationMode)
    {
        _logger.LogInformation("Setting light operation mode to {OperationMode}.", operationMode);
        var status = _lightDriver.setOperationMode((int)operationMode);
        _logger.LogInformation("Setting light operation mode to {OperationMode} returned status {Status}.", operationMode, status);
    }

    private void SetSelectionMode(SelectionMode selectionMode)
    {
        _logger.LogInformation("Setting light operation mode to {SelectionMode}.", selectionMode);
        var status = _lightDriver.setSelectionMode((int)selectionMode);
        _logger.LogInformation("Setting light operation mode to {SelectionMode} returned status {Status}.", selectionMode, status);
    }
}
