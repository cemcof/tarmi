using System.IO;
using AsyncAwaitBestPractices;
using Betrian.CflmNavi.App.Controls;
using Betrian.CflmNavi.App.Views;
using Betrian.Imaging.Common;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class StackedImageChildVM : ImageChildVM
{
    public ZStackDescriptor Descriptor { get; private set; }

    public override CorrelationInfo CorrelationInfo
        => Descriptor?.CorrelationInfo ?? new CorrelationInfo();

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial int MaxIndex { get; private set; }

    [ObservableProperty]
    public partial int Count { get; private set; }

    public StackedImageChildVM(
        RoiVM parentRoi, VirtualChildVM? parentVM, ZStackDescriptor descriptor,
        RoiChildBehaviors behaviors, ImageAttributes enforcedAttributes = ImageAttributes.None
    )
        : base(parentRoi, parentVM, behaviors, enforcedAttributes)
    {
        Descriptor = descriptor;
        CanBeMarkedAsReference = false;
        UpdateInternal();
        OnIndexChanged(0);
    }

    public override Guid SortId => ImageMetadata.LayerId;

    private void UpdateInternal()
    {
        Count = Descriptor.ImagesCount;
        MaxIndex = Count - 1;
        if (Index >= MaxIndex)
        {
            Index = MaxIndex;
        }
    }

    public override void Update()
        => UpdateInternal();

    protected override ImageAttributes GetAttributes()
        => base.GetAttributes() | ImageAttributes.ZStack;

    public override (LayerDescriptor, LayerContentDescriptor) GetActiveImageDescriptors()
        => (Descriptor, Descriptor.Images[Index]);

    protected override void OnSelectionTypeChangedImplementation(ImageSelection value)
    {
        if (value != ImageSelection.Unselected)
        {
            return;
        }

        if (RoiVM.Parent.ActiveDevice?.SecondaryPanelContent is CorrelationOptionsControl coc)
        {
            if (coc.DataContext is CorrelationOptionsViewModel covm)
            {
                if (Equals(covm.ImageChild))
                {
                    // hide correlation options control
                    RoiVM.Parent.ActiveDevice.SecondaryPanelContent = null;
                }
            }
        }
    }

    partial void OnIndexChanged(int value)
    {
        if (value < Count)
        {
            string imagePath;
            if (
                ParentVM is ZStackVirtualChildVM zStackVirtual &&
                zStackVirtual.ParentVM is StackedTilesVirtualChildVM tileSetVirtual
            )
            {
                imagePath = _observableProject.GetContentFilePath(tileSetVirtual.Descriptor, Descriptor, Descriptor.Images[value]);
            }
            else
            {
                imagePath = _observableProject.GetContentFilePath(Descriptor, Descriptor.Images[value]);
            }

            ImageMetadata = TiffImage.LoadMetadata(imagePath);
            RoiVM.Parent.ImagesStateManager.OnImageLayerChanged(this).SafeFireAndForget();
        }
    }

    protected override void RemoveChildSpecific()
    {
        var directoryPath = _observableProject.GetLayerDirectoryPath(Descriptor);
        _observableProject.RemoveDescriptor(Descriptor, save: true, notify: false);

        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch (Exception ex)
        {
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove z-stack images, {Path}", directoryPath);
        }
    }
}
