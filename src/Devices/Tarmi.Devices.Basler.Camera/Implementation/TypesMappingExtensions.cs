using Basler.Pylon;
using Tarmi.Imaging.Algorithms.Helpers;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Imaging.Common.OpenCvWrapper;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.Devices.Basler.Camera.Implementation;

internal static class TypesMappingExtensions
{
    public static ImagePixelFormat ToImagePixelFormat(this string pixelFormat)
    {
        return pixelFormat switch
        {
            nameof(PixelType.Mono8) => ImagePixelFormat.Mono8,
            nameof(PixelType.Mono12) => ImagePixelFormat.Mono12,
            // Not supported by acA3088-57um
            //nameof(PixelType.Mono16) => ImagePixelFormat.Mono16,
            //nameof(PixelType.RGB8packed) => ImagePixelFormat.Rgb8,
            //nameof(PixelType.RGB8) => ImagePixelFormat.Rgb8,
            //nameof(PixelType.RGB16packed) => ImagePixelFormat.Rgb16,
            //nameof(PixelType.BGR8packed) => ImagePixelFormat.Bgr8,
            //nameof(PixelType.BGR8) => ImagePixelFormat.Bgr8,
            _ => ImagePixelFormat.Unknown
        };
    }

    public static string ToPixelTypeString(this ImagePixelFormat pixelFormat)
    {
        return pixelFormat switch
        {
            ImagePixelFormat.Mono8 => PLCamera.PixelFormat.Mono8,
            ImagePixelFormat.Mono12 => PLCamera.PixelFormat.Mono12,
            // Not supported by acA3088-57um
            //ImagePixelFormat.Mono16 => PLCamera.PixelFormat.Mono16,
            //ImagePixelFormat.Rgb8 => PLCamera.PixelFormat.RGB8,
            //ImagePixelFormat.Rgb16 => PLCamera.PixelFormat.RGB16Packed,
            //ImagePixelFormat.Bgr8 => PLCamera.PixelFormat.BGR8,
            _ => throw new NotSupportedException(pixelFormat.ToString())
        };
    }

    public static ImagePixelFormat ToImagePixelFormat(this PixelType pixelType)
    {
        return pixelType switch
        {
            PixelType.Mono8 => ImagePixelFormat.Mono8,
            PixelType.Mono12 => ImagePixelFormat.Mono12,
            // Not supported by acA3088-57um
            //PixelType.Mono16 => ImagePixelFormat.Mono16,
            //PixelType.RGB8packed => ImagePixelFormat.Rgb8,
            //PixelType.RGB8 => ImagePixelFormat.Rgb8,
            //PixelType.RGB16packed => ImagePixelFormat.Rgb16,
            //PixelType.BGR8packed => ImagePixelFormat.Bgr8,
            //PixelType.BGR8 => ImagePixelFormat.Bgr8,
            _ => ImagePixelFormat.Unknown
        };
    }

    public static MatType ToMatType(this PixelType pixelType)
    {
        return pixelType switch
        {
            PixelType.Mono8 => MatType.CV_8UC1,
            PixelType.Mono12 => MatType.CV_16UC1,
            // Not supported by acA3088-57um
            //PixelType.Mono16 => MatType.CV_16UC1,
            //PixelType.RGB8packed => MatType.CV_8UC3,
            //PixelType.RGB8 => MatType.CV_8UC3,
            //PixelType.RGB16packed => MatType.CV_16UC3,
            //PixelType.BGR8packed => MatType.CV_8UC3,
            //PixelType.BGR8 => MatType.CV_8UC3,
            _ => throw new NotSupportedException(pixelType.ToString())
        };
    }

    public static int GetPixelDataSize(this PixelType pixelType)
    {
        return pixelType switch
        {
            PixelType.Mono8 => 1,
            PixelType.Mono12 => 2,
            // Not supported by acA3088-57um
            //PixelType.Mono16 => 2,
            //PixelType.RGB8packed => 3,
            //PixelType.RGB8 => 3,
            //PixelType.RGB16packed => 6,
            //PixelType.BGR8packed => 3,
            //PixelType.BGR8 => 3,
            _ => throw new NotSupportedException(pixelType.ToString())
        };
    }

    public static ImageOrientation ToOrientation(this global::Basler.Pylon.ImageOrientation orientation)
    {
        return orientation switch
        {
            global::Basler.Pylon.ImageOrientation.BottomUp => ImageOrientation.BottomUp,
            _ => ImageOrientation.TopDown
        };
    }

    public unsafe static Imaging.Common.IImage ConvertToMat(this IGrabResult grabResult, bool isSimulated, bool isLiveStreamImage)
    {
        var buffer = grabResult.PixelData as byte[];
        var mat = new Mat(grabResult.Height, grabResult.Width, grabResult.PixelTypeValue.ToMatType());
        fixed (byte* src = buffer)
        {
            // no padding in algorithm, we should have aligned array
            MemoryHelpers.CopyMemory((void*)src, (void*)mat.Data, grabResult.Width * grabResult.Height * grabResult.PixelTypeValue.GetPixelDataSize());
        }

        if (grabResult.PixelTypeValue == PixelType.Mono12)
        {
            // shift 12bit data to 16bit (8bit to 16bit in simulation)
            mat *= 16; // isSimulated ? 256 : 16;
        }

        if (isSimulated && isLiveStreamImage)
        {
            // Add slight noise to the image for the live stream
            //using var nonSimulatedOriginalMat = mat;
            //mat =
            mat.ApplyGaussianNoiseInplace(mean: 3, stdDev: 2);
        }
        if (mat.Type() == MatType.CV_8UC1)
        {
            return Image<Gray, byte>.FromMat(mat);
        }
        else
        {
            using var originalMat = mat;
            return Image<Gray, byte>.ConvertFromMat(mat);
        }
    }

    public static Metadata ConvertToMetadata(this IGrabResult grabResult)
    {
        return new Metadata
        {
            Camera = new CameraParameters
            {
                ExposureTime = grabResult.ChunkData.Contains(PLChunkData.ChunkExposureTime) ?
                    Duration.FromMilliseconds(grabResult.ChunkData[PLChunkData.ChunkExposureTime].GetValue()) :
                    Duration.Zero,
            }
        };
    }

    public static ImageWithMetadata ConvertToImageWithMetadata(this IGrabResult grabResult, ICamera camera, bool isLiveStreamImage)
    {
        var isSimulated = camera.CameraInfo[CameraInfoKey.TLType].StartsWith("CamEmu", StringComparison.OrdinalIgnoreCase);
        return new ImageWithMetadata
        {
            Image = ConvertToMat(grabResult, isSimulated, isLiveStreamImage),
            MemoryOrigin = true,
            LuminescenceMetadata = ConvertToMetadata(grabResult),
            TiffMetadata = new Imaging.Common.Metadata.TiffMetadata
            {
                TimeOfAcquisition = DateTimeOffset.Now,
                CameraModel = camera.CameraInfo[CameraInfoKey.ModelName]
            },
            ImageId = UUIDNext.Uuid.NewSequential(),
            Coordinates = new()
        };
    }

    public static AutoGainMode ToAutoGainMode(this string gainAutoString)
    {
        return gainAutoString switch
        {
            nameof(AutoGainMode.Once) => AutoGainMode.Once,
            nameof(AutoGainMode.Continuous) => AutoGainMode.Continuous,
            _ => AutoGainMode.Off
        };
    }

    public static string ToGainAutoString(this AutoGainMode mode)
    {
        return mode switch
        {
            AutoGainMode.Once => nameof(AutoGainMode.Once),
            AutoGainMode.Continuous => nameof(AutoGainMode.Continuous),
            _ => nameof(AutoGainMode.Off)
        };
    }

    public static BinningMode ToBinningMode(this string binningModeString)
    {
        return binningModeString switch
        {
            nameof(BinningMode.Average) => BinningMode.Average,
            _ => BinningMode.Sum
        };
    }

    public static string ToBinningModeString(this BinningMode mode)
    {
        return mode switch
        {
            BinningMode.Average => nameof(BinningMode.Average),
            _ => nameof(BinningMode.Sum)
        };
    }
}
