using System.IO;
using Tarmi.App.ViewModels.FocusPoints;
using Tarmi.App.Views.FocusPoints;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Tarmi.App.ViewModels.ROIs;

public sealed partial class TileSetVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors MipImageBehaviors = new()
    {
        HasContextMenu = true,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = true,
        CanExportToMaps = true,
        CanBindCorrelation = true,
        CanEditCorrelationOptions = true,
        CanEditFiducials = true,
    };

    private static readonly RoiChildBehaviors GroupedImagesBehaviors = new()
    {
        HasContextMenu = false,
        SupportsRemoveCommand = false,
        HasMarkAsReferenceMenu = false,
        CanHaveReferenceAttribute = false,
        CanExportToMaps = true,
    };

    public TileSetDescriptor Descriptor { get; }

    public override CorrelationInfo CorrelationInfo
        => Descriptor.CorrelationInfo;

    public TileSetVirtualChildVM(
        RoiVM roiVM, VirtualChildVM? parentVM, TileSetDescriptor layerDescriptor, RoiChildBehaviors behaviors
    )
        : base(roiVM, parentVM, behaviors)
    {
        Descriptor = layerDescriptor;

        var stitchedImage = new SingleImageChildVM(
            roiVM, this, layerDescriptor, layerDescriptor.StitchedImage,
            MipImageBehaviors, enforcedAttributes: ImageAttributes.TileSet
        );
        var gridImages = new GroupedImagesChildVM(
            roiVM, this, "Tiles", layerDescriptor, layerDescriptor.Images,
            GroupedImagesBehaviors
         );
        _children.Add(stitchedImage);
        _children.Add(gridImages);

        // TODO: allow reference flag after propagation?
        Attributes = stitchedImage.Attributes & ~ImageAttributes.Reference;
        Name = stitchedImage.Name;
    }

    [RelayCommand]
    private void ShowFocusMap()
    {
        var viewModel = new FocusPointsWindowViewModel(_observableProject, this);
        // TODO: move somewhere else
        var mainWindow = Application.Current.MainWindow;
        var window = new FocusPointsWindow(viewModel)
        {
            Owner = mainWindow
        };
        window.Show();
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
            RoiVM.Parent.Logger.LogError(ex, "Failed to remove tile set images, {Path}", path);
        }
        base.RemoveFiles();
    }
}
