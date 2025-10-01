using System.IO;
using CFLMnavi.Projects;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public sealed partial class TileSet3DVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors _mipTilesetBehaviors = new()
    {
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
        CanExportToMaps = true,
    };

    private static readonly RoiChildBehaviors _nestedStackedTilesBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
        CanExportToMaps = true,
    };

    public TileSet3DDescriptor Descriptor { get; }

    public override CorrelationInfo CorrelationInfo
        => Descriptor.CorrelationInfo;

    public TileSet3DVirtualChildVM(
        RoiVM roiVM, VirtualChildVM? parentVM, TileSet3DDescriptor layerDescriptor,
        RoiChildBehaviors behaviors

    )
        : base(roiVM, parentVM, behaviors)
    {
        Descriptor = layerDescriptor;

        var stitchedImage =
            new SingleImageChildVM(
                roiVM, this, layerDescriptor, layerDescriptor.StitchedImage,
                _mipTilesetBehaviors, enforcedAttributes: ImageAttributes.TileSet | ImageAttributes.MipImage
            );
        var gridStackedImages = new StackedTilesVirtualChildVM(roiVM, this, "Tiles", layerDescriptor, _nestedStackedTilesBehaviors);
        _children.Add(stitchedImage);
        _children.Add(gridStackedImages);

        Attributes = stitchedImage.Attributes & ~ImageAttributes.Reference;
        Name = stitchedImage.Name;
    }

    public override async Task RemoveImplementation()
    {
        await Task.Run(async () =>
        {
            await DeselectAllVisibleImages();

            if (Descriptor.CorrelationInfo.IsReferenceImage)
            {
                _observableProject.UnsetReference();
                await RoiVM.Parent.ImagesStateManager.OnReferenceChanged();
            }

            // remove from parent
            RoiVM.RemoveChild(this);

            var path = _observableProject.GetLayerDirectoryPath(Descriptor);
            try
            {
                // remove files structure
                Directory.Delete(path, recursive: true);

                // finally remove descriptor from project
                _observableProject.RemoveDescriptor(Descriptor, save: true, notify: false);
            }
            catch (Exception ex)
            {
                RoiVM.Parent.Logger.LogError(ex, "Failed to remove tile set images, {Path}", path);
            }
        });
    }
}
