using Tarmi.ImagePipeline.Filters;
using Tarmi.ImagePipeline.Sinks;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Pipelines;

public class IonImagingPipeline : ImagingPipeline
{
    private readonly IIonBeamMode _ionBeamMode;

    public IonImagingPipeline(ILogger<IonImagingPipeline> logger, IIonBeamMode ionBeamMode, IProjectManager projectManager, IStageNavigation stageNavigation)
        : base(logger, new FilteredSourceSink(), projectManager, stageNavigation)
    {
        _ionBeamMode = ionBeamMode;

        // lifetime ownership of all filters is in sink
        List<FilterBase> filters = [ new AxesTransformationFilter(logger) ];
        // no filters required

        if (_primarySink is FilteredSourceSink sourceSink)
        {
            sourceSink.Initialize(ionBeamMode.Image, [.. filters]);
        }
    }

    public override async Task GrabOneAsync()
    {
#pragma warning disable CS0618 // Type or member is obsolete: LEGAL use here
        var image = await _ionBeamMode.GrabImageAsync();
#pragma warning restore CS0618
        if (_primarySink is FilteredSourceSink primarySink)
        {
            await primarySink.Set(image);
        }
    }
}
