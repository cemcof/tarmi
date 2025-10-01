using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Betrian.Imaging.Common;
using Betrian.Imaging.Common.Overlays;
using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using UnitsNet;
using System.Collections.Concurrent;
using Betrian.Models;
using System.Collections.Immutable;
using static Betrian.PointMapping.ImagesAnd5DPoints;

namespace CFLMnavi.ImagePipeline.Pipelines;

public class ImageMultiplexer
{
    private record CorrelationImage : IDisposable
    {
        public required ImageWithMetadata ImageWithMetadata { get; init; }
        public required CorrelationInfo CorrelationInfo { get; init; }
        public void Dispose() => ImageWithMetadata.Dispose();
    }

    private readonly ConcurrentDictionary<Guid, CorrelationImage> _overlayImages = [];
    private readonly BehaviorSubject<ImageWithMetadata> _output = new(ImageWithMetadata.Empty);
    private readonly CompositeDisposable _disposables = [];
    private readonly IStageNavigation _stageNavigation;
    private ImmutableDictionary<StageCameraView, Angle> _viewsPretilt;
    private CorrelationInfo _inputCorrelationInfo = new();

    public bool CorrelateByFiducials { get; set; } = false;

    public ImageMultiplexer(IObservable<ImageWithMetadata> primaryOutput, IStageNavigation stageNavigation)
    {
        _stageNavigation = stageNavigation;
        _viewsPretilt = ImmutableDictionary<StageCameraView, Angle>.Empty;
        _disposables.Add(primaryOutput.Subscribe(ApplyLayers));
    }

    public void InitializeHolderData()
    {
        _viewsPretilt = _stageNavigation.GetViewsPretilt().ToImmutableDictionary();
    }

    public IObservable<ImageWithMetadata> Output => _output.AsObservable();

    public Task<ImageWithMetadata> GetOutputCopyAsync()
    {
        var image = _output.Value;
        return Task.FromResult(image with { Image = image.Image.Clone() });
    }

    private async Task<CorrelationImage> CreateCorrelationImage(string fileName, CorrelationInfo correlationInfo)
    {
        return await Task.Run(() =>
        {
            var image = TiffImage.Load(fileName);
            image.TransformToInplace(ImageTransformationType.View);
            return new CorrelationImage()
            {
                ImageWithMetadata = image,
                CorrelationInfo = correlationInfo
            };
        });
    }

    public async Task AddOverlayImage(Guid imageId, string fileName, CorrelationInfo correlationInfo)
    {
        var info = await CreateCorrelationImage(fileName, correlationInfo);
        _ = _overlayImages.GetOrAdd(imageId, info);
    }

    public async Task UpdateOverlayImage(Guid imageId, string fileName, CorrelationInfo correlationInfo)
    {
        var info = await CreateCorrelationImage(fileName, correlationInfo);
        _ = _overlayImages.AddOrUpdate(
            imageId,
            id => info,
            (id, oldValue) => info
        );
    }

    public void RemoveOverlayImage(Guid imageId)
    {
        if (_overlayImages.TryRemove(imageId, out var correlationImage))
        {
            correlationImage.Dispose();
        }
    }

    public void UpdateInputCorrelationInfo(CorrelationInfo correlationInfo)
    {
        _inputCorrelationInfo = correlationInfo;
    }

    public void Clear()
    {
        var keys = _overlayImages.Keys.ToArray();
        foreach (var key in keys)
        {
            if (_overlayImages.TryRemove(key, out var correlationImage))
            {
                correlationImage.Dispose();
            }
        }
    }

    private void ApplyLayers(ImageWithMetadata inputImage)
    {
        if (_overlayImages.IsEmpty)
        {
            _output.OnNext(inputImage);
            return;
        }

        if (CorrelateByFiducials)
        {
            List<FiducialMontageImage> autoMontageImages = [];
            var keys = _overlayImages.Keys.ToArray();
            foreach (var key in keys)
            {
                if (_overlayImages.TryGetValue(key, out var correlationImage) && correlationImage.CorrelationInfo != null)
                {
                    var autoMontageImage = new FiducialMontageImage()
                    {
                        ImageWithMetadata = correlationImage.ImageWithMetadata,
                        Opacity = correlationImage.CorrelationInfo.Opacity.DecimalFractions,
                        FiducialList = [.. GetFiducialListInDouble(correlationImage.CorrelationInfo.FiducialPoints, correlationImage.ImageWithMetadata)],
                        ImageCenterNeutralPosition = _stageNavigation.GetPlanePosition(correlationImage.ImageWithMetadata.GetStagePosition(), correlationImage.ImageWithMetadata.GetSource())
                    };

                    if (autoMontageImage.FiducialList.Count > 0)
                    {
                        autoMontageImages.Add(autoMontageImage);
                    }
                }
            }

            var refImage = new FiducialMontageImage()
            {
                ImageWithMetadata = inputImage,
                Opacity = 1,
                FiducialList = [.. GetFiducialListInDouble(_inputCorrelationInfo.FiducialPoints, inputImage)],
                ImageCenterNeutralPosition = _stageNavigation.GetPlanePosition(inputImage.GetStagePosition(), inputImage.GetSource())
            };

            var result = FiducialCorrelation(refImage, autoMontageImages);
            _output.OnNext(result);
        }
        else
        {
            List<AutoMontageImage> autoMontageImages = [];
            var keys = _overlayImages.Keys.ToArray();
            foreach(var key in keys)
            {
                if (_overlayImages.TryGetValue(key, out var correlationImage))
                {
                    var autoMontageImage = new AutoMontageImage()
                    {
                        ImageWithMetadata = correlationImage.ImageWithMetadata,
                        Opacity = correlationImage.CorrelationInfo != null ? correlationImage.CorrelationInfo.Opacity.DecimalFractions : 0.5,
                        ImageCenterNeutralPosition = _stageNavigation.GetPlanePosition(correlationImage.ImageWithMetadata.GetStagePosition(), correlationImage.ImageWithMetadata.GetSource())
                    };
                    autoMontageImages.Add(autoMontageImage);
                }
            }

            var refImage = new AutoMontageImage()
            {
                ImageWithMetadata = inputImage,
                Opacity = 1,
                ImageCenterNeutralPosition = _stageNavigation.GetPlanePosition(inputImage.GetStagePosition(), inputImage.GetSource())
            };

            var result = AutoCorrelation(refImage, autoMontageImages);
            _output.OnNext(result);
        }
    }

    private ImageWithMetadata AutoCorrelation(AutoMontageImage refImage, List<AutoMontageImage> secondaryImages) =>
        MergeImagesAutocorrelation(refImage, [.. secondaryImages], _viewsPretilt);

    private ImageWithMetadata FiducialCorrelation(FiducialMontageImage refImage, List<FiducialMontageImage> secondaryImages) =>
        MergeImagesByFiducialsPoints(refImage, [.. secondaryImages], _viewsPretilt);

    private IEnumerable<KeyValuePair<Guid, DoublePoint>> GetFiducialListInDouble(IEnumerable<FiducialPoint> fiducials, ImageMetadata imageMetadata)
    {
        foreach (FiducialPoint point in fiducials)
        {
            var fiducial = _stageNavigation.GetImageLocationFromPlanePosition(point.Position, imageMetadata);
            if (fiducial != DoublePoint.Invalid)
            {
                yield return new(point.Id, fiducial);
            }
        }
    }
}
