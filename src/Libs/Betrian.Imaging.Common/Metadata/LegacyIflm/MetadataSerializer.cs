using System.Globalization;
using System.Text.RegularExpressions;
using UnitsNet;

namespace Betrian.Imaging.Common.Metadata.LegacyIflm;

public static partial class MetadataSerializer
{
    [GeneratedRegex(@"stack_z(\d{3}).*")]
    private static partial Regex StackFilenameRegex();

    private static int? GetSliceIdxFromFilename(string fileName)
    {
        var match = StackFilenameRegex().Match(fileName);
        if (match.Success)
        {
            var value = match.Groups[1].Value;
            return int.Parse(value);
        }

        return null;
    }

    private static Metadata AddOrUpdateStackInfo(this Metadata metadata, Func<ZStackInfo, ZStackInfo> update)
    {
        return metadata.ZStackInfo is null
            ? (metadata with { ZStackInfo = update(new ZStackInfo()) })
            : (metadata with { ZStackInfo = update(metadata.ZStackInfo) });
    }

    private static Metadata UpdateFromLightDictionary(this Metadata metadata, string value)
    {
        // light_dict={<LightName.LED470: 3>: (100.0, 3000000.0)}
        var frequencyStr = value.GetAfterOrEmpty("LightName.LED").GetBeforeOrEmpty(":");
        var frequency = Frequency.FromTerahertz(double.Parse(frequencyStr, CultureInfo.InvariantCulture));
        var exposureStr = value.GetBeforeOrEmpty(")").GetAfterOrEmpty(", ");
        var exposure = Duration.FromMicroseconds(double.Parse(exposureStr, CultureInfo.InvariantCulture));
        var intensityStr = value.GetAfterOrEmpty("(").GetBeforeOrEmpty(",");
        var intensity = Ratio.FromPercent(double.Parse(intensityStr, CultureInfo.InvariantCulture));

        return metadata with { LightFrequency = frequency, ExposureTime = exposure, LightIntensity = intensity };
    }

    private static Metadata UpdateMetadata(string line, Metadata metadata)
    {
        var parts = line.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var (key, value) = (parts[0], parts[1]);

        return key switch
        {
            "exposure_time" => metadata with { ExposureTime = Duration.FromMilliseconds(double.Parse(value, CultureInfo.InvariantCulture)) },
            "light_name" => metadata with { LightFrequency = Frequency.FromTerahertz(double.Parse(value.GetAfterOrEmpty("LightName.LED"), CultureInfo.InvariantCulture))},
            "light_power" => metadata with { LightIntensity = Ratio.FromPercent(double.Parse(value, CultureInfo.InvariantCulture)) },
            "gain" => metadata with { Gain = Level.FromDecibels(double.Parse(value, CultureInfo.InvariantCulture)) },
            "gamma" => metadata with { Gamma = double.Parse(value, CultureInfo.InvariantCulture) },
            "binning" => metadata with { Binning = int.Parse(value, CultureInfo.InvariantCulture) },
            "flip_image_ud" => metadata with { FlipImageUD = bool.Parse(value) },
            "flip_image_lr" => metadata with { FlipImageLR = bool.Parse(value) },
            "rotation" => metadata with { Rotation = Angle.FromDegrees(double.Parse(value, CultureInfo.InvariantCulture)) },
            "z_slice_distance" => metadata.AddOrUpdateStackInfo(si => si with { SliceBaseDistance = Length.FromPicometers(double.Parse(value, CultureInfo.InvariantCulture)) }),
            "num_z_slices" => metadata.AddOrUpdateStackInfo(si => si with { StepsCount = int.Parse(value) }),
            "light_dict" => metadata.UpdateFromLightDictionary(value),
            // ignored: images, hyperstack, mode, ImageJ, timestamp
            _ => metadata
        };
    }

    private static Metadata? Parse(string content, int? idx)
    {
        try
        {
            var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
            var result = lines.Aggregate(new Metadata(), (metadata, line) => UpdateMetadata(line, metadata));

            if (idx is null && result.IsStackImage)
            {
                return null;
            }
            else if (idx is not null && result.IsStackImage)
            {
                result = result.AddOrUpdateStackInfo(si =>
                    si with
                    {
                        Step = idx.Value + 1,
                        StepDistance = (si.SliceBaseDistance / si.StepsCount) * (idx.Value + 1)
                    });
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    public static Metadata? Deserialize(string content, string fileName)
    {
        int? idx = GetSliceIdxFromFilename(fileName);
        return Parse(content, idx);
    }
}
