using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Tarmi.Imaging.Common;
using Tarmi.ImagePipeline.Sinks;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Pipelines;

public abstract class ImagingPipeline : IImagingPipelineGrabber
{
    protected readonly ILogger _logger;
    protected readonly IImageSink _primarySink;
    private readonly ImageMultiplexer _multiplexer;

    public IObservable<ImageWithMetadata> Output { get; }

    protected ImagingPipeline(ILogger logger, IImageSink primarySink, IProjectManager projectManager, IStageNavigation stageNavigation)
    {
        _logger = logger;
        _primarySink = primarySink;
        _multiplexer = new ImageMultiplexer(primarySink.Output, stageNavigation);
        Output = _multiplexer.Output;
        projectManager.ActiveProject
            .Delay(TimeSpan.FromMilliseconds(50))
            .Where(p => p != null)
            .Subscribe(_ => _multiplexer.InitializeHolderData());

    }

    public async Task SetCorrelationMode(bool correlationByFiducials)
    {
        if (correlationByFiducials == _multiplexer.CorrelateByFiducials)
        {
            return;
        }
        _multiplexer.CorrelateByFiducials = correlationByFiducials;
        // TODO: check if this is needed, invalidate only if all conditions are fulfilled
        await Clear();
    }

    public async Task AddOverlayImage(Guid imageId, string fileName, CorrelationInfo correlationInfo)
    {
        await _multiplexer.AddOverlayImage(imageId, fileName, correlationInfo);
        await Invalidate();
    }

    public async Task UpdateOverlayImage(Guid imageId, string fileName, CorrelationInfo correlationInfo)
    {
        await _multiplexer.UpdateOverlayImage(imageId, fileName, correlationInfo);
        await Invalidate();
    }

    public async Task RemoveOverlayImage(Guid imageId)
    {
        _multiplexer.RemoveOverlayImage(imageId);
        await Invalidate();
    }

    public async Task Clear()
    {
        await ClearOverlayImages();
        await ClearPrimaryImage();
    }

    public async Task ClearOverlayImages()
    {
        _multiplexer.Clear();
        await Invalidate();
    }

    public async Task ClearPrimaryImage()
    {
        _multiplexer.UpdateInputCorrelationInfo(new CorrelationInfo());
        await _primarySink.Clear();
    }

    public abstract Task GrabOneAsync();

    protected volatile bool _liveStreamEnabled = false;
    public void SetLiveStreamEnabled(bool enabled)
    {
        _liveStreamEnabled = enabled;
    }

    public async Task SetPrimaryImageFile(string path, CorrelationInfo correlationInfo)
    {
        if (_liveStreamEnabled)
        {
            throw new InvalidOperationException("Cannot set image file when live stream is enabled");
        }

        if (_primarySink is FilteredSourceSink sourceSink)
        {
            _multiplexer.UpdateInputCorrelationInfo(correlationInfo);
            await sourceSink.SetSourceFile(path);
        }
    }

    public async Task Invalidate()
    {
        await _primarySink.Invalidate();
    }

    public async Task<ImageWithMetadata> GetImageCopyAsync(ImageProcessingStage processingStage)
    {
        return processingStage switch
        {
            ImageProcessingStage.Input => await _primarySink.GetInputCopyAsync(),
            ImageProcessingStage.FilteredInput => await _primarySink.GetOutputCopyAsync(),
            _ => await _multiplexer.GetOutputCopyAsync()
        };
    }

    public void Dispose()
    {
        _primarySink.Dispose();
    }

    public async Task<ImageWithMetadata> GrabOneWithResultCopyAsync(ImageProcessingStage processingStage = ImageProcessingStage.Input)
    {
        var timestamp = DateTimeOffset.Now;
        var imageSink = processingStage switch
        {
            ImageProcessingStage.Input => _primarySink.Input,
            ImageProcessingStage.FilteredInput => _primarySink.Output,
            _ => _multiplexer.Output
        };

        var imageTask = imageSink
            .Where(im => im.TiffMetadata!.TimeOfAcquisition! > timestamp)
            .Take(1)
            .ToTask();

        if (!_liveStreamEnabled)
        {
            await GrabOneAsync();
        }

        var result = await imageTask;

        return result with { Image = result.Image.Clone() };
    }
}
