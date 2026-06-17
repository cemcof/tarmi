using System.Reflection;
using Tarmi.Imaging.Common;
using Tarmi.Imaging.Common.Metadata;
using Tarmi.Imaging.Common.Metadata.Luminescence;
using Tarmi.Models;
using Tarmi.Projects.Implementation;
using Conf = Tarmi.Imaging.Common.Metadata.Confocal;

namespace Tarmi.Projects.Transactions;

public class ZStackCreationTransaction : IDisposable
{
    protected readonly ObservableProject _project;
    protected readonly StageCameraView _stageCameraView;
    protected readonly Func<StagePosition, StageCameraView, LengthPoint> _getPlanePosition;
    protected bool _nameUpdated = false;
    protected ZStackDescriptor _descriptor;
    protected bool _cancelled = false;

    public ZStackCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, Guid? linkId)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = project.CreateZStackDescriptor(stageCameraView, project.ActiveRegionOfInterestId, linkId);
    }

    public ZStackCreationTransaction(ObservableProject project, StageCameraView stageCameraView, Func<StagePosition, StageCameraView, LengthPoint> getPlanePosition, ZStackDescriptor stackDescriptor)
    {
        _project = project;
        _stageCameraView = stageCameraView;
        _getPlanePosition = getPlanePosition;
        _descriptor = stackDescriptor;
    }

    protected virtual string CreateName(string gridName, ImageWithMetadata image)
    {
        if (image.LuminescenceMetadata is Metadata lm)
        {
            var mode = lm.Mode == LuminescenceMode.Fluorescence ? "FLUO" : "REFL";
            var color = lm.LightWavelength.ToString();
            return $"{mode} {color} {gridName}";
        }

        if (image.ConfocalMetadata is Conf.Metadata cm)
        {
            var mode = cm.Mode == Conf.LuminescenceMode.Fluorescence ? "CFLUO" : "CREFL";
            var color = cm.LightWavelength.ToString();
            return $"{mode} {color} {gridName}";
        }

        return $"LM {gridName}";
    }

    protected void UpdateLayerName(ImageWithMetadata image)
    {
        if (!_nameUpdated)
        {
            var planePosition = _getPlanePosition(image.GetStagePosition(), _stageCameraView);
            var grid = _project.Holder.Grids.FirstOrDefault(g => g.BoundingRectangle.IsPointInsideRectangle(planePosition));
            if (grid is not null)
            {
                _descriptor = _descriptor with
                {
                    Name = CreateName(grid.Name, image)
                };
                _nameUpdated = true;
            }
        }
    }

    protected static string CreateSoftwareName()
        => $"{AppDomain.CurrentDomain.FriendlyName} {Assembly.GetEntryAssembly()!.GetName().Version}";

    protected virtual TiffMetadata CreateOrUpdateImageTiffMetadata(TiffMetadata metadata, int index)
    {
        return metadata with
        {
            Software = CreateSoftwareName(),
            ImageDescription = $"Z-Stack Image #{index + 1}"
        };
    }

    protected virtual TiffMetadata CreateOrUpdateMipImageTiffMetadata(TiffMetadata metadata)
    {
        return metadata with
        {
            Software = CreateSoftwareName(),
            ImageDescription = $"Z-Stack MIP Image",
        };
    }

    public virtual void AddImage(ImageWithMetadata image, int index)
    {
        UpdateLayerName(image);
        image = image with
        {
            TiffMetadata = CreateOrUpdateImageTiffMetadata(image.TiffMetadata, index),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id
        };
        var filename = $"{index + 1:00000}{ProjectExtensions.ImageExtension}";
        var path = Path.Combine(_project.GetLayerDirectoryPath(_descriptor), filename);
        TiffImage.Save(image, path);
        _descriptor = _descriptor with
        {
            Images = [.. _descriptor.Images, new LayerContentDescriptor { SubDirectory = null, Filename = filename, Id = image.ImageId }]
        };
    }

    public virtual void AddMipImage(ImageWithMetadata image)
    {
        image = image with
        {
            TiffMetadata = CreateOrUpdateMipImageTiffMetadata(image.TiffMetadata),
            RegionOfInterestId = _descriptor.RegionOfInterestId,
            LayerId = _descriptor.Id,
        };
        var filename = $"_mip{ProjectExtensions.ImageExtension}";
        var path = Path.Combine(_project.GetLayerDirectoryPath(_descriptor), filename);
        TiffImage.Save(image, path);
        _descriptor.MipImage = new LayerContentDescriptor { SubDirectory = null, Filename = filename, Id = image.ImageId };
    }

    public virtual IEnumerable<string> GetFilePaths()
    {
        foreach (var contentDescriptor in _descriptor.Images)
        {
            yield return _project.GetContentFilePath(_descriptor, contentDescriptor);
        }
    }

    public void Cancel() => _cancelled = true;

    public virtual void Dispose()
    {
        if (_cancelled)
        {
            Directory.Delete(_project.GetLayerDirectoryPath(_descriptor), recursive: true);
        }
        else
        {
            _project.AddOrUpdateDescriptor(_descriptor);
        }
        GC.SuppressFinalize(this);
    }

    public async Task RegenerateMipImage(Func<IEnumerable<ImageWithMetadata>, ImageWithMetadata> mipFunction)
    {
        static IEnumerable<ImageWithMetadata> TraverseImages(string[] paths)
        {
            foreach (var path in paths)
            {
                using var image = TiffImage.Load(path);
                yield return image;
            }
        }

        await Task.Run(() =>
        {
            var paths = GetFilePaths().ToArray();
            if (paths is { Length: 0 })
            {
                return Task.CompletedTask;
            }

            var resultImage = mipFunction(TraverseImages(paths));
            AddMipImage(resultImage);
            return Task.CompletedTask;
        });
    }
}

