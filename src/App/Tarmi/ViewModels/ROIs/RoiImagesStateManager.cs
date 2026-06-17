using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using Tarmi.Imaging.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Tarmi.App.Services.Application;
using Tarmi.App.Infrastructure;
using Tarmi.App.Controls;
using Tarmi.App.ViewModels.Modes;

namespace Tarmi.App.ViewModels.ROIs;

public partial class RoiImagesStateManager : ObservableObject
{
    private readonly IWindowService _windowService;
    private readonly RoiControlViewModel _roiControlViewModel;
    private VirtualDeviceViewModel? _virtualDeviceViewModel;
    private RoiVM? _selectedRoi;
    private ImageChildVM? _primarySource;
    private ImageChildVM? _secondarySource;
    private readonly List<ImageChildVM> _overlaySources = [];
    private readonly Subject<TileSetVirtualChildVM?> _primaryTileSet = new();

    public bool IsZStackVisible
        => ControllableZStack is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsZStackVisible))]
    public partial StackedImageChildVM? ControllableZStack { get; private set; }

    public bool IsImageShown
        => _primarySource is not null;

    public bool IsCorrelationActive
        => _overlaySources.Count > 0;

    public bool IsSecondaryImageShown
        => _secondarySource is not null;

    public IObservable<TileSetVirtualChildVM?> PrimaryTileSet
        => _primaryTileSet.AsObservable();

    public RoiImagesStateManager(RoiControlViewModel roiControlViewModel, IWindowService windowService)
        : base()
    {
        _roiControlViewModel = roiControlViewModel;
        _windowService = windowService;
    }

    private void ClearSecondaryImage()
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            device.SecondaryFiducialPointControl.SetFiducialsSource(null);
            device.ShowSecondaryImage = false;
            device.SecondaryImageWithMetadata?.Dispose();
            device.SecondaryImageWithMetadata = null;
            _secondarySource?.Unselect();
            _secondarySource = null;
            UpdateCanToggleVisibilities();
        }
    }

    private async Task AddPrimaryImage(ImageChildVM childVM)
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            _primarySource = childVM;
            device.PrimaryFiducialPointControl.SetFiducialsSource(childVM);
            var filePath = childVM.GetImageFilePath();
            if (filePath is not null)
            {
                using (_windowService.ShowBusyMessage(Messages.LoadingImageBusyMessage))
                {
                    await device.ImagingPipeline.SetPrimaryImageFile(filePath, childVM.CorrelationInfo);
                }
            }
        }
    }

    private async Task RemovePrimaryImage()
        => await ClearState();

    private async Task AddOverlayImage(ImageChildVM childVM)
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            _overlaySources.Add(childVM);
            var filePath = childVM.GetImageFilePath();
            using (_windowService.ShowBusyMessage(Messages.LoadingImageBusyMessage))
            {
                await device.ImagingPipeline.AddOverlayImage(GetPipelineImageId(childVM), filePath!, childVM.CorrelationInfo);
            }
        }
    }

    private void ChangeBoundImageLayers(StackedImageChildVM sourceChildVM)
    {
        var linkId = sourceChildVM.Descriptor.LinkId;
        if (
            _primarySource is StackedImageChildVM primaryStack &&
            primaryStack.Descriptor.LinkId == linkId &&
            primaryStack.Index != sourceChildVM.Index
        )
        {
            primaryStack.Index = sourceChildVM.Index;
        }

        foreach (var overlaySource in _overlaySources.OfType<StackedImageChildVM>())
        {
            if (
                overlaySource.Descriptor.LinkId == linkId &&
                overlaySource.Index != sourceChildVM.Index
            )
            {
                overlaySource.Index = sourceChildVM.Index;
            }
        }
    }

    public async Task OnImageLayerChanged(StackedImageChildVM childVM)
    {
        if (_primarySource is null)
        {
            return;
        }

        var (layer, _) = childVM.GetActiveImageDescriptors();
        var layerId = layer.Id;
        var filePath = childVM.GetImageFilePath();

        var (primaryLayer, _) = _primarySource!.GetActiveImageDescriptors();

        if (layerId == primaryLayer.Id)
        {
            if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
            {
                await device.ImagingPipeline.SetPrimaryImageFile(filePath, childVM.Descriptor.CorrelationInfo);
                ChangeBoundImageLayers(childVM);
            }
        }
        else
        {
            var overlayVm = _overlaySources.FirstOrDefault(source => GetPipelineImageId(source) == layerId);
            if (
                overlayVm is not null &&
                _virtualDeviceViewModel is VirtualDeviceViewModel device
            )
            {
                await device.ImagingPipeline.UpdateOverlayImage(GetPipelineImageId(childVM), filePath, childVM.Descriptor.CorrelationInfo);
                ChangeBoundImageLayers(childVM);
            }
        }
    }

    public void OnTilesetChanged()
    {
        _virtualDeviceViewModel?.OverviewImageVM.UpdateTilesets();
    }

    private static Guid GetPipelineImageId(ImageChildVM childVM)
    {
        return childVM switch
        {
            StackedImageChildVM stackImageVM => stackImageVM.Descriptor.Id,
            _ => childVM.ImageMetadata.ImageId
        };
    }

    private async Task RemoveOverlayImage(ImageChildVM childVM)
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            _ = _overlaySources.Remove(childVM);
            await device.ImagingPipeline.RemoveOverlayImage(GetPipelineImageId(childVM));
            await device.ImagingPipeline.Invalidate();
        }
    }

    private static IEnumerable<ImageChildVM> GetAllSelectedImageChildren(IEnumerable<RoiChildVM>? nodes)
    {
        if (nodes is null)
        {
            yield break;
        }

        foreach (var node in nodes)
        {
            switch (node)
            {
                case ImageChildVM imageChild:
                    yield return imageChild;
                    break;
                case VirtualChildVM virtualChild:
                    var children = GetAllSelectedImageChildren(virtualChild.Children);
                    foreach (var child in children)
                    {
                        yield return child;
                    }
                    break;
            }
        }
    }

    public void UpdateCanToggleVisibilities()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var imageChildren = GetAllSelectedImageChildren(_selectedRoi?.UngroupedRoiChildVMs).ToImmutableArray();

            if (_virtualDeviceViewModel?.IsGrabbingImage ?? false)
            {
                foreach (var childVM in imageChildren)
                {
                    childVM.UpdateCanToggleVisibility(false);
                }
            }
            else
            {
                foreach (var childVM in imageChildren)
                {
                    childVM.UpdateCanToggleVisibility(CanToggleImage(childVM));
                }
            }
        });
    }

    public void UpdateCanNavigateTo()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var imageChildren = GetAllSelectedImageChildren(_selectedRoi?.UngroupedRoiChildVMs).ToImmutableArray();
            foreach (var childVM in imageChildren)
            {
                childVM.NavigateToImageCommand.NotifyCanExecuteChanged();
            }
        });
    }

    private bool CanToggleImage(ImageChildVM childVM)
    {
        if (childVM.SelectionType != ImageSelection.Unselected)
        {
            // already shown, allow removing
            return true;
        }

        var correlationFiducials = _selectedRoi?.CorrelationByFiducials ?? false;
        if (correlationFiducials && childVM.ValidFiducialsCount < 3)
        {
            // fiducials correlation mode enabled, child does not have enough fiducials, do not allow adding
            return false;
        }

        if (_primarySource is null)
        {
            // no image shown yet, allow adding
            return true;
        }
        if (IsSecondaryImageShown)
        {
            // fiducials edition case
            return false;
        }

        if (childVM is StackedImageChildVM stackImageVM)
        {
            // allow only linked z-stack images to be correlated
            if (_primarySource is StackedImageChildVM primaryStackImageVM)
            {
                return primaryStackImageVM.Descriptor.LinkId == stackImageVM.Descriptor.LinkId;
            }

            var overlays = _overlaySources.OfType<StackedImageChildVM>();
            if (
                overlays.Any() &&
                !overlays.Any(overlaySource => overlaySource.Descriptor.LinkId == stackImageVM.Descriptor.LinkId)
            )
            {
                // z-stack is already visible and tested is not linked
                return false;
            }
        }

        // allow only images that intersect with primary image
        return _primarySource.Area.IntersectsWith(childVM.Area);
    }

    private void UpdateControllableZStack()
    {
        var secondaryStackVM = _overlaySources.OfType<StackedImageChildVM>().FirstOrDefault();
        if (_primarySource is StackedImageChildVM primaryStack)
        {
            ControllableZStack = primaryStack;
        }
        else
        {
            ControllableZStack = secondaryStackVM;
        }

        if (ControllableZStack is not null)
        {
            ChangeBoundImageLayers(ControllableZStack);
        }
    }

    public async Task<ImageSelection> ToggleImage(ImageChildVM childVM)
    {
        if (_virtualDeviceViewModel is null)
        {
            return ImageSelection.Unselected;
        }
        switch (childVM.SelectionType)
        {
            case ImageSelection.Primary:
                await RemovePrimaryImage();
                UpdateControllableZStack();
                UpdatePrimaryTileSet();
                break;
            case ImageSelection.Secondary:
                ClearSecondaryImage();
                UpdateControllableZStack();
                break;
            case ImageSelection.Overlay:
                await RemoveOverlayImage(childVM);
                UpdateControllableZStack();
                break;
            //case ImageSelection.Unselected:
            default:
                if (_primarySource is null)
                {
                    await AddPrimaryImage(childVM);
                    UpdateControllableZStack();
                    UpdatePrimaryTileSet();
                    return ImageSelection.Primary;
                }
                await AddOverlayImage(childVM);
                UpdateControllableZStack();
                return ImageSelection.Overlay;
        }
        return ImageSelection.Unselected;
    }

    private void UpdatePrimaryTileSet()
    {
        _primaryTileSet.OnNext(_primarySource?.ParentVM as TileSetVirtualChildVM);
    }

    public async Task<ImageSelection> ShowAsSecondaryImage(ImageChildVM childVM)
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            ClearSecondaryImage();
            var filePath = childVM.GetImageFilePath();
            if (filePath?.IsNotNullOrEmpty() ?? false)
            {
                using var messageGuard = _windowService.ShowBusyMessage(Messages.LoadingImageBusyMessage);
                using var image = await Task.Run(() => TiffImage.Load(filePath));
                if (image is not null)
                {
                    image.TransformToInplace(ImageTransformationType.View);
                    device.SecondaryImageWithMetadata = image;
                    _secondarySource = childVM;
                    device.ShowSecondaryImage = true;
                    device.SecondaryFiducialPointControl.SetFiducialsSource(childVM);
                    return ImageSelection.Secondary;
                }
            }
        }
        return ImageSelection.Unselected;
    }

    public async Task OnSelectedRoiChanged()
    {
        if (_selectedRoi?.Id != _roiControlViewModel.SelectedRoi?.Id)
        {
            await ClearState();
            _selectedRoi = _roiControlViewModel.SelectedRoi;
            if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
            {
                await device.ImagingPipeline.SetCorrelationMode(_selectedRoi?.CorrelationByFiducials ?? false);
            }
            UpdateCanToggleVisibilities();
        }
    }

    internal async Task OnRoiCorrelationModeChanged(RoiVM roiVM)
    {
        if (
            _virtualDeviceViewModel is VirtualDeviceViewModel device &&
            _selectedRoi?.Id == roiVM.Id
        )
        {
            var targetState = !roiVM.CorrelationByFiducials;
            await device.ImagingPipeline.SetCorrelationMode(targetState);
            roiVM.UpdateCorrelationMode(targetState);
            await ClearState();
        }
    }

    public async Task OnImageCorrelationSettingUpdated(ImageChildVM childVM)
    {
        if (
            _virtualDeviceViewModel is VirtualDeviceViewModel device &&
            _overlaySources.Any(source => GetPipelineImageId(source) == GetPipelineImageId(childVM))
        )
        {
            await device.ImagingPipeline.Invalidate();
        }
    }

    public async Task ClearState()
    {
        if (_virtualDeviceViewModel is VirtualDeviceViewModel device)
        {
            device.SecondaryFiducialPointControl.SetFiducialsSource(null);
            device.ShowSecondaryImage = false;
            device.SecondaryImageWithMetadata = null;
            if (!device.IsGrabbingImage)
            {
                await device.ImagingPipeline.Clear();
            }
            device.PrimaryFiducialPointControl.SetFiducialsSource(null);
        }
        _secondarySource?.Unselect();
        _secondarySource = null;

        foreach (var source in _overlaySources)
        {
            source.Unselect();
        }
        ControllableZStack = null;
        _overlaySources.Clear();
        _primarySource?.Unselect();
        _primarySource = null;
        UpdateCanToggleVisibilities();
        UpdateCanNavigateTo();
    }

    public async Task OnActiveDeviceChanged(VirtualDeviceViewModel? value)
    {
        await ClearState();
        _virtualDeviceViewModel = value;
    }

    public bool WouldSettingReferenceCauseChanges(RoiChildVM childVM)
    {
        // TODO: update logic
        var imageChildren = GetAllSelectedImageChildren(_selectedRoi?.UngroupedRoiChildVMs).ToImmutableArray();
        var activeReference = imageChildren.FirstOrDefault(vm => vm.IsReference);

        if (childVM is ImageChildVM childImageVM)
        {
            return
                activeReference is not null &&
                GetPipelineImageId(activeReference) != GetPipelineImageId(childImageVM) &&
                activeReference.CorrelationInfo.FiducialPoints.Count > 0;
        }
        else if (childVM is VirtualChildVM virtualChildVM)
        {
            return
                activeReference is not null &&
                activeReference.CorrelationInfo.FiducialPoints.Count > 0 &&
                virtualChildVM.GetImageChildrenVMs(_ => true).Any(childImage => GetPipelineImageId(activeReference) != GetPipelineImageId(childImage));
        }
        return false;
    }

    public async Task OnReferenceChanged()
    {
        await ClearState();
        var imageChildren = GetAllSelectedImageChildren(_selectedRoi?.UngroupedRoiChildVMs).ToImmutableArray();
        foreach (ImageChildVM source in imageChildren)
        {
            source.UpdateAttributes();
        }
    }
}
