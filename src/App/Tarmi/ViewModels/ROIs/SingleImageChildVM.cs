using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tarmi.App.Controls;
using Tarmi.App.Infrastructure;
using Tarmi.App.ViewModels.Milling;
using Tarmi.App.ViewModels.Modes.FIB;
using Tarmi.App.Views;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using UnitsNet;
using UnitsNet.Units;

namespace Tarmi.App.ViewModels.ROIs;

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
        UpdateInternal();
    }

    public override Guid SortId => ImageMetadata.ImageId;

    protected override ImageAttributes GetAttributes()
    {
        var attributes = base.GetAttributes();
        if (Behaviors.CanHaveReferenceAttribute && CorrelationInfo.IsReferenceImage)
        {
            attributes |= ImageAttributes.Reference;
        }
        return attributes;
    }

    private void UpdateInternal()
    {
        string imagePath = GetImageFilePath();
        ImageMetadata = TiffImage.LoadMetadata(imagePath);
        _millingViewModel.UpdateMillingAreasOverlay();
    }

    public override void Update()
    {
        UpdateInternal();
    }

    public override CorrelationInfo CorrelationInfo => (Descriptor, Content) switch
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

        if (
            _millingViewModel is not null &&
            RoiVM.Parent.ActiveDevice?.SecondaryPanelContent is MillingControl mc &&
            mc.DataContext.Equals(_millingViewModel)
        )
        {
            // hide milling control
            RoiVM.Parent.ActiveDevice.SecondaryPanelContent = null;
        }

        if (
            RoiVM.Parent.ActiveDevice?.SecondaryPanelContent is CorrelationOptionsControl coc &&
            coc.DataContext is CorrelationOptionsViewModel covm &&
            Equals(covm.ImageChild)
        )
        {
            // hide correlation options control
            RoiVM.Parent.ActiveDevice.SecondaryPanelContent = null;
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

            var right = new Ratio((millingArea.X + millingArea.RealWidth) / ImageMetadata.Coordinates.ImageSize.Width, RatioUnit.DecimalFraction);
            var bottom = new Ratio((millingArea.Y + millingArea.RealHeight) / ImageMetadata.Coordinates.ImageSize.Height, RatioUnit.DecimalFraction);

            var newMillingAreaInfo = rawMillingArea with
            {
                Definition = new RatioRectangle()
                {
                    Left = left,
                    Right = right,
                    Top = top,
                    Bottom = bottom
                }
            };
            _ = ciContent.MillingAreas.Remove(rawMillingArea);
            ciContent.MillingAreas.Add(newMillingAreaInfo);
            _observableProject.AddOrUpdateDescriptor(Descriptor, save: true, notify: false);
            _millingViewModel.UpdateMillingAreasOverlay();
            TransferMillingAreasCommand.NotifyCanExecuteChanged();
        }
    }

    internal void RemoveMillingArea(MillingAreaInfo millingAreaInfo)
    {
        if (RoiVM.Parent.ImageWithMetadata is not null && Content is LayerContentDescriptorWithCorrelationInfo ciContent)
        {
            _ = ciContent.MillingAreas.Remove(millingAreaInfo);
            _observableProject.AddOrUpdateDescriptor(Descriptor, save: true, notify: false);
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

    public override Task RemoveFromTree()
    {
        switch (Descriptor)
        {
            case LayeredImageDescriptor layeredImageDescriptor:
                if (Content is LayerContentDescriptorWithCorrelationInfo ciContent)
                {
                    _ = layeredImageDescriptor.Images.Remove(ciContent);
                    _observableProject.RemoveDescriptor(layeredImageDescriptor, save: true);
                }
                break;
            default:
                Debugger.Break();
                throw new NotSupportedException();
        }
        return base.RemoveFromTree();
    }

    public override void RemoveFiles()
    {
        var filename = _observableProject.GetContentFilePath(Descriptor, Content);
        try
        {
            File.Delete(filename);
        }
        catch (Exception ex)
        {
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove image, {Path}", filename);
        }
        base.RemoveFiles();
    }
}
