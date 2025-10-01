using System.Diagnostics;
using System.IO;
using Betrian.CflmNavi.App.Controls;
using Betrian.CflmNavi.App.Infrastructure;
using Betrian.CflmNavi.App.ViewModels.Milling;
using Betrian.CflmNavi.App.ViewModels.Modes.FIB;
using Betrian.CflmNavi.App.Views;
using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class SingleImageChildVM : ImageChildVM
{
    private readonly MillingViewModel _millingViewModel;

    public LayerDescriptor Descriptor { get; private set; }

    public LayerContentDescriptor Content { get; private set; }

    public SingleImageChildVM(
        RoiVM parentRoi, VirtualChildVM? parentVM,
        LayerDescriptor descriptor, LayerContentDescriptor content,
        RoiChildBehaviors behaviors, ImageAttributes enforcedAttributes = ImageAttributes.None
    )
        : base(parentRoi, parentVM, behaviors, enforcedAttributes)
    {
        Descriptor = descriptor;
        Content = content;
        _millingViewModel = new MillingViewModel(this);
        Update();
    }

    public override Guid SortId => ImageMetadata.ImageId;

    protected override ImageAttributes GetAttributes()
    {
        var attributes = base.GetAttributes();
        if (CorrelationInfo.IsReferenceImage)
        {
            attributes |= ImageAttributes.Reference;
        }
        return attributes;
    }

    public override void Update()
    {
        string imagePath = GetImageFilePath();
        ImageMetadata = TiffImage.LoadMetadata(imagePath);
        _millingViewModel.UpdateMillingAreasOverlay();
    }

    public override CorrelationInfo CorrelationInfo
        => (Descriptor, Content) switch
    {
        (LayeredImageDescriptor, LayerContentDescriptorWithCorrelationInfo layeredImageContent) => layeredImageContent.CorrelationInfo,
        (TileSetDescriptor tileSetDescriptor, _) => tileSetDescriptor.CorrelationInfo,
        (TileSet3DDescriptor tileSet3DDescriptor, _) => tileSet3DDescriptor.CorrelationInfo,
        (ZStackDescriptor zStackDescriptor, _) => zStackDescriptor.CorrelationInfo,
        _ => new CorrelationInfo()
    };


    protected override void OnSelectionTypeChangedImplementation(ImageSelection value)
    {
        if (value != ImageSelection.Unselected)
        {
            return;
        }

        if (_millingViewModel is not null)
        {
            if (RoiVM.Parent.ActiveDevice?.SecondaryPanelContent is MillingControl mc)
            {
                if (mc.DataContext.Equals(_millingViewModel))
                {
                    // hide milling control
                    RoiVM.Parent.ActiveDevice.SecondaryPanelContent = null;
                }
            }
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

    public override (LayerDescriptor, LayerContentDescriptor) GetActiveImageDescriptors() => (Descriptor, Content);

    private bool CanExecuteEditMilling()
    {
        return
            RoiVM.Parent.ActiveDevice is IonBeamModeViewModel &&
            SelectionType == ImageSelection.Primary &&
            ImageMetadata.GetSource().IsOneOf(StageCameraView.FIB_Milling, StageCameraView.FIB_RightAngle);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteEditMilling))]
    private void EditMilling()
    {
        if (RoiVM.Parent.ActiveDevice is not null)
        {
            var millingControl = new MillingControl
            {
                DataContext = _millingViewModel
            };

            RoiVM.Parent.ActiveDevice.SecondaryPanelContent = millingControl;
        }
    }

    internal int MillingAreasCount
        => Content is LayerContentDescriptorWithCorrelationInfo ciContent ? ciContent.MillingAreas.Count : 0;

    internal void AddMillingArea(MillingAreaInfo millingAreaInfo)
    {
        if (RoiVM.Parent.ImageWithMetadata is not null && Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            ciContent.MillingAreas.Add(millingAreaInfo);
            RoiVM.Parent.ActiveProject?.Save();
            _millingViewModel.UpdateMillingAreasOverlay();
        }
        TransferMillingAreasCommand.NotifyCanExecuteChanged();
    }

    internal void UpdateMillingArea(MillingAreaInfo rawMillingArea, MillingArea millingArea)
    {
        if (RoiVM.Parent.ImageWithMetadata is not null && Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            var left = new Ratio(millingArea.X / ImageMetadata.Coordinates.ImageSize.Width, RatioUnit.DecimalFraction);
            var top = new Ratio(millingArea.Y / ImageMetadata.Coordinates.ImageSize.Height, RatioUnit.DecimalFraction);

            var right = new Ratio((millingArea.X + millingArea.Width) / ImageMetadata.Coordinates.ImageSize.Width, RatioUnit.DecimalFraction);
            var bottom = new Ratio((millingArea.Y + millingArea.Height) / ImageMetadata.Coordinates.ImageSize.Height, RatioUnit.DecimalFraction);

            var newMillingAreaInfo = rawMillingArea with { Definition = new RatioRectangle() { Left = left, Right = right, Top = top, Bottom = bottom } };
            _ = ciContent.MillingAreas.Remove(rawMillingArea);
            ciContent.MillingAreas.Add(newMillingAreaInfo);
            _observableProject.AddOrUpdateDescriptor(Descriptor);
            _millingViewModel.UpdateMillingAreasOverlay();
            TransferMillingAreasCommand.NotifyCanExecuteChanged();
        }
    }

    internal void RemoveMillingArea(MillingAreaInfo millingAreaInfo)
    {
        if (RoiVM.Parent.ImageWithMetadata is not null && Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            _ = ciContent.MillingAreas.Remove(millingAreaInfo);
            _observableProject.AddOrUpdateDescriptor(Descriptor);
            _millingViewModel.UpdateMillingAreasOverlay();
            TransferMillingAreasCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExecuteTransferMillingAreas()
    {
        return
            RoiVM.Parent.ActiveDevice is IonBeamModeViewModel &&
            SelectionType == ImageSelection.Primary &&
            Content is LayerContentDescriptorWithCorrelationInfo ciContent &&
            ciContent.MillingAreas.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTransferMillingAreas))]
    private async Task TransferMillingAreas()
    {
        if (RoiVM.Parent.ActiveDevice is IonBeamModeViewModel ionBeamDevice && Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            using var busyGuard = RoiVM.Parent.WindowService.ShowBusyMessage(Messages.TransferringMillingAreasBusyMessage);
            await ionBeamDevice.TransferMillingAreas(ImageMetadata, ciContent.MillingAreas);
        }
    }

    protected override void RemoveChildSpecific()
    {
        var filename = _observableProject.GetContentFilePath(Descriptor, Content);
        switch (Descriptor)
        {
            case LayeredImageDescriptor layeredImageDescriptor:
                if (Content is LayerContentDescriptorWithCorrelationInfo ciContent)
                {
                    _ = layeredImageDescriptor.Images.Remove(ciContent);
                    _observableProject.AddOrUpdateDescriptor(layeredImageDescriptor, save: true, notify: false);
                }
                break;
            default:
                Debugger.Break();
                throw new NotSupportedException();
        }

        try
        {
            File.Delete(filename);
        }
        catch (Exception ex)
        {
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove image, {Path}", filename);
        }
    }
}
