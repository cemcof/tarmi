using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using Betrian.App.Infrastructure;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.Configuration.Holders;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.Projects.Transactions;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes;

public enum TilesetGrabbingOptions
{
    [Display(Name = "Tile Set 2D")]
    Tileset2D,

    [Display(Name = "Auto Focus Tile Set 2D")]
    AutoFocusTileset2D,

    [Display(Name = "Tile Set 3D")]
    Tileset3D,
}

public partial class TileSetGrabbingViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly TileSetGrabbingService _tileSetGrabbingService;
    private readonly TileSet3DGrabbingService? _tileSet3DGrabbingService;
    private readonly ZStackGrabbingViewModel? _zStackGrabbingVM;
    private readonly IImagingPipelineGrabber _pipelineGrabber;
    private readonly IWindowService _windowService;
    private readonly IStageNavigation _stageNavigation;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly ApplicationConfig _applicationConfig;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly VirtualDeviceViewModel _parent;

    public List<TilesetGrabbingOptions> TilesetGrabbingOptions { get; init; } = [Modes.TilesetGrabbingOptions.Tileset2D, Modes.TilesetGrabbingOptions.AutoFocusTileset2D];
    public ObservableCollection<ImageWithMetadata> TileSetThumbnails { get; } = [];

    [ObservableProperty]
    private TilesetGrabbingOptions _selectedGrabbingOption = Modes.TilesetGrabbingOptions.Tileset2D;

    [ObservableProperty]
    public Visibility _message3DVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private AreaOfInterest? _selectedGrid;

    [ObservableProperty]
    private ObservableProject? _activeProject;

    [ObservableProperty]
    private bool _customAreaToggleEnabled;

    [ObservableProperty]
    private bool _showFocusPoints;

    [ObservableProperty]
    private ImageWithMetadata? _selectedTileSetThumbnail;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcquireLineByLineCommand))]
    [NotifyCanExecuteChangedFor(nameof(AcquireSpiralCommand))]
    private bool _canAcquireTileset = true;

    [ObservableProperty]
    private bool _acquireTileSetOnReducedArea;

    [ObservableProperty]
    private Length _customTileSetRectangleWidth = Length.FromMillimeters(0.7);

    [ObservableProperty]
    private Length _customTileSetRectangleHeight = Length.FromMillimeters(0.7);

    private bool CanExecuteTilesetAcquisition() => CanAcquireTileset;

    private static string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(TileSetGrabbingViewModel)}::{methodName}";

    public TileSetGrabbingViewModel(
        ILogger logger,
        IWindowService windowService,
        IStageNavigation stageNavigation,
        IProjectManager projectManager,
        IImagingPipelineGrabber pipelineGrabber,
        ISafeStageControlling safeStageControlling,
        TileSetGrabbingService tileSetGrabbingService,
        TileSet3DGrabbingService? tileSet3DGrabbingService,
        ZStackGrabbingViewModel? zStackGrabbingVM,
        ApplicationConfig applicationConfig,
        VirtualDeviceViewModel parent
    )
    {
        _logger = logger;
        _windowService = windowService;
        _stageNavigation = stageNavigation;
        _tileSetGrabbingService = tileSetGrabbingService;
        _tileSet3DGrabbingService = tileSet3DGrabbingService;
        _zStackGrabbingVM = zStackGrabbingVM;
        _pipelineGrabber = pipelineGrabber;
        // TODO: Dispose
        _subscriptions.Add(projectManager.ActiveProject.Subscribe(HandleActiveProjectChange));
        _safeStageControlling = safeStageControlling;
        _applicationConfig = applicationConfig;
        _parent = parent;
    }

    [RelayCommand]
    public void SetTilesetGrabbing(TilesetGrabbingOptions option)
    {
        SelectedGrabbingOption = option;
        Message3DVisibility = option == Modes.TilesetGrabbingOptions.Tileset3D ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTilesetAcquisition))]
    public async Task AcquireLineByLine()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _parent.RoiControl.ImagesStateManager.ClearState();
        using var cts = new CancellationTokenSource();
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Tileset acquisition in progress",
            async progress => await TileSetAcquisitionImplementation(AcquisitionStrategy.Linear, progress, cts.Token),
            cts.Cancel,
            "Stop"
        );
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTilesetAcquisition))]
    public async Task AcquireSpiral()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _parent.RoiControl.ImagesStateManager.ClearState();
        using var cts = new CancellationTokenSource();
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Tileset acquisition in progress",
            async progress => await TileSetAcquisitionImplementation(AcquisitionStrategy.Spiral, progress, cts.Token),
            cts.Cancel,
            "Stop"
        );
    }

    [RelayCommand]
    public async Task ReacquireTileSet(TileSetDescriptor descriptor)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _parent.RoiControl.ImagesStateManager.ClearState();
        using var cts = new CancellationTokenSource();
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Tileset re-acquisition with focus points in progress",
            async progress => await TileSetReAcquisitionImplementation(descriptor with { GrabbingOptions = descriptor.GrabbingOptions with { FocusStrategy = FocusStrategy.Auto } }, progress, cts.Token),
            cts.Cancel,
            "Stop"
        );
    }

    [RelayCommand]
    public async Task RestitchTileSet(TileSetDescriptor descriptor)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        using var transaction = new TileSetCreationTransaction(ActiveProject!, descriptor.Source, _stageNavigation.GetPlanePosition, descriptor, PrematureTerminationMode.KeepResults);
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Re-stitching Tileset",
            async progress => await _tileSetGrabbingService.ProcessTileSetImages(transaction, _stageNavigation.GetPlanePosition, progress, default),
            null
        );
    }

    [RelayCommand]
    public async Task RestitchTileSet3D(TileSet3DDescriptor descriptor)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        using var transaction = new TileSet3DCreationTransaction(ActiveProject!, descriptor.Source, _stageNavigation.GetPlanePosition, descriptor, PrematureTerminationMode.KeepResults);
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Re-stitching Tileset3D",
            async progress => await _tileSet3DGrabbingService!.ProcessTileSetMipImages(transaction, _stageNavigation.GetPlanePosition, progress, default),
            null
        );
    }

    partial void OnShowFocusPointsChanged(bool value)
    {
        if (ActiveProject is not null)
        {
            // TODO
        }
    }

    private async Task TileSetReAcquisitionImplementation(TileSetDescriptor descriptor, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        if (ActiveProject is not null)
        {
            await _tileSetGrabbingService.ReacquireTileSet(ActiveProject, _stageNavigation, _safeStageControlling, _pipelineGrabber, descriptor, progress, _logger, cancellationToken);
        }
    }

    protected virtual async Task TileSetAcquisitionImplementation(AcquisitionStrategy acquisitionStrategy, IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        if (ActiveProject is null)
        {
            return;
        }

        var (area, roi, focusPoints) = GetAreaForTileSetAcquisition();
        TileSetOptions options = ResolveGrabbingOptions(acquisitionStrategy, area, focusPoints);

        if (SelectedGrabbingOption != Modes.TilesetGrabbingOptions.Tileset3D)
        {
            await _tileSetGrabbingService.GrabTileSetAsync(ActiveProject, _stageNavigation, _safeStageControlling, _pipelineGrabber, roi, options, progress, _logger, cancellationToken);
        }
        else if (_tileSet3DGrabbingService != null && _zStackGrabbingVM != null)
        {
            await _tileSet3DGrabbingService.GrabTileSet3DAsync(ActiveProject, _stageNavigation, _safeStageControlling, _pipelineGrabber, roi, options, _zStackGrabbingVM.GetZStackSettings(), null, progress, _logger, cancellationToken);
        }
    }

    private TileSetOptions ResolveGrabbingOptions(AcquisitionStrategy acquisitionStrategy, AreaOfInterest areaOfInterest, CFLMnavi.Projects.FocusPoint[] focusPoints)
    {
        var cameraView = _safeStageControlling.ActiveCameraView;
        var strategy = SelectedGrabbingOption == Modes.TilesetGrabbingOptions.AutoFocusTileset2D ? FocusStrategy.Auto : FocusStrategy.Fixed;

        var overlap = (cameraView, strategy) switch
        {
            (StageCameraView.LM, _) => _applicationConfig.UserPreferences.Algorithms.TileSetPreferences.FixedHfwImageOverlap,
            (_, FocusStrategy.Fixed) => _applicationConfig.UserPreferences.Algorithms.TileSetPreferences.FixedHfwImageOverlap,
            (_, _) => _applicationConfig.UserPreferences.Algorithms.TileSetPreferences.VariableHfwImageOverlap,
        };

        return new()
        {
            AcquisitionStrategy = acquisitionStrategy,
            FocusStrategy = strategy,
            Overlap = overlap,
            AreaOfInterest = areaOfInterest,
            FocusPoints = [.. focusPoints]
        };
    }

    private void HandleActiveProjectChange(ObservableProject? project)
    {
        if (project is not null)
        {
            ActiveProject = project;
            _subscriptions.Add(ActiveProject.ActiveRegionOfInterestIdChanges.Subscribe(HandleActiveRegionOfInterestChange));
            SelectedGrid = ActiveProject?.Holder.Grids.FirstOrDefault();
            HandleActiveRegionOfInterestChange(project.ActiveRegionOfInterestId);
        }
    }

    private void HandleActiveRegionOfInterestChange(Guid guid)
    {
        if (ActiveProject is not null)
        {
            var enabled = ActiveProject!.GetActiveRegionOfInterest() is not null;

            if (!enabled)
            {
                AcquireTileSetOnReducedArea = false;
            }
            CustomAreaToggleEnabled = enabled;
        }
    }

    private AreaOfInterest GetSelectedGrid() => SelectedGrid ?? throw new InvalidOperationException("No grid available");

    private (AreaOfInterest, RegionOfInterest, CFLMnavi.Projects.FocusPoint[]) GetAreaForTileSetAcquisition()
    {
        RegionOfInterest roi;
        AreaOfInterest area;
        CFLMnavi.Projects.FocusPoint[] focusPoints = [];
        if (AcquireTileSetOnReducedArea && ActiveProject is not null)
        {
            roi = ActiveProject?.GetActiveRegionOfInterest() ?? throw new InvalidOperationException("No ROI selected.");
            area = new RectangularAreaOfInterest
            {
                Center = roi.Position,
                Height = CustomTileSetRectangleHeight,
                Width = CustomTileSetRectangleWidth,
                Name = "Custom"
            };
        }
        else
        {
            area = GetSelectedGrid();
            roi = ActiveProject?.RegionsOfInterest
                .OfType<GridCenterRegionOfInterest>()
                .FirstOrDefault(gridRoi => gridRoi.GridName == area.Name) ?? throw new InvalidOperationException("No Grid selected.");
        }
        return (area, roi, focusPoints);
    }
}
