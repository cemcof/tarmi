using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Tarmi.App.Infrastructure;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Tarmi.Configuration.Application;
using Tarmi.Configuration.Devices;
using Tarmi.Confocal;
using Tarmi.Confocal.Implementation;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.SmarAct.Stage;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Devices.Thorlabs.FilterWheel;
using Tarmi.Devices.Thorlabs.PinHoleWheel;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata.Confocal;
using Tarmi.Models;
using UnitsNet;
using ImageMetadata = Tarmi.Imaging.Common.ImageMetadata;

namespace Tarmi.VirtualDevices.Implementation;

public sealed class ConfocalMode : StageControllingModeBase, IConfocalMode, IDisposable
{
    private CancellationTokenSource? _grabbingTokenSource;
    private readonly ILogger _logger;
    private readonly ILinearStage _linearStage;
    private readonly LinearStageAlignment _linearStageAlignment;
    private readonly IImageGrabber _imageGrabber;
    private readonly IPinHoleWheelController _pinHoleWheelController;
    private readonly IFilterWheelController _filterWheelController;
    private readonly IFilterHandler _filterHandler;
    private readonly ConfocalStaff _confocalConfig;
    private readonly PythonConfig _pythonConfig;
    private readonly PythonController _pythonController;
    private readonly ConfocalCamera _cameraConfig;
    private readonly ThorlabsPinHoleWheel _thorlabsPinHoleWheel;
    private readonly ThorlabsFilterWheel _thorlabsFilterWheel;

    private readonly bool _simulationEnabled;
    private readonly BehaviorSubject<bool> _grabbingActive = new(false);
    private readonly ConfocalImageController _imageController;
    private readonly Length _manualFocusStep = Length.FromMicrometers(1);
    private readonly NonblockingBufferedSubject<ImageWithMetadata> _imageGrabberSubject;
    private readonly LuminescenceAberrations _aberrations;
    private Length _activeAberration = Length.Zero;

    public IConfocalDevice ConfocalData { get; private set; }

    public Length HorizontalFieldWidth => ConfocalData.FieldWidth;
    public Length VerticalFieldWidth => ConfocalData.FieldHeight;
    public Length FocusStep { get; set; } = Length.FromPicometers(100);
    public IEnumerable<Length> FocusStepSizes { get; }
    public IEnumerable<Length> PixelSizes { get; }
    public IEnumerable<ElectricPotential> ADCRanges { get; }
    public IEnumerable<Duration> DwellRanges { get; }

    public IEnumerable<Length> PinHoleSizes { get; }
    public IList<ConfocalLight> ConfocalLights { get; }
    public StageState StageState => _instrument.CurrentStageState;
    public IObservable<Length> LinearStagePosition => _linearStage.Position;
    public IObservable<StageState> Stage => _instrument.Stage;
    public IObservable<bool> IsProtracted => _linearStage.IsProtracted;
    public IObservable<bool> GrabbingActiveChanges => _grabbingActive.AsObservable().DistinctUntilChanged();
    public bool IsGrabbingActive => _grabbingActive.Value;
    public IObservable<ImageWithMetadata> Image => _imageGrabberSubject.AsObservable();
    public IObservable<FilterType> ModeChanges => _imageController.CurrentFilterChanges;
    public Dictionary<FilterType, ConfocalConfiguration> DefaultLightConfigurations => _imageController.DefaultLightConfigurations;

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(ConfocalMode)}::{methodName}";

    public ConfocalMode(
        ILogger<ConfocalMode> logger,
        IImageGraberFactory imageGrabberFactory,
        IPinHoleWheelControllerFactory pinHoleWheelControllerFactory,
        IFilterWheelControllerFactory filterWheelControllerFactory,
        IFilterHandlerFactory filterHandlerFactory,
        ILinearStage linearStage,
        IInstrument instrument,
        ISafeStageControlling safeStageControlling,
        ApplicationConfig applicationConfig
    )
        : base(instrument, safeStageControlling)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        _logger = logger;
        _filterWheelController = filterWheelControllerFactory.CreateFilterWheelController();
        _pinHoleWheelController = pinHoleWheelControllerFactory.CreatePinHoleWheelController();
        _filterHandler = filterHandlerFactory.CreateFilterHandler();
        _linearStage = linearStage;
        _linearStageAlignment = applicationConfig.Microscope.Alignment.LinearStage;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _confocalConfig = applicationConfig.Microscope.ConfocalConfig;
        _thorlabsPinHoleWheel = applicationConfig.Microscope.ThorlabsPinHoleWheel;
        _thorlabsFilterWheel = applicationConfig.Microscope.ThorlabsFilterWheel;
        _cameraConfig = applicationConfig.Microscope.ConfocalConfig.ConfocalCamera;
        PinHoleSizes = GetPinHoleSizes(applicationConfig.Microscope.ThorlabsPinHoleWheel.PinHoleWheelAlignments);

        _aberrations = applicationConfig.UserPreferences.ConfocalAberrations;
        ConfocalData = GetDefaultConfocalData();
        FocusStepSizes = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Order().ToArray();
        PixelSizes = applicationConfig.UserPreferences.ConfocalSettings.PixelSizes.Order().ToArray();
        ADCRanges = applicationConfig.UserPreferences.ConfocalSettings.ADCRanges.Order().ToArray();
        DwellRanges = applicationConfig.UserPreferences.ConfocalSettings.DwellRanges.Order().ToArray();
        GainRange = applicationConfig.UserPreferences.ConfocalSettings.GainRanges;
        ConfocalLights = 
            [ 
                _confocalConfig.ConfocalLights.ConfocalLight1,
                _confocalConfig.ConfocalLights.ConfocalLight2,
                _confocalConfig.ConfocalLights.ConfocalLight3,
                _confocalConfig.ConfocalLights.ConfocalLight4
            ];

        if (applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Count > 0)
        {
            var focusStepMidIdx = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Count / 2;
            FocusStep = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps[focusStepMidIdx];
        }

        if (applicationConfig.UserPreferences.ConfocalSettings.PixelSizes.Count > 0)
        {
            var middleValueIdx = applicationConfig.UserPreferences.ConfocalSettings.PixelSizes.Count / 2;
            PixelSize = applicationConfig.UserPreferences.ConfocalSettings.PixelSizes[middleValueIdx];
        }

        if (applicationConfig.UserPreferences.ConfocalSettings.ADCRanges.Count > 0)
        {
            var middleValueIdx = applicationConfig.UserPreferences.ConfocalSettings.ADCRanges.Count / 2;
            ADC = applicationConfig.UserPreferences.ConfocalSettings.ADCRanges[middleValueIdx];
        }

        if (applicationConfig.UserPreferences.ConfocalSettings.DwellRanges.Count > 0)
        {
            var middleValueIdx = applicationConfig.UserPreferences.ConfocalSettings.DwellRanges.Count / 2;
            Dwell = applicationConfig.UserPreferences.ConfocalSettings.DwellRanges[middleValueIdx];
        }

        if (PinHoleSizes.Any())
        {
            var middleValueIdx = PinHoleSizes.Count() / 2;
            PinHoleWheelPosition = PinHoleSizes.ElementAt(middleValueIdx);
        }

        SetImageSizes();
        _pythonConfig = applicationConfig.UserPreferences.PythonConfig;
        _pythonConfig.ScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _pythonConfig.ScriptPath);
        _pythonConfig.ScriptTuningPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _pythonConfig.ScriptTuningPath);
        _pythonController = new PythonController(_pythonConfig);
        _imageGrabber = imageGrabberFactory.CreateGrabber(_simulationEnabled, _pythonController);
        _imageGrabberSubject = new(
            bufferSize: 1,
            fullMode: System.Threading.Channels.BoundedChannelFullMode.DropNewest,
            itemDropped: _ => _logger.LogDebug("Image dropped due newer image available.")
        );

        _ = _imageGrabber.GrabbedImage.Select(grabbed => TransformToImageWithMetadata(grabbed)).Subscribe(_imageGrabberSubject.OnNext);
        _imageController = new(_pinHoleWheelController, _filterWheelController, _filterHandler, ConfocalData, applicationConfig);
    }

    public async Task<FilterType> GetModeAsync()
    {
        // suppress so far unused members warnings
        _ =  _pinHoleWheelController;
        _ = _filterWheelController;
        _ = _pythonConfig;
        _ = _thorlabsPinHoleWheel;

        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        return await _filterHandler.ReadFilterPositionAsync();
    }

    public async Task ChangeModeAsync(FilterType mode)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _imageController.SwitchFilter(mode, default);

        if (GetConfocalLightColorFromWaveLength(ConfocalData.LaserColor) is ConfocalLight color)
        {
            await SetActiveAberration(color, default);

            if (_imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.LuminescenceMode));
            }
        }

        if (_simulationEnabled && _imageGrabber is ISimulatedImageGrabber simulatedGrabber)
        {
            simulatedGrabber.ImageFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mode == FilterType.Fluorescence ? "fluoConf.tif" : "reflConf.tif"));
        }
    }

    public Duration Dwell
    {
        get => ConfocalData.Dwell;
        set
        {
            ConfocalData.Dwell = Duration.FromNanoseconds(value.Value);

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.Dwell));
            }
        }
    }

    public ConfocalLight LaserColor
    {
        get => GetConfocalLightColorFromWaveLength(ConfocalData.LaserColor);
        set
        {
            ConfocalData.LaserColor = value.Wavelength;

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.LaserColor));
            }
        }
    }

    public Length FieldWidth
    {
        get => ConfocalData.FieldWidth;
        set
        {
            ConfocalData.FieldWidth = value;
            SetImageSizes();

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.FieldWidth));
            }
        }
    }

    public Length FieldHeight
    {
        get => ConfocalData.FieldHeight;
        set
        {
            ConfocalData.FieldHeight = value;
            SetImageSizes();

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.FieldHeight));
            }
        }
    }

    public int Width
    {
        get => ConfocalData.Width;
        set => ConfocalData.Width = value;
    }

    public int Height
    {
        get => ConfocalData.Height;
        set =>  ConfocalData.Height = value;
    }

    public string Resolution
    {
        get => ConfocalData.Resolution;
        set => ConfocalData.Resolution = value;
    }

    private void SetImageSizes()
    {
        Width = (int)(FieldWidth / PixelSize);
        Height = (int)(FieldHeight / PixelSize);
        Resolution = $"{Width}x{Height}";
    }

    private async Task SetActiveAberration(ConfocalLight color, CancellationToken cancellationToken)
    {
        if (_linearStage.GetIsProtracted())
        {
            await EliminateActiveAberration(cancellationToken);
            SetAberrationValue(color);
            await _linearStage.MoveRelativeAsync(_activeAberration, cancellationToken);
        }
    }

    private async Task EliminateActiveAberration(CancellationToken cancellationToken)
    {
        if (_linearStage.GetIsProtracted() && !Equals(_activeAberration, Length.Zero))
        {
            await _linearStage.MoveRelativeAsync(-_activeAberration, cancellationToken);
            _activeAberration = Length.Zero;
        }
    }

    private void SetAberrationValue(ConfocalLight color) // TODO: optimize/refactor
    {
        _logger.LogDebug("Set active aberration");

        if (_filterHandler.FilterPosition == FilterType.Fluorescence)
        {
            _activeAberration = _aberrations.FluorescenceAberrations.FirstOrDefault(x => x.Key.Equals(color.LightColor.ToString(), StringComparison.OrdinalIgnoreCase)).Value;
            ConfocalData.LuminescenceMode = Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Fluorescence;
        }
        else if (_filterHandler.FilterPosition == FilterType.Reflection)
        {
            _activeAberration = _aberrations.ReflectionAberrations.FirstOrDefault(x => x.Key.Equals(color.LightColor.ToString(), StringComparison.OrdinalIgnoreCase)).Value;
            ConfocalData.LuminescenceMode = Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Reflection;
        }
    }

    public RangeDescriptor<Ratio> IntensityRange { get; } = new()
    {
        Min = Ratio.FromPercent(0),
        Max = Ratio.FromPercent(100)
    };

    public Ratio Intensity
    {
        get => ConfocalData.Intensity;
        set
        {
            ConfocalData.Intensity = value;
            _imageController.SetIntensity(value);

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.Intensity));
            }
        }
    }

    public Length PinHolePosition => _imageController.GetPinHoleWheelPosition();

    public Length PinHoleWheelPosition
    {
        get => ConfocalData.PinHolePosition;
        set => ConfocalData.PinHolePosition = value;
    }

    public async Task SetPinHolePositionAsync(Length pinhole, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        await _imageController.SetPinHolePosition((long)pinhole.Nanometers, cancellationToken);
    }

    public Length FilterWheelColor => _imageController.GetFilterWheelColor();

    public async Task SetFilterWheelColorAsync(Length laserColor, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        Length filterColor;

        if (Equals(laserColor, _thorlabsFilterWheel.EmissionFilters.Filter1.LaserColor))
        {
            filterColor = _thorlabsFilterWheel.EmissionFilters.Filter1.FilterColor;
        }
        else if (Equals(laserColor, _thorlabsFilterWheel.EmissionFilters.Filter2.LaserColor))
        {
            filterColor = _thorlabsFilterWheel.EmissionFilters.Filter2.FilterColor;
        }
        else if (Equals(laserColor, _thorlabsFilterWheel.EmissionFilters.Filter3.LaserColor))
        {
            filterColor = _thorlabsFilterWheel.EmissionFilters.Filter3.FilterColor;
        }
        else if (Equals(laserColor, _thorlabsFilterWheel.EmissionFilters.Filter4.LaserColor))
        {
            filterColor = _thorlabsFilterWheel.EmissionFilters.Filter4.FilterColor;
        }
        else
        {
            _logger.LogError("ConfocalMode, {Name}, Emission filter color was not found for laser color {LaserColor}", nameof(SetFilterWheelColorAsync), laserColor);
            throw new NotSupportedException($"Emission filter color was not found for laser color {laserColor}");
        }

        await _imageController.SetFilterWheelColor(filterColor.Nanometers, cancellationToken);
    }

    public void FieldSelection() => SetImageSizes();

    public RangeDescriptor<Level> GainRange { get; init; }

    public Level Gain
    {
        get => ConfocalData.Gain;
        set
        {
            ConfocalData.Gain = value;

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.Gain));
            }
        }
    }

    public ElectricPotential ADC
    {
        get => ConfocalData.ADC;
        set
        {
            ConfocalData.ADC = value;
            
            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.ADC));
            }
        }
    }

    public Length PixelSize
    {
        get => ConfocalData.PixelSize.X;
        set
        {
            ConfocalData.PixelSize = new LengthPoint() { X = value, Y = value };
            SetImageSizes();

            if (_imageGrabber != null && _imageGrabber.IsGrabbing)
            {
                CallPythonCommandExecution(nameof(ConfocalData.PixelSize));
            }
        }
    }

    private void CallPythonCommandExecution(string propertyName)
    {
        var command = PythonController.GetPythonArg(ConfocalData, propertyName);

        if (command.IsNotNullOrEmpty())
        {
            _pythonController.ExecuteTuningCommand(command);
            _logger.LogDebug("Confocal script execution for {PropertyName}", propertyName);
        }
        else
        {
            _logger.LogError("Confocal script execution for {PropertyName} failed. Property not found", propertyName);
        }
    }

    public Length CurrentLinearStagePosition => _linearStage.CurrentPosition;

    public async Task EnableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _instrument.SwitchMode(InstrumentMode.StageOnly);
        _ = await SwitchStageViewAsync(StageCameraView.Confocal);

        ConfocalData.Width = _cameraConfig.Width;
        ConfocalData.Height = _cameraConfig.Height;

        if (_simulationEnabled && _imageGrabber is ISimulatedImageGrabber simulatedGrabber)
        {
            simulatedGrabber.ImageFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _filterHandler.FilterPosition == FilterType.Fluorescence ? "fluoConf.tif" : "reflConf.tif"));
        }

        // init light
        await _imageController.Initialize(cancellationToken);
    }

    public async Task DisableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            await _grabbingTokenSource.CancelAsync();
        }
        //await _logger.SwallowAsync(() => _linearStage.RetractAsync(cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task StopMovementsAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.StopAsync(cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        await _logger.SwallowAsync(_instrument.StageStopMoving);
    }

    public Task<ImageWithMetadata> GrabImageAsync()
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        // TODO
        Task.Run(SetComponentsBeforeGrabbing);

        return Task.Run(async () =>
        {
            //var result = _simulationEnabled
            //    ? TiffImage.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _filterHandler.FilterPosition == FilterType.Fluorescence ? "fluoConf.tif" : "reflConf.tif"))
            //    : await _imageGrabber.GrabImage(ConfocalData);

            if (_simulationEnabled)
            {
                var testImage = TiffImage.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _filterHandler.FilterPosition == FilterType.Fluorescence ? "fluoConf.tif" : "reflConf.tif"));
                TiffImage.Save(testImage, _imageGrabber.DefaultImagePath);
            }

            var result = await _imageGrabber.GrabImage(ConfocalData);
            return TransformToImageWithMetadata(result);
        });
    }

    private ImageWithMetadata TransformToImageWithMetadata(ImageWithMetadata source, StackInfo? stackInfo = null)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        var confocalMetadata = source.ConfocalMetadata;

        confocalMetadata = (confocalMetadata ?? new()) with
        {
            PixelSizeX = ConfocalData.FieldWidth / source.Image.Width,
            PixelSizeY = ConfocalData.FieldHeight / source.Image.Height,
            LightWavelength = ConfocalData.LaserColor,
            LightIntensity = ConfocalData.Intensity,
            WorkingDistance = _linearStage.CurrentPosition,
            Mode = _filterHandler.FilterPosition == FilterType.Fluorescence ?
                    Imaging.Common.Metadata.Confocal.LuminescenceMode.Fluorescence :
                    Imaging.Common.Metadata.Confocal.LuminescenceMode.Reflection,
            StackInfo = stackInfo,
            Gain = ConfocalData.Gain,
            Dwell = ConfocalData.Dwell,
            ADC = ConfocalData.ADC,
            //ImagePath = "",
            PinholePosition = _imageController.GetPinHoleWheelPosition(),
            FilterWheelColor = _imageController.GetFilterWheelColor(),
        };

        return source with
        {
            ImageId = source.ImageId == Guid.Empty ? UUIDNext.Uuid.NewSequential() : source.ImageId,
            ConfocalMetadata = confocalMetadata,
            TiffMetadata = source.TiffMetadata! with
            {
                CameraModel = _cameraConfig.CameraName,
            },
            Coordinates = new()
            {
                PixelSize = new()
                {
                    X = ConfocalData.FieldWidth / source.Image.Width,
                    Y = ConfocalData.FieldHeight / source.Image.Height,
                },
                ElectronBeamStagePosition = _instrument.CurrentStageState.CurrentPosition,
                ImageSize = new()
                {
                    Width = source.Image.Width,
                    Height = source.Image.Height
                },
                CameraView = StageCameraView.Confocal
            }
        };
    }

    /// <summary>
    /// Stop capturing by canceling the provided token.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartGrabbingAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            throw new InvalidOperationException("Grabbing was already started.");
        }

        await _imageGrabber.StartContinuousGrabbing(ConfocalData);
        _grabbingActive.OnNext(true);
        _grabbingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = _grabbingTokenSource.Token.Register(() =>
        {
            _imageGrabber.StopContinuousGrabbing();
            using var cts = _grabbingTokenSource;
            _grabbingTokenSource = null;
            _grabbingActive.OnNext(false);
        });
    }

    public void StopGrabbing() => _grabbingTokenSource?.Cancel();

    public async Task FocusAsync(double change, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.MoveRelativeAsync(change * _manualFocusStep, cancellationToken));
    }

    public override Task FocusAtAsync(Length focusLength, CancellationToken cancellationToken) => _linearStage.MoveAbsoluteAsync(focusLength, cancellationToken);

    public override Length GetCurrentFocusLength() => _linearStage.CurrentPosition;

    public async Task ProtractAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.ProtractAsync(cancellationToken));
        _activeAberration = Length.Zero;
        if (GetConfocalLightColorFromWaveLength(ConfocalData.LaserColor) is ConfocalLight color)
        {
            await SetActiveAberration(color, cancellationToken);
        }
    }

    public async Task RetractAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.RetractAsync(cancellationToken));
        _activeAberration = Length.Zero;
    }

    public async Task MoveLinearStageToAsync(Length position, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.MoveAbsoluteAsync(position, cancellationToken));
    }

    public async Task MoveLinearStageRelativeAsync(Length position, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.MoveRelativeAsync(position, cancellationToken));
    }

    //private ConfocalLightColor GetColorFromWaveLength(Length lightWavelength)
    //{
    //    var waveLength = lightWavelength.Nanometers;

    //    if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight1.Wavelength.Nanometers)
    //    {
    //        return ConfocalLightColor.COLOR1;
    //    }
    //    else if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight2.Wavelength.Nanometers)
    //    {
    //        return ConfocalLightColor.COLOR2;
    //    }
    //    else if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight3.Wavelength.Nanometers)
    //    {
    //        return ConfocalLightColor.COLOR3;
    //    }
    //    else
    //    {
    //        return ConfocalLightColor.COLOR4;
    //    }
    //}

    private ConfocalLight GetConfocalLightColorFromWaveLength(Length lightWavelength)
    {
        var waveLength = lightWavelength.Nanometers;

        if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight1.Wavelength.Nanometers)
        {
            return _confocalConfig.ConfocalLights.ConfocalLight1;
        }
        else if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight2.Wavelength.Nanometers)
        {
            return _confocalConfig.ConfocalLights.ConfocalLight2;
        }
        else if (waveLength == _confocalConfig.ConfocalLights.ConfocalLight3.Wavelength.Nanometers)
        {
            return _confocalConfig.ConfocalLights.ConfocalLight3;
        }
        else
        {
            return _confocalConfig.ConfocalLights.ConfocalLight4;
        }
    }

    //private Length GetWaveLengthFromLightColor(ConfocalLightColor lightColor)
    //{
    //    return lightColor switch
    //    {
    //        ConfocalLightColor.COLOR1 => _confocalConfig.ConfocalLights.ConfocalLight1.Wavelength,
    //        ConfocalLightColor.COLOR2 => _confocalConfig.ConfocalLights.ConfocalLight2.Wavelength,
    //        ConfocalLightColor.COLOR3 => _confocalConfig.ConfocalLights.ConfocalLight3.Wavelength,
    //        ConfocalLightColor.COLOR4 => _confocalConfig.ConfocalLights.ConfocalLight4.Wavelength,
    //        _ => throw new NotImplementedException($"ConfocalMode, light color {lightColor} is not supported"),
    //    };
    //}

    public async Task RestoreImageState(ImageMetadata imageMetadata, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.Confocal ||
            !_linearStage.GetIsProtracted() ||
            imageMetadata.ConfocalMetadata == null
        )
        {
            return;
        }

        var confocalMetadata = imageMetadata.ConfocalMetadata;

        var linearStagePosition = confocalMetadata!.WorkingDistance;
        var stagePosition = imageMetadata.GetStagePosition();
        if (linearStagePosition < _linearStage.CurrentPosition)
        {
            if (linearStagePosition >= _linearStageAlignment.FocusMinimum && linearStagePosition <= _linearStageAlignment.FocusMaximum)
            {
                await MoveLinearStageToAsync(linearStagePosition, cancellationToken);
            }
            _ = await _safeStageControlling.MoveStageAsync(stagePosition, cancellationToken);
        }
        else
        {
            _ = await _safeStageControlling.MoveStageAsync(stagePosition, cancellationToken);
            if (linearStagePosition >= _linearStageAlignment.FocusMinimum && linearStagePosition <= _linearStageAlignment.FocusMaximum)
            {
                await MoveLinearStageToAsync(linearStagePosition, cancellationToken);
            }
        }

        Dwell = Duration.FromNanoseconds(confocalMetadata!.Dwell.Value);
        Gain = Level.FromDecibels(confocalMetadata!.Gain.Value);
        ADC = confocalMetadata!.ADC;

        ConfocalData.LaserColor = confocalMetadata.LightWavelength;
        ConfocalData.Intensity = confocalMetadata!.LightIntensity;
    }

    public async Task SetComponentsBeforeGrabbing()
    {
        await SetPinHolePositionAsync(PinHoleWheelPosition, default);
        await SetFilterWheelColorAsync(LaserColor.Wavelength, default);
    }
    
    private ConfocalDevice GetDefaultConfocalData()
    {
        return new()
        {
            FieldWidth = _cameraConfig.FieldWidth,
            FieldHeight = _cameraConfig.FieldHeight,
            LaserColor = Length.FromNanometers(405),
            Intensity = ConfocalConfiguration.DefaultReflection.Intensity,
            Gain = Level.FromDecibels(1.0),
            Dwell = Duration.FromNanoseconds(0.1),
            ADC = ElectricPotential.FromVolts(0.1),
            PinHolePosition = Length.Zero,
            FilterPosition = Length.Zero,
            LuminescenceMode = _filterHandler.FilterPosition == FilterType.Fluorescence ?
                    Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Fluorescence :
                    Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Reflection,
        };
    }

    private static IEnumerable<Length> GetPinHoleSizes(PinHoleWheelAlignments alignments) =>
        alignments.PinHoleAlignments.Select(alignment => alignment.PinHoleSize);

    public void Dispose()
    {
        _grabbingTokenSource?.Dispose();
        _grabbingTokenSource = null;
    }

    public Task TurnLightOnAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task TurnLightOffAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
