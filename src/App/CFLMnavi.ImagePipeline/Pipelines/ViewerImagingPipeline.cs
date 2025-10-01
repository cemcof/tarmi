using CFLMnavi.ImagePipeline.Filters;
using CFLMnavi.ImagePipeline.Sinks;
using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Pipelines;

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
