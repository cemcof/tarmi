using Tarmi.ImagePipeline.Filters;
using Tarmi.ImagePipeline.Sinks;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Pipelines;

public class ViewerImagingPipeline : ImagingPipeline
{
    public ViewerImagingPipeline(ILogger<ViewerImagingPipeline> logger, IProjectManager projectManager, IStageNavigation stageNavigation)
        : base(logger, new FilteredFileSink(), projectManager, stageNavigation)
    {
        // lifetime ownership of all filters is in sink
        List<FilterBase> filters = [];
        // no filters required

        if (_primarySink is IImageFileSink sourceSink)
        {
            sourceSink.Initialize([.. filters]);
        }
    }

    public override Task GrabOneAsync() => throw new NotSupportedException();
}
