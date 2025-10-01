using System.IO;
using Betrian.App.Infrastructure;
using Betrian.Imaging.Algorithms.Utilities;
using Betrian.Imaging.Common;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Transactions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public sealed partial class ZStackVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors _nestedMipImageBehaviors = new()
    {
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
        CanExportToMaps = true,
    };

    private static readonly RoiChildBehaviors _nestedStackedImageBehaviors = new()
    {
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
        CanExportToMaps = true,
    };

    public ZStackDescriptor Descriptor { get; }

    public override CorrelationInfo CorrelationInfo
        => Descriptor.CorrelationInfo;

    public ZStackVirtualChildVM(
        RoiVM roiVM, VirtualChildVM? parentVM, ZStackDescriptor layerDescriptor,
        RoiChildBehaviors behaviors, string? displayName = null
    )
        : base(roiVM, parentVM, behaviors)
    {
        Descriptor = layerDescriptor;

        var mipImage = new SingleImageChildVM(
            roiVM, this, layerDescriptor, layerDescriptor.MipImage,
            _nestedMipImageBehaviors, enforcedAttributes: ImageAttributes.MipImage
        );
        var stackedImage = new StackedImageChildVM(
            roiVM, this, layerDescriptor,
            _nestedStackedImageBehaviors
        );
        _children.Add(mipImage);
        _children.Add(stackedImage);

        Attributes = stackedImage.Attributes & ~ImageAttributes.Reference;
        Name = displayName ?? stackedImage.Name;
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
