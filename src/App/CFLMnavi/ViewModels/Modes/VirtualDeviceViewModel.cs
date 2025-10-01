using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Betrian.App.Infrastructure;
using Betrian.CflmNavi.App.Infrastructure;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.CflmNavi.App.ViewModels.Fiducials;
using Betrian.CflmNavi.App.ViewModels.FocusPoints;
using Betrian.Imaging.Common;
using Betrian.Models;
using CFLMnavi.Configuration;
using CFLMnavi.ImagePipeline.Pipelines;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CFLMnavi.WPF;
using CFLMnavi.WPF.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes;

public abstract partial class VirtualDeviceViewModel : ApplicationModeViewModelBase
{
    private const int _stageOverviewTabIndex = 0;
    private readonly IVirtualDevice _virtualDevice;
    protected readonly IWindowService _windowService;
    protected readonly ILogger _logger;
    protected readonly IStageNavigation _stageNavigation;
    private readonly FocusingService _focusingService;
    protected readonly ISafeStageControlling _safeStageControlling;
    protected readonly ILimits _limits;
    protected readonly IProjectManager _projectManager;
    protected readonly ImagingPipeline _genericImagingPipeline;
    private readonly SemaphoreSlim _activationLock = new(1, 1);
    private volatile bool _isActive;
    private CancellationTokenSource? _imageGrabbingCts;
    protected readonly List<IDisposable> _subscriptions = [];
    protected readonly TileSetGrabbingService _tileSetGrabbingService;
    protected abstract string ModeName { get; }
    protected abstract StageCameraView CameraView { get; }

    public abstract TileSetGrabbingViewModel TileSetGrabbing { get; }
    public abstract StageOverviewViewModel StageOverview { get; }

    [ObservableProperty]
    private ObservableProject? _activeProject;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptureImageEnabled))]
    [NotifyPropertyChangedFor(nameof(GrabImageEnabled))]
    private bool _isGrabbingImage;

    [ObservableProperty]
    private bool _manualFocusEnabled;

    [ObservableProperty]
    private bool _manualTiltEnabled;

    [ObservableProperty]
    public partial bool IsScaleBarVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsMarksLayerVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptureImageEnabled))]
    protected ImageWithMetadata? _imageWithMetadata;
    
    [ObservableProperty]
    protected ImageWithMetadata? _secondaryImageWithMetadata; // populate with desired image overlay, set to null to not display it

    [ObservableProperty]
    private bool _showSecondaryImage;

    [ObservableProperty]
    private int _selectedLowerRightPanelTab;

    [ObservableProperty]
    public partial UIElement? SecondaryPanelContent { get; set; }

    public bool CaptureImageEnabled =>
        !IsGrabbingImage &&
        ImageWithMetadata is not null &&
        ImageWithMetadata.ImageId != Guid.Empty &&
        RoiControl.SelectedRoi is not null &&
        // should be enough for single images
        !RoiControl.SelectedRoi.UngroupedRoiChildVMs.Any(vm => vm.SortId == ImageWithMetadata.ImageId) &&
        !RoiControl.ImagesStateManager.IsImageShown;

    public bool GrabImageEnabled => !IsGrabbingImage;

    public RoiControlViewModel RoiControl { get; }

    public FocusPointControlViewModel FocusPointControl { get; }

    public FiducialPointControlViewModel PrimaryFiducialPointControl { get; }
    public FiducialPointControlViewModel SecondaryFiducialPointControl { get; }

    public ImageViewerControlsViewModel ImageViewerVM { get; } = new ImageViewerControlsViewModel();

    public ImageOverlayToggles ImageOverlayToggles { get; } = new();

    public OverviewImageViewModel OverviewImageVM { get; }

    internal ImagingPipeline ImagingPipeline => _genericImagingPipeline;
    internal IStageNavigation StageNavigation =>_stageNavigation;

    protected VirtualDeviceViewModel(
        ILogger logger,
        IVirtualDevice virtualDevice,
        IWindowService windowService,
        IProjectManager projectManager,
        ImagingPipeline imagingPipeline,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ILimits limits,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel,
        ApplicationConfig applicationConfig
    )
    {
        _logger = logger;
        _virtualDevice = virtualDevice;
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _limits = limits;
        _windowService = windowService;
        _projectManager = projectManager;
        _genericImagingPipeline = imagingPipeline;
        _stageNavigation = stageNavigation;
        _focusingService = new FocusingService(logger, virtualDevice, limits, applicationConfig);
        _tileSetGrabbingService = new TileSetGrabbingService(logger, virtualDevice, _focusingService);
        RoiControl = roiControlViewModel;
        FocusPointControl = focusPointControlViewModel;
        PrimaryFiducialPointControl = new(logger, projectManager, stageNavigation);
        SecondaryFiducialPointControl = new(logger, projectManager, stageNavigation);
        OverviewImageVM = overviewImageViewModel;
    }

    [RelayCommand]
    public async Task StartGrabbingAsync()
        => await ControlGrabbingImageAsync(default);

    [RelayCommand(CanExecute = nameof(GrabImageEnabled))]
    public async Task GrabImageAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using (_windowService.ShowBusyMessage(Messages.AcquiringImageBusyMessage))
        {
            await RoiControl.ImagesStateManager.ClearState();
            await _genericImagingPipeline.GrabOneAsync();
        }
    }

    [RelayCommand]
    public async Task CaptureImage()
    {
        static string GenerateNewFilename(StageCameraView stageCameraView)
        {
            var datePart = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss-fffzz");
            return $"{datePart}_{stageCameraView}{ProjectExtensions.ImageExtension}";
        }

        if (ActiveProject is not null)
        {
            var selectedRoi = ActiveProject.GetActiveRegionOfInterest();
            if (selectedRoi is not null) {

                var image = await _genericImagingPipeline.GetImageCopyAsync(ImageProcessingStage.FilteredInput);
                try
                {
                    // no need to distinguish between fluorescence and reflection images, layer is just a group, images are shown directly in ROI structure
                    var cameraSource = image.GetSource();
                    var layeredImageDescriptor = ActiveProject.GetOrCreateLayeredImageDescriptor(_safeStageControlling.ActiveCameraView, selectedRoi.Id);
                    var imageContent = new LayerContentDescriptorWithCorrelationInfo() { SubDirectory = null, Filename = GenerateNewFilename(cameraSource), Id = image.ImageId };

                    layeredImageDescriptor.Images.Add(imageContent);
                    var fullPath = ActiveProject.GetContentFilePath(layeredImageDescriptor, imageContent);
                    var tiffMetadata = image.TiffMetadata;
                    var dto = tiffMetadata.TimeOfAcquisition;
                    tiffMetadata = tiffMetadata with { ImageDescription = $"{dto:dd.MM. HH:mm:ss}" };
                    image = image with { LayerId = layeredImageDescriptor.Id, RegionOfInterestId = selectedRoi.Id, ImageId = imageContent.Id, TiffMetadata = tiffMetadata };
                    TiffImage.Save(image, fullPath);
                    ActiveProject.AddOrUpdateDescriptor(layeredImageDescriptor);

                    // first SEM ROI image/map is marked as reference
                    if (cameraSource == StageCameraView.SEM)
                    {
                        var hasReference =
                            selectedRoi.Images.Any(id => id.Images.Any(desc => desc.CorrelationInfo.IsReferenceImage)) ||
                            selectedRoi.TileSets.Any(id => id.CorrelationInfo.IsReferenceImage);

                        if (!hasReference)
                        {
                            ActiveProject.SetReference(imageContent.CorrelationInfo);
                        }
                    }
                    OnPropertyChanged(nameof(CaptureImageEnabled));
                }
                finally
                {
                    image.Dispose();
                }
            }
        }
    }

    protected virtual bool CanManualFocus(double change)
    {
        var limit = _limits.GetFocusRangeForActiveBeam();
        var actualPosition = _virtualDevice.GetCurrentFocusLength();
        var targetPosition = actualPosition + (change * limit.Step);
        return limit.IsValueInRange(targetPosition);
    }

    [RelayCommand(CanExecute = nameof(CanManualFocus))]
    private async Task FocusManuallyAsync(double change)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        //using var uiGuard = _windowService.ShowBusyMessage(Messages.FocusChangeBusyMessage);
        await _virtualDevice.FocusAsync(change, default);
    }

    protected virtual bool CanAutoFocus() => true;

    [RelayCommand(CanExecute = nameof(CanAutoFocus))]
    private async Task AutoFocusAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        using CancellationTokenSource cancellationTokenSource = new();
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Auto-focus in-progress",
            progress => _focusingService.FocusAutomaticallyAsync(_stageNavigation, _safeStageControlling, _genericImagingPipeline, [], progress, cancellationTokenSource.Token),
            cancellationTokenSource.Cancel
        );
    }

    protected virtual bool CanManualTilt(double change)
    {
        var limit = _limits.GetTiltRangeForView(_safeStageControlling.ActiveCameraView);
        var actualPosition = _virtualDevice.StageState.CurrentPosition.Tilt;
        var targetPosition = actualPosition + ((int)change * limit.Step);
        return limit.IsValueInRange(targetPosition);
    }

    [RelayCommand(CanExecute = nameof(CanManualTilt))]
    private async Task TiltManuallyAsync(double change)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        var limit = _limits.GetTiltRangeForView(_safeStageControlling.ActiveCameraView);
        //using var uiGuard = _windowService.ShowBusyMessage(Messages.StageMoveBusyMessage);
        _ = await _virtualDevice.TiltStageAsync((int)change * limit.Step);
    }

    [RelayCommand(CanExecute = nameof(CanAutoTilt))]
    private async Task AutoTiltAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        using CancellationTokenSource cancellationTokenSource = new();
        await _windowService.ShowDeterminateWaitingDialogAsync(
            "Auto-tilt in-progress",
            progress => _focusingService.TiltStageAutomaticallyAsync(_safeStageControlling, _genericImagingPipeline, progress, cancellationTokenSource.Token),
            cancellationTokenSource.Cancel
        );
    }

    protected virtual bool CanAutoTilt() => true;

    [RelayCommand]
    private async Task StageMoveClickAsync(System.Windows.Point point)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        if (ImageWithMetadata is not null && ImageWithMetadata.ImageId != Guid.Empty)
        {
            var ratioPoint = new RatioPoint()
            {
                X = Ratio.FromDecimalFractions((double)point.X / ImageWithMetadata.Coordinates.ImageSize.Width),
                Y = Ratio.FromDecimalFractions((double)point.Y / ImageWithMetadata.Coordinates.ImageSize.Height),
            };
            var stagePosition = _stageNavigation.GetStagePositionFromImageLocation(ratioPoint, ImageWithMetadata, CameraView);
            
            using (_windowService.ShowBusyMessage(Messages.StageMoveBusyMessage))
            {
                var success = await _virtualDevice.MoveStageAsync(stagePosition);
                // TODO: in case of false, show limits error notification
            }
        }
    }

    private bool CanCaptureScreenshot() => ImageWithMetadata is not null;

    [RelayCommand(CanExecute = nameof(CanCaptureScreenshot))]
    private async Task CaptureScreenshot()
    {
        using (_windowService.ShowBusyMessage(Messages.CapturingScreenshotBusyMessage))
        {
            await Task.Run(() =>
            {
                var directoryPath = Path.Combine(
                    ActiveProject?.ProjectDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Screenshots"
                );
                _ = Directory.CreateDirectory(directoryPath);
                var filePath = Path.Combine(
                    directoryPath,
                    $"cflmnavi_screen_{DateTimeOffset.Now:yyyy-MM-dd_HH_mm_ss}.tiff"
                );
                TiffImage.Save(ImageWithMetadata!, filePath);

                _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            });
        }
    }

    private void HandleTileSetGrabbingRunning(bool isRunning)
    {
        if (isRunning)
        {
            SelectedLowerRightPanelTab = _stageOverviewTabIndex;
        }
    }

    partial void OnManualFocusEnabledChanged(bool value)
    {
        if (value)
        {
            ManualTiltEnabled = false;
        }
    }

    partial void OnManualTiltEnabledChanged(bool value)
    {
        if (value)
        {
            ManualFocusEnabled = false;
        }
    }

    protected virtual Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task DeInitializeInternalAsync(ApplicationMode nextMode, CancellationToken cancellationToken) => Task.CompletedTask;

    protected override async Task InitializeCoreAsync(ApplicationMode prevMode)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _activationLock.UseOnceAsync(default);

        if (!_isActive)
        {
            await _windowService.ShowIndeterminateWaitingDialogAsync($"Switching to {ModeName} mode.", async progress =>
            {
                progress.Report("Enabling devices.");
                await _virtualDevice.EnableAsync(default);
                _subscriptions.Add(_projectManager.ActiveProject.Subscribe(HandleActiveProjectChanged));
                HandleActiveProjectChanged(_projectManager.GetActiveProject());
                _subscriptions.Add(_genericImagingPipeline.Output.Subscribe(HandleImageUpdate));
                _subscriptions.Add(_tileSetGrabbingService.TileSetGrabbingRunning.Subscribe(HandleTileSetGrabbingRunning));
                _subscriptions.Add(_virtualDevice.GrabbingActiveChanges.Subscribe(HandleGrabbingActiveState));
                await StageOverview.Initialize(default);
                await InitializeInternalAsync(prevMode, default);
                await base.InitializeCoreAsync(prevMode);
                await OverviewImageVM.Initialize(); // TODO: this should be automatic and only once
                _isActive = true;
            });
            RoiControl.ActiveDevice = this;
        }
    }

    protected override async Task DeInitializeCoreAsync(ApplicationMode nextMode)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        using var guard = await _activationLock.UseOnceAsync(default);

        if (_isActive)
        {
            RoiControl.ActiveDevice = null;
            await _windowService.ShowIndeterminateWaitingDialogAsync($"Switching from {ModeName} mode.", async progress =>
            {
                _virtualDevice.StopGrabbing();
                CancelAllSubscriptions();
                ClearActiveView();
                await _genericImagingPipeline.Clear();
                ImageWithMetadata = null;
                await StageOverview.DeInitialize();
                await DeInitializeInternalAsync(nextMode, default);
                await _virtualDevice.DisableAsync(default);
                await base.DeInitializeCoreAsync(nextMode);
                _isActive = false;
            });
        }
    }

    protected virtual string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(VirtualDeviceViewModel)}::{methodName}";

    protected virtual async Task ControlGrabbingImageAsync(CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        if (!IsGrabbingImage)
        {
            await RoiControl.ImagesStateManager.ClearState();
            _imageGrabbingCts = new();
            await _virtualDevice.StartGrabbingAsync(_imageGrabbingCts.Token);
            RoiControl.ImagesStateManager.UpdateCanToggleVisibilities();
        }
        else
        {
            _virtualDevice.StopGrabbing();
            RoiControl.ImagesStateManager.UpdateCanToggleVisibilities();
        }
    }

    protected virtual void HandleImageUpdate(ImageWithMetadata imageWithMetadata)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        ImageWithMetadata = imageWithMetadata with { Image = imageWithMetadata.Image.Clone() };
        RoiControl.ImageWithMetadata = ImageWithMetadata;
        FocusPointControl.ImageWithMetadata = ImageWithMetadata;
    }

    partial void OnSecondaryImageWithMetadataChanged(ImageWithMetadata? value)
    {
        RoiControl.SecondaryImageWithMetadata = value;
    }

    protected virtual void HandleActiveProjectChanged(ObservableProject? project)
    {
        ActiveProject = project;
        RoiControl.ActiveProject = project;
        if (project is not null)
        {
            _subscriptions.Add(project.ActiveRegionOfInterestIdChanges.Subscribe(_ => OnPropertyChanged(nameof(CaptureImageEnabled))));
        }
    }

    private void HandleGrabbingActiveState(bool active)
    {
        IsGrabbingImage = active;
        OnPropertyChanged(nameof(CaptureImageEnabled));
        OnPropertyChanged(nameof(GrabImageEnabled));
        _genericImagingPipeline.SetLiveStreamEnabled(active);
        RoiControl.ImagesStateManager.UpdateCanToggleVisibilities();
        if (!active)
        {
            _imageGrabbingCts?.Cancel();
            _imageGrabbingCts = null;
        }        
    }

    private void CancelAllSubscriptions()
    {
        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
    }

    partial void OnImageWithMetadataChanged(ImageWithMetadata? value) => PropagateActiveViewChange();

    partial void OnIsGrabbingImageChanged(bool value) => PropagateActiveViewChange();

    private void ClearActiveView()
    {
        try
        {
            ActiveProject?.PublishActiveView(ActiveView.Zero);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear active view.");
        }
    }

    private void PropagateActiveViewChange()
    {
        try
        {
            ActiveView activeView = ActiveView.Zero;
            if (ImageWithMetadata?.ImageId != Guid.Empty && ImageWithMetadata?.Coordinates is not null)
            {
                var planePosition = _stageNavigation.GetPlanePosition(
                    ImageWithMetadata.Coordinates.ElectronBeamStagePosition, ImageWithMetadata.Coordinates.CameraView
                );
                var width = ImageWithMetadata.Coordinates.ImageSize.Width * ImageWithMetadata.Coordinates.PixelSize.X;
                var height = ImageWithMetadata.Coordinates.ImageSize.Height * ImageWithMetadata.Coordinates.PixelSize.Y;
                activeView = new()
                {
                    Center = planePosition,
                    Size = new() { Height = height, Width = width },
                    LiveStreamIsActive = IsGrabbingImage
                };
            }

            ActiveProject?.PublishActiveView(activeView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate active view change.");
        }
    }
}
