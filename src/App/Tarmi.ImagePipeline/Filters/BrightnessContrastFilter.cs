using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Filters;

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
