using Tarmi.Projects;

namespace Tarmi.App.ViewModels.ROIs;

public sealed partial class StackedTilesVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors ZStackBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = false,
    };

    private static readonly RoiChildBehaviors ZStackMipImageBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanExportToMaps = true,
    };

    public TileSet3DDescriptor Descriptor { get; }

    public StackedTilesVirtualChildVM(
        RoiVM roiVM, VirtualChildVM? parentVM,
        string name, TileSet3DDescriptor layerDescriptor,
        RoiChildBehaviors behaviors
    )
        : base(roiVM, parentVM, behaviors)
    {
        Descriptor = layerDescriptor;

        int index = 0;
        foreach (var stackDescriptor in layerDescriptor.Images)
        {
            var virtualStackedImage = new ZStackVirtualChildVM(
                roiVM, this, stackDescriptor,
                ZStackBehaviors, ZStackMipImageBehaviors,
                $"Tile Stack #{++index}"
            );
            _children.Add(virtualStackedImage);
        }

        Attributes = ImageAttributes.Folder | ImageAttributes.ZStack;
        Name = name;
    }
}
