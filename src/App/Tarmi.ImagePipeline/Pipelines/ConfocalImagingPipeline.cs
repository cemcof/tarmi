using Tarmi.Configuration;
using Tarmi.ImagePipeline.Filters;
using Tarmi.ImagePipeline.Sinks;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Pipelines;

public class ConfocalImagingPipeline : ImagingPipeline
{
    private readonly IConfocalMode _confocalMode;

    public ConfocalImagingPipeline(ILogger<ConfocalImagingPipeline> logger, IConfocalMode confocalMode, IProjectManager projectManager, ApplicationConfig applicationConfig, IStageNavigation stageNavigation)
        : base(logger, new FilteredSourceSink(), projectManager, stageNavigation)
    {
        _confocalMode = confocalMode;

        // lifetime ownership of all filters is in sink
        List<FilterBase> filters = [];

        filters.Add(new AxesTransformationFilter(logger));
        filters.Add(new ColorizeFilterMetadata(logger, applicationConfig));

        if (_primarySink is FilteredSourceSink sourceSink)
        {
            sourceSink.Initialize(confocalMode.Image, [.. filters]);
        }
    }

    public override async Task GrabOneAsync()
    {
#pragma warning disable CS0618 // Type or member is obsolete - VALID case here
        var image = await _confocalMode.GrabImageAsync();
#pragma warning restore CS0618
        if (_primarySink is FilteredSourceSink primarySink)
        {
            await primarySink.Set(image);
        }
    }
}
