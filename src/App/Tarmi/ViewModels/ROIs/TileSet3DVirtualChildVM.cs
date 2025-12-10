using System.IO;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels.ROIs;

public sealed partial class TileSet3DVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors MipImageBehaviors = new()
    {
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = true,
        CanExportToMaps = true,
        CanEditFiducials = true,
        CanBindCorrelation = true,
        CanEditCorrelationOptions = true,
    };

    private static readonly RoiChildBehaviors StackedTilesBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = false,
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
                roiVM, this, layerDescriptor, layerDescriptor.StitchedImage, MipImageBehaviors,
                enforcedAttributes: ImageAttributes.TileSet | ImageAttributes.MipImage
            );
        var gridStackedImages = new StackedTilesVirtualChildVM(
            roiVM, this, "Tiles", layerDescriptor, StackedTilesBehaviors);
        _children.Add(stitchedImage);
        _children.Add(gridStackedImages);

        // TODO: allow reference flag after propagation?
        Attributes = stitchedImage.Attributes & ~ImageAttributes.Reference;
        Name = stitchedImage.Name;
    }

    public bool CanBindCorrelation => RoiVM.IsBindable(this) && FiducialsGroupId.IsEmpty();

    public bool CanUnbindCorrelation => FiducialsGroupId.IsNotEmpty();

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanUnbindCorrelation))]
    private void UnbindCorrelation() => RoiVM.UnbindCorrelation(this);

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanBindCorrelation))]
    public void BindCorrelation() => RoiVM.BindCorrelation(this);

    public async override Task RemoveFromTree()
    {
        await DeselectAllVisibleImages();

        if (Descriptor.CorrelationInfo.IsReferenceImage)
        {
            _observableProject.UnsetReference();
            await RoiVM.Parent.ImagesStateManager.OnReferenceChanged();
        }

        // Remove descriptor from project
        _observableProject.RemoveDescriptor(Descriptor, save: true);
        RoiVM.Parent.ImagesStateManager.OnTilesetChanged();
        await base.RemoveFromTree();
    }

    public override void RemoveFiles()
    {
        var path = _observableProject.GetLayerDirectoryPath(Descriptor);
        try
        {
            // remove files structure
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove tileset images, {Path}", path);
        }
        base.RemoveFiles();
    }
}
