using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Thorlabs.DC4100_64.Interop;
using UnitsNet;

namespace Tarmi.Devices.Thorlabs.Light.Implementation;
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

    public LightColor? SelectedLight => _selectedLightSubject.Value;

    public Ratio Brightness => _brightnessSubject.Value;

    private readonly BehaviorSubject<LightColor?> _selectedLightSubject = new(null);
    private readonly BehaviorSubject<bool> _isLightActiveSubject = new(false);
    public IObservable<LightColor?> CurrentSelectedLight => _selectedLightSubject.DistinctUntilChanged();
    public IObservable<bool> CurrentIsLightActive => _isLightActiveSubject.DistinctUntilChanged();

    private readonly BehaviorSubject<Ratio> _brightnessSubject = new(Ratio.Zero);
    public IObservable<Ratio> CurrentBrightness => _brightnessSubject.DistinctUntilChanged();

    public bool IsLightActive
    {
        get => _isLightActiveSubject.Value;
        private set
        {
            _logger.Swallow(() => _isLightActiveSubject.OnNext(value));
        }
    }

    public async Task Deinitialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deinitializing Thorlabs light controller.");
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        TurnAllLightsOff();
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
        TurnAllLightsOff();
        SetSelectionMode(SelectionMode.Single);
        IsLightActive = false;

        _logger.LogInformation("Thorlabs light controller initialized.");
    }

    public async Task SelectLightAsync(LightColor? color, CancellationToken cancellationToken)
    {
        if (color == SelectedLight)
        {
            _logger.LogInformation("Ignored light setting as it was already active.");
            return;
        }
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        if (SelectedLight.HasValue && IsLightActive)
        {
            SetLightState(SelectedLight.Value, false);
        }
        if (color.HasValue && IsLightActive)
        {
            SetBrightness(color.Value, Brightness);
            SetLightState(color.Value, true);
        }
        _logger.Swallow(() => _selectedLightSubject.OnNext(color));
    }

    public async Task SetBrightnessAsync(Ratio brightness, CancellationToken cancellationToken)
    {
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);
        if (SelectedLight.HasValue)
        {
            SetBrightness(SelectedLight.Value, brightness);
        }
        _logger.Swallow(() => _brightnessSubject.OnNext(brightness));
    }

    public async Task TurnLightOnAsync(CancellationToken cancellationToken)
    {
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);

        _logger.LogInformation("Setting light on.");
        if (SelectedLight.HasValue)
        {
            SetBrightness(SelectedLight.Value, Brightness);
            SetLightState(SelectedLight.Value, true);
        }
        IsLightActive = true;
    }

    public async Task TurnLightOffAsync(CancellationToken cancellationToken)
    {
        using var guard = await _semaphore.UseOnceAsync(cancellationToken);

        _logger.LogInformation("Setting light off.");
        if (SelectedLight.HasValue)
        {
            SetLightState(SelectedLight.Value, false);
        }
        IsLightActive = false;
    }

    private void TurnAllLightsOff()
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
