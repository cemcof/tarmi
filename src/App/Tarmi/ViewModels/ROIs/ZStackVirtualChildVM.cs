using System.IO;
using Tarmi.App.Infrastructure;
using Tarmi.Imaging.Algorithms.Utilities;
using Tarmi.Imaging.Common;
using Tarmi.Projects;
using Tarmi.Projects.Transactions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Tarmi.App.ViewModels.ROIs;

public sealed partial class ZStackVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors MipImageBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = true,
        CanExportToMaps = true,
        CanEditFiducials = true,
        CanBindCorrelation = true,
        CanEditCorrelationOptions = true,
    };

    private static readonly RoiChildBehaviors StackedImageBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanExportToMaps = true,
    };

    public ZStackDescriptor Descriptor { get; }

    public override CorrelationInfo CorrelationInfo
        => Descriptor.CorrelationInfo;

    public ZStackVirtualChildVM(
        RoiVM roiVM, VirtualChildVM? parentVM,
        ZStackDescriptor layerDescriptor,
        RoiChildBehaviors behaviors,
        RoiChildBehaviors? mipImageBehaviors = null,
        string? displayName = null
    )
        : base(roiVM, parentVM, behaviors)
    {
        Descriptor = layerDescriptor;

        var mipImage = new SingleImageChildVM(
            roiVM, this, layerDescriptor, layerDescriptor.MipImage,
            mipImageBehaviors ?? MipImageBehaviors, enforcedAttributes: ImageAttributes.MipImage
        );

        var stackedImage = new StackedImageChildVM(
            roiVM, this, layerDescriptor,
            StackedImageBehaviors
        );

        _children.Add(mipImage);
        _children.Add(stackedImage);

        // TODO: allow reference flag after propagation?
        Attributes = stackedImage.Attributes & ~ImageAttributes.Reference;
        Name = displayName ?? stackedImage.Name;
    }

    public async override Task RemoveFromTree()
    {
        await DeselectAllVisibleImages();

        if (Descriptor.CorrelationInfo.IsReferenceImage)
        {
            _observableProject.UnsetReference();
            await RoiVM.Parent.ImagesStateManager.OnReferenceChanged();
        }

        await base.RemoveFromTree();
    }

    public bool CanBindCorrelation => RoiVM.IsBindable(this) && FiducialsGroupId.IsEmpty();

    public bool CanUnbindCorrelation => FiducialsGroupId.IsNotEmpty();

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanUnbindCorrelation))]
    private void UnbindCorrelation() => RoiVM.UnbindCorrelation(this);

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanBindCorrelation))]
    public void BindCorrelation() => RoiVM.BindCorrelation(this);

    public override void RemoveFiles()
    {
        var path = _observableProject.GetLayerDirectoryPath(Descriptor);
        try
        {
            // remove files structure
            Directory.Delete(path, recursive: true);

            // finally remove descriptor from project
            _observableProject.RemoveDescriptor(Descriptor, save: true);
        }
        catch (Exception ex)
        {
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove tile set images, {Path}", path);
        }
        base.RemoveFiles();
    }


    [RelayCommand]
    private async Task RegenerateMipImage()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity($"{nameof(ZStackVirtualChildVM)}::{nameof(RegenerateMipImage)}");
        using var transaction = new ZStackCreationTransaction(
            RoiVM.Parent.ActiveProject!,
            Children.OfType<SingleImageChildVM>().First().ImageMetadata.GetSource(),
            RoiVM.StageNavigation.GetPlanePosition,
            Descriptor
        );
        await RoiVM.WindowService.ShowDeterminateWaitingDialogAsync(
            "Re-generating MIP Image",
            async progress => await transaction.RegenerateMipImage(Overlays.GetMaxIntensityImage),
            null
        );
        // TODO: update child later when tree structure is implemented
    }
}
