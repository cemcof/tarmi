using CommunityToolkit.Diagnostics;
using UnitsNet;

namespace Tarmi.Imaging.Algorithms.Utilities;

public static class ColorSpace
{
    private const int MinLightWavelength = 380;
    private const int MaxLightWavelength = 780;
    internal const double ColorTo02Range = 255 / 2;
    internal const double Color16To02Range = ushort.MaxValue / 2;

    private readonly static double[,] RedSteps_AlgCIE =
    {
        { 442,   0.0624, 0.0374, 0.362, 3.2406255  },
        { 599.8, 0.0264, 0.0323, 1.056, -1.537208  },
        { 501.1, 0.0490, 0.0382, 0.065, -0.4986286 },
    };

    private readonly static double[,] GreenSteps_AlgCIE =
    {
        { 568.8, 0.0213, 0.0247, 0.821 },
        { 530.9, 0.0613, 0.0322, 0.286 }
    };

    private readonly static double[,] BlueSteps_AlgCIE =
    {
        { 437,  0.0845, 0.0278, 1.217 },
        { 459,  0.0385, 0.0725, 0.681 }
    };

    private readonly static double[,] RGBFinalSteps_AlgCIE =
    {
        {  3.2406255, -1.537208, -0.4986286 },
        { -0.9689307,  1.8757561, 0.0415175 },
        {  0.0557101, -0.2040211, 1.0569959 }
    };

    /// <summary>
    /// Helper method for get RGB from light frequency.
    /// </summary>
    /// <param name="colorValue">Current color value. Range 0..1</param>
    /// <param name="binCount">Bin count for 8-bit is 255. For 16-bit should be 65 535.</param>
    /// <returns>Result color value in int.</returns>
    private static ushort GetValueByGammaCorrection(this double colorValue, int binCount)
    {
        Guard.IsLessThanOrEqualTo(binCount, ushort.MaxValue);

        var byteValue = colorValue switch
        {
            <= 0 => 0,
            <= 0.0031308 => (byte)(255 * colorValue * 12.92),
            <= 1 => (byte)Math.Min(255 * 1.055 * Math.Pow(colorValue, 1 / 2.4) - 0.055, 255),
            _ => 255,
        };

        return (ushort)((binCount == ushort.MaxValue) ? byteValue * 128 : byteValue);
    }

    /// <summary>
    /// Change light frequency by CIE match to BGR.
    /// </summary>
    /// <param name="lightFrequency">Light frequency in nm. Range 380..780</param>
    /// <param name="bitDepth">Bin count for 8-bit is 255. For 16-bit should be 65 535.</param>
    /// <returns>Array with blue, green and red byte values.</returns>
    public static Array ChangeLightWavelengthToBGR(double lightFrequency, int bitDepth)
    {
        var bgr = ChangeLightWavelengthToBGRWithoutGammaCorrection(lightFrequency);

        return bitDepth == 8
            ? new byte[3] { 
                (byte)bgr[0].GetValueByGammaCorrection(bitDepth),
                (byte)bgr[1].GetValueByGammaCorrection(bitDepth),
                (byte)bgr[2].GetValueByGammaCorrection(bitDepth)
            }
            : new ushort[3] { 
                bgr[0].GetValueByGammaCorrection(bitDepth),
                bgr[1].GetValueByGammaCorrection(bitDepth),
                bgr[2].GetValueByGammaCorrection(bitDepth)
            };
    }

    /// <summary>
    /// Change light frequency by CIE match to BGR.
    /// </summary>
    /// <param name="lightFrequency">Light frequency in nm. Range 380..780</param>
    /// <returns>Array with blue, green and red byte values.</returns>
    private static double[] ChangeLightWavelengthToBGRWithoutGammaCorrection(double lightFrequency)
    {
        Guard.IsBetween(lightFrequency, MinLightWavelength, MaxLightWavelength);

        var xt1 = lightFrequency - RedSteps_AlgCIE[0, 0];
        xt1 *= lightFrequency < RedSteps_AlgCIE[0, 0] ? RedSteps_AlgCIE[0, 1] : RedSteps_AlgCIE[0, 2];

        var xt2 = lightFrequency - RedSteps_AlgCIE[1, 0];
        xt2 *= lightFrequency < RedSteps_AlgCIE[1, 0] ? RedSteps_AlgCIE[1, 1] : RedSteps_AlgCIE[1, 2];

        var xt3 = lightFrequency - RedSteps_AlgCIE[2, 0];
        xt3 *= lightFrequency < RedSteps_AlgCIE[2, 0] ? RedSteps_AlgCIE[2, 1] : RedSteps_AlgCIE[2, 2];

        var x = RedSteps_AlgCIE[0, 3] * Math.Exp(-0.5 * Math.Pow(xt1, 2));
        x += RedSteps_AlgCIE[1, 3] * Math.Exp(-0.5 * Math.Pow(xt2, 2));
        x -= RedSteps_AlgCIE[2, 3] * Math.Exp(-0.5 * Math.Pow(xt3, 2));

        // ********************************

        var yt1 = lightFrequency - GreenSteps_AlgCIE[0, 0];
        yt1 *= lightFrequency < GreenSteps_AlgCIE[0, 0] ? GreenSteps_AlgCIE[0, 1] : GreenSteps_AlgCIE[0, 2];

        var yt2 = lightFrequency - GreenSteps_AlgCIE[1, 0];
        yt2 *= lightFrequency < GreenSteps_AlgCIE[1, 0] ? GreenSteps_AlgCIE[1, 1] : GreenSteps_AlgCIE[1, 2];

        var y = GreenSteps_AlgCIE[0, 3] * Math.Exp(-0.5 * Math.Pow(yt1, 2));
        y += GreenSteps_AlgCIE[1, 3] * Math.Exp(-0.5 * Math.Pow(yt2, 2));

        // ********************************

        var zt1 = lightFrequency - BlueSteps_AlgCIE[0, 0];
        zt1 *= lightFrequency < BlueSteps_AlgCIE[0, 0] ? BlueSteps_AlgCIE[0, 1] : BlueSteps_AlgCIE[0, 2];

        var zt2 = lightFrequency - BlueSteps_AlgCIE[1, 0];
        zt2 *= lightFrequency < BlueSteps_AlgCIE[1, 0] ? BlueSteps_AlgCIE[1, 1] : BlueSteps_AlgCIE[1, 2];

        var z = BlueSteps_AlgCIE[0, 3] * Math.Exp(-0.5 * Math.Pow(zt1, 2));
        z += BlueSteps_AlgCIE[1, 3] * Math.Exp(-0.5 * Math.Pow(zt2, 2));

        // ********************************

        var redFloat = (RGBFinalSteps_AlgCIE[0, 0] * x) + (RGBFinalSteps_AlgCIE[0, 1] * y) + (RGBFinalSteps_AlgCIE[0, 2] * z);
        var greenFloat = (RGBFinalSteps_AlgCIE[1, 0] * x) + (RGBFinalSteps_AlgCIE[1, 1] * y) + (RGBFinalSteps_AlgCIE[1, 2] * z);
        var blueFloat = (RGBFinalSteps_AlgCIE[2, 0] * x) + (RGBFinalSteps_AlgCIE[2, 1] * y) + (RGBFinalSteps_AlgCIE[2, 2] * z);

        // ********************************

        return [blueFloat, greenFloat, redFloat];
    }

    private static Speed SpeedOfLight = Speed.FromMetersPerSecond(299_792_458);

    /// <summary>
    /// Frequency to wavelength calculation. Determines the wavelength of a waveform based on the frequency. (f = C/λ)
    /// </summary>
    /// <param name="frequency">Frequency of a waveform.</param>
    /// <returns>Wavelength of a waveform.</returns>
    public static Length FrequencyToWavelength(Frequency frequency)
        => Length.FromMeters(SpeedOfLight.MetersPerSecond / frequency.Hertz);

    /// <summary>
    /// Wavelength to frequency calculation. Determines the frequency of a waveform based on the wavelength. (λ = C/f)
    /// </summary>
    /// <param name="wavelength">Wavelength of a waveform</param>
    /// <returns>Frequency of a waveform.</returns>
    public static Frequency WavelengthToFrequency(Length wavelength)
        => Frequency.FromHertz(SpeedOfLight.MetersPerSecond / wavelength.Meters);
}
