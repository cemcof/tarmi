using AsyncAwaitBestPractices;
using CFLMnavi.Configuration;
using CFLMnavi.ImagePipeline.Filters;
using CFLMnavi.ImagePipeline.Sinks;
using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Pipelines;

public class LuminescenceImagingPipeline : ImagingPipeline
{
    private readonly ILuminescenceMode _luminescenceMode;
    private readonly HistogramFilter _histogramFilter;

    public LuminescenceImagingPipeline(ILogger<LuminescenceImagingPipeline> logger, ILuminescenceMode luminescenceMode, IProjectManager projectManager, ApplicationConfig applicationConfig, IStageNavigation stageNavigation)
        : base(logger, new FilteredSourceSink(), projectManager, stageNavigation)
    {
        _luminescenceMode = luminescenceMode;

        // lifetime ownership of all filters is in sink
        List<FilterBase> filters = [];
        filters.Add(new AxesTransformationFilter(logger));
        _histogramFilter = new HistogramFilter(logger, applicationConfig);
        filters.Add(_histogramFilter);
        filters.Add(new ColorizeFilterMetadata(logger, applicationConfig));

        if (_primarySink is FilteredSourceSink sourceSink)
        {
            sourceSink.Initialize(luminescenceMode.Image, [.. filters]);
        }
    }

    public override async Task GrabOneAsync()
    {
#pragma warning disable CS0618 // Type or member is obsolete - VALID case here
        var image = await _luminescenceMode.GrabImageAsync();
#pragma warning restore CS0618
        if (_primarySink is FilteredSourceSink primarySink)
        {
            await primarySink.Set(image);
        }
    }

    public IObservable<SortedDictionary<int, double>> Histogram => _histogramFilter.Histogram;

    public int HistogramLowerBound
    {
        get => _histogramFilter.LowerBound;
        set
        {
            if (_histogramFilter.LowerBound != value)
            {
                _histogramFilter.LowerBound = value;
                if (!_liveStreamEnabled)
                {
                    Invalidate().SafeFireAndForget();
                }
            }
        }
    }

    public int HistogramUpperBound
    {
        get => _histogramFilter.UpperBound;
        set
        {
            if (_histogramFilter.UpperBound != value)
            {
                _histogramFilter.UpperBound = value;
                if (!_liveStreamEnabled)
                {
                    Invalidate().SafeFireAndForget();
                }
            }
        }
    }

    public async Task UseAutoEqualize(bool enabled)
    {
        _histogramFilter.AutoEqualize = enabled;
        if (!_liveStreamEnabled)
        {
            await Invalidate();
        }
    }
}
