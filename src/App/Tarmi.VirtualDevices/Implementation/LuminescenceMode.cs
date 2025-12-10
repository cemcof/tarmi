using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Basler.Camera;
using Tarmi.Devices.SmarAct.Stage;
using Tarmi.Devices.Thermofisher.Instrument;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Devices.Thorlabs.Light;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Models;
using Tarmi.Configuration;
using Tarmi.Configuration.Alignments;
using Tarmi.Configuration.Application;
using Tarmi.Configuration.Devices;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using UnitsNet;
using ImageMetadata = Tarmi.Imaging.Common.ImageMetadata;
using LightColor = Tarmi.Devices.Thorlabs.Light.LightColor;

namespace Tarmi.VirtualDevices.Implementation;

public sealed class LuminescenceMode : StageControllingModeBase, ILuminescenceMode, IDisposable
{
    private CancellationTokenSource? _grabbingTokenSource;
    private readonly ILogger _logger;
    private readonly ILinearStage _linearStage;
    private readonly LinearStageAlignment _linearStageAlignment;
    private readonly IImageGrabber _imageGrabber;
    private readonly ILightController _lightController;
    private readonly IFilterHandler _filterHandler;
    private readonly Thorlabs4100 _thorlabs4100Config;
    private readonly BaslerCamera _cameraConfig;
    private static readonly TimeSpan _imageGrabberOpenTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _grabImageTimeout = TimeSpan.FromSeconds(15);
    private readonly bool _simulationEnabled;
    private readonly BehaviorSubject<bool> _grabbingActive = new(false);
    private readonly LuminescenceImageController _imageController;
    private readonly Length _manualFocusStep = Length.FromMicrometers(1);
    private readonly NonblockingBufferedSubject<ImageWithMetadata> _imageGrabberSubject;
    private readonly LuminescenceAberrations _aberrations;
    private Length _activeAberration = Length.Zero;

    public Length HorizontalFieldWidth => _cameraConfig.FieldWidth;
    public Length VerticalFieldWidth => _cameraConfig.FieldHeight;
    public Length FocusStep { get; set; } = Length.FromPicometers(100);
    public IEnumerable<Length> FocusStepSizes { get; }
    public StageState StageState => _instrument.CurrentStageState;
    public IObservable<Length> LinearStagePosition => _linearStage.Position;
    public IObservable<StageState> Stage => _instrument.Stage;
    public IObservable<bool> IsProtracted => _linearStage.IsProtracted;
    public IObservable<bool> GrabbingActiveChanges => _grabbingActive.AsObservable().DistinctUntilChanged();
    public bool IsGrabbingActive => _grabbingActive.Value;
    public IObservable<ImageWithMetadata> Image => _imageGrabberSubject.AsObservable();
    public IObservable<FilterType> ModeChanges => _imageController.CurrentFilterChanges;
    public Dictionary<FilterType, LightConfiguration> DefaultLightConfigurations => _imageController.DefaultLightConfigurations;

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(LuminescenceMode)}::{methodName}";

    public LuminescenceMode(
        ILogger<LuminescenceMode> logger,
        ICameraDiscoveryService cameraDiscoveryService,
        IImageGraberFactory imageGrabberFactory,
        ILightControllerFactory lightControllerFactory,
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
        _lightController = lightControllerFactory.CreateLightController();
        _filterHandler = filterHandlerFactory.CreateFilterHandler();
        _linearStage = linearStage;
        _linearStageAlignment = applicationConfig.Microscope.Alignment.LinearStage;
        _simulationEnabled = applicationConfig.Simulation.Enabled;
        _thorlabs4100Config = applicationConfig.Microscope.Thorlabs4100;
        _cameraConfig = applicationConfig.Microscope.BaslerCamera;

        _aberrations = applicationConfig.UserPreferences.LuminescenceAberrations;
        FocusStepSizes = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Order().ToArray();
        var focusStepMidIdx = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Count / 2;

        if (applicationConfig.UserPreferences.LinearStageFocus.FocusSteps.Count > 0)
        {
            FocusStep = applicationConfig.UserPreferences.LinearStageFocus.FocusSteps[focusStepMidIdx];
        }

        cameraDiscoveryService.Refresh();
        var cameras = cameraDiscoveryService.GetCameras();
        var camera = cameras.FirstOrDefault(ci => ci.Name == _cameraConfig.CameraName);
        if (camera is null)
        {
            _logger.LogCritical("Invalid camera name in configuration: {CameraName}", _cameraConfig.CameraName);
            camera = cameras[0];
        }
        _imageGrabber = imageGrabberFactory.CreateGrabber(camera);
        _imageGrabberSubject = new(
            bufferSize: 1,
            fullMode: System.Threading.Channels.BoundedChannelFullMode.DropNewest,
            itemDropped: _ => _logger.LogDebug("Image dropped due newer image available.")
        );

        _ = _imageGrabber.GrabbedImage.Select(grabbed => TransformToImageWithMetadata(grabbed)).Subscribe(_imageGrabberSubject.OnNext);
        _imageController = new(_lightController, _filterHandler, _imageGrabber, applicationConfig);
    }

    private void AssignWithStoppedGrabbing(Action assignment)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            _imageGrabber.StopContinuousGrabbing();
        }
        assignment.Invoke();
        if (_grabbingTokenSource is not null)
        {
            _imageGrabber.StartContinuousGrabbing();
        }
    }

    public async Task<FilterType> GetModeAsync()
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        return await _filterHandler.ReadFilterPositionAsync();
    }

    public async Task ChangeModeAsync(FilterType mode)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _imageController.SwitchFilter(mode, default);
        if (_lightController.ActiveLight is LightColor color)
        {
            await SetActiveAberration(color, default);
        }
        //_ = await _filterHandler.SwitchFilterAsync(mode, CancellationToken.None);
        if (_simulationEnabled && _imageGrabber is ISimulatedImageGrabber simulatedGrabber)
        {
            simulatedGrabber.ImageFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mode == FilterType.Fluorescence ? "fluorescence.tif" : "reflection.tif"));
            simulatedGrabber.SimulationMode = SimulationImageMode.File;
            simulatedGrabber.Width = _cameraConfig.Width;
            simulatedGrabber.Height = _cameraConfig.Height;
        }
    }

    public RangeDescriptor<Duration> ExposureTimeRange => _imageGrabber.ExposureTimeRange;
    public Duration ExposureTime
    {
        get => _imageGrabber.ExposureTime;
        set => _imageController.SetExposure(value); // _imageGrabber.ExposureTime = value;
    }

    public LightColor? ActiveLightColor => _lightController.ActiveLight;

    public IObservable<LightColor?> CurrentActiveLightColor => _lightController.CurrentActiveLight;

    public async Task TurnLightOn(LightColor color, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await SetActiveAberration(color, cancellationToken);
        await _lightController.SetActiveLightAsync(color, cancellationToken);
    }

    public async Task TurnLightOff(CancellationToken cancellationToken)
    {
        await EliminateActiveAberration(cancellationToken);
        await _lightController.SetActiveLightAsync(null, cancellationToken);
    }

    private async Task SetActiveAberration(LightColor color, CancellationToken cancellationToken)
    {
        if (_linearStage.GetIsProtracted())
        {
            await EliminateActiveAberration(cancellationToken);
            SetAberrationValue(color);
            await _logger.SwallowAsync(_linearStage.MoveRelativeAsync(_activeAberration, cancellationToken));
        }
    }

    private async Task EliminateActiveAberration(CancellationToken cancellationToken)
    {
        if (_linearStage.GetIsProtracted() && !Equals(_activeAberration, Length.Zero))
        {
            await _logger.SwallowAsync(_linearStage.MoveRelativeAsync(-_activeAberration, cancellationToken));
            _activeAberration = Length.Zero;
        }
    }

    private void SetAberrationValue(LightColor color) // TODO: optimize/refactor
    {
        if (_filterHandler.FilterPosition == FilterType.Fluorescence)
        {
            _activeAberration = _aberrations.FluorescenceAberrations.FirstOrDefault(x => x.Key.Equals(color.ToString(), StringComparison.OrdinalIgnoreCase)).Value;
        }
        else if (_filterHandler.FilterPosition == FilterType.Reflection)
        {
            _activeAberration = _aberrations.ReflectionAberrations.FirstOrDefault(x => x.Key.Equals(color.ToString(), StringComparison.OrdinalIgnoreCase)).Value;
        }
    }

    public RangeDescriptor<Ratio> IntensityRange { get; } = new()
    {
        Min = Ratio.FromPercent(0),
        Max = Ratio.FromPercent(100)
    };

    public Ratio Intensity => _lightController.Brightness;

    public async Task SetIntensityAsync(Ratio intensity, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());
        await _imageController.SetIntensity(intensity, cancellationToken);
        //await _lightController.SetBrightnessAsync(intensity, cancellationToken);
    }

    public RangeDescriptor<double> GammaRange => _imageGrabber.GammaRange;
    public double Gamma
    {
        get => _imageGrabber.Gamma;
        set => _imageGrabber.Gamma = value;
    }

    public RangeDescriptor<Level> GainRange => _imageGrabber.GainRange;
    public Level Gain
    {
        get => _imageGrabber.Gain;
        set => _imageGrabber.Gain = value;
    }

    public BinningSize Binning
    {
        get => (BinningSize)_imageGrabber.Binning;
        set => AssignWithStoppedGrabbing(() => _imageGrabber.Binning = (int)value);
    }

    public Length CurrentLinearStagePosition => _linearStage.CurrentPosition;    

    public async Task EnableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _instrument.SwitchMode(InstrumentMode.StageOnly);
        _ = await SwitchStageViewAsync(StageCameraView.LM);
        // do not protract automatically
        // await _linearStage.ProtractAsync(cancellationToken);
        _imageGrabber.Open(_imageGrabberOpenTimeout);
        _imageGrabber.Binning = 1;
        _imageGrabber.Width = _cameraConfig.Width;
        _imageGrabber.Height = _cameraConfig.Height;
        _imageGrabber.PixelFormat = ImagePixelFormat.Mono12;
        if (_simulationEnabled && _imageGrabber is ISimulatedImageGrabber simulatedGrabber)
        {
            simulatedGrabber.ImageFile = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fluorescence.tif"));
            simulatedGrabber.SimulationMode = SimulationImageMode.File;
        }
        await _lightController.Initialize(cancellationToken);
        //await _lightController.SetBrightnessAsync(Ratio.FromPercent(89), cancellationToken);
        await _lightController.SetActiveLightAsync(null, cancellationToken);
        //await ChangeModeAsync(FilterType.Reflection);
        //await _imageController.SwitchFilter(FilterType.Reflection, cancellationToken);
        await _imageController.Initialize(cancellationToken);
    }

    public async Task DisableAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            await _grabbingTokenSource.CancelAsync();
            _grabbingTokenSource = null;
        }
        //await _logger.SwallowAsync(() => _linearStage.RetractAsync(cancellationToken));
        //cancellationToken.ThrowIfCancellationRequested();
        await _logger.SwallowAsync(() => _lightController.Deinitialize(cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        _logger.Swallow(_imageGrabber.Close);
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
        return Task.Run(() =>
        {
            using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

            var result = _imageGrabber.GrabImage(_grabImageTimeout);
            result = TransformToImageWithMetadata(result);
            return result;
        });
    }

    private Length GetActiveLightWaveLength()
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        return _lightController.ActiveLight switch
        {
            LightColor.Red => _thorlabs4100Config.Lights.RedWavelength,
            LightColor.Green => _thorlabs4100Config.Lights.GreenWavelength,
            LightColor.Blue => _thorlabs4100Config.Lights.BlueWavelength,
            LightColor.UltraViolet => _thorlabs4100Config.Lights.UltraVioletWavelength,
            _ => Length.Zero,
        };
    }

    private ImageWithMetadata TransformToImageWithMetadata(ImageWithMetadata source, Tarmi.Imaging.Common.Metadata.Luminescence.StackInfo? stackInfo = null)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_cameraConfig.FlipImageOnX)
        {
            source.Image.FlipInplace(FlipMode.X);
        }

        if (_cameraConfig.FlipImageOnY)
        {
            source.Image.FlipInplace(FlipMode.Y);
        }

        return source with
        {
            LuminescenceMetadata = source.LuminescenceMetadata! with
            {
                PixelSizeX = _cameraConfig.FieldWidth / source.Image.Width,
                PixelSizeY = _cameraConfig.FieldHeight / source.Image.Height,
                LightWavelength = GetActiveLightWaveLength(),
                LightIntensity = _lightController.Brightness,
                WorkingDistance = _linearStage.CurrentPosition,
                Mode = _filterHandler.FilterPosition == FilterType.Fluorescence ?
                    Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence :
                    Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Reflection,
                StackInfo = stackInfo,
                Camera = source.LuminescenceMetadata!.Camera with
                {
                    Binning = _imageGrabber.Binning,
                    BlackLevel = _imageGrabber.BlackLevel,
                    ExposureTime = _imageGrabber.ExposureTime,
                    FrameRate = _imageGrabber.FrameRate,
                    Gamma = _imageGrabber.Gamma,
                    Gain = _imageGrabber.Gain,
                }
            },
            TiffMetadata = source.TiffMetadata! with
            {
                CameraModel = _cameraConfig.CameraName,
            },
            Coordinates = new()
            {
                PixelSize = new()
                {
                    X = _cameraConfig.FieldWidth / source.Image.Width,
                    Y = _cameraConfig.FieldHeight / source.Image.Height,
                },
                ElectronBeamStagePosition = _instrument.CurrentStageState.CurrentPosition,
                ImageSize = new()
                {
                    Width = source.Image.Width,
                    Height = source.Image.Height
                },
                CameraView = StageCameraView.LM
            }
        };
    }

    /// <summary>
    /// Stop capturing by canceling the provided token.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public Task StartGrabbingAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (_grabbingTokenSource is not null)
        {
            throw new InvalidOperationException("Grabbing was already started.");
        }
        _imageGrabber.StartContinuousGrabbing();
        _grabbingActive.OnNext(true);
        _grabbingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = _grabbingTokenSource.Token.Register(() =>
        {
            _imageGrabber.StopContinuousGrabbing();
            using var cts = _grabbingTokenSource;
            _grabbingTokenSource = null;
            _grabbingActive.OnNext(false);
        });
        return Task.CompletedTask;
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
        if (_lightController.ActiveLight is LightColor color)
        {
            await SetActiveAberration(color, cancellationToken);
        }
    }

    public async Task RetractAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _lightController.SetActiveLightAsync(null, cancellationToken);
        await _logger.SwallowAsync(() => _linearStage.RetractAsync(cancellationToken));
        _activeAberration = Length.Zero;
    }

    public async Task MoveLinearStageToAsync(Length position, CancellationToken cancellation)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.MoveAbsoluteAsync(position, cancellation));
    }

    public async Task MoveLinearStageRelativeAsync(Length position, CancellationToken cancellation)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        await _logger.SwallowAsync(() => _linearStage.MoveRelativeAsync(position, cancellation));
    }

    private LightColor GetColorFromImageMetadata(Metadata luminescenceMetadata)
    {
        var waveLength = luminescenceMetadata.LightWavelength.Nanometers;

        if (waveLength == _thorlabs4100Config.Lights.RedWavelength.Nanometers)
        {
            return LightColor.Red;
        }
        else if (waveLength == _thorlabs4100Config.Lights.GreenWavelength.Nanometers)
        {
            return LightColor.Green;
        }
        else if (waveLength == _thorlabs4100Config.Lights.BlueWavelength.Nanometers)
        {
            return LightColor.Blue;
        }
        else
        {
            return LightColor.UltraViolet;
        }
    }

    public async Task RestoreImageState(ImageMetadata imageMetadata, CancellationToken cancellation)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.LM ||
            _linearStage.GetIsProtracted() == false ||
            imageMetadata.LuminescenceMetadata == null
        )
        {
            return;
        }

        var luminescenceMetadata = imageMetadata.LuminescenceMetadata;

        var linearStagePosition = luminescenceMetadata!.WorkingDistance;
        var stagePosition = imageMetadata.GetStagePosition();
        if (linearStagePosition < _linearStage.CurrentPosition)
        {
            if (linearStagePosition >= _linearStageAlignment.FocusMinimum && linearStagePosition <= _linearStageAlignment.FocusMaximum)
            {
                await MoveLinearStageToAsync(linearStagePosition, cancellation);
            }
            _ = await _safeStageControlling.MoveStageAsync(stagePosition, cancellation);
        }
        else
        {
            _ = await _safeStageControlling.MoveStageAsync(stagePosition, cancellation);
            if (linearStagePosition >= _linearStageAlignment.FocusMinimum && linearStagePosition <= _linearStageAlignment.FocusMaximum)
            {
                await MoveLinearStageToAsync(linearStagePosition, cancellation);
            }
        }

        ExposureTime = Duration.FromMicroseconds(luminescenceMetadata!.Camera.ExposureTime.Value);
        Gain = Level.FromDecibels(luminescenceMetadata!.Camera.Gain.Value);
        Gamma = luminescenceMetadata!.Camera.Gamma;

        var light = GetColorFromImageMetadata(luminescenceMetadata);
        await _lightController.SetActiveLightAsync(light, cancellation);
        await SetIntensityAsync(luminescenceMetadata!.LightIntensity, cancellation);

        Binning = (BinningSize)luminescenceMetadata!.Camera.Binning;
    }

    public void Dispose()
    {
        _grabbingTokenSource?.Dispose();
        _grabbingTokenSource = null;
    }
}
