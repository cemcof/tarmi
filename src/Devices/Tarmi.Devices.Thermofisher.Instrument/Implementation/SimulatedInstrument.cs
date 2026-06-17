using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using AsyncAwaitBestPractices;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.TileSet.ImageSimulator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument.Implementation;

public class SimulatedInstrument : IInstrument
{
    private readonly ILogger _logger;
    private readonly ManualResetEventSlim _stageIdleEvent = new(true);
    private readonly SemaphoreSlim _stageLock = new(1, 1);
    private readonly BehaviorSubject<bool> _isConnected = new(true);
    private readonly BehaviorSubject<InstrumentMode> _modeSubject = new(InstrumentMode.StageOnly);
    private readonly Subject<ImageWithMetadata> _imageSubject = new();
    private readonly BehaviorSubject<ChamberState> _chamberStateSubject = new(GetInitialChamberState());
    private readonly BehaviorSubject<StageState> _stageStateSubject = new(GetInitialStageState());
    private readonly BehaviorSubject<BeamState> _beamStateSubject = new(GetInitialBeamState());
    private readonly BehaviorSubject<DetectorState> _detectorStateSubject = new(DetectorState.Zero);
    private readonly BehaviorSubject<ImageFilterState> _imageFilterSubject = new(ImageFilterState.Zero);

    private CancellationTokenSource? _imageStreamCts = null;
    private CancellationTokenSource? _imageStreamLinkedCts = null;
    private CancellationTokenSource? _stageMoveCts = null;

    private readonly ImageWithMetadata _defaultSemImage;
    private readonly ImageWithMetadata _defaultIonImage;
    private ITileSetImageSimulator? _imageSimulator;

    public SimulatedInstrument(ILogger<SimulatedInstrument> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _defaultSemImage = TiffImage.Load(@"sem\default-raw.tiff");
        _defaultIonImage = TiffImage.Load(@"fib\default-raw.tiff");

        Task.Run(() => InitializeImageSimulator(serviceProvider))
            .SafeFireAndForget(ex => _logger.LogError(ex, "InitializeImageSimulator"));
    }

    private void InitializeImageSimulator(IServiceProvider serviceProvider)
    {
        _imageSimulator = serviceProvider.GetService<ITileSetImageSimulator>();
        // TODO: implement if needed
    }

    private static ChamberState GetInitialChamberState()
    {
        return new ChamberState
        {
            State = VacuumCompartmentState.Pumped,
            Pressure = Simulator.Chamber.InitialValues.Pressure
        };
    }

    private static StageState GetInitialStageState()
    {
        return new StageState
        {
            IsLinked = Simulator.Stage.InitialValues.IsLinked,
            IsInError = Simulator.Stage.InitialValues.IsInError,
            IsMoving = Simulator.Stage.InitialValues.IsMoving,
            CurrentPosition = Simulator.Stage.InitialValues.CurrentPosition
        };
    }

    private static BeamState GetInitialBeamState()
    {
        return new BeamState
        {
            IsOn = Simulator.Beam.InitialValues.IsOn,
            IsBlanked = Simulator.Beam.InitialValues.IsBlanked,
            BeamShift = Simulator.Beam.InitialValues.BeamShift,
            DwellTime = Simulator.Beam.InitialValues.DwellTime,
            Stigmator = Simulator.Beam.InitialValues.Stigmator,
            FreeWorkingDistance = Simulator.Beam.InitialValues.FreeWorkingDistance,
            HV = Simulator.Beam.InitialValues.HV,
            ScanRotation = Simulator.Beam.InitialValues.ScanRotation,
            HorizontalFieldWidth = Simulator.Beam.InitialValues.HorizontalFieldWidth,
            VerticalFieldWidth = Simulator.Beam.InitialValues.VerticalFieldWidth,
            Resolution = Simulator.Beam.InitialValues.Resolution,
            PixelSize = Simulator.Beam.InitialValues.PixelSize,
            LensMode = string.Empty,
            LineIntegration = 1,
            ScanInterlacing = 1,
            Gas = string.Empty,
            BeamCurrents = Simulator.Beam.InitialValues.BeamCurrents,
            BeamCurrentIndex = Simulator.Beam.InitialValues.BeamCurrentIndex
        };
    }

    private void ThrowIfStageOnly()
    {
        if (_modeSubject.Value == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Beam functionality is not available in StageOnly mode");
        }
    }

    private void ThrowIfStageOnlyOrBeamIsOff()
    {
        ThrowIfStageOnly();

        if (!_beamStateSubject.Value.IsOn)
        {
            throw new InvalidOperationException("Beam is off");
        }
    }

    public IObservable<bool> IsConnected => _isConnected.AsObservable().DistinctUntilChanged();
    public IObservable<InstrumentMode> Mode => _modeSubject.AsObservable().DistinctUntilChanged();
    public IObservable<ImageWithMetadata> ImageStream => _imageSubject.AsObservable();
    public IObservable<ChamberState> Chamber => _chamberStateSubject.AsObservable().DistinctUntilChanged();
    public IObservable<StageState> Stage => _stageStateSubject.AsObservable().DistinctUntilChanged();
    public IObservable<BeamState> Beam => _beamStateSubject.AsObservable().DistinctUntilChanged();
    public IObservable<DetectorState> Detector => _detectorStateSubject.AsObservable().DistinctUntilChanged();
    public InstrumentMode ActiveMode => _modeSubject.Value;
    public IObservable<ImageFilterState> ImageFilter => _imageFilterSubject.AsObservable().DistinctUntilChanged();

    public StageState CurrentStageState => _stageStateSubject.Value;
    public ChamberState CurrentChamberState => _chamberStateSubject.Value;
    public DetectorState CurrentDetectorState => _detectorStateSubject.Value;
    public BeamState CurrentBeamState
    {
        get
        {
            ThrowIfStageOnly();
            return _beamStateSubject.Value;
        }
    }

    public ImageFilterState CurrentImageFilterState
    {
        get
        {
            ThrowIfStageOnly();
            return _imageFilterSubject.Value;
        }
    }

    public async Task AutoContrastBrightness(CancellationToken cancellationToken)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        // TODO: change detector values?
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        _logger.Swallow(() => _detectorStateSubject.OnNext(
            _detectorStateSubject.Value with
            {
                Contrast = Ratio.FromPercent(Random.Shared.Next(20, 80)),
                Brightness = Ratio.FromPercent(Random.Shared.Next(20, 80))
            })
        );
    }

    public async Task AutoFocus(CancellationToken cancellationToken)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        var iterations = 10;
        while (!cancellationToken.IsCancellationRequested && iterations != 0)
        {
            var newWd = Random.Shared.Next(
                (int)Simulator.Beam.Limits.FreeWorkingDistance.Min.Nanometers,
                (int)Simulator.Beam.Limits.FreeWorkingDistance.Max.Nanometers
            );
            SetBeamFreeWorkingDistance(Length.FromNanometers(newWd));
            --iterations;
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        }
    }

    public async Task AutoStigmation(CancellationToken cancellationToken)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        // TODO: change stigmator?
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
    }

    public Length GetBeamFreeWorkingDistance()
    {
        ThrowIfStageOnlyOrBeamIsOff();
        return _beamStateSubject.Value.FreeWorkingDistance;
    }

    public LengthRangeDescriptorWithStep GetBeamFreeWorkingDistanceRange()
    {
        ThrowIfStageOnly();
        return Simulator.Beam.Limits.FreeWorkingDistance;
    }

    public void SetBeamFreeWorkingDistance(Length value)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        if (!Simulator.Beam.Limits.FreeWorkingDistance.IsValueInRange(value))
        {
            throw new InvalidOperationException("Target Current is out of range");
        }

        Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();

        var beamState = _beamStateSubject.Value with { FreeWorkingDistance = value };
        _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
    }

    public Length GetHorizontalFieldWidth()
    {
        ThrowIfStageOnlyOrBeamIsOff();
        return _beamStateSubject.Value.HorizontalFieldWidth;
    }

    public void SetHorizontalFieldWidth(Length hfw)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        if (!Simulator.Beam.Limits.HorizontalFieldWidthRange.IsValueInRange(hfw))
        {
            throw new InvalidOperationException("Target HFW is out of range");
        }

        Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();

        // FYI: does not affect the image, in fact it get out of sync, just for milling definitions transfer testing
        var beamState = _beamStateSubject.Value with { HorizontalFieldWidth = hfw };
        _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
    }

    public LengthRangeDescriptor GetHorizontalFieldWidthRange()
    {
        ThrowIfStageOnlyOrBeamIsOff();
        return Simulator.Beam.Limits.HorizontalFieldWidthRange;
    }

    public void SetBeamBlank(bool beamBlank)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        if (beamBlank != _beamStateSubject.Value.IsBlanked)
        {
            Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();
            var beamState = _beamStateSubject.Value with { IsBlanked = beamBlank };
            _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
        }
    }

    public void SetBeamCurrentIndex(int currentIndex)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        if (currentIndex < Simulator.Beam.Limits.BeamCurrentIndex.Min || currentIndex > Simulator.Beam.Limits.BeamCurrentIndex.Max)
        {
            throw new InvalidOperationException("Target Current Index is out of range");
        }

        Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();
        var beamState = _beamStateSubject.Value with { BeamCurrentIndex = currentIndex };
        _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
    }

    public void SetBeamOn(bool beamOn)
    {
        ThrowIfStageOnly();

        // TODO: change beam values initial/last
        if (beamOn != _beamStateSubject.Value.IsOn)
        {
            Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();
            var beamState = _beamStateSubject.Value with { IsOn = beamOn };
            _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
        }
    }

    public void SetResolution(Resolution resolution)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam resolution in StageOnly mode");
        }
        Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();
    }

    public void SetDwellTime(Duration dwellTime)
    {
        ThrowIfStageOnlyOrBeamIsOff();

        Task.Delay(TimeSpan.FromMilliseconds(100)).SyncResult();
        var beamState = _beamStateSubject.Value with { DwellTime = dwellTime };
        _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
    }

    public void SetBeamRotation(Angle rotation)
    {
        if (ActiveMode == InstrumentMode.StageOnly)
        {
            throw new InvalidOperationException("Cannot set beam rotation in StageOnly mode");
        }

        Task.Delay(TimeSpan.FromMilliseconds(20)).SyncResult();
        var beamState = _beamStateSubject.Value with { ScanRotation = rotation };
        _logger.Swallow(() => _beamStateSubject.OnNext(beamState));
    }

    public async Task StageMove(StagePosition axesPositions)
    {
        using var guard = await _stageLock.UseOnceAsync();

        await _stageIdleEvent.WaitAsync();

        _stageMoveCts = new CancellationTokenSource();
        try
        {
            var position = CurrentStageState.CurrentPosition;
            if (position != axesPositions)
            {

                if (!Simulator.Stage.Limits.Axes.IsWithinLimits(axesPositions))
                {
                    throw new InvalidOperationException("Target position is out of stage limits");
                }

                _stageIdleEvent.Reset();
                const int rounds = 10;
                var obs = Observable.Interval(TimeSpan.FromMilliseconds(50))
                    .Take(rounds + 1)
                    .Do(i =>
                    {
                        if (CurrentStageState.CurrentPosition.Equals(axesPositions))
                        {
                            _logger.Swallow(() => _stageStateSubject.OnNext(CurrentStageState with { IsMoving = false }));
                            //_stageIdleEvent.Set();
                        }
                        else
                        {
                            var currentPosition = CurrentStageState.CurrentPosition;
                            currentPosition = new StagePosition
                            {
                                X = currentPosition.X + (axesPositions.X - position.X) / rounds,
                                Y = currentPosition.Y + (axesPositions.Y - position.Y) / rounds,
                                Z = currentPosition.Z + (axesPositions.Z - position.Z) / rounds,
                                Rotation = currentPosition.Rotation + (axesPositions.Rotation - position.Rotation) / rounds,
                                Tilt = currentPosition.Tilt + (axesPositions.Tilt - position.Tilt) / rounds
                            };
                            _logger.Swallow(() => _stageStateSubject.OnNext(CurrentStageState with { CurrentPosition = currentPosition, IsMoving = true }));
                        }
                    });

                _ = await obs.ToTask(_stageMoveCts.Token);
            }
        }
        finally
        {
            _stageMoveCts?.Dispose();
            _stageMoveCts = null;
            _stageIdleEvent.Set();
        }
    }

    public async Task StageMoveBy(StagePosition axesOffsets)
    {
        await _stageIdleEvent.WaitAsync();

        var position = CurrentStageState.CurrentPosition;
        var targetPosition = new StagePosition
        {
            X = position.X + axesOffsets.X,
            Y = position.Y + axesOffsets.Y,
            Z = position.Z + axesOffsets.Z,
            Rotation = position.Rotation + axesOffsets.Rotation,
            Tilt = position.Tilt + axesOffsets.Tilt
        };
        await StageMove(targetPosition);
    }

    public async Task StageStopMoving()
    {
        if (_stageMoveCts is not null)
        {
            await _stageMoveCts.CancelAsync();
        }
    }

    private void UpdateCoreStateFromImage(ImageWithMetadata imageWithMetadata)
    {
        var image = imageWithMetadata.Image;
        var pixelSize = imageWithMetadata.GetPixelSize();
        _logger.Swallow(() => _beamStateSubject.OnNext(_beamStateSubject.Value with
        {
            Resolution = new()
            {
                Width = image.Width,
                Height = image.Height,
                Depth = image.Depth
            },
            PixelSize = pixelSize,
            HorizontalFieldWidth = image.Width * pixelSize.X,
            VerticalFieldWidth = image.Height * pixelSize.Y,
        }));
    }

    public async Task SwitchMode(InstrumentMode mode)
    {
        if (mode == _modeSubject.Value)
        {
            return;
        }
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        _modeSubject.OnNext(mode);
        switch (mode)
        {
            case InstrumentMode.Sem:
                UpdateCoreStateFromImage(_defaultSemImage);
                _logger.Swallow(() => _detectorStateSubject.OnNext(new DetectorState
                {
                    Name = "TLD",
                    Brightness = Ratio.FromPercent(50),
                    Contrast = Ratio.FromPercent(50)
                }));
                _logger.Swallow(() => _beamStateSubject.OnNext(_beamStateSubject.Value with
                {
                    Gas = string.Empty,
                    LensMode = "Field Free"
                }));
                _logger.Swallow(() => _imageFilterSubject.OnNext(new ImageFilterState
                {
                    Type = ImageFilterType.Average,
                    Frames = 4
                }));
                break;
            case InstrumentMode.Fib:
                UpdateCoreStateFromImage(_defaultIonImage);

                _logger.Swallow(() => _detectorStateSubject.OnNext(new DetectorState
                {
                    Name = "ETD (SE)",
                    Brightness = Ratio.FromPercent(50),
                    Contrast = Ratio.FromPercent(50)
                }));
                _logger.Swallow(() => _beamStateSubject.OnNext(_beamStateSubject.Value with
                {
                    Gas = "Xenon",
                    LensMode = string.Empty
                }));
                _logger.Swallow(() => _imageFilterSubject.OnNext(new ImageFilterState
                {
                    Type = ImageFilterType.Integrate,
                    Frames = 2
                }));
                break;
        }

        if (!_beamStateSubject.Value.IsOn)
        {
            SetBeamOn(true);
        }

        // simulate some delay
        await Task.Delay(TimeSpan.FromSeconds(1));
    }

    public ImageWithMetadata GrabImage()
    {
        ThrowIfStageOnlyOrBeamIsOff();
        var image = _modeSubject.Value == InstrumentMode.Sem ? _defaultSemImage : _defaultIonImage;

        // TODO: update metadata
        // TODO: fill with black when beam blank

        if (
            _imageSimulator is not null &&
            _modeSubject.Value != InstrumentMode.StageOnly &&
            _imageSimulator.IsViewSupported(_imageSimulator.CurrentContextCameraView)
        )
        {
            image = _imageSimulator.GrabOne();
            UpdateCoreStateFromImage(image);
        }
        else
        {
            image = image.Clone();
        }
        image.Image.Mat.ApplyGaussianNoiseInplace(mean: 50, stdDev: 32);

        return UpdateImageMetadata(image);
    }

    private ImageWithMetadata UpdateImageMetadata(ImageWithMetadata image)
    {
        image = UpdateXmlImageMetadata(image);

        return image with
        {
            TiffMetadata = new Imaging.Common.Metadata.TiffMetadata
            {
                TimeOfAcquisition = DateTimeOffset.Now
            },
            ImageId = UUIDNext.Uuid.NewSequential(),
            Coordinates = new()
            {
                PixelSize = new()
                {
                    X = image.FeiXmlMetadata!.BinaryResult!.PixelSize!.X.ToLength(),
                    Y = image.FeiXmlMetadata!.BinaryResult!.PixelSize!.Y.ToLength()
                },
                ImageSize = new()
                {
                    Width = image.Image.Width,
                    Height = image.Image.Height
                },
                ElectronBeamStagePosition = image.FeiXmlMetadata!.StageSettings!.StagePosition
            }
        };
    }

    private ImageWithMetadata UpdateXmlImageMetadata(ImageWithMetadata image)
    {
        if (image.FeiXmlMetadata is Imaging.Common.Metadata.Thermofisher.XmlFormat.Metadata xmlMetadata)
        {
            // TODO: update more metadata
            var currentStageState = _stageStateSubject.Value;
            var currentBeamState = _beamStateSubject.Value;
            var updatedXmlMetadata = image.FeiXmlMetadata with
            {
                StageSettings = xmlMetadata.StageSettings! with
                {
                    StagePosition = xmlMetadata.StageSettings.StagePosition with
                    {
                        X = currentStageState.CurrentPosition.X.Meters,
                        Y = currentStageState.CurrentPosition.Y.Meters,
                        Z = currentStageState.CurrentPosition.Z.Meters,
                        Rotation = currentStageState.CurrentPosition.Rotation.Radians,
                        Tilt = new Imaging.Common.Metadata.Thermofisher.XmlFormat.Tilt()
                        {
                            Alpha = currentStageState.CurrentPosition.Tilt.Radians,
                            Beta = 0
                        }
                    }
                },
                ScanSettings = xmlMetadata.ScanSettings! with
                {
                    DwellTime = currentBeamState.DwellTime.Seconds,
                    ScanRotation = currentBeamState.ScanRotation.Radians,
                    ScanSize = new Imaging.Common.Metadata.Thermofisher.XmlFormat.Size
                    {
                        Width = image.Image.Width,
                        Height = image.Image.Height
                    }
                },
                Optics = xmlMetadata.Optics! with
                {
                    BeamCurrent = Simulator.Beam.InitialValues.BeamCurrents[currentBeamState.BeamCurrentIndex - 1].Amperes,
                    EucentricWorkingDistance = currentBeamState.FreeWorkingDistance.Meters, // ???
                    AccelerationVoltage = currentBeamState.HV.Volts,
                    BeamShift = new Imaging.Common.Metadata.Thermofisher.XmlFormat.PointD
                    {
                        X = currentBeamState.BeamShift.X.Meters,
                        Y = currentBeamState.BeamShift.Y.Meters
                    },
                    WorkingDistance = currentBeamState.FreeWorkingDistance.Meters, // ???
                    StigmatorRaw = new Imaging.Common.Metadata.Thermofisher.XmlFormat.PointD
                    {
                        X = currentBeamState.Stigmator.X.Meters,
                        Y = currentBeamState.Stigmator.Y.Meters
                    },
                    FullScanFieldOfView = new Imaging.Common.Metadata.Thermofisher.XmlFormat.PointD
                    {
                        X = currentBeamState.HorizontalFieldWidth.Meters,
                        Y = currentBeamState.VerticalFieldWidth.Meters
                    }
                }
            };

            return image with { FeiXmlMetadata = updatedXmlMetadata };
        }
        return image;
    }

    public void StartImageStream(CancellationToken cancellationToken)
    {
        ThrowIfStageOnlyOrBeamIsOff();
        if (_imageStreamCts is not null)
        {
            return;
        }

        var image = _modeSubject.Value == InstrumentMode.Sem ? _defaultSemImage : _defaultIonImage;
        TimeSpan imageDelay() => (TimeSpan)(_beamStateSubject.Value.DwellTime * image.Image.Width * image.Image.Height);

        _imageStreamCts = new CancellationTokenSource();
        _imageStreamLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _imageStreamCts.Token);
        _ = _imageStreamLinkedCts.Token.Register(() =>
        {
            _imageStreamCts = null;
            _imageStreamLinkedCts = null;
        });

        Observable
            .Generate(0, _ => true, i => i, i => i, i => imageDelay())
            .Subscribe(_ =>
            {
                var image = _logger.Swallow(() => GrabImage());
                if (image is not null && _imageStreamLinkedCts is not null && !_imageStreamLinkedCts.IsCancellationRequested)
                {
                    _logger.Swallow(() => _imageSubject.OnNext(image));
                }
            }, _imageStreamLinkedCts.Token);
    }

    public void Dispose()
    {
        _imageStreamCts?.Cancel();
        _imageStreamCts?.Dispose();
        _imageStreamLinkedCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    public StageLimits GetStageLimits() => Simulator.Stage.Limits.Axes;

    public void ClearMillingDefinitions()
    {
        if (_modeSubject.Value != InstrumentMode.Fib)
        {
            throw new InvalidOperationException("Patterning functionality is available only in FIB mode");
        }
    }
    public void AddMillingDefinition(RatioRectangle rectangle)
    {
        if (_modeSubject.Value != InstrumentMode.Fib)
        {
            throw new InvalidOperationException("Patterning functionality is available only in FIB mode");
        }
    }

    public void SetReducedArea(BeamCoordinatesRectangle rectangle, Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        // dummy simulation
        _ = rectangle;
        _ = imageFilterType;
        _ = frames;
        _ = lineIntegration;
        SetDwellTime(dwellTime);
    }

    public void SetFullFrameMode(Duration dwellTime, ImageFilterType imageFilterType, int frames, int lineIntegration)
    {
        // dummy simulation
        SetDwellTime(dwellTime);
        _logger.Swallow(() => _imageFilterSubject.OnNext(new ImageFilterState { Type = imageFilterType, Frames = frames }));
        _logger.Swallow(() => _beamStateSubject.OnNext(_beamStateSubject.Value with { LineIntegration = lineIntegration }));
    }
}
