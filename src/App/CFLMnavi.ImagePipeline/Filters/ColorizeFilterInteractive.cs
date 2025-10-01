using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using UnitsNet;

namespace CFLMnavi.ImagePipeline.Filters;

public sealed class ColorizeFilterInteractive : ColorizeFilterBase
{
    public ColorizeFilterInteractive(ILogger logger) : base(logger) { }

    protected override Array GetLutFilter(ImageWithMetadata image)
    {
        Length wavelength = LightWavelength ?? Length.Zero;
        var frequency = ColorSpace.WavelengthToFrequency(wavelength);
        return Lut.GetLutFilterFromWavelength(lightWavelength: frequency.Hertz, image.Image.Mat.Type().Depth != MatType.CV_8U ? ushort.MaxValue : byte.MaxValue);
    }

    public Length? LightWavelength { get; set; } = null;

    override protected void ProcessImageImplementation(ImageWithMetadata image)
    {
        if (LightWavelength is null)
        {
            _logger.LogWarning("The Wavelength information us set to null, skipping LUT filter");
            return;
        }

        base.ProcessImageImplementation(image);
    }
}
