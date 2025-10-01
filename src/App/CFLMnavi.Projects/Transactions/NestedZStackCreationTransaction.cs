using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Projects.Implementation;

namespace CFLMnavi.Projects.Transactions;

public sealed class NestedZStackCreationTransaction : ZStackCreationTransaction
{
    private readonly TileSet3DDescriptor _parentLayer;
    private readonly string _baseName;

    public NestedZStackCreationTransaction(
        ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, Guid? linkId,
        TileSet3DDescriptor parentTileset3DLayer, string baseName
    )
        : base(project, stageCameraView, getPlanePosition, linkId)
    {
        _parentLayer = parentTileset3DLayer;
        _baseName = baseName;
    }

    protected override string CreateName(string gridName, ImageWithMetadata image)
        => _baseName;

    public override void AddImage(ImageWithMetadata image, int index)
    {
        UpdateLayerName(image);
        image = image with
        {
            TiffMetadata = CreateOrUpdateImageTiffMetadata(image.TiffMetadata, index),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id
        };
        var filename = $"{++index:00000}{ProjectExtensions.ImageExtension}";
        var path = Path.Combine(_project.GetLayerDirectoryPath(_parentLayer, _descriptor), filename);
        TiffImage.Save(image, path);
        _descriptor = _descriptor with
        {
            Images = [.. _descriptor.Images, new LayerContentDescriptor { SubDirectory = null, Filename = filename, Id = image.ImageId }]
        };
    }

    public override void AddMipImage(ImageWithMetadata image)
    {
        image = image with
        {
            TiffMetadata = CreateOrUpdateMipImageTiffMetadata(image.TiffMetadata),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id,
        };
        var filename = $"_mip{ProjectExtensions.ImageExtension}";
        var path = Path.Combine(_project.GetLayerDirectoryPath(_parentLayer, _descriptor), filename);
        TiffImage.Save(image, path);
        _descriptor.MipImage = new LayerContentDescriptor { SubDirectory = null, Filename = filename, Id = image.ImageId };
    }

    public override IEnumerable<string> GetFilePaths()
    {
        foreach (var contentDescriptor in _descriptor.Images)
        {
            yield return _project.GetContentFilePath(_parentLayer, _descriptor, contentDescriptor);
        }
    }

    public override void Dispose()
    {
        if (_cancelled)
        {
            Directory.Delete(_project.GetLayerDirectoryPath(_descriptor), recursive: true);
        }
        else
        {
            _parentLayer.Images.Add(_descriptor);
        }
        GC.SuppressFinalize(this);
    }
}
