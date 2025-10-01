using System.Collections.ObjectModel;
using System.Windows;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.CflmNavi.App.ViewModels.Modes;
using Betrian.CflmNavi.App.ViewModels.ROIs;
using Betrian.Imaging.Common;
using Betrian.Models;
using Betrian.WPF;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using DynamicData;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;
using System.Reactive.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Betrian.CflmNavi.App.ViewModels;

public partial class RoiControlViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ImageWithMetadata? ImageWithMetadata { get; set; }

    [ObservableProperty]
    public partial ImageWithMetadata? SecondaryImageWithMetadata { get; set; }

    internal readonly IStageNavigation _stageNavigation;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly IWindowService _windowService;
    private readonly ReadOnlyObservableCollection<RoiVM> _regionsOfInterest;
    private readonly ObservableCollectionExtended<RoiVM> _regionsOfInterestSource = [];
    private readonly SourceList<RoiVM> _regionsOfInterestSourceList;

    private readonly ReadOnlyObservableCollection<ROIPoint> _roiPoints;
    private readonly ObservableCollectionExtended<ROIPoint> _roiPointsSource = [];
    private readonly SourceList<ROIPoint> _roiPointsSourceList;

    private readonly ReadOnlyObservableCollection<ROIPoint> _secondaryRoiPoints;
    private readonly ObservableCollectionExtended<ROIPoint> _secondaryRoiPointsSource = [];
    private readonly SourceList<ROIPoint> _secondaryRoiPointsSourceList;

    [ObservableProperty]
    public partial ObservableProject? ActiveProject { get; set; }

    [ObservableProperty]
    public partial RoiVM? SelectedRoi { get; set; }

    private IDisposable? _regionsOfInterestSubscription;

    [ObservableProperty]
    public partial VirtualDeviceViewModel? ActiveDevice { get; internal set; }

    public ReadOnlyObservableCollection<RoiVM> RegionsOfInterest => _regionsOfInterest;

    public ReadOnlyObservableCollection<ROIPoint> ROIPoints => _roiPoints;

    public ReadOnlyObservableCollection<ROIPoint> SecondaryROIPoints => _secondaryRoiPoints;

    [ObservableProperty]
    public partial RoiImagesStateManager ImagesStateManager { get; private set; }

    internal IWindowService WindowService => _windowService;
    internal ILogger Logger { get; }

    public RoiControlViewModel(IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IWindowService windowService, ILogger<RoiControlViewModel> logger)
    {
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _windowService = windowService;
        Logger = logger;

        _regionsOfInterestSourceList = new(_regionsOfInterestSource.ToObservableChangeSet());
        _ = _regionsOfInterestSourceList
            .Connect()
            .Sort(SortExpressionComparer<RoiVM>.Ascending(vm => vm.Name))
            .ObserveOnDispatcher()
            .Bind(out _regionsOfInterest)
            .Subscribe();

        _roiPointsSourceList = new(_roiPointsSource.ToObservableChangeSet());
        _ = _roiPointsSourceList
            .Connect()
            .ObserveOnDispatcher()
            .Bind(out _roiPoints)
            .Subscribe();

        _secondaryRoiPointsSourceList = new(_secondaryRoiPointsSource.ToObservableChangeSet());
        _ = _secondaryRoiPointsSourceList
            .Connect()
            .ObserveOnDispatcher()
            .Bind(out _secondaryRoiPoints)
            .Subscribe();

        ImagesStateManager = new RoiImagesStateManager(this, windowService);
    }

    [SuppressMessage("Usage", "VSTHRD100: Avoid async void methods", Justification = "<Pending>")]
    async partial void OnActiveDeviceChanged(VirtualDeviceViewModel? value)
    {
        await ImagesStateManager.OnActiveDeviceChanged(value);

        if (value is null)
        {
            RegionsOfInterest.ForEach(vm => vm.ModeDeInitialized());
        }
    }

    [RelayCommand]
    private void AddRoi()
    {
        if (ImageWithMetadata != null)
        {
            var planePosition = _stageNavigation.GetPlanePositionFromImageLocation(RatioPoint.Center, ImageWithMetadata);

            ActiveProject?.AddROI(new()
            {
                Name = "New Roi",
                Id = UUIDNext.Uuid.NewSequential(),
                Position = planePosition,
            });

            Application.Current?.Dispatcher.Invoke(() => UpdateRoiPoints(ImageWithMetadata, _roiPointsSource));
        }
    }

    partial void OnActiveProjectChanged(ObservableProject? value)
    {
        _regionsOfInterestSubscription?.Dispose();
        BuildProjectRegionsOfInterest(value);
        _regionsOfInterestSubscription = value?.RegionsOfInterestObservable
            .ObserveOnDispatcher()
            .Subscribe(HandleRegionsOfInterestChange);
    }

    private void BuildProjectRegionsOfInterest(ObservableProject? project)
    {
        if (project is null)
        {
            _regionsOfInterestSource.Clear();
        }
        else
        {
            foreach (var roi in project.RegionsOfInterest)
            {
                Application.Current?.Dispatcher.Invoke(() => UpdateRegionsOfInterest(roi.Id));
            }
            Application.Current?.Dispatcher.Invoke(() => UpdateRoiPoints(ImageWithMetadata, _roiPointsSource));
            Application.Current?.Dispatcher.Invoke(() => UpdateRoiPoints(SecondaryImageWithMetadata, _secondaryRoiPointsSource, false));
        }
    }

    private void HandleRegionsOfInterestChange(Guid id)
    {
        UpdateRegionsOfInterest(id);
        UpdateRoiPoints(ImageWithMetadata, _roiPointsSource);
        UpdateRoiPoints(SecondaryImageWithMetadata, _secondaryRoiPointsSource, false);
    }

    private void UpdateRegionsOfInterest(Guid id)
    {
        var roi = ActiveProject?.RegionsOfInterest.FirstOrDefault(roi => roi.Id == id);
        if (roi is not null)
        {
            var roiVM = RegionsOfInterest.FirstOrDefault(roiVM => roiVM.Id == id);
            if (roiVM is null)
            {
                _regionsOfInterestSource.Add(new RoiVM(ActiveProject!, this, roi, _stageNavigation, _safeStageControlling, _windowService));
            }
            else
            {
                roiVM.Update();
            }
        }
        else
        {
            // roi removed
            var roiVM = RegionsOfInterest.FirstOrDefault(roiVM => roiVM.Id == id);
            if (roiVM is not null)
            {
                if (SelectedRoi is not null && SelectedRoi.Id.Equals(id))
                {
                    SelectedRoi = null;
                    ImagesStateManager.ClearState().SyncResult();
                }
                _ = _regionsOfInterestSource.Remove(roiVM);
            }
        }
    }

    partial void OnImageWithMetadataChanged(ImageWithMetadata? value)
    {
        _ = Application.Current?.Dispatcher.InvokeAsync(() => UpdateRoiPoints(value, _roiPointsSource));
    }

    partial void OnSecondaryImageWithMetadataChanged(ImageWithMetadata? value)
    {
        _ = Application.Current?.Dispatcher.InvokeAsync(() => UpdateRoiPoints(value, _secondaryRoiPointsSource, false));
    }

    private void UpdateRoiPoints(ImageWithMetadata? image, ObservableCollectionExtended<ROIPoint> roiSource, bool interactive = true)
    {
        roiSource.Clear();
        if (ActiveProject != null && image != null)
        {
            foreach (var item in ActiveProject.RegionsOfInterest)
            {
                if (_stageNavigation.IsPlanePositionInImage(item.Position, image))
                {
                    var point = _stageNavigation.GetImageLocationFromPlanePosition(item.Position, image);
                    var roi = item switch
                    {
                        GridCenterRegionOfInterest => new GridCenterROIPoint { X = point.X, Y = point.Y, Label = item.Name, RegionOfInterest = item, OnMoveFinished = UpdateROIPosition },
                        _ => new ROIPoint() { X = point.X, Y = point.Y, Label = item.Name, RegionOfInterest = item, IsInteractive = interactive, OnMoveFinished = UpdateROIPosition }
                    };
                    roiSource.Add(roi);
                }
            }
        }
    }

    private void UpdateROIPosition(XYPoint point)
    {
        if (point is ROIPoint { RegionOfInterest: not null } roiPoint && ImageWithMetadata != null)
        {
            LengthPoint planePosition = _stageNavigation.GetPlanePositionFromImageLocation(new RatioPoint()
            {
                X = new Ratio(point.X / ImageWithMetadata.Coordinates.ImageSize.Width, RatioUnit.DecimalFraction),
                Y = new Ratio(point.Y / ImageWithMetadata.Coordinates.ImageSize.Height, RatioUnit.DecimalFraction)
            }, ImageWithMetadata);

            ActiveProject?.UpdateROI(roiPoint.RegionOfInterest, roi => roi.Position = planePosition);
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    async partial void OnSelectedRoiChanged(RoiVM? value)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        ActiveProject?.PublishSelectedRoiChange(value?.Id ?? Guid.Empty);
        _regionsOfInterestSource.ForEach(roiVm => roiVm.CanChangeCorrelationMode = false);
        if (value is not null)
        {
            value.CanChangeCorrelationMode = true;
        }
        await ImagesStateManager.OnSelectedRoiChanged();
    }
}
