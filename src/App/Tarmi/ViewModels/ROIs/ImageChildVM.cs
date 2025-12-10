using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tarmi.App.Controls;
using Tarmi.App.Infrastructure;
using Tarmi.App.ViewModels.Modes.Confocal;
using Tarmi.App.ViewModels.Modes.FIB;
using Tarmi.App.ViewModels.Modes.LM;
using Tarmi.App.ViewModels.Modes.SEM;
using Tarmi.App.Views;
using Tarmi.Configuration.Application;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Imaging.Common;
using Tarmi.Maps.DataFormat;
using Tarmi.Maps.DataFormat.TfsDataModel;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;

namespace Tarmi.App.ViewModels.ROIs;

public abstract partial class ImageChildVM : RoiChildVM
{
    protected readonly ImageAttributes _enforcedAttributes;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditFiducialsCommand))]
    public partial ImageSelection SelectionType { get; private set; }

    public bool IsReference => Behaviors.CanHaveReferenceAttribute && CorrelationInfo.IsReferenceImage;

    public int ValidFiducialsCount
        => CorrelationInfo.FiducialPoints.Count(fiducial => RoiVM.Parent._stageNavigation.IsPlanePositionInImage(fiducial.Position, ImageMetadata));

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
            if (ImageMetadata.ImageId.IsEmpty() || RoiVM.Parent.ActiveDevice is null)
            {
                return LengthRectangle.Zero;
            }
            return ImageMetadata.GetImageArea(RoiVM.Parent.ActiveDevice.StageNavigation.GetPlanePosition);
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
        => ImageMetadata.TiffMetadata?.ImageDescription ?? string.Empty;

    public abstract (LayerDescriptor, LayerContentDescriptor) GetActiveImageDescriptors();

    public virtual string GetImageFilePath()
    {
        var (layer, content) = GetActiveImageDescriptors();
        if (ParentVM is ZStackVirtualChildVM { ParentVM: StackedTilesVirtualChildVM tileSetVirtual })
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
        if (lmMetadata is null)
        {
            return result;
        }
        var colorMaps =
            lmMetadata.Mode == Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence ?
                _observableProject.Config.UserPreferences.ImageColoring.Fluorescence :
                _observableProject.Config.UserPreferences.ImageColoring.Reflection;

        var mapping = colorMaps.LightMappings
            .FirstOrDefault(
                mapping => mapping.WaveLength.Equals(lmMetadata.LightWavelength, Length.Zero),
                new LightMapping
                {
                    WaveLength = lmMetadata.LightWavelength,
                    Color = new LightColor
                    {
                        Blue = 255,
                        Green = 255,
                        Red = 255
                    }
                }
            );
        if (mapping is null)
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
        else
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

    public bool CanBindCorrelation => RoiVM.IsBindable(this) && FiducialsGroupId.IsEmpty();

    public bool CanUnbindCorrelation => FiducialsGroupId.IsNotEmpty();

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanUnbindCorrelation))]
    private void UnbindCorrelation() => RoiVM.UnbindCorrelation(this);

    // TODO: Updates?
    [RelayCommand(CanExecute = nameof(CanBindCorrelation))]
    public void BindCorrelation() => RoiVM.BindCorrelation(this);

    protected virtual void OnSelectionTypeChangedImplementation(ImageSelection value) { }

    partial void OnSelectionTypeChanged(ImageSelection value)
    {
        OnSelectionTypeChangedImplementation(value);
    }

    private bool CanEditFiducials()
    {
        return
            SelectionType != ImageSelection.Primary &&
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
        //var (descriptor, _) = GetActiveImageDescriptors();
        CorrelationInfo.Opacity = opacity;
        _observableProject.Save(); // .AddOrUpdateDescriptor(descriptor, save: true, notify: false);
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

        switch (cameraView)
        {
            case StageCameraView.SEM:
                attributes |= ImageAttributes.SEM;
                break;
            case StageCameraView.FIB_Milling or StageCameraView.FIB_RightAngle:
                attributes |= ImageAttributes.FIB;
                break;
            case StageCameraView.LM:
                var lmMetadata = ImageMetadata.LuminescenceMetadata;
                if (lmMetadata is null)
                {
                    break;
                }

                attributes |= lmMetadata.Mode switch
                {
                    Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence => ImageAttributes.Luminescence,
                    Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Reflection => ImageAttributes.Reflection,
                    _ => ImageAttributes.None,
                };

                var wavelength = lmMetadata.LightWavelength;
                var luminescenceLights = _observableProject.Config.Microscope.Thorlabs4100.Lights;
                if (wavelength.Equals(luminescenceLights.GreenWavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Green;
                }
                else if (wavelength.Equals(luminescenceLights.RedWavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Red;
                }
                else if (wavelength.Equals(luminescenceLights.BlueWavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Blue;
                }
                else if (wavelength.Equals(luminescenceLights.UltraVioletWavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.UltraViolet;
                }
                break;
            case StageCameraView.Confocal:
                var confocalMetadata = ImageMetadata.ConfocalMetadata;
                if (confocalMetadata is null)
                {
                    break;
                }
                attributes |= confocalMetadata.Mode switch
                {
                    Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Fluorescence => ImageAttributes.Luminescence,
                    Tarmi.Imaging.Common.Metadata.Confocal.LuminescenceMode.Reflection => ImageAttributes.Reflection,
                    _ => ImageAttributes.None,
                };

                var confocalWavelength = confocalMetadata.LightWavelength;
                var confocalLights = _observableProject.Config.Microscope.ConfocalConfig.ConfocalLights;
                if (confocalWavelength.Equals(confocalLights.ConfocalLight1.Wavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.UltraViolet;
                }
                else if (confocalWavelength.Equals(confocalLights.ConfocalLight2.Wavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Blue;
                }
                else if (confocalWavelength.Equals(confocalLights.ConfocalLight3.Wavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Green;
                }
                else if (confocalWavelength.Equals(confocalLights.ConfocalLight4.Wavelength, Length.Zero))
                {
                    attributes |= ImageAttributes.Red;
                }
                break;
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

    public override async Task RemoveFromTree()
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
        await base.RemoveFromTree();
    }

    public bool CanNavigateToImage()
    {
        StageCameraView activeMode = RoiVM.Parent.ActiveDevice switch
        {
            IonBeamModeViewModel { SelectedViewMode: IonBeamViewMode.RightAngle } => StageCameraView.FIB_RightAngle,
            IonBeamModeViewModel => StageCameraView.FIB_Milling,
            ElectronBeamModeViewModel => StageCameraView.SEM,
            LuminescenceModeViewModel
            {
                IsProtracted: true,
                FilterType: FilterType.Reflection
            } when ImageMetadata.LuminescenceMetadata is { Mode: Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Reflection } => StageCameraView.LM,
            LuminescenceModeViewModel
            {
                IsProtracted: true,
                FilterType: FilterType.Fluorescence
            } when ImageMetadata.LuminescenceMetadata is { Mode: Tarmi.Imaging.Common.Metadata.Luminescence.LuminescenceMode.Fluorescence } => StageCameraView.LM,
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
                case ConfocalModeViewModel cfVm:
                    await cfVm.RestoreImageState(ImageMetadata);
                    break;
                default:
                    break;
            }
        }
    }
}
