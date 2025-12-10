using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.OpenCvWrapper;
using Microsoft.Extensions.Logging;

namespace Tarmi.ImagePipeline.Filters;

public abstract class ColorizeFilterBase : FilterBase
{
    protected ColorizeFilterBase(ILogger logger) : base(logger) { }

    protected abstract Array GetLutFilter(ImageWithMetadata image);

    protected override void ProcessImageImplementation(ImageWithMetadata image)
    {
        if (!image.MemoryOrigin)
        {
            _logger.LogDebug("The image is loaded from disk, skipping LUT filter");
            return;
        }

        var filter = GetLutFilter(image);
        if (image.Image.NumberOfChannels == 1)
        {
            using var img = image.Image;
            image.Image =  Image<Bgr, byte>.ConvertFromImage(img);
        }

        Lut.LutWithBgrFilterInplace(image.Image.Mat, filter);
    }
}
