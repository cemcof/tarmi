using System.Reflection;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Models;
using Tarmi.Projects.Implementation;
using Conf = Tarmi.Imaging.Common.Metadata.Confocal;

namespace Tarmi.Projects.Transactions;

public enum PrematureTerminationMode
{
    DeleteAll,
    KeepResults
}

public class TileSetCreationTransaction : IDisposable
{
    private readonly ObservableProject _project;
    private readonly StageCameraView _stageCameraView;
    private readonly Func<StagePosition, StageCameraView, LengthPoint> _getPlanePosition;
    private readonly PrematureTerminationMode _terminationMode;
    private bool _nameUpdated = false;
    private TileSetDescriptor _descriptor;
    private bool _cancelled = false;

    public TileSetCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, RegionOfInterest roi, TileSetOptions tileSetOptions, PrematureTerminationMode mode = PrematureTerminationMode.KeepResults)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = project.CreateTileSetDescriptor(stageCameraView, roi.Id, tileSetOptions);
        _descriptor = _descriptor with
        {
            Name = "New Tile Set"
        };
        _terminationMode = mode;
    }

    public TileSetCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, TileSetDescriptor tileSetDescriptor, PrematureTerminationMode mode = PrematureTerminationMode.KeepResults)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = tileSetDescriptor;
        _terminationMode = mode;
    }

    private string CreateName(string gridName, ImageWithMetadata image)
    {
        switch (_stageCameraView)
        {
            case StageCameraView.SEM:
                return $"SEM {gridName}";
            case StageCameraView.FIB_RightAngle:
                return $"FIB RA {gridName}";
            case StageCameraView.LM:
                {
                    if (image.LuminescenceMetadata is Metadata lm)
                    {
                        var mode = lm.Mode == LuminescenceMode.Fluorescence ? "FLUO" : "REFL";
                        var color = lm.LightWavelength.ToString();
                        return $"{mode} {color} {gridName}";
                    }
                    return $"LM {gridName}";
                }
            case StageCameraView.Confocal:
                {
                    if (image.ConfocalMetadata is Conf.Metadata confocal)
                    {
                        var mode = confocal.Mode == Conf.LuminescenceMode.Fluorescence ? "CFLUO" : "CREFL";
                        var color = confocal.LightWavelength.ToString();
                        return $"{mode} {color} {gridName}";
                    }
                    return $"CONFOCAL {gridName}";
                }
            default:
                return gridName;
        }
    }

    private void UpdateLayerName(ImageWithMetadata image)
    {
        if (_nameUpdated)
        {
            return;
        }
        var planePosition = _getPlanePosition(image.GetStagePosition(), _stageCameraView);
        var grid = _project.Holder.Grids.FirstOrDefault(g => g.BoundingRectangle.IsPointInsideRectangle(planePosition));
        if (grid is null)
        {
            return;
        }
        _descriptor = _descriptor with
        {
            Name = CreateName(grid.Name, image)
        };
        _nameUpdated = true;
    }

    private static string CreateSoftwareName()
        => $"{AppDomain.CurrentDomain.FriendlyName} {Assembly.GetEntryAssembly()!.GetName().Version}";

    private TiffMetadata CreateOrUpdateImageTiffMetadata(TiffMetadata? metadata, int index)
    {
        metadata ??= new TiffMetadata { TimeOfAcquisition = DateTimeOffset.Now };

        return metadata with
        {
            Software = CreateSoftwareName(),
            ImageDescription = $"{_descriptor.Name} TileSet Image #{index++}"
        };
    }

    private TiffMetadata CreateOrUpdateStitchedImageTiffMetadata(TiffMetadata metadata, bool isThumbnail)
    {
        //var namePart = isThumbnail ? "Stitched TileSet Thumbnail " : "Stitched TileSet";
        return metadata with
        {
            Software = CreateSoftwareName(),
            //ImageDescription = $"{_descriptor.Name} {namePart}"
            ImageDescription = $"{metadata.TimeOfAcquisition:dd.MM. HH:mm:ss}"
        };
    }

    public void AddImage(ImageWithMetadata image, int index)
    {
        UpdateLayerName(image);
        image = image with
        {
            TiffMetadata = CreateOrUpdateImageTiffMetadata(image.TiffMetadata, index),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id
        };
        var filename = $"{index++:00000}{ProjectExtensions.ImageExtension}";
        var path = Path.Combine(_project.GetLayerDirectoryPath(_descriptor), filename);
        TiffImage.Save(image, path);
        _descriptor = _descriptor with
        {
            Images = [.. _descriptor.Images, new LayerContentDescriptor { SubDirectory = null, Filename = filename, Id = image.ImageId }]
        };
    }

    public void AddStitchedImage(ImageWithMetadata image)
    {
        image = image with
        {
            TiffMetadata = CreateOrUpdateStitchedImageTiffMetadata(image.TiffMetadata, isThumbnail: false),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id,
            ImageId = UUIDNext.Uuid.NewSequential()
        };
        var imagePath = _project.GetContentFilePath(_descriptor, _descriptor.StitchedImage);
        TiffImage.Save(image, imagePath);
    }

    public void AddStitchedImageThumbnail(ImageWithMetadata image)
    {
        image = image with
        {
            TiffMetadata = CreateOrUpdateStitchedImageTiffMetadata(image.TiffMetadata, isThumbnail: true),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id,
            ImageId = UUIDNext.Uuid.NewSequential()
        };
        var imagePath = _project.GetContentFilePath(_descriptor, _descriptor.StitchedImageThumbnail);
        TiffImage.Save(image, imagePath);
    }

    public IEnumerable<string> GetFilePaths()
    {
        foreach (var contentDescriptor in _descriptor.Images)
        {
            yield return _project.GetContentFilePath(_descriptor, contentDescriptor);
        }
    }

    public void Cancel() => _cancelled = true;

    public void Dispose()
    {
        if (_cancelled && (_terminationMode == PrematureTerminationMode.DeleteAll || _descriptor.ImagesCount == 0))
        {
            Directory.Delete(_project.GetLayerDirectoryPath(_descriptor), recursive: true);
        }
        else
        {
            _project.AddOrUpdateDescriptor(_descriptor);
        }
        GC.SuppressFinalize(this);
    }
}
