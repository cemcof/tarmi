using CFLMnavi.Projects;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public sealed partial class StackedTilesVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors _nestedStackedTilesBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
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
            var virtualStackedImage = new ZStackVirtualChildVM(roiVM, this, stackDescriptor, _nestedStackedTilesBehaviors, $"Tile Stack #{++index}");
            _children.Add(virtualStackedImage);
        }

        Attributes = ImageAttributes.Folder | ImageAttributes.ZStack;
        Name = name;
    }
}
