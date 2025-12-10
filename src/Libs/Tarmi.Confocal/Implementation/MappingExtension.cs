using OpenCvSharp;
using Tarmi.Imaging.Common.Metadata.Confocal;

namespace Tarmi.Confocal.Implementation;

internal static class MappingExtension
{
    public static Metadata ConvertToMetadata(this IConfocalDevice confocalDevice, Mat image)
    {
        return new Metadata
        {
            Dwell = confocalDevice.Dwell,
            ADC = confocalDevice.ADC,
            Gain = confocalDevice.Gain,
            LightIntensity = confocalDevice.Intensity,
            LightWavelength = confocalDevice.LaserColor,
            Mode = confocalDevice.LuminescenceMode,
            PixelSizeX = confocalDevice.FieldWidth / image.Width,
            PixelSizeY = confocalDevice.FieldHeight / image.Height,
        };
    }
}
