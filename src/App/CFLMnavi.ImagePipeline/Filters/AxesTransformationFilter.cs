using Betrian.Imaging.Common;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Filters;

public sealed class AxesTransformationFilter : FilterBase
{
    public AxesTransformationFilter(ILogger logger) : base(logger) { }

    protected override void ProcessImageImplementation(ImageWithMetadata image)
        => image.TransformToInplace(ImageTransformationType.View);
}
