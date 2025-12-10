using Tarmi.ImagePipeline.Filters;
using Tarmi.ImagePipeline.Sinks;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Pipelines;

public class SemImagingPipeline : ImagingPipeline
{
    private readonly IElectronBeamMode  _electronBeamMode;

    public SemImagingPipeline(ILogger<SemImagingPipeline> logger, IElectronBeamMode electronBeamMode, IProjectManager projectManager, IStageNavigation stageNavigation)
        : base(logger, new FilteredSourceSink(), projectManager, stageNavigation)
    {
        _electronBeamMode = electronBeamMode;

        // lifetime ownership of all filters is in sink
        List<FilterBase> filters = [ new AxesTransformationFilter(logger) ];
        // no filters required

        if (_primarySink is IImageObservableSink sourceSink)
        {
            sourceSink.Initialize(electronBeamMode.Image, [.. filters]);
        }
    }

    public override async Task GrabOneAsync()
    {
#pragma warning disable CS0618 // Type or member is obsolete - VALID case here
        var image = await _electronBeamMode.GrabImageAsync();
#pragma warning restore CS0618
        if (_primarySink is FilteredSourceSink primarySink)
        {
            await primarySink.Set(image);
        }
    }
}
