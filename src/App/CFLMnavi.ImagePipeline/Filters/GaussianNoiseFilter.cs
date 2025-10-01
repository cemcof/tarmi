using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Filters;

public sealed class GaussianNoiseFilter : FilterBase
{
    public GaussianNoiseFilter(ILogger logger) : base(logger) { }

    public double Mean { get; set; } = 35;
    public double StdDev { get; set; } = 10;

    protected override void ProcessImageImplementation(ImageWithMetadata image)
    {
        image.Image.Mat.ApplyGaussianNoiseInplace(Mean, StdDev);
    }
}
