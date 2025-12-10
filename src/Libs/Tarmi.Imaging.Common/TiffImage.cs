using System.Text;
using Tarmi.Imaging.Common.OpenCvWrapper;
using BitMiracle.LibTiff.Classic;
using CommunityToolkit.Diagnostics;
using OpenCvSharp;
using UnitsNet;

namespace Tarmi.Imaging.Common;

public static class TiffImage
{
    private const TiffTag FeiImageXmlMementoMetadata = (TiffTag)0x8778;
    private const TiffTag FeiImageIniMetadata = (TiffTag)0x877a;
    private const TiffTag FeiImageXmlMetadata = (TiffTag)0x877b;
    private const TiffTag BetrianLuminescenceXmlMetadata = (TiffTag)0x888a;
    private const TiffTag BetrianCoordinatesXmlMetadata = (TiffTag)0x888b;
    private const TiffTag BetrianLayerIdMetadata = (TiffTag)0x888c;
    private const TiffTag BetrianImageIdMetadata = (TiffTag)0x888d;
    private const TiffTag BetrianRegionOfInterestIdMetadata = (TiffTag)0x888e;
    private const TiffTag BetrianConfocalXmlMetadata = (TiffTag)0x888f;

    private const string TiffDateTimeFormat = "yyyy:MM:dd HH:mm:ss";

    static TiffImage() => Tiff.SetTagExtender(TagMetadataExtender);

    public static ImageWithMetadata Load(string filePath)
    {
        Mat? mat = null;
        try
        {
            mat = Cv2.ImRead(filePath, ImreadModes.Unchanged);

            using var tiffFile = Tiff.Open(filePath, "r");

            var matType = mat.Type();
            IImage image = matType switch
            {
                _ when matType == MatType.CV_8UC1 => new Image<Gray, byte>(mat),
                _ when matType == MatType.CV_8UC3 => new Image<Bgr, byte>(mat),
                _ when matType == MatType.CV_16UC1 => Image<Gray, byte>.ConvertFromMat(mat),
                _ when matType == MatType.CV_16UC3 => Image<Bgr, byte>.ConvertFromMat(mat),
                _ => throw new NotSupportedException($"Unsupported image type: {matType}")
            };
            

            var result = new ImageWithMetadata
            {
                Image = image,
                MemoryOrigin = false,
                TiffMetadata = LoadTiffMetadata(tiffFile),
                FeiXmlMetadata = LoadFeiXmlMetadata(tiffFile),
                FeiIniMetadata = LoadFeiIniMetadata(tiffFile),
                FeiMementoMetadata = LoadFeiMementoMetadata(tiffFile),
                LuminescenceMetadata = LoadLuminescenceMetadata(tiffFile),
                ConfocalMetadata = LoadConfocalMetadata(tiffFile),
                Coordinates = LoadCoordinatesMetadata(tiffFile),
                LegacyIflmMetadata = LoadLegacyIflmMetadata(tiffFile, Path.GetFileName(filePath)),
                ImageId = LoadIdMetadata(tiffFile, BetrianImageIdMetadata),
                LayerId = LoadIdMetadata(tiffFile, BetrianLayerIdMetadata),
                RegionOfInterestId = LoadIdMetadata(tiffFile, BetrianLayerIdMetadata)
            };

            return result;
        }
        catch
        {
            mat?.Dispose();
            throw;
        }
    }

    public static ImageMetadata LoadMetadata(string filePath)
    {
        using var tiffFile = Tiff.Open(filePath, "r");

        return new ImageMetadata
        {
            TiffMetadata = LoadTiffMetadata(tiffFile),
            FeiXmlMetadata = LoadFeiXmlMetadata(tiffFile),
            FeiIniMetadata = LoadFeiIniMetadata(tiffFile),
            FeiMementoMetadata = LoadFeiMementoMetadata(tiffFile),
            LuminescenceMetadata = LoadLuminescenceMetadata(tiffFile),
            Coordinates = LoadCoordinatesMetadata(tiffFile),
            LegacyIflmMetadata = LoadLegacyIflmMetadata(tiffFile, Path.GetFileName(filePath)),
            ImageId = LoadIdMetadata(tiffFile, BetrianImageIdMetadata),
            LayerId = LoadIdMetadata(tiffFile, BetrianLayerIdMetadata),
            RegionOfInterestId = LoadIdMetadata(tiffFile, BetrianRegionOfInterestIdMetadata)
        };
    }

    private static string GetStringMetadataValue(FieldValue[] values)
    {
        return values[0].Value switch
        {
            string str => str.TrimEnd('\0'),
            byte[] array => Encoding.UTF8.GetString(array).TrimEnd('\0'),
            _ => Encoding.UTF8.GetString(values[1].GetBytes()).TrimEnd('\0')
        };
    }

    private static Metadata.TiffMetadata LoadTiffMetadata(Tiff tiffFile)
    {
        var metadata = new Metadata.TiffMetadata();
        var entries = tiffFile.GetField(TiffTag.SOFTWARE);
        if (entries is { Length: >= 1 })
        {
            metadata = metadata with { Software = GetStringMetadataValue(entries) };
        }

        entries = tiffFile.GetField(TiffTag.IMAGEDESCRIPTION);
        if (entries is { Length: >= 1 })
        {
            metadata = metadata with { ImageDescription = GetStringMetadataValue(entries) };
        }

        entries = tiffFile.GetField(TiffTag.DATETIME);
        if (entries is { Length: >= 1 })
        {
            metadata = metadata with
            {
                TimeOfAcquisition =
                    DateTimeOffset.TryParseExact(
                        GetStringMetadataValue(entries),
                        TiffDateTimeFormat,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var result
                    ) ? result : DateTimeOffset.Now
            };
        }

        entries = tiffFile.GetField(TiffTag.MODEL);
        if (entries is { Length: >= 1 })
        {
            metadata = metadata with { CameraModel = GetStringMetadataValue(entries) };
        }

        return metadata;
    }

    private static Metadata.LegacyIflm.Metadata? LoadLegacyIflmMetadata(Tiff tiffFile, string fileName)
    {
        var entries = tiffFile.GetField(TiffTag.IMAGEDESCRIPTION);
        if (entries is { Length: >= 1 })
        {
            return Metadata.LegacyIflm.MetadataSerializer.Deserialize(GetStringMetadataValue(entries), fileName);
        }
        return null;
    }

    private static Metadata.Thermofisher.XmlFormat.Metadata? LoadFeiXmlMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(FeiImageXmlMetadata);
            if (entries is { Length: >= 1 })
            {
                return Metadata.Thermofisher.XmlFormat.MetadataXmlSerializer.Deserialize(GetStringMetadataValue(entries));
            }
        }
        catch { }
        return null;
    }

    private static Metadata.Thermofisher.IniFormat.Metadata? LoadFeiIniMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(FeiImageIniMetadata);
            if (entries is { Length: >= 1 })
            {
                return Metadata.Thermofisher.IniFormat.MetadataIniSerializer.Deserialize(GetStringMetadataValue(entries));
            }
        }
        catch { }
        return null;
    }

    private static Metadata.Thermofisher.MementoFormat.Metadata? LoadFeiMementoMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(FeiImageXmlMementoMetadata);
            if (entries is { Length: >= 1 })
            {
                return new Metadata.Thermofisher.MementoFormat.Metadata { Data = GetStringMetadataValue(entries) };
            }
        }
        catch { }
        return null;
    }

    private static Metadata.Luminescence.Metadata? LoadLuminescenceMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(BetrianLuminescenceXmlMetadata);
            if (entries is { Length: >= 1 })
            {
                return Metadata.Luminescence.MetadataXmlSerializer.Deserialize(GetStringMetadataValue(entries));
            }
        }
        catch { }
        return null;
    }

    private static Metadata.Confocal.Metadata? LoadConfocalMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(BetrianConfocalXmlMetadata);
            if (entries is { Length: >= 1 })
            {
                return Metadata.Confocal.MetadataXmlSerializer.Deserialize(GetStringMetadataValue(entries));
            }

            // TODO : just for first usage before added real image, than delete it
            return new Metadata.Confocal.Metadata()
            {
                LightWavelength = Length.FromNanometers(405),
                LightIntensity = Ratio.FromPercent(0),
                PinholePosition = Length.Zero,
                FilterWheelColor = Length.Zero,
                Dwell = Duration.FromNanoseconds(0.1),
                Gain = Level.Zero,
                ADC = ElectricPotential.FromVolts(0.1),
                PixelSizeX = Length.FromMicrometers(2.4),
                PixelSizeY = Length.FromMicrometers(2.4),
                Mode = Metadata.Confocal.LuminescenceMode.Reflection,
                WorkingDistance = Length.Zero,
                ImagePath = string.Empty,
            };
        }
        catch { }
        return null;
    }

    private static Metadata.Coordinates.Metadata LoadCoordinatesMetadata(Tiff tiffFile)
    {
        try
        {
            var entries = tiffFile.GetField(BetrianCoordinatesXmlMetadata);
            if (entries is { Length: >= 1 })
            {
                return Metadata.Coordinates.MetadataXmlSerializer.Deserialize(GetStringMetadataValue(entries));
            }
        }
        catch
        {
        }
        return new();
    }

    private static Guid LoadIdMetadata(Tiff tiffFile, TiffTag tag)
    {
        try
        {
            var entries = tiffFile.GetField(tag);
            if (entries is { Length: >= 1 })
            {
                return Guid.Parse(GetStringMetadataValue(entries));
            }
        }
        catch { }
        return Guid.Empty;
    }

    private static void TagMetadataExtender(Tiff tif)
    {
        TiffFieldInfo[] tiffFieldInfo =
        [
            new TiffFieldInfo(TiffTag.SOFTWARE, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(TiffTag.SOFTWARE)),
            new TiffFieldInfo(TiffTag.IMAGEDESCRIPTION, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(TiffTag.IMAGEDESCRIPTION)),
            new TiffFieldInfo(TiffTag.DATETIME, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(TiffTag.DATETIME)),
            new TiffFieldInfo(TiffTag.MODEL, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(TiffTag.MODEL)),
            new TiffFieldInfo(FeiImageXmlMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(FeiImageXmlMetadata)),
            new TiffFieldInfo(FeiImageIniMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(FeiImageIniMetadata)),
            new TiffFieldInfo(FeiImageXmlMementoMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(FeiImageXmlMementoMetadata)),
            new TiffFieldInfo(BetrianLuminescenceXmlMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianLuminescenceXmlMetadata)),
            new TiffFieldInfo(BetrianConfocalXmlMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianConfocalXmlMetadata)),
            new TiffFieldInfo(BetrianCoordinatesXmlMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianCoordinatesXmlMetadata)),
            new TiffFieldInfo(BetrianImageIdMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianImageIdMetadata)),
            new TiffFieldInfo(BetrianLayerIdMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianLayerIdMetadata)),
            new TiffFieldInfo(BetrianRegionOfInterestIdMetadata, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, nameof(BetrianRegionOfInterestIdMetadata)),
        ];

        tif.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
    }

    private static void UpdateTiffMetadata(this Tiff tiff, Metadata.TiffMetadata? tiffMetadata)
    {
        if (tiffMetadata is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(tiffMetadata.Software))
        {
            _ = tiff.SetField(TiffTag.SOFTWARE, tiffMetadata.Software);
        }

        if (!string.IsNullOrWhiteSpace(tiffMetadata.ImageDescription))
        {
            _ = tiff.SetField(TiffTag.IMAGEDESCRIPTION, tiffMetadata.ImageDescription);
        }

        _ = tiff.SetField(TiffTag.DATETIME, tiffMetadata.TimeOfAcquisition.UtcDateTime.ToString(TiffDateTimeFormat));

        if (!string.IsNullOrWhiteSpace(tiffMetadata.CameraModel))
        {
            _ = tiff.SetField(TiffTag.MODEL, tiffMetadata.CameraModel);
        }
    }

    private static void UpdateFeiXmlMetadata(this Tiff tiff, Metadata.Thermofisher.XmlFormat.Metadata? feiXmlMetadata)
    {
        if (feiXmlMetadata is not null)
        {
            var data = Metadata.Thermofisher.XmlFormat.MetadataXmlSerializer.Serialize(feiXmlMetadata);
            _ = tiff.SetField(FeiImageXmlMetadata, [data]);
        }
    }

    private static void UpdateFeiIniMetadata(this Tiff tiff, Metadata.Thermofisher.IniFormat.Metadata? feiIniMetadata)
    {
        if (feiIniMetadata is not null)
        {
            //var data = Metadata.Thermofisher.IniFormat.MetadataIniSerializer.Serialize(feiIniMetadata);
            //_ = tiff.SetField(FeiImageIniMetadata, [data]);
        }
    }

    private static void UpdateFeiMementoMetadata(this Tiff tiff, Metadata.Thermofisher.MementoFormat.Metadata? feiMementoMetadata)
    {
        if (feiMementoMetadata is not null)
        {
            _ = tiff.SetField(FeiImageXmlMementoMetadata, [feiMementoMetadata.Data]);
        }
    }

    private static void UpdateLuminescenceMetadata(this Tiff tiff, Metadata.Luminescence.Metadata? luminescenceMetadata)
    {
        if (luminescenceMetadata is not null)
        {
            var data = Metadata.Luminescence.MetadataXmlSerializer.Serialize(luminescenceMetadata);
            _ = tiff.SetField(BetrianLuminescenceXmlMetadata, [data]);
        }
    }

    private static void UpdateConfocalMetadata(this Tiff tiff, Metadata.Confocal.Metadata? confocalMetadata)
    {
        if (confocalMetadata is not null)
        {
            var data = Metadata.Confocal.MetadataXmlSerializer.Serialize(confocalMetadata);
            _ = tiff.SetField(BetrianConfocalXmlMetadata, [data]);
        }
    }

    private static void UpdateCoordinatesMetadata(this Tiff tiff, Metadata.Coordinates.Metadata? coordinatesMetadata)
    {
        if (coordinatesMetadata is not null)
        {
            var data = Metadata.Coordinates.MetadataXmlSerializer.Serialize(coordinatesMetadata);
            _ = tiff.SetField(BetrianCoordinatesXmlMetadata, [data]);
        }
    }

    private static void UpdateIdMetadata(this Tiff tiff, Guid id, TiffTag tag)
    {
        _ = tiff.SetField(tag, id.ToString());
    }

    public static void Save(ImageWithMetadata imageWithMetadata, string filePath)
    {
        Guard.IsNotNull(imageWithMetadata);

        if (
            !filePath.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) &&
            !filePath.EndsWith(".tif", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new ArgumentException("The file extension must be .tiff or .tif", nameof(filePath));
        }

        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        if (!imageWithMetadata.Image.SaveTiff(filePath))
        {
            throw new InvalidOperationException("Failed to save the image. Check that the directory exist and user has proper rights for writing.");
        }

        using var tiffImage = Tiff.Open(filePath, "a");
        _ = tiffImage.SetDirectory(0);

        tiffImage.UpdateTiffMetadata(imageWithMetadata.TiffMetadata);
        tiffImage.UpdateFeiXmlMetadata(imageWithMetadata.FeiXmlMetadata);
        tiffImage.UpdateFeiIniMetadata(imageWithMetadata.FeiIniMetadata);
        tiffImage.UpdateFeiMementoMetadata(imageWithMetadata.FeiMementoMetadata);
        tiffImage.UpdateLuminescenceMetadata(imageWithMetadata.LuminescenceMetadata);
        tiffImage.UpdateConfocalMetadata(imageWithMetadata.ConfocalMetadata);
        tiffImage.UpdateCoordinatesMetadata(imageWithMetadata.Coordinates);
        tiffImage.UpdateIdMetadata(imageWithMetadata.ImageId, BetrianImageIdMetadata);
        tiffImage.UpdateIdMetadata(imageWithMetadata.LayerId, BetrianLayerIdMetadata);
        tiffImage.UpdateIdMetadata(imageWithMetadata.RegionOfInterestId, BetrianRegionOfInterestIdMetadata);

        _ = tiffImage.CheckpointDirectory();

        imageWithMetadata.MemoryOrigin = false;
    }
}
