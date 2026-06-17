#pragma warning disable OCVS002 // Mat property accessed in loop condition
#pragma warning disable CS0618 // Type or member is obsolete

using Tarmi.Imaging.Common.Metadata.LegacyIflm;
using AwesomeAssertions;
using OpenCvSharp;
using UnitsNet;
using Xunit;

namespace Tarmi.Imaging.Common.Tests.Metadata.LegacyIflm;

public class LegacyIflmSerializationTests
{
    private const string ImageFilename = "stack_z033_L470.tif";
    private const string ZStackMetadata = """
        ImageJ=1.11a
        images=1
        hyperstack=true
        mode=grayscale
        z_slice_distance=-100000.0
        num_z_slices=52
        light_dict={<LightName.LED470: 3>: (100.0, 3000000.0)}
        gain=12.0
        gamma=1.0
        binning=2
        flip_image_ud=True
        flip_image_lr=False
        rotation=0
        """;

    [Fact]
    public void Deserialization_should_return_null_when_wrongly_formatted_z_stack_filename_provided()
    {
        const string WrongFilename = "wrong_z_stack_name.tif";

        var metadata = MetadataSerializer.Deserialize(ZStackMetadata, WrongFilename);
        _ = metadata.Should().BeNull();
    }

    [Fact]
    public void Deserialization_of_single_image_should_succeed()
    {
        const string Metadata = """
            ImageJ=1.11a
            images=1
            hyperstack=true
            mode=grayscale
            exposure_time=3000.0
            light_name=LightName.LED470
            light_power=100.0
            timestamp=25-Aug-2022_15-26PM
            gain=12.0
            gamma=1.0
            binning=2
            flip_image_ud=True
            flip_image_lr=False
            rotation=0
            """;

        var metadata = MetadataSerializer.Deserialize(Metadata, "any.tif");
        _ = metadata.Should().NotBeNull();

        _ = metadata!.IsStackImage.Should().BeFalse();
        _ = metadata!.ZStackInfo.Should().BeNull();
        _ = metadata!.Binning.Should().Be(2);
        _ = metadata!.Gain.Decibels.Should().Be(12.0);
        _ = metadata!.Gamma.Should().Be(1.0);
        _ = metadata!.ExposureTime.Should().Be(Duration.FromMilliseconds(3_000.0));
        _ = metadata!.Rotation.Should().Be(Angle.FromDegrees(0));
        _ = metadata!.FlipImageUD.Should().BeTrue();
        _ = metadata!.FlipImageLR.Should().BeFalse();
        _ = metadata!.LightIntensity.Should().Be(Ratio.FromPercent(100.0));
        _ = metadata!.LightFrequency.Should().Be(Frequency.FromTerahertz(470.0));
    }

    [Fact]
    public void Deserialization_of_z_stack_image_should_succeed()
    {
        var metadata = MetadataSerializer.Deserialize(ZStackMetadata, ImageFilename);
        _ = metadata.Should().NotBeNull();

        _ = metadata!.IsStackImage.Should().BeTrue();
        _ = metadata!.Binning.Should().Be(2);
        _ = metadata!.Gain.Decibels.Should().Be(12.0);
        _ = metadata!.Gamma.Should().Be(1.0);
        _ = metadata!.ExposureTime.ToTimeSpan().Should().Be(TimeSpan.FromMilliseconds(3_000.0));
        _ = metadata!.Rotation.Should().Be(Angle.FromDegrees(0));
        _ = metadata!.FlipImageUD.Should().BeTrue();
        _ = metadata!.FlipImageLR.Should().BeFalse();
        _ = metadata!.LightIntensity.Should().Be(Ratio.FromPercent(100.0));
        _ = metadata!.LightFrequency.Should().Be(Frequency.FromTerahertz(470.0));
        _ = metadata!.ZStackInfo.Should().NotBeNull();
        _ = metadata!.ZStackInfo!.StepsCount.Should().Be(52);
        _ = metadata!.ZStackInfo!.Step.Should().Be(34);
        _ = metadata!.ZStackInfo!.SliceBaseDistance.Nanometers.Should().BeApproximately(-100.0, 0.000_000_000_001);
        var expectedStepDistance = (metadata!.ZStackInfo!.SliceBaseDistance / metadata!.ZStackInfo!.StepsCount) * metadata!.ZStackInfo!.Step;
        _ = metadata!.ZStackInfo!.StepDistance.Meters.Should().Be(expectedStepDistance.Meters);
    }

    [Fact]
    public void Loading_of_image_with_metadata_should_succeed_and_image_is_converted_from_12bit_to_16bit()
    {
        var imageWithMetadata = TiffImage.Load(ImageFilename);
        var rawMat = Cv2.ImRead(ImageFilename, ImreadModes.Unchanged);

        var imgIndexer = imageWithMetadata.Image.Mat.GetGenericIndexer<ushort>();
        var rawIndexer = rawMat.GetGenericIndexer<ushort>();

        for (int y = 0; y < rawMat.Height; y++)
        {
            for (int x = 0; x < rawMat.Width; x++)
            {
                var value = imgIndexer[y, x];
                if (value != 0)
                {
                    _ = value.Should().NotBe(rawIndexer[y, x]);
                    _ = value.Should().Be((ushort)(rawIndexer[y, x] << 4));
                }
            }
        }
    }
}
