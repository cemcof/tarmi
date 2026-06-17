using System.Collections.ObjectModel;
using System.Windows;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.WPF;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnitsNet;
using UnitsNet.Units;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.ViewModels.FocusPoints;

public partial class FocusPointControlViewModel : ViewModelBase
{
    private readonly IStageNavigation _stageNavigation;
    private IProjectManager _projectManager;
    private readonly RoiControlViewModel _roiControlViewModel;
    private readonly List<IDisposable> _subscriptions = [];

    [ObservableProperty]
    private ImageWithMetadata? _imageWithMetadata;

    [ObservableProperty]
    private TileSetVirtualChildVM? _activeTileSet;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddFocusPointCommand))]
    public partial bool CanAddFocusPoint { get; private set; }

    [ObservableProperty]
    private ObservableProject? _activeProject;

    public ObservableCollection<FocusPoint> AvailableFocusPoints { get; } = [];

    public FocusPointControlViewModel(IStageNavigation stageNavigation, IProjectManager projectManager, RoiControlViewModel roiControlViewModel)
    {
        _stageNavigation = stageNavigation;
        _projectManager = projectManager;
        _roiControlViewModel = roiControlViewModel;
        _subscriptions.Add(_projectManager.ActiveProject.Subscribe(project => ActiveProject = project));
        _subscriptions.Add(_roiControlViewModel.ImagesStateManager.PrimaryTileSet.Subscribe(tileset => ActiveTileSet = tileset));
    }

    protected override Task DeInitializeCoreAsync()
    {
        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        return base.DeInitializeCoreAsync();
    }

    [RelayCommand(CanExecute = nameof(CanAddFocusPoint))]
    private void AddFocusPoint()
    {
        if (ActiveTileSet is not null &&
            ImageWithMetadata is not null &&
            ActiveProject is not null
        ) 
        {
            ActiveProject.AddFocusPoint(ActiveTileSet.Descriptor, new() { PlaneLocation = _stageNavigation.GetPlanePositionFromImageLocation(RatioPoint.Center, ImageWithMetadata) });
            UpdateFocusPoints();
        }
    }

    partial void OnImageWithMetadataChanged(ImageWithMetadata? value)
    {
        _ = Application.Current?.Dispatcher.InvokeAsync(UpdateFocusPoints);
    }

    partial void OnActiveTileSetChanged(TileSetVirtualChildVM? value)
    {
        CanAddFocusPoint = value is not null;
        _ = Application.Current?.Dispatcher.InvokeAsync(UpdateFocusPoints);
    }

    private void UpdateFocusPoints()
    {
        AvailableFocusPoints.Clear();
        if (
            ActiveTileSet is not null &&
            ImageWithMetadata is not null
        )
        {
            foreach (var focusPoint in ActiveTileSet.Descriptor.GrabbingOptions.FocusPoints)
            {
                //if (_stageNavigation.IsPlanePositionInImage(focusPoint.PlaneLocation, ImageWithMetadata)) // TODO
                //{
                    var point = _stageNavigation.GetImageLocationFromPlanePosition(focusPoint.PlaneLocation, ImageWithMetadata);
                    AvailableFocusPoints.Add(new FocusPoint() { X = point.X, Y = point.Y, IsAutoFocused = focusPoint.IsAutofocused, ParentFocusPoint = focusPoint, OnMoveFinished = UpdateFocusPointPosition });
                //}
            }
        }
    }

    private void UpdateFocusPointPosition(XYPoint point)
    {
        if (
            ActiveTileSet is not null &&
            ActiveTileSet.Descriptor is TileSetDescriptor tileSetDescriptor &&
            point is FocusPoint focusPoint &&
            focusPoint.ParentFocusPoint is not null &&
            ImageWithMetadata is not null
        )
        {
            StagePosition position = _stageNavigation.GetStagePositionFromImageLocation(new RatioPoint()
            {
                X = new Ratio(point.X / ImageWithMetadata.Coordinates.ImageSize.Width, RatioUnit.DecimalFraction),
                Y = new Ratio(point.Y / ImageWithMetadata.Coordinates.ImageSize.Height, RatioUnit.DecimalFraction)
            }, ImageWithMetadata, ImageWithMetadata.GetSource());
            LengthPoint planePosition = _stageNavigation.GetPlanePosition(position, ImageWithMetadata.GetSource());
            Tarmi.Projects.FocusPoint updatedFocusPoint = new() { PlaneLocation = planePosition, WorkingDistance = focusPoint.ParentFocusPoint.WorkingDistance };
            ActiveProject?.UpdateFocusPoint(tileSetDescriptor, focusPoint.ParentFocusPoint, updatedFocusPoint);
            UpdateFocusPoints();
        }
    }
}
