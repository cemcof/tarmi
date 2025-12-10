using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Configuration;
using Tarmi.Configuration.Application;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.ImagePipeline.Filters;

public sealed class ColorizeFilterMetadata : ColorizeFilterBase
{
    private readonly ImageColoring _imageColoring;
    // TODO: cache LUT filters?

    public ColorizeFilterMetadata(ILogger logger, ApplicationConfig applicationConfig)
        : base(logger)
    {
        _imageColoring = applicationConfig.UserPreferences.ImageColoring;
    }

    private static byte[] GetBgr8(LightColor color)
        => [color.Blue, color.Green, color.Red];

    private static ushort[] GetBgr16(LightColor color)
        => [(ushort)(color.Blue << 8), (ushort)(color.Green << 8), (ushort)(color.Red << 8)];

    private static LightColor? GetLightColor(LightSettings lightSettings, Length wavelength)
    {
        var lightMapping = lightSettings.LightMappings.FirstOrDefault(
            lm => lm.WaveLength.Nanometers == wavelength.Nanometers,
            new LightMapping { WaveLength = wavelength, Color = new LightColor { Red = 127, Green = 127, Blue = 127 } }
        );
        return lightMapping?.Color;
    }


    protected override Array GetLutFilter(ImageWithMetadata image)
    {
        LightSettings lightSettings;
        LightColor? lightColor = null;

        if (image.LuminescenceMetadata != null)
        {
            lightSettings = image.LuminescenceMetadata!.Mode == Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence
                ? _imageColoring.Fluorescence
                : _imageColoring.Reflection;
            lightColor = GetLightColor(lightSettings, image.LuminescenceMetadata.LightWavelength);
        }
        else if (image.ConfocalMetadata != null)
        {
            lightSettings = image.ConfocalMetadata!.Mode == Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Fluorescence
                ? _imageColoring.Fluorescence
                : _imageColoring.Reflection;
            lightColor = GetLightColor(lightSettings, image.ConfocalMetadata.LightWavelength);
        }

        if (lightColor is not null)
        {
            switch (image.Image.Mat.Type().Depth)
            {
                case MatType.CV_8U:
                    return Lut.GetLutFilterFromBgr(GetBgr8(lightColor));
                case MatType.CV_16U:
                    return Lut.GetLutFilterFromBgr(GetBgr16(lightColor));
                default:
                    throw new NotSupportedException($"Unsupported image depth: {image.Image.Mat.Type().Depth}");
            }
        }

        return image.ConfocalMetadata != null
            ? Lut.GetLutFilterFromWavelength(lightWavelength: image.ConfocalMetadata!.LightWavelength.Nanometers, image.Image.Depth)
            : Lut.GetLutFilterFromWavelength(lightWavelength: image.LuminescenceMetadata!.LightWavelength.Nanometers, image.Image.Depth);
    }

    override protected void ProcessImageImplementation(ImageWithMetadata image)
    {
        if ((
            image.LuminescenceMetadata is null ||
            image.LuminescenceMetadata.LightWavelength.Nanometers == 0
            ) &&
            (
            image.ConfocalMetadata is null ||
            // TODO: allow reflection coloring in future
            image.ConfocalMetadata.Mode == Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Reflection ||
            image.ConfocalMetadata.LightWavelength.Nanometers == 0
            )
        )
        {
            _logger.LogTrace("Image does not contain luminescence metadata, no light is on, or it's reflection mode image, skipping LUT filter");
            return;
        }
        base.ProcessImageImplementation(image);
    }
}
