using System.IO;
using Betrian.CflmNavi.App.ViewModels.FocusPoints;
using Betrian.CflmNavi.App.Views.FocusPoints;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public sealed partial class TileSetVirtualChildVM : VirtualChildVM
{
    private static readonly RoiChildBehaviors _mipImageBehaviors = new()
    {
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
        CanExportToMaps = true,
    };

    private static readonly RoiChildBehaviors _nestedTilesBehaviors = new()
    {
        SupportsRemoveCommand = false,
        CanBeMarkedAsReference = false,
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
            _mipImageBehaviors, enforcedAttributes: ImageAttributes.TileSet
        );
        var gridImages = new GroupedImagesChildVM(
            roiVM, this, "Tiles", layerDescriptor, layerDescriptor.Images,
            _nestedTilesBehaviors
         );
        _children.Add(stitchedImage);
        _children.Add(gridImages);

        Attributes = stitchedImage.Attributes;// & ~ImageAttributes.Reference;
        Name = stitchedImage.Name;
    }

    [RelayCommand]
    private void ShowFocusMap()
    {
        var viewModel = new FocusPointsWindowViewModel(_observableProject, this);
        // TODO: move somewhere else
        var mainWindow = App.Current.MainWindow;
        var window = new FocusPointsWindow(viewModel)
        {
            Owner = mainWindow
        };
        window.Show();
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
}
