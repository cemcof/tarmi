using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument.Implementation;

internal class Instrument : IInstrument
{
    private readonly ILogger _logger;
    private readonly BehaviorSubject<InstrumentMode> _modeSubject = new(InstrumentMode.StageOnly);
    private readonly BehaviorSubject<BeamState> _beamState = new(BeamState.Zero);
    private readonly BehaviorSubject<StageState> _stageState = new(StageState.Zero);
    private readonly BehaviorSubject<ChamberState> _chamberState = new(ChamberState.Zero);
    private readonly BehaviorSubject<DetectorState> _detectorState = new(DetectorState.Zero);
    private readonly BehaviorSubject<ImageFilterState> _imageFilterState = new(ImageFilterState.Zero);
    private readonly Subject<ImageWithMetadata> _imageStream = new();
    private readonly ElectronBeam _electronBeam;
    private readonly IonBeam _ionBeam;
    private readonly SemImageSource _semImageSource;
    private readonly IonImageSource _ionImageSource;
    private readonly Chamber _chamber;
    private readonly Stage _stage;
    private readonly CompositeDisposable _disposables = [];

    private ImageSourceBase? _activeImageSource = null;
    private InstrumentBeamBase? _activeBeam = null;
    private CancellationTokenSource? _activeImageSourceTokenSource;
    private CancellationTokenSource? _linkedTokenSource;
    private CompositeDisposable _activeImageSourceDisposables = [];

    private IDisposable? _beamCombineSubscription = null; // TODO: merge with _activeImageSourceDisposables later
    //private IDisposable? _imageStreamSubscription = null;

    public Instrument(
        ILogger<Instrument> logger,
        IBrickConnector brickConnector,
        Chamber chamber,
        ElectronBeam electronBeam,
        IonBeam ionBeam,
        SemImageSource semImageSource,
        IonImageSource ionImageSource,
        Stage stage
    )
    {
        _logger = logger;
        _electronBeam = electronBeam;
        _ionBeam = ionBeam;
        _semImageSource = semImageSource;
        _ionImageSource = ionImageSource;
        _stage = stage;
        _chamber = chamber;

        IsConnected = brickConnector.IsConnected;

        _disposables.Add(
            Observable.CombineLatest(
                _chamber.ChamberPressure,
                _chamber.State,
                (pressure, state) => new ChamberState
                {
                    Pressure = pressure,
                    State = state
                }
            ).Subscribe(state => _logger.Swallow(() => _chamberState.OnNext(state)))
        );

        _disposables.Add(
            Observable.CombineLatest(
                _stage.IsLinked,
                _stage.IsMoving,
                _stage.IsInError,
                _stage.CurrentPosition,
                (linked, moving, error, axes) => new StageState
                {
                    IsLinked = linked,
                    IsMoving = moving,
                    IsInError = error,
                    CurrentPosition = axes
                }
            ).Subscribe(state => _logger.Swallow(() => _stageState.OnNext(state)))
         );
    }

    public IObservable<bool> IsConnected { get; }
    public IObservable<InstrumentMode> Mode => _modeSubject.AsObservable().DistinctUntilChanged();
    public InstrumentMode ActiveMode => _modeSubject.Value;
    public IObservable<ImageWithMetadata> ImageStream => _imageStream.AsObservable();
    public IObservable<ChamberState> Chamber => _chamberState.AsObservable();
    public IObservable<StageState> Stage => _stageState.AsObservable();
    public IObservable<BeamState> Beam => _beamState.AsObservable();
    public IObservable<DetectorState> Detector => _detectorState.AsObservable();
    public IObservable<ImageFilterState> ImageFilter => _imageFilterState.AsObservable();

    public StageState CurrentStageState => _stageState.Value;
    public ChamberState CurrentChamberState => _chamberState.Value;

    public BeamState CurrentBeamState
    {
        get
        {
            if (ActiveMode == InstrumentMode.StageOnly)
            {
                throw new InvalidOperationException("Cannot provide beam state in StageOnly mode");
            }
            return _beamState.Value;
        }
    }

    public DetectorState CurrentDetectorState
    {
        get
        {
            if (ActiveMode == InstrumentMode.StageOnly)
            {
                throw new InvalidOperationException("Cannot provide detector state in StageOnly mode");
            }
            return _detectorState.Value;
        }
    }

    public ImageFilterState CurrentImageFilterState
    {
        get
        {
            if (ActiveMode == InstrumentMode.StageOnly)
            {
                throw new InvalidOperationException("Cannot provide image filter state in StageOnly mode");
            }
            return _imageFilterState.Value;
        }
    }

    private TimeSpan GetApproximateFrameAcquisitionTime()
    {
        var resolution = CurrentBeamState.Resolution;
        var lineIntegration = CurrentBeamState.LineIntegration;
        var frameIntegration = CurrentImageFilterState.Frames;

        var lineTime = resolution.Width * CurrentBeamState.DwellTime * lineIntegration;
        var frameTime = lineTime * resolution.Height;
        var totalTime = frameTime * frameIntegration;
        totalTime *= 3; // triple the time for graceful frame integration and slow CPUs in case of small dwells

        return totalTime.ToTimeSpan();
    }

    public ImageWithMetadata GrabImage()
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot grab image in StageOnly mode");
        }

        var timeout = GetApproximateFrameAcquisitionTime();

        var result = _activeImageSource!.GrabImage(timeout);
        if (result.IsSuccess)
        {
            return result.Value!;
        }

        throw new InvalidOperationException("Failed to grab image", result.Exception);
    }

    public void StartImageStream(CancellationToken cancellationToken)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot start image stream in StageOnly mode");
        }
        if (_activeImageSourceTokenSource is not null)
        {
            throw new InvalidOperationException("Image stream is already running");
        }

        _activeImageSourceTokenSource = new();
        _linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _activeImageSourceTokenSource.Token);

        _ = _linkedTokenSource.Token.Register(() =>
        {
            _activeImageSourceTokenSource?.Dispose();
            _activeImageSourceTokenSource = null;
            _linkedTokenSource?.Dispose();
            _linkedTokenSource = null;
        }); 

        var result = _activeImageSource!.StartImageStream(_linkedTokenSource.Token);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Failed to grab image", result.Exception);
        }
    }

    private void SubscribeState(InstrumentMode mode)
    {
        _beamCombineSubscription?.Dispose();
        if (mode == InstrumentMode.StageOnly) { return; }

        InstrumentBeamBase beam = mode == InstrumentMode.Sem ? _electronBeam : _ionBeam;

        _beamCombineSubscription =
            Observable.CombineLatest(
                Observable.CombineLatest(beam.BeamIsOn, beam.BeamIsBlanked, beam.HV, beam.BeamCurrentIndex, beam.FreeWorkingDistance, beam.HorizontalFieldWidth, beam.VerticalFieldWidth, (on, blanked, hv, beamCurrentIndex, fwd, hfw, vfw) => (on, blanked, hv, beamCurrentIndex, fwd, hfw, vfw)),
                Observable.CombineLatest(beam.BeamShift, beam.Stigmator, beam.ScanRotation, beam.DwellTime, beam.ScanResolution, beam.ScanPixelSize, beam.Gas, (shift, stigmator, rotation, /*spot,*/ dwell, resolution, pixelSize, gas) => (shift, stigmator, rotation, dwell, resolution, pixelSize, gas)),
                Observable.CombineLatest(beam.ScanInterlacing, beam.LineIntegration, beam.LensMode, beam.BeamCurrents, (scanInterlacing, lineIntegration, lensMode, beamCurrents) => (scanInterlacing, lineIntegration, lensMode, beamCurrents)),
                (
                    beamPart1,
                    beamPart2,
                    beamPart3
                )
                    => new BeamState
                    {
                        IsOn = beamPart1.on,
                        IsBlanked = beamPart1.blanked,
                        HV = beamPart1.hv,
                        BeamCurrentIndex = beamPart1.beamCurrentIndex,
                        FreeWorkingDistance = beamPart1.fwd,
                        HorizontalFieldWidth = beamPart1.hfw,
                        VerticalFieldWidth = beamPart1.vfw,
                        BeamShift = beamPart2.shift,
                        Stigmator = beamPart2.stigmator,
                        ScanRotation = beamPart2.rotation,
                        DwellTime = beamPart2.dwell,
                        Resolution = beamPart2.resolution,
                        PixelSize = beamPart2.pixelSize,
                        Gas = beamPart2.gas,
                        ScanInterlacing = beamPart3.scanInterlacing,
                        LineIntegration = beamPart3.lineIntegration,
                        LensMode = beamPart3.lensMode,
                        BeamCurrents = beamPart3.beamCurrents
                    }
            )
            .Subscribe(state => _logger.Swallow(() => _beamState.OnNext(state)));
    }

    public async Task SwitchMode(InstrumentMode mode)
    {
        if (mode == ActiveMode) { return; }

        _activeImageSourceDisposables.Dispose();
        _activeImageSourceTokenSource?.Cancel();
        _activeImageSourceTokenSource?.Dispose();
        _activeImageSourceTokenSource = null;
        _linkedTokenSource?.Dispose();
        _linkedTokenSource = null;

        if (_activeImageSource is not null)
        {
            await _activeImageSource.Deactivate();
        }

        if (_activeBeam is not null)
        {
            await _activeBeam.Deactivate();
        }

        // subscribe state for immediate changes publishing
        SubscribeState(mode);

        _activeImageSourceDisposables = [];
        _activeBeam = mode switch { InstrumentMode.Sem => _electronBeam, InstrumentMode.Fib => _ionBeam, _ => null };
        _activeImageSource = mode switch { InstrumentMode.Sem => _semImageSource, InstrumentMode.Fib => _ionImageSource, _ => null };

        if (_activeBeam is not null)
        {
            await _activeBeam.Activate();
            _activeBeam.EnforceScanRotation();
        }

        if (_activeImageSource is not null)
        {
            _activeImageSourceDisposables.Add(
                _activeImageSource.ImageStream.Subscribe(_imageStream.OnNext)
            );
            await _activeImageSource.Activate();
        }

        _activeImageSourceDisposables = [];
        if (_activeImageSource is not null)
        {
            _activeImageSourceDisposables.Add(_activeImageSource.Detector.Subscribe(_detectorState.OnNext));
            _activeImageSourceDisposables.Add(_activeImageSource.ImageFilter.Subscribe(_imageFilterState.OnNext));
        }

        // publish mode change
        _logger.Swallow(() => _modeSubject.OnNext(mode));
    }

    public Task StageMove(StagePosition axesPositions)
    {
        return Task.Run(() =>
        {
            var result = _stage.Move(axesPositions);
            if (!result.IsSuccess)
            {
                throw result.Exception!;
            }
        });
    }

    public Task StageMoveBy(StagePosition axesOffsets)
    {
        return Task.Run(() =>
        {
            var result = _stage.MoveBy(axesOffsets);
            if (!result.IsSuccess)
            {
                throw result.Exception!;
            }
        });
    }

    public Task StageStopMoving()
    {
        return Task.Run(() =>
        {
            var result = _stage.Stop();
            if (!result.IsSuccess)
            {
                throw result.Exception!;
            }
        });
    }

    public void SetBeamCurrentIndex(int currentIndex)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam current in StageOnly mode");
        }

        var result = _activeBeam!.SetBeamCurrentIndex(currentIndex);
    }

    public Length GetBeamFreeWorkingDistance()
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot get beam free working distance in StageOnly mode");
        }

        var result = _activeBeam!.GetFreeWorkingDistance();

        return !result.IsSuccess ? throw result.Exception! : result.Value;
    }

    public void SetBeamFreeWorkingDistance(Length value)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam free working distance in StageOnly mode");
        }

        var result = _activeBeam!.SetFreeWorkingDistance(value);
        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public Length GetHorizontalFieldWidth()
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot get beam horizontal field width in StageOnly mode");
        }

        var result = _activeBeam!.GetHorizontalFieldWidth();
        return !result.IsSuccess ? throw result.Exception! : result.Value;
    }

    public void SetHorizontalFieldWidth(Length hfw)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam horizontal field width in StageOnly mode");
        }

        var result = _activeBeam!.SetHorizontalFieldWidth(hfw);
        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public LengthRangeDescriptor GetHorizontalFieldWidthRange()
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot get beam horizontal field width range in StageOnly mode");
        }

        var result = _activeBeam!.GetHorizontalFieldWidthRange();
        return result.IsSuccess ? result.Value! : throw result.Exception!;
    }

    public void SetBeamRotation(Angle rotation)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam rotation in StageOnly mode");
        }

        var result = _activeBeam!.SetBeamRotation(rotation);
        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public void SetDwellTime(Duration dwellTime)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam free working distance in StageOnly mode");
        }

        var result = _activeBeam!.SetDwellTime(dwellTime);
        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public void SetBeamOn(bool beamOn)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam free working distance in StageOnly mode");
        }

        Result result;

        if (beamOn)
        {
            result = _activeBeam!.BeamOn();
        }
        else
        {
            result = _activeBeam!.BeamOff();
        }

        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public void SetBeamBlank(bool beamBlank)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam free working distance in StageOnly mode");
        }

        Result result;

        if (beamBlank)
        {
            result = _activeBeam!.BlankBeam();
        }
        else
        {
            result = _activeBeam!.UnblankBeam();
        }

        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public void SetResolution(Resolution resolution)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam resolution in StageOnly mode");
        }
        var result = _activeBeam!.SetResolution(resolution);
        if (!result.IsSuccess)
        {
            throw result.Exception!;
        }
    }

    public LengthRangeDescriptorWithStep GetBeamFreeWorkingDistanceRange()
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot get beam free working distance range in StageOnly mode");
        }

        var result = _activeBeam!.GetFreeWorkingDistanceRange();
        return result.IsSuccess ? result.Value! : throw result.Exception!;
    }

    public Task AutoFocus(CancellationToken cancellationToken)
    {
        return ActiveMode == InstrumentMode.StageOnly
            ? throw new InvalidOperationException("Cannot run auto-focus in StageOnly mode")
            : _activeImageSource!.AutoFocus(cancellationToken);
    }

    public Task AutoStigmation(CancellationToken cancellationToken)
    {
        return ActiveMode == InstrumentMode.StageOnly
            ? throw new InvalidOperationException("Cannot run auto-stigmation in StageOnly mode")
            : _activeImageSource!.AutoStigmation(cancellationToken);
    }

    public Task AutoContrastBrightness(CancellationToken cancellationToken)
    {
        return ActiveMode == InstrumentMode.StageOnly
            ? throw new InvalidOperationException("Cannot run auto contrast brightness in StageOnly mode")
            : _activeImageSource!.AutoContrastBrightness(cancellationToken);
    }

    public void Dispose()
    {
        _beamCombineSubscription?.Dispose();
        _disposables.Dispose();
        _activeImageSourceTokenSource?.Dispose();
        _linkedTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }

    public StageLimits GetStageLimits()
    {
        Result<StageLimits>? result = _stage.GetStageLimits();
        return result.IsSuccess ? result.Value! : throw result.Exception!;
    }

    public void ClearMillingDefinitions()
    {
        if (_modeSubject.Value != InstrumentMode.Fib)
        {
            throw new InvalidOperationException("Patterning functionality is available only in FIB mode");
        }
        _ionImageSource.ClearMillingDefinitions();

    }
    public void AddMillingDefinition(RatioRectangle rectangle)
    {
        if (_modeSubject.Value != InstrumentMode.Fib)
        {
            throw new InvalidOperationException("Patterning functionality is available only in FIB mode");
        }
        _ionImageSource.AddMillingDefinition(_ionBeam, rectangle);
    }

    public void SetReducedArea(BeamCoordinatesRectangle rectangle, Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set reduced area in StageOnly mode");
        }

        var fullFrameResolution = _activeBeam?.GetResolution().Value;
        if (fullFrameResolution is not null)
        {
            _activeImageSource!.SetReducedAreaFilterPresets(imageFilterType, frames);
            _ = _activeBeam!.SetReducedAreaMode(fullFrameResolution, rectangle, dwellTime.ToTimeSpan(), lineIntegration);
        }
    }

    public void SetFullFrameMode(Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set full frame in StageOnly mode");
        }

        _activeImageSource!.SetFullFrameFilterPresets(imageFilterType, frames);
        _ = _activeBeam!.SetFullFrameMode(dwellTime.ToTimeSpan(), lineIntegration);
    }
}
