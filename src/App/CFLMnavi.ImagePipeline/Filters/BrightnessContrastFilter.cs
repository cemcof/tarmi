using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using Microsoft.Extensions.Logging;

namespace CFLMnavi.ImagePipeline.Filters;

public sealed class BrightnessContrastFilter : FilterBase
{
    public BrightnessContrastFilter(ILogger logger) : base(logger) { }

    public static int Brightness { get; set; } = 50;
    public static int Contrast { get; set; } = 50;

    protected override void ProcessImageImplementation(ImageWithMetadata image)
    {
        image.Image.UpdateBrightnessContrastInplace(Brightness, Contrast);
    }
}
