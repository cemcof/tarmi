using CFLMnavi.Projects;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class GroupedImagesChildVM : VirtualChildVM
{
    public GroupedImagesChildVM(
        RoiVM parentRoi, VirtualChildVM parentVM, string displayName,
        LayerDescriptor layerDescriptor, IEnumerable<LayerContentDescriptor> contentDescriptors,
        RoiChildBehaviors behaviors, ImageAttributes childrenEnforcedAttributes = ImageAttributes.None
    )
        : base(parentRoi, parentVM, behaviors)
    {
        foreach (var contentDescriptor in contentDescriptors)
        {
            _children.Add(
                new SingleImageChildVM(
                    parentRoi, this, layerDescriptor, contentDescriptor,
                    behaviors: behaviors,
                    enforcedAttributes: childrenEnforcedAttributes
                )
            );

            Attributes = ImageAttributes.Folder;
            Name = displayName;
        }
    }
}
