using Betrian.CflmNavi.App.Controls;
using Betrian.CflmNavi.App.Infrastructure;
using Betrian.CflmNavi.App.ViewModels.Modes.FIB;
using Betrian.CflmNavi.App.ViewModels.Modes.LM;
using Betrian.CflmNavi.App.ViewModels.Modes.SEM;
using Betrian.CflmNavi.App.Views;
using Betrian.Devices.Arduino.FilterHandler;
using Betrian.Imaging.Common;
using Betrian.Imaging.Common.Metadata.Luminescence;
using Betrian.Maps.DataFormat;
using Betrian.Maps.DataFormat.TfsDataModel;
using Betrian.Models;
using CFLMnavi.Configuration.Application;
using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public abstract partial class ImageChildVM : RoiChildVM
{
    protected readonly ImageAttributes _enforcedAttributes;

    [ObservableProperty]
    public partial ImageSelection SelectionType { get; private set; }

    public bool IsReference => CorrelationInfo.IsReferenceImage;

    public int ValidFiducialsCount
    {
        get
        {
            var fiducials = CorrelationInfo.FiducialPoints;
            return fiducials.Count(fiducial => RoiVM.Parent._stageNavigation.IsPlanePositionInImage(fiducial.Position, ImageMetadata));
        }
    }

    [ObservableProperty]
    public partial ImageAttributes Attributes { get; protected set; }

    [ObservableProperty]
    public partial ImageMetadata ImageMetadata { get; protected set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleVisibilityCommand))]
    public partial bool CanToggle { get; private set; }

    // TODO: optimize
    public LengthRectangle Area
    {
        get
        {
            if (
                ImageMetadata.ImageId != Guid.Empty &&
                RoiVM.Parent.ActiveDevice is not null
            )
            {
                return ImageMetadata.GetImageArea(RoiVM.Parent.ActiveDevice.StageNavigation.GetPlanePosition);
            }
            return LengthRectangle.Zero;
        }
    }

    protected ImageChildVM(
        RoiVM parentRoi, VirtualChildVM? parentVM,
        RoiChildBehaviors behaviors, ImageAttributes enforcedAttributes = ImageAttributes.None

    )
        : base(parentRoi, parentVM, behaviors)
    {
        _enforcedAttributes = enforcedAttributes;
        ImageMetadata = GetMinimalImageMetadata();
    }

    protected static ImageMetadata GetMinimalImageMetadata()
    {
        return new ImageMetadata
        {
            TiffMetadata = new(),
            Coordinates = new()
            {
                CameraView = StageCameraView.Unknown,
                ImageSize = IntSize2d.Zero,
                ElectronBeamStagePosition = StagePosition.Zero,
                PixelSize = PixelSize.Zero
            }
        };
    }

    partial void OnImageMetadataChanged(ImageMetadata value)
    {
        UpdateAttributes();
        Name = GetNameFromImageMetadata();

        if (RoiVM.Parent.ActiveDevice is not null)
        {
            RoiVM.Parent.ImagesStateManager.UpdateCanToggleVisibilities();
        }
    }

    public abstract void Update();

    internal void UpdateAttributes()
        => Attributes = GetAttributes() | _enforcedAttributes;

    protected string GetNameFromImageMetadata()
        => ImageMetadata.TiffMetadata?.ImageDescription ?? "";

    public abstract (LayerDescriptor, LayerContentDescriptor) GetActiveImageDescriptors();

    public virtual string GetImageFilePath()
    {
        var (layer, content) = GetActiveImageDescriptors();
        if (
            ParentVM is ZStackVirtualChildVM zStackVirtual &&
            zStackVirtual.ParentVM is StackedTilesVirtualChildVM tileSetVirtual
        )
        {
            return _observableProject.GetContentFilePath(tileSetVirtual.Descriptor, layer, content);
        }
        else
        {
            return _observableProject.GetContentFilePath(layer, content);
        }
    }

    internal void Unselect()
        => SelectionType = ImageSelection.Unselected;

    private Channels CreateMapsChannels(ImageMetadata imageMetadata)
    {
        var result = new Channels();

        var lmMetadata = imageMetadata.LuminescenceMetadata;
        if (lmMetadata is not null)
        {
            var colorMaps =
                lmMetadata.Mode == Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence ?
                    _observableProject.Config.UserPreferences.ImageColoring.Fluorescence :
                    _observableProject.Config.UserPreferences.ImageColoring.Reflection;

            var mapping = colorMaps.LightMappings
                .FirstOrDefault(
                    m => Math.Equals(m.WaveLength.Nanometers, lmMetadata.LightWavelength.Nanometers),
                    new LightMapping { WaveLength = lmMetadata.LightWavelength, Color = new LightColor { Blue = 255, Green = 255, Red = 255 } }
                );
            if (mapping is not null)
            {
                result.Items.Add(new()
                {
                    Guid = Guid.NewGuid(),
                    Index = 0,
                    Name = $"Color_{mapping.WaveLength.Nanometers}",
                    CameraBits = 16,
                    Additive = true,
                    Color = new()
                    {
                        A = 255,
                        R = mapping.Color.Red,
                        G = mapping.Color.Green,
                        B = mapping.Color.Blue
                    }
                });
            }
            else
            {
                result.Items.Add(new()
                {
                    Guid = Guid.NewGuid(),
                    Index = 0,
                    Name = "White",
                    CameraBits = 16,
                    Additive = true,
                    Color = new()
                    {
                        A = 255,
                        R = 255,
                        G = 255,
                        B = 255
                    }
                });
            }
        }

        return result;
    }

    protected override void ExportToMapsImplementation(string baseExportDirectory)
    {
        List<(LayerDescriptor, LayerContentDescriptor)> contentList = [GetActiveImageDescriptors()];
        MapsExportService.CreateMapsExport(baseExportDirectory, contentList, _observableProject.GetContentFilePath, RoiVM.StageNavigation.TransformPosition, CreateMapsChannels);
    }

    protected override bool CanExecuteMakeReference()
    {
        return
            base.CanExecuteMakeReference() &&
            ImageMetadata.GetSource().IsOneOf(StageCameraView.Unknown, StageCameraView.FIB_Milling) == false &&
            IsReference == false;
    }

    [RelayCommand(CanExecute = nameof(CanToggle))]
    protected async Task ToggleVisibility()
    {
        using var winGuard = RoiVM.Parent.WindowService.ShowBusyMessage(Messages.CorrelationBusyMessage);
        SelectionType = await RoiVM.Parent.ImagesStateManager.ToggleImage(this);
        RoiVM.Parent.ImagesStateManager.UpdateCanToggleVisibilities();
    }

    internal void UpdateCanToggleVisibility(bool canToggle)
        => CanToggle = canToggle;

    public bool CanBindCorrelation
        => ImageMetadata.GetSource() == StageCameraView.LM && FiducialsGroupId == Guid.Empty;

    public bool CanUnbindCorrelation
        => FiducialsGroupId != Guid.Empty;

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanUnbindCorrelation))]
    private void UnbindCorrelation()
    {
        _observableProject.UnbindCorrelation(CorrelationInfo);
        RoiVM.Update();
    }

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanBindCorrelation))]
    public void BindCorrelation()
    {
        var luminescenceImages = RoiVM.RoiChildVMs
            .OfType<ImageChildVM>()
            .Where(child => child.ImageMetadata.GetSource() == StageCameraView.LM && child.SortId != SortId);

        var image = RoiVM.WindowService.ShowImageSelectionDialog(this, luminescenceImages);
        if (image is null)
        {
            return;
        }

        _observableProject.BindCorrelation(image.CorrelationInfo, CorrelationInfo);
        RoiVM.Update();
    }

    protected virtual void OnSelectionTypeChangedImplementation(ImageSelection value)
    {
    }

    partial void OnSelectionTypeChanged(ImageSelection value)
    {
        OnSelectionTypeChangedImplementation(value);
    }

    private bool CanEditFiducials()
    {
        return
            RoiVM.Parent.ImagesStateManager.IsImageShown &&
            !RoiVM.Parent.ImagesStateManager.IsCorrelationActive &&
            !RoiVM.Parent.ImagesStateManager.IsSecondaryImageShown;
    }

    [RelayCommand(CanExecute = nameof(CanEditFiducials))]
    private async Task EditFiducials()
    {
        _observableProject.PrepareFiducials(CorrelationInfo);
        var (descriptor, _) = GetActiveImageDescriptors();
        _observableProject.AddOrUpdateDescriptor(descriptor, save: true, notify: false);
        UpdateAttributes();
        SelectionType = await RoiVM.Parent.ImagesStateManager.ShowAsSecondaryImage(this);
        RoiVM.Parent.ImagesStateManager.UpdateCanToggleVisibilities();
    }

    private bool CanEditCorrelationsOptions()
        => SelectionType == ImageSelection.Overlay;


    [RelayCommand(CanExecute = nameof(CanEditCorrelationsOptions))]
    private void EditCorrelationsOptions() => EditCorrelationsOptionsImplementation();


    protected virtual void EditCorrelationsOptionsImplementation()
    {
        if (RoiVM.Parent.ActiveDevice is not null)
        {
            var correlationOptionsControl = new CorrelationOptionsControl();
            var correlationOptionsViewModel = new CorrelationOptionsViewModel(this);
            correlationOptionsControl.DataContext = correlationOptionsViewModel;
            RoiVM.Parent.ActiveDevice.SecondaryPanelContent = correlationOptionsControl;
        }
    }

    public virtual async Task UpdateOpacitySettings(Ratio opacity)
    {
        var (descriptor, _) = GetActiveImageDescriptors();
        CorrelationInfo.Opacity = opacity;
        _observableProject.AddOrUpdateDescriptor(descriptor, save: true, notify: false);
        using (RoiVM.WindowService.ShowBusyMessage(Messages.CorrelationBusyMessage))
        {
            await RoiVM.Parent.ImagesStateManager.OnImageCorrelationSettingUpdated(this);
        }
    }

    protected virtual void CloseCorrelationsOptionsImplementation()
    {
        RoiVM.Parent.ActiveDevice?.SecondaryPanelContent = null;
    }

    [RelayCommand]
    private void CloseCorrelationsOptions()
    {
        CloseCorrelationsOptionsImplementation();
    }

    protected virtual ImageAttributes GetAttributes()
    {
        var attributes = ImageAttributes.None;

        var cameraView = ImageMetadata.GetSource();
        if (cameraView == StageCameraView.SEM)
        {
            attributes |= ImageAttributes.SEM;
        }
        else if (cameraView.IsOneOf(StageCameraView.FIB_Milling, StageCameraView.FIB_RightAngle))
        {
            attributes |= ImageAttributes.FIB;
        }
        else if (cameraView != StageCameraView.Unknown)
        {
            var lmMetadata = ImageMetadata.LuminescenceMetadata;
            if (lmMetadata is not null)
            {
                if (lmMetadata!.Mode == Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence)
                {
                    attributes |= ImageAttributes.Luminescence;
                }
                else if (lmMetadata.Mode == Imaging.Common.Metadata.Luminescence.LuminescenceMode.Reflection)
                {
                    attributes |= ImageAttributes.Reflection;
                }

                if (Comparison.EqualsAbsolute(lmMetadata.LightWavelength.Nanometers, _observableProject.Config.Microscope.Thorlabs4100.Lights.GreenWavelength.Nanometers, 0))
                {
                    attributes |= ImageAttributes.Green;
                }
                else if (Comparison.EqualsAbsolute(lmMetadata.LightWavelength.Nanometers, _observableProject.Config.Microscope.Thorlabs4100.Lights.RedWavelength.Nanometers, 0))
                {
                    attributes |= ImageAttributes.Red;
                }
                else if (Comparison.EqualsAbsolute(lmMetadata.LightWavelength.Nanometers, _observableProject.Config.Microscope.Thorlabs4100.Lights.BlueWavelength.Nanometers, 0))
                {
                    attributes |= ImageAttributes.Blue;
                }
                else if (Comparison.EqualsAbsolute(lmMetadata.LightWavelength.Nanometers, _observableProject.Config.Microscope.Thorlabs4100.Lights.UltraVioletWavelength.Nanometers, 0))
                {
                    attributes |= ImageAttributes.UltraViolet;
                }
            }
        }

        if (IsReference)
        {
            attributes |= ImageAttributes.Reference;
        }

        return attributes;
    }

    public override void OnModeDeInitialized()
    {
        SelectionType = ImageSelection.Unselected;
        base.OnModeDeInitialized();
    }

    protected virtual void RemoveChildSpecific()
    {
    }

    public override async Task RemoveImplementation()
    {
        await Task.Run(async () =>
        {
            if (SelectionType != ImageSelection.Unselected)
            {
                await ToggleVisibility();
            }

            if (IsReference)
            {
                _observableProject.UnsetReference();
                await RoiVM.Parent.ImagesStateManager.OnReferenceChanged();
            }

            RoiVM.RemoveChild(this);
            RemoveChildSpecific();
        });
    }

    public bool CanNavigateToImage()
    {
        StageCameraView activeMode = RoiVM.Parent.ActiveDevice switch
        {
            IonBeamModeViewModel iBeamVm
                => iBeamVm.SelectedViewMode == IonBeamViewMode.RightAngle ? StageCameraView.FIB_RightAngle : StageCameraView.FIB_Milling,
            ElectronBeamModeViewModel
                => StageCameraView.SEM,
            LuminescenceModeViewModel lmVm
                when lmVm.IsProtracted &&
                lmVm.FilterType == FilterType.Reflection &&
                ImageMetadata.LuminescenceMetadata is { Mode: LuminescenceMode.Reflection }
                => StageCameraView.LM,
            LuminescenceModeViewModel lmVm
                when lmVm.IsProtracted &&
                lmVm.FilterType == FilterType.Fluorescence &&
                ImageMetadata.LuminescenceMetadata is { Mode: LuminescenceMode.Fluorescence }
                => StageCameraView.LM,
            _ => StageCameraView.Unknown
        };

        return activeMode == ImageMetadata.GetSource();
    }

    [RelayCommand(CanExecute = nameof(CanNavigateToImage))]
    private async Task NavigateToImage()
    {
        using (RoiVM.WindowService.ShowBusyMessage(Messages.RestoringImageStateBusyMessage))
        {
            switch (RoiVM.Parent.ActiveDevice)
            {
                case IonBeamModeViewModel iBeamVm:
                    await iBeamVm.RestoreImageState(ImageMetadata);
                    break;
                case ElectronBeamModeViewModel eBeamVm:
                    await eBeamVm.RestoreImageState(ImageMetadata);
                    break;
                case LuminescenceModeViewModel lmVm:
                    await lmVm.RestoreImageState(ImageMetadata);
                    break;
                default:
                    break;
            }
        }
    }
}
