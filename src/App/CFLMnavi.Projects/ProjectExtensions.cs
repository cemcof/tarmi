using Betrian.Models;

namespace CFLMnavi.Projects;

public static class ProjectExtensions
{
    public const string ImageExtension = ".tiff";
    public const string TileSetsDirectoryName = "TileSets";
    public const string ZStacksDirectoryName = "ZStacks";
    public const string TileSets3DDirectoryName = "TileSets3D";
    public const string ImageLayersDirectoryName = "ImageLayers";
    public const string ThumbnailsDirectoryName = "Thumbnails";
    public const string StitchedSubDirectoryName = "Stitched";

    public static TileSetDescriptor CreateTileSetDescriptor(this Project project, StageCameraView cameraView, Guid regionOfInterestId, TileSetOptions tileSetOptions)
    {
        var id = UUIDNext.Uuid.NewSequential();
        var layerDirectoryPath = Path.Combine(project.Directory, TileSetsDirectoryName, id.ToString());

        return new TileSetDescriptor
        {
            Id = id,
            RegionOfInterestId = regionOfInterestId,
            Name = "New Tile Set",
            Source = cameraView,
            Images = [],
            StitchedImage = new() { SubDirectory = StitchedSubDirectoryName, Filename = $"stitched{ImageExtension}", Id = UUIDNext.Uuid.NewSequential() },
            StitchedImageThumbnail = new() { SubDirectory = StitchedSubDirectoryName, Filename = $"thumbnail{ImageExtension}", Id = UUIDNext.Uuid.NewSequential() },
            GrabbingOptions = tileSetOptions
        };
    }

    public static ZStackDescriptor CreateZStackDescriptor(this Project project, StageCameraView cameraView, Guid regionOfInterestId, Guid? linkId)
    {
        var id = UUIDNext.Uuid.NewSequential();
        var layerDirectoryPath = Path.Combine(project.Directory, ZStacksDirectoryName, id.ToString());

        return new ZStackDescriptor
        {
            Id = id,
            RegionOfInterestId = regionOfInterestId,
            Name = "New ZStack",
            Source = cameraView,
            LinkId = linkId,
            Images = []
        };
    }

    public static TileSet3DDescriptor CreateTileSet3DDescriptor(
        this Project project,
        StageCameraView cameraView,
        Guid regionOfInterestId,
        TileSetOptions tileSetOptions,
        Guid? linkId
    )
    {
        var id = UUIDNext.Uuid.NewSequential();
        var layerDirectoryPath = Path.Combine(project.Directory, TileSets3DDirectoryName, id.ToString());

        return new TileSet3DDescriptor
        {
            Id = id,
            RegionOfInterestId = regionOfInterestId,
            Name = "New Tile Set 3D",
            Source = cameraView,
            LinkId = linkId,
            Images = [],
            StitchedImage = new() { SubDirectory = StitchedSubDirectoryName, Filename = $"stitched{ImageExtension}", Id = UUIDNext.Uuid.NewSequential() },
            StitchedImageThumbnail = new() { SubDirectory = StitchedSubDirectoryName, Filename = $"thumbnail{ImageExtension}", Id = UUIDNext.Uuid.NewSequential() },
            GrabbingOptions = tileSetOptions
        };
    }

    public static LayeredImageDescriptor CreateLayeredImageDescriptor(this Project project, StageCameraView cameraView, Guid regionOfInterestId)
    {
        var id = UUIDNext.Uuid.NewSequential();
        var layerDirectoryPath = Path.Combine(project.Directory, ImageLayersDirectoryName, id.ToString());

        return new LayeredImageDescriptor
        {
            Id = id,
            RegionOfInterestId = regionOfInterestId,
            Name = "New Image Layers",
            Source = cameraView,
            Images = []
        };
    }

    public static string GetContentFilePath(this Project project, LayerDescriptor layerDescriptor, LayerContentDescriptor contentDescriptor)
        => Path.Combine(project.GetLayerDirectoryPath(layerDescriptor), contentDescriptor.FilePath);

    public static string GetContentFilePath(this Project project, LayerDescriptor layerDescriptor, LayerDescriptor subLayerDescriptor, LayerContentDescriptor contentDescriptor)
        => Path.Combine(project.GetLayerDirectoryPath(layerDescriptor), subLayerDescriptor.Id.ToString(), contentDescriptor.FilePath);

    public static string GetLayerDirectoryPath(this Project project, LayerDescriptor layerDescriptor)
    {
        return layerDescriptor switch
        {
            TileSetDescriptor => Path.Combine(project.Directory, TileSetsDirectoryName, layerDescriptor.Id.ToString()),
            TileSet3DDescriptor => Path.Combine(project.Directory, TileSets3DDirectoryName, layerDescriptor.Id.ToString()),
            ZStackDescriptor => Path.Combine(project.Directory, ZStacksDirectoryName, layerDescriptor.Id.ToString()),
            LayeredImageDescriptor => Path.Combine(project.Directory, ImageLayersDirectoryName, layerDescriptor.Id.ToString()),
            _ => throw new NotSupportedException($"Layer type {layerDescriptor.GetType().Name} is not supported.")
        };
    }

    public static string GetLayerDirectoryPath(this Project project, LayerDescriptor layerDescriptor, LayerDescriptor subLayerDescriptor)
    {
        return layerDescriptor switch
        {
            TileSet3DDescriptor => Path.Combine(project.Directory, TileSets3DDirectoryName, layerDescriptor.Id.ToString(), subLayerDescriptor.Id.ToString()),
            _ => throw new NotSupportedException($"Layer type {layerDescriptor.GetType().Name} does not support sub layers.")
        };
    }
}
