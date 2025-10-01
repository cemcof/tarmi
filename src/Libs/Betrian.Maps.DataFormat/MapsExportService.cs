using Betrian.Imaging.Common;
using Betrian.Maps.DataFormat.TfsDataModel;
using Betrian.Models;
using CFLMnavi.Projects;

namespace Betrian.Maps.DataFormat;

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
            LayerContentDescriptor[] images = [];
            LayerContentDescriptor mainImageDescriptor;

            switch (layer)
            {
                case LayeredImageDescriptor imageDescriptor:
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
        Func<LayerDescriptor, LayerContentDescriptor, string> GetContentFilePath,
        Func<StagePosition, StageCameraView, StageCameraView, StagePosition> TransformPosition
    )
    {
        List<Image> imageList = [];
        List<(LayerContentDescriptor, ImagePosition, string)> list = [];

        foreach (var image in images)
        {
            var path = GetContentFilePath(layer, image);
            var imageWithMetadata = TiffImage.Load(path);
            imageWithMetadata.TransformToInplace(ImageTransformationType.Maps);
            var imagePosition = MapImage(imageWithMetadata, image, exportPath, TransformPosition);
            list.Add(new(image, imagePosition, imageWithMetadata.TiffMetadata!.TimeOfAcquisition.DateTime.ToString()));
        }

        if (layer is ZStackDescriptor)
        {
            byte plane = 0;

            for (int j = 0; j < list.Count; j++)
            {
                imageList.Add(new Image()
                {
                    Guid = list[j].Item1.Id,
                    Index = new ImageIndex() { Plane = plane++ },
                    Position = list[j].Item2,
                    RelativePath = Path.Combine(Path.GetFileName(exportPath)!, list[j].Item1.Filename),
                    Time = list[j].Item3
                });
            }

            return new Images() { Items = imageList };
        }

        var sortedImages = list.OrderByDescending(img => img.Item2.Y).ToList();
        byte row = 0;
        byte col = 0;

        for (int i = 0; i < sortedImages.Count; i++)
        {
            List<(LayerContentDescriptor, ImagePosition, string)> rowList = [];
            rowList.Add(sortedImages[i]);
            var imagePosition = sortedImages[i].Item2;
            int j;

            for (j = i + 1; j < sortedImages.Count; j++)
            {
                var currentPosition = sortedImages[j].Item2;

                if (imagePosition.Y <= currentPosition.Y + RowTolerance && imagePosition.Y >= currentPosition.Y - RowTolerance)
                {
                    rowList.Add(sortedImages[j]);
                }
                else
                {
                    break;
                }
            }

            i = j < (sortedImages.Count - 1) ? j - 1 : sortedImages.Count - 1;
            var resultList = rowList.OrderByDescending(img => img.Item2.X).ToList();

            for (j = 0; j < resultList.Count; j++)
            {
                imageList.Add(new Image()
                {
                    Guid = resultList[j].Item1.Id,
                    Index = new ImageIndex() { Row = row, Column = col++ },
                    Position = resultList[j].Item2,
                    RelativePath = Path.Combine(Path.GetFileName(exportPath)!, resultList[j].Item1.Filename),
                    Time = resultList[j].Item3
                });
            }

            row++;
            col = 0;
        }

        return new Images() { Items = imageList };
    }

    private static ImagePosition MapImage(
        ImageWithMetadata imageWithMetadata,
        LayerContentDescriptor image,
        string exportPath,
        Func<StagePosition, StageCameraView, StageCameraView, StagePosition> TransformPosition)
    {
        string destinationImagePath = Path.Combine(exportPath, image.Filename);
        TiffImage.Save(imageWithMetadata, destinationImagePath);

        ImagePosition imagePosition = new();

        if (imageWithMetadata.GetSource() == StageCameraView.LM)
        {
            var stagePosition = imageWithMetadata.GetStagePosition();
            var transformedPosition = TransformPosition(stagePosition, StageCameraView.LM, StageCameraView.SEM);
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
