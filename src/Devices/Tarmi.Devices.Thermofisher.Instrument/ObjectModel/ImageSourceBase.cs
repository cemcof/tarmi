using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Tarmi.Devices.Thermofisher.Instrument.ObjectModel.Abstractions;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Imaging.Algorithms.Helpers;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.OpenCvWrapper;
using Fei.Imaging.gen;
using Fei.XT.Imaging.gen;
using Fei.XT.Instrument.gen;
using Fei.XT.Server.BrickConnector;
using Fei.XT.ViewServer.gen;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.Devices.Thermofisher.Instrument.ObjectModel;

internal record ImageSourceDefinitions
{
    public required VsViewType ViewType { get; init; }
    public required enDBBeamType BeamType { get; init; }
    public required int CompositeImageEventsClientId { get; init; }
}

internal abstract class ImageSourceBase
{
    private readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

    protected readonly ILogger _logger;
    private readonly IXtObjectsCollection _xtObjectsCollection;
    protected readonly IXtObjectHandle<ViewServer> _viewServer;
    protected readonly IXtObjectHandle<View> _view;
    protected ImageDataSource? _dataSource;
    private IDisposable? _pollingDisposable;
    private readonly int _compositeImageEventsClientId;
    private readonly ICompositeImageEvents _imageStreamEvents;
    private readonly object _imageAccessLock = new();
    private volatile bool _isImageStreamActive;
    private volatile bool _isActivated;
    private readonly BehaviorSubject<DetectorState> _detectorStateSubject = new(DetectorState.Zero);
    private readonly BehaviorSubject<ImageFilterState> _imageFilterStateSubject = new(ImageFilterState.Zero);
    //private CompositeDisposable _disposables = [];
    private CompositeDisposable _detectorDisposables = [];
    private readonly Subject<ImageWithMetadata> _imageStreamProducer = new();
    private readonly enDBBeamType _beamType;
    private Filter? _fullFrameFilterPresets;
    private Filter? _reducedAreaFilterPresets;

    public IObservable<ImageWithMetadata> ImageStream => _imageStreamProducer;

    protected static VsViewType GetViewName(int quadIndex)
    {
        return quadIndex switch
        {
            1 => VsViewType.View1,
            2 => VsViewType.View2,
            3 => VsViewType.View3,
            4 => VsViewType.View4,
            _ => throw new NotSupportedException()
        };
    }

    protected static int GetViewIndex(VsViewType viewType)
    {
        return viewType switch
        {
            VsViewType.View1 => 1,
            VsViewType.View2 => 2,
            VsViewType.View3 => 3,
            _ => 4
        };
    }

    protected static string GetDataSourcePath(ImageSourceDefinitions imageSourceDefinitions)
    {
        return (imageSourceDefinitions.BeamType, GetViewIndex(imageSourceDefinitions.ViewType)) switch
        {
            (enDBBeamType.enElectronBeam, int i) => $"Instrument.DataSources.DataSource{i}",
            (enDBBeamType.enIonBeam, int i) => $"Instrument.DataSources.DataSource{i + 4}",
            _ => throw new NotSupportedException()
        };
    }

    protected ImageSourceBase(ILogger logger, IXtObjectsCollection xtObjectsCollection, ImageSourceDefinitions imageSourceDefinitions)
    {
        _logger = logger;
        _xtObjectsCollection = xtObjectsCollection;
        _beamType = imageSourceDefinitions.BeamType;
        _viewServer = xtObjectsCollection.GetViewServer();
        _view = xtObjectsCollection.GetView(imageSourceDefinitions.ViewType);
        _compositeImageEventsClientId = imageSourceDefinitions.CompositeImageEventsClientId;
        _imageStreamEvents = new ImageStreamEvents(OnFrameCompleted);

        _view.Connected += (obj, args) => Connect();
        _view.Disconnecting += (obj, args) => Disconnect();
    }

    public IObservable<DetectorState> Detector => _detectorStateSubject.AsObservable().DistinctUntilChanged();
    public IObservable<ImageFilterState> ImageFilter => _imageFilterStateSubject.AsObservable();


    public virtual Task Activate()
    {
        try
        {
            _viewServer.Object.ActiveView = _view.Object;
            var dualBeamDataSource = _view.Object.GetDualBeamDataSource();
            dualBeamDataSource.ActiveBeam = dualBeamDataSource.Beams.GetBeamType(_beamType);
            _isActivated = true;
            double minGain = 0.0, maxGain = 0.0, minOffset = 0.0, maxOffset = 0.0;
            _dataSource?.AttachedDetector.Gain.GetLogicalLimits(out minGain, out maxGain);
            _dataSource?.AttachedDetector.Offset.GetLogicalLimits(out minOffset, out maxOffset);
            var gain = _dataSource?.AttachedDetector.Gain.Value ?? 0.0;
            var offset = _dataSource?.AttachedDetector.Offset.Value ?? 0.0;

            // brightness = offset, contrast = gain
            _logger.Swallow(() => _detectorStateSubject.OnNext(new DetectorState
            {
                Name = _dataSource?.AttachedDetector.Name ?? "",
                Contrast = ConvertContrast(gain, minGain, maxGain),
                Brightness = ConvertBrightness(offset, minOffset, maxOffset)
            }));

            OnDetectorChanged();
            _pollingDisposable = Observable.Interval(PollingInterval).Subscribe(_ => PollingValues());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(Activate));
            throw;
        }
        return Task.CompletedTask;
    }

    public virtual Task Deactivate()
    {
        try
        {
            _pollingDisposable?.Dispose();
            _isActivated = false;
            if(_dataSource?.State == enDataSourceState.enDataSourceStateAcquiring)
            {
                _dataSource.Stop();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(Deactivate));
            throw;
        }
        return Task.CompletedTask;
    }

    private void PollingValues()
    {
        try
        {
            if (_dataSource is not null)
            {

                _logger.Swallow(() => _imageFilterStateSubject.OnNext(new ImageFilterState { Type = ToImageFilterType(_dataSource.FilterFullFrame.Type), Frames = _dataSource.FilterFullFrame.NumberOfFrames }));

                _dataSource.AttachedDetector.Gain.GetLogicalLimits(out var gainMin, out var gainMax);
                _dataSource.AttachedDetector.Offset.GetLogicalLimits(out var offsetMin, out var offsetMax);

                _logger.Swallow(() => _detectorStateSubject.OnNext(_detectorStateSubject.Value with
                {
                    Contrast = ConvertContrast(_dataSource.AttachedDetector.Gain.Value, gainMin, gainMax),
                    Brightness = ConvertBrightness(_dataSource.AttachedDetector.Offset.Value, offsetMin, offsetMax)
                }));
            }
            
        }
        catch { }
    }

    protected virtual void Connect()
    {
        _dataSource = _view.Object.GetDualBeamDataSource().Beams.GetBeamType(_beamType).Image;
        _dataSource.ImageSource.Image.RegisterClient(_imageStreamEvents, _compositeImageEventsClientId);
        _dataSource.OnStateChanged += state => DataSourceStateChangedHandler(state);

        _dataSource.OnAttachedDetectorChanged += OnDetectorChanged;

        _fullFrameFilterPresets = _dataSource.FilterFullFrame;
        _fullFrameFilterPresets.OnFilterSettingsChanged += OnFilterSettingsChanged;
        _logger.Swallow(() => _imageFilterStateSubject.OnNext(new ImageFilterState { Type = ToImageFilterType(_fullFrameFilterPresets.Type), Frames = _fullFrameFilterPresets.NumberOfFrames }));

        _reducedAreaFilterPresets = _dataSource.FilterReducedArea;
    }

    private void DataSourceStateChangedHandler(enDataSourceState state)
    {
        if (state == enDataSourceState.enDataSourceStateAcquiring)
        {
            _logger.LogDebug("Image source is acquiring");
        }
        else if (state == enDataSourceState.enDataSourceStateIdle)
        {
            _logger.LogDebug("Image source is idle");
            if (_isImageStreamActive && _isActivated)
            {
                _logger.LogDebug("Image source is idle, but image stream is active, enforcing state");
                _logger.Swallow(() => _dataSource!.Start());
            }
        }
        else
        {
            _logger.LogDebug("Image source is not acquiring, is in error state");
        }
    }


    private void OnFilterSettingsChanged(enFilterType FilterType, int Factor)
    {
        _logger.Swallow(() => _imageFilterStateSubject.OnNext(new ImageFilterState { Type = ToImageFilterType(FilterType), Frames = Factor }));
    }

    protected virtual void Disconnect()
    {
    }

    public void SetFullFrameFilterPresets(ImageFilterType imageFilterType, int frames)
    {
        _fullFrameFilterPresets!.Type = imageFilterType switch
        {
            ImageFilterType.Average => enFilterType.enFilterTypeAverage,
            ImageFilterType.Integrate => enFilterType.enFilterTypeIntegrate,
            _ => enFilterType.enFilterTypeNone
        };
        if (imageFilterType != ImageFilterType.None)
        {
            _fullFrameFilterPresets!.NumberOfFrames = frames;
        }

    }

    public (ImageFilterType imageFilterType, int frames) GetFullFrameFilterPresets()
    {
        return (ToImageFilterType(_fullFrameFilterPresets!.Type), _fullFrameFilterPresets.NumberOfFrames);
    }

    public void SetReducedAreaFilterPresets(ImageFilterType imageFilterType, int frames)
    {
        _reducedAreaFilterPresets!.Type = imageFilterType switch
        {
            ImageFilterType.Average => enFilterType.enFilterTypeAverage,
            ImageFilterType.Integrate => enFilterType.enFilterTypeIntegrate,
            _ => enFilterType.enFilterTypeNone
        };
        if (imageFilterType != ImageFilterType.None)
        {
            _reducedAreaFilterPresets!.NumberOfFrames = frames;
        }
    }

    public (ImageFilterType imageFilterType, int frames) GetReducedAreaFilterPresets()
    {
        return (ToImageFilterType(_reducedAreaFilterPresets!.Type), _reducedAreaFilterPresets.NumberOfFrames);
    }

    private static ImageFilterType ToImageFilterType(enFilterType filterType)
        => filterType switch { enFilterType.enFilterTypeAverage => ImageFilterType.Average, enFilterType.enFilterTypeIntegrate => ImageFilterType.Integrate, _ => ImageFilterType.None };

    private static Ratio ConvertBrightness(double value, double min, double max)
    {
        return Ratio.FromDecimalFractions((value - min) / (max - min));
    }

    private static Ratio ConvertContrast(double value, double min, double max)
    {
        return Ratio.FromDecimalFractions((value - min) / (max - min));
    }

    private void OnDetectorChanged()
    {
        try
        {
            var detectorName = _dataSource!.AttachedDetector.Name;
            _logger.LogInformation("Detector changed to {Detector}", detectorName);
            _detectorDisposables.Dispose();
            _detectorDisposables = [];
            _dataSource!.AttachedDetector.Gain.GetLogicalLimits(out var minGain, out var maxGain);
            _dataSource!.AttachedDetector.Offset.GetLogicalLimits(out var minOffset, out var maxOffset);
            var gain = _dataSource!.AttachedDetector.Gain.Value;
            var offset = _dataSource!.AttachedDetector.Offset.Value;

            // brightness = offset, contrast = gain
            _logger.Swallow(() => _detectorStateSubject.OnNext(new DetectorState
            {
                Name = detectorName,
                Contrast = ConvertContrast(gain, minGain, maxGain),
                Brightness = ConvertBrightness(offset, minOffset, maxOffset)
            }));

            _detectorDisposables.Add(
                Observable.FromEvent<double>(
                    h => _dataSource.AttachedDetector.Gain.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                    h => _ = h // not necessary when COM object is disconnected
                ).Subscribe(value => _logger.Swallow(() => _detectorStateSubject.OnNext(_detectorStateSubject.Value with { Contrast = ConvertContrast(value, minGain, maxGain) })))
            );

            _detectorDisposables.Add(
                Observable.FromEvent<double>(
                    h => _dataSource.AttachedDetector.Offset.OnValueChanged += new Fei.XT.Common.gen.IControlItemFloatEvents_OnValueChangedEventHandler(h),
                    h => _ = h // not necessary when COM object is disconnected
                ).Subscribe(value => _logger.Swallow(() => _detectorStateSubject.OnNext(_detectorStateSubject.Value with { Brightness = ConvertContrast(value, minOffset, maxOffset) })))
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(OnDetectorChanged));
        }
    }

    public Result<bool> GetIsAcquiring()
    {
        try
        {
            return new(_dataSource?.State == enDataSourceState.enDataSourceStateAcquiring);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(GetIsAcquiring));
            return ex.MapToResult<bool>();
        }
    }

    private static unsafe IImage GetImageFromRawImage(IntPtr rawImagePtr, int width, int height, int bits, RawImageEncoding imageEncoding)
    {
        // aligned full frame images does not have strides, so we don't need to calculate it
        var sourceDepth = bits / 8;
        var sourceLength = width * height * sourceDepth;

        var matType = sourceDepth switch
        {
            1 => MatType.CV_8UC1,
            2 => MatType.CV_16UC1,
            3 => MatType.CV_8UC3,
            _ => throw new NotSupportedException($"Unsupported source depth: {sourceDepth}")
        };

        var mat = new Mat(height, width, matType);
        MemoryHelpers.CopyMemory((void*)rawImagePtr, (void*)mat.Data, sourceLength);

        // always output BRG image 8-bit, or grayscale 8-bit
        if (sourceDepth == 1)
        {
            return Image<Gray, byte>.FromMat(mat);
        }
        else if (sourceDepth == 2)
        {
            using var tmpImage = Image<Gray, ushort>.FromMat(mat);
            return tmpImage.Convert<Gray, byte>();
        }
        else if (imageEncoding == RawImageEncoding.RawImage_RGB)
        {
            using var tmpImage = Image<Rgb, byte>.FromMat(mat);
            return tmpImage.Convert<Bgr, byte>();
        }
        else
        {
            return Image<Bgr, byte>.FromMat(mat);
        }
    }

    public Result<ImageWithMetadata> GrabImage(TimeSpan timeout)
    {
        ImageWithMetadata result;
        lock (_imageAccessLock)
        {
            try
            {
                if (_dataSource!.State != enDataSourceState.enDataSourceStateAcquiring)
                {
                    _dataSource!.Start();
                }

                var rawImage = _dataSource!.ImageSource.Image
                    .GetImage(enGetImageValidation.enGetImageValidation_ValidFrame, 1, timeout.TotalSeconds)
                    .Clone(RawImageAccess.RawImage_ReadOnly);
                var memento = _dataSource!.ImageSource.Memento;

                var imageDescriptor = rawImage.Descriptor;
                var sourceImageWidth = imageDescriptor.Width;
                var sourceImageHeight = imageDescriptor.Height;
                var sourceBitsPerSample = imageDescriptor.Bits[0];
                //var offset = imageDescriptor.Offset[0];
                var encoding = rawImage.Descriptor.Encoding[0];

                var imagePtr = rawImage.GetReadAccess().Ptr();

                var image = GetImageFromRawImage(imagePtr, sourceImageWidth, sourceImageHeight, sourceBitsPerSample, encoding);
                var xmlMetadata = memento.ToXmlMetadata(_xtObjectsCollection);
                var coordinatesMetadata = new Imaging.Common.Metadata.Coordinates.Metadata
                {
                    PixelSize = new()
                    {
                        X = xmlMetadata.BinaryResult!.PixelSize!.X.ToLength(),
                        Y = xmlMetadata.BinaryResult!.PixelSize!.Y.ToLength()
                    },
                    ImageSize = new()
                    {
                        Width = sourceImageWidth,
                        Height = sourceImageHeight
                    },
                    ElectronBeamStagePosition = xmlMetadata.StageSettings!.StagePosition
                };

                result = new ImageWithMetadata
                {
                    Image = image,
                    MemoryOrigin = true,
                    TiffMetadata = new()
                    {
                        TimeOfAcquisition = DateTimeOffset.Now
                    },
                    ImageId = UUIDNext.Uuid.NewSequential(),
                    FeiXmlMetadata = xmlMetadata,
                    Coordinates = coordinatesMetadata,
                    // TODO: ini metadata + memento serialization
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to grab image");
                result = ImageWithMetadata.Empty;
            }
            finally
            {
                _logger.Swallow(() => _dataSource!.Stop());
            }
        }

        return new Result<ImageWithMetadata>(result);
    }

    public Result StartImageStream(CancellationToken cancellationToken)
    {
        try
        {
            if (_isImageStreamActive)
            {
                throw new InvalidOperationException("Image stream is already active");
            }

            _ = cancellationToken.Register(() =>
            {
                _isImageStreamActive = false;
                _logger.Swallow(() => _dataSource!.Stop());
            });

            _isImageStreamActive = true;
            if (_dataSource?.State != enDataSourceState.enDataSourceStateAcquiring)
            {
                _logger.Swallow(() => _dataSource!.Start());
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, nameof(StartImageStream));
            return ex.MapToResult();
        }
    }

    private void OnFrameCompleted()
    {
        if (_isImageStreamActive)
        {
            _logger.LogDebug("Image frame available");
            ProcessStreamFrame();
        }
    }

    private void ProcessStreamFrame()
    {
        try
        {
            ImageWithMetadata result;
            lock (_imageAccessLock)
            {
                // small timeout, image frame is ready
                var rawImage = _dataSource!.ImageSource.Image
                    .GetImage(enGetImageValidation.enGetImageValidation_ValidFrame, 0, TimeSpan.FromSeconds(1).TotalSeconds)
                    .Clone(RawImageAccess.RawImage_ReadOnly);
                var memento = _dataSource!.ImageSource.Memento;

                var imageDescriptor = rawImage.Descriptor;
                var sourceImageWidth = imageDescriptor.Width;
                var sourceImageHeight = imageDescriptor.Height;
                var sourceBitsPerSample = imageDescriptor.Bits[0];
                //var offset = imageDescriptor.Offset[0];
                var encoding = rawImage.Descriptor.Encoding[0];

                var imagePtr = rawImage.GetReadAccess().Ptr();

                var mat = GetImageFromRawImage(imagePtr, sourceImageWidth, sourceImageHeight, sourceBitsPerSample, encoding);
                var xmlMetadata = memento.ToXmlMetadata(_xtObjectsCollection);

                var coordinatesMetadata = new Imaging.Common.Metadata.Coordinates.Metadata
                {
                    PixelSize = new()
                    {
                        X = xmlMetadata.BinaryResult!.PixelSize!.X.ToLength(),
                        Y = xmlMetadata.BinaryResult!.PixelSize!.Y.ToLength()
                    },
                    ImageSize = new()
                    {
                        Width = sourceImageWidth,
                        Height = sourceImageHeight
                    },
                    ElectronBeamStagePosition = xmlMetadata.StageSettings!.StagePosition
                };

                result = new ImageWithMetadata
                {
                    Image = mat,
                    MemoryOrigin = true,
                    TiffMetadata = new() { TimeOfAcquisition = DateTimeOffset.Now },
                    ImageId = UUIDNext.Uuid.NewSequential(),
                    FeiXmlMetadata = xmlMetadata,
                    Coordinates = coordinatesMetadata,
                    // TODO: ini metadata + memento serialization
                };
            }

            _logger.Swallow(() => _imageStreamProducer.OnNext(result));
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, nameof(ProcessStreamFrame));
        }
    }

    public abstract Task AutoFocus(CancellationToken cancellationToken);
    public abstract Task AutoStigmation(CancellationToken cancellationToken);
    public abstract Task AutoContrastBrightness(CancellationToken cancellationToken);
}
