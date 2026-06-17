using Tarmi.Imaging.Common;
using Tarmi.Maps.DataFormat.TfsDataModel;
using Tarmi.Models;
using Tarmi.Projects;

namespace Tarmi.Maps.DataFormat;

public static class MapsExportService
{
    public const double RowTolerance = 10;

    public static void CreateMapsExport(
        string baseExportDirectory,
        List<(LayerDescriptor, LayerContentDescriptor)> contentList,
        Func<LayerDescriptor, LayerContentDescriptor, string> GetContentFilePath,
        Func<StagePosition, StageCameraView, StageCameraView, StagePosition> TransformPosition,
        Func<ImageMetadata, Channels> CreateChannels
    )
    {
        if (contentList.Count == 0)
        {
            throw new ArgumentException("There is no content to maps export.");
        }

        MapsExport mapsExport = new(baseExportDirectory);
        var imagesDirectory = mapsExport.ImagesPath;
        var manualImportDirectory = mapsExport.ManualImportPath;
        TfsData exportData = new() { Items = [] };
        bool directoryCreated = false;

        foreach (var (layer, content) in contentList)
        {
            LayerContentDescriptor[] images;
            LayerContentDescriptor mainImageDescriptor;

            switch (layer)
            {
                case LayeredImageDescriptor:
                    images = [content];
                    mainImageDescriptor = content;
                    break;
                case TileSetDescriptor tileSetDescriptor:
                    images = tileSetDescriptor.Source.IsOneOf(StageCameraView.LM) ?
                        [tileSetDescriptor.StitchedImage] :
                        [.. tileSetDescriptor.Images];
                    mainImageDescriptor = tileSetDescriptor.StitchedImage;
                    break;
                case TileSet3DDescriptor tileSet3DDescriptor:
                    images = [tileSet3DDescriptor.StitchedImage];
                    mainImageDescriptor = tileSet3DDescriptor.StitchedImage;
                    break;
                case ZStackDescriptor zStackDescriptor:
                    images = [.. zStackDescriptor.Images];
                    mainImageDescriptor = images[0];
                    break;
                default:
                    throw new NotImplementedException("Unknown layer descriptor type in maps export.");
            }

            if (images.Length == 0)
            {
                continue;
            }

            var path = GetContentFilePath(layer, mainImageDescriptor);
            var imageWithMetadata = TiffImage.Load(path);

            if (imageWithMetadata.GetSource() == StageCameraView.LM)
            {
                var mappedImages = MapImages(layer, images, imagesDirectory, GetContentFilePath, TransformPosition);
                var imageSize = imageWithMetadata.Coordinates.ImageSize;
                var pixelSize = imageWithMetadata.GetPixelSize();

                ImageMatrix imageMatrix = new()
                {
                    Guid = content.Id,
                    Name = layer.Name, // TODO: propagate description from image
                    TileWidth = pixelSize.X.Nanometers,
                    TileHeight = pixelSize.Y.Nanometers,
                    TilePixelWidth = imageSize.Width,
                    TilePixelHeight = imageSize.Height,
                    Channels = CreateChannels(imageWithMetadata),
                    Images = mappedImages,
                };

                exportData.Items.Add(imageMatrix);
            }
            else
            {
                if (!directoryCreated)
                {
                    _ = Directory.CreateDirectory(manualImportDirectory);
                    directoryCreated = true;
                }

                foreach (var image in images)
                {
                    var imagePath = GetContentFilePath(layer, image);
                    var imageToExport = TiffImage.Load(imagePath);
                    imageToExport.TransformToInplace(ImageTransformationType.Raw);
                    TiffImage.Save(imageToExport, Path.Combine(manualImportDirectory, image.Filename));
                }
            }
        }

        if (exportData.Items.Count > 0)
        {
            mapsExport.XmlExport(exportData);
        }
    }

    private static Images MapImages(
        LayerDescriptor layer,
        LayerContentDescriptor[] images,
        string exportPath,
        Func<LayerDescriptor, LayerContentDescriptor, string> getContentFilePath,
        Func<StagePosition, StageCameraView, StageCameraView, StagePosition> transformPosition
    )
    {
        List<Image> imageList = [];
        (LayerContentDescriptor Descriptor, ImagePosition Position, string Time)[] list = images
            .Select(image =>
            {
                var path = getContentFilePath(layer, image);
                var imageWithMetadata = TiffImage.Load(path);
                imageWithMetadata.TransformToInplace(ImageTransformationType.Maps);
                var imagePosition = MapImage(imageWithMetadata, image, exportPath, transformPosition);
                return (image, imagePosition, imageWithMetadata.TiffMetadata!.TimeOfAcquisition.DateTime.ToString());
            })
            .ToArray();

        if (layer is ZStackDescriptor)
        {
            byte plane = 0;

            imageList = list.Select(image => new Image
            {
                Guid = image.Descriptor.Id,
                Index = new ImageIndex() { Plane = plane++ },
                Position = image.Position,
                RelativePath = Path.Combine(Path.GetFileName(exportPath)!, image.Descriptor.Filename),
                Time = image.Time
            }).ToList();

            return new Images() { Items = imageList };
        }

        var sortedImages = list.OrderByDescending(img => img.Position.Y).ToArray();
        byte row = 0;
        int startIndex = 0;
        while (startIndex < sortedImages.Length)
        {
            var imagePosition = sortedImages[startIndex].Position;
            var nextIndex = Array.FindLastIndex(sortedImages, startIndex, image => Math.Abs(image.Position.Y - imagePosition.Y) <= RowTolerance) + 1;
            var rowArray = sortedImages[startIndex..nextIndex];

            imageList.AddRange(
                rowArray.OrderByDescending(a => a.Position.X).Select((img, col) => new Image()
                {
                    Guid = img.Descriptor.Id,
                    Index = new ImageIndex()
                    {
                        Row = row,
                        Column = (byte)col
                    },
                    Position = img.Position,
                    RelativePath = Path.Combine(Path.GetFileName(exportPath), img.Descriptor.Filename),
                    Time = img.Time
                })
            );

            row++;
            startIndex = nextIndex;
        }

        return new Images() { Items = imageList };
    }

    private static ImagePosition MapImage(
        ImageWithMetadata imageWithMetadata,
        LayerContentDescriptor image,
        string exportPath,
        Func<StagePosition, StageCameraView, StageCameraView, StagePosition> transformPosition)
    {
        string destinationImagePath = Path.Combine(exportPath, image.Filename);
        TiffImage.Save(imageWithMetadata, destinationImagePath);

        ImagePosition imagePosition = new();

        if (imageWithMetadata.GetSource() == StageCameraView.LM)
        {
            var stagePosition = imageWithMetadata.GetStagePosition();
            var transformedPosition = transformPosition(stagePosition, StageCameraView.LM, StageCameraView.SEM);
            imagePosition = new()
            {
                X = transformedPosition.X.Micrometers,
                Y = transformedPosition.Y.Micrometers,
                Z = transformedPosition.Z.Micrometers
            };
        }

        return imagePosition;
    }
}
