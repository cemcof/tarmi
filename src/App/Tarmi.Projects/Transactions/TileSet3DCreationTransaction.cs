using System.Reflection;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Models;
using Tarmi.Projects.Implementation;

namespace Tarmi.Projects.Transactions;

public class TileSet3DCreationTransaction : IDisposable
{
    private readonly ObservableProject _project;
    private readonly StageCameraView _stageCameraView;
    private readonly Func<StagePosition, StageCameraView, LengthPoint> _getPlanePosition;
    private readonly PrematureTerminationMode _terminationMode;
    private readonly TileSet3DDescriptor _descriptor;
    private bool _cancelled = false;

    public TileSet3DCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, RegionOfInterest roi, TileSetOptions tileSetOptions, ZStackOptions zStackOptions, Guid? linkId, PrematureTerminationMode mode = PrematureTerminationMode.KeepResults)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = project.CreateTileSet3DDescriptor(stageCameraView, roi.Id, tileSetOptions, zStackOptions, linkId);
        _descriptor = _descriptor with
        {
            Name = "New Tile Set 3D"
        };
        _terminationMode = mode;
    }

    public TileSet3DCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, TileSet3DDescriptor tileSetDescriptor, PrematureTerminationMode mode = PrematureTerminationMode.KeepResults)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = tileSetDescriptor;
        _terminationMode = mode;

        if (_terminationMode == PrematureTerminationMode.DeleteAll)
        {
            DeleteImages();
            _descriptor.Images.Clear();
        }
    }
    private static string CreateSoftwareName()
        => $"{AppDomain.CurrentDomain.FriendlyName} {Assembly.GetEntryAssembly()!.GetName().Version}";

    private TiffMetadata CreateOrUpdateStitchedImageTiffMetadata(TiffMetadata metadata, bool isThumbnail)
    {
        _ = isThumbnail; // currently not used, but could be in the future
        return metadata with
        {
            Software = CreateSoftwareName(),
            ImageDescription = $"{metadata.TimeOfAcquisition:dd.MM. HH:mm:ss}"
        };
    }

    public ZStackCreationTransaction CreateZStackTransaction()
    {
        return new NestedZStackCreationTransaction(
            _project, _stageCameraView, _getPlanePosition, null,
            _descriptor, $"Tile Set 3D - Tile {_descriptor.Images.Count + 1:000}");
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
        foreach (var stackDescriptor in _descriptor.Images)
        {
            foreach (var contentDescriptor in stackDescriptor.Images)
            {
                yield return _project.GetContentFilePath(_descriptor, stackDescriptor, contentDescriptor);
            }
        }
    }

    public IEnumerable<string> GetMipFilePaths()
    {
        foreach (var stackDescriptor in _descriptor.Images)
        {
            yield return _project.GetContentFilePath(_descriptor, stackDescriptor, stackDescriptor.MipImage);
        }
    }

    private void DeleteImages()
    {
        foreach (var stackDescriptor in _descriptor.Images)
        {
            foreach (var contentDescriptor in stackDescriptor.Images)
            {
                File.Delete(_project.GetContentFilePath(_descriptor, stackDescriptor, contentDescriptor));
            }
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
