using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tarmi.App.Infrastructure;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.FocusPoints;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.App.WPF;
using Tarmi.Configuration;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.Devices.Thorlabs.Light;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;
using Duration = UnitsNet.Duration;

namespace Tarmi.App.ViewModels.Modes.LM;

public partial class LuminescenceModeViewModel : VirtualDeviceViewModel
{
    private readonly ILuminescenceMode _virtualDevice;
    private readonly LuminescenceImagingPipeline _imagingPipeline;
    private readonly TileSet3DGrabbingService _tileSet3DGrabbingService;
    private readonly bool _isSimulated;

    protected override string ModeName => "Luminescence";
    protected override StageCameraView CameraView => StageCameraView.LM;
    public int InputDelayMillis { get; } = 700;


    public RangeDescriptor<Duration> ExposureTimeRange => _virtualDevice.ExposureTimeRange;
    public double ExposureStep { get; } = 1;


    public RangeDescriptor<Ratio> IntensityRange => _virtualDevice.IntensityRange;
    public double IntensityStep { get; } = 0.1;


    public RangeDescriptor<double> GammaRange => _virtualDevice.GammaRange;
    public double GammaStep { get; } = 0.1;


    public RangeDescriptor<Level> GainRange => _virtualDevice.GainRange;
    public Level GainStep { get; } = Level.FromDecibels(0.1);

    [ObservableProperty]
    private FilterType _filterType;

    public Length FocusStep
    {
        get => _virtualDevice.FocusStep;
        set => _virtualDevice.FocusStep = value;
    }
    public IEnumerable<Length> AvailableFocusSteps => _virtualDevice.FocusStepSizes;

    [ObservableProperty]
    private double _intensity;

    [ObservableProperty]
    private double _exposure;

    [ObservableProperty]
    private BinningSize _selectedBinningSize;
    public List<int> BinningSizes { get; } = [.. Enum.GetValues<BinningSize>().Select(x => (int)x)];

    [ObservableProperty]
    private double _stageTilt = 1.0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AutoFocusCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusManuallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(AutoTiltCommand))]
    [NotifyCanExecuteChangedFor(nameof(TiltManuallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusDecrementManualCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusIncrementManualCommand))]
    public bool _isProtracted;

    [ObservableProperty]
    private double _lowerBound = 0;

    [ObservableProperty]
    private double _upperBound = 255;

    [ObservableProperty]
    private bool _autoExposureEnabled = false;

    [ObservableProperty]
    private SortedDictionary<int, double>? _histogramData;

    public override StageOverviewViewModel StageOverview { get; }

    public LuminescenceImagingViewModel LuminescenceImaging { get; }

    public override TileSetGrabbingViewModel TileSetGrabbing { get; }

    public ZStackGrabbingViewModel ZStackGrabbing { get; }

    public PersistentImagingSettings PersistentImagingSettings { get; }

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(LuminescenceModeViewModel)}::{methodName}";

    public LuminescenceModeViewModel(
        ILoggerFactory loggerFactory,
        IWindowService windowService,
        ILuminescenceMode luminescenceVirtualDevice,
        IProjectManager projectManager,
        ApplicationConfig applicationConfig,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ILimits limits,
        PersistentImagingSettings persistentImagingSettings,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel,
        LuminescenceImagingViewModel luminescenceImagingViewModel
    )
        : base(
            loggerFactory.CreateLogger<LuminescenceModeViewModel>(),
            luminescenceVirtualDevice,
            windowService,
            projectManager,
            new LuminescenceImagingPipeline(
                loggerFactory.CreateLogger<LuminescenceImagingPipeline>(),
                luminescenceVirtualDevice,
                projectManager,
                applicationConfig,
                stageNavigation
            ),
            stageNavigation,
            safeStageControlling,
            limits,
            overviewImageViewModel,
            roiControlViewModel,
            focusPointControlViewModel,
            applicationConfig
        )
    {
        _virtualDevice = luminescenceVirtualDevice;
        PersistentImagingSettings = persistentImagingSettings;
        _isSimulated = applicationConfig.Simulation.Enabled;
        _imagingPipeline = (LuminescenceImagingPipeline)_genericImagingPipeline;
        var zStackGrabbingService = new ZStackGrabbingService(luminescenceVirtualDevice, luminescenceVirtualDevice, loggerFactory.CreateLogger<ZStackGrabbingService>());
        StageOverview = new StageOverviewLMModeViewModel(luminescenceVirtualDevice, stageNavigation, safeStageControlling, projectManager);
        LuminescenceImaging = luminescenceImagingViewModel;
        _tileSet3DGrabbingService = new TileSet3DGrabbingService(luminescenceVirtualDevice, _logger, _virtualDevice);
        ZStackGrabbing = new(windowService, stageNavigation, projectManager, _genericImagingPipeline, safeStageControlling, luminescenceVirtualDevice, zStackGrabbingService, LuminescenceImaging, applicationConfig, loggerFactory.CreateLogger<ZStackGrabbingViewModel>(), this);
        TileSetGrabbing = new LuminescenceTilesetGrabbingViewModel(_logger, windowService, stageNavigation, luminescenceVirtualDevice, projectManager, _genericImagingPipeline, safeStageControlling, LuminescenceImaging, _tileSetGrabbingService, _tileSet3DGrabbingService, ZStackGrabbing, applicationConfig, this);
    }

    protected override void DisposeCore()
    {
        LuminescenceImaging.Dispose();
        base.DisposeCore();
    }

    protected override async Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        FilterType = await _virtualDevice.GetModeAsync();
        SelectedBinningSize = _virtualDevice.Binning;
        PersistentImagingSettings.Initialize(_virtualDevice);
        await LuminescenceImaging.Initialize();
        _subscriptions.AddRange([
            _imagingPipeline.Histogram.Subscribe(histogram => HistogramData = histogram),
            _virtualDevice.IsProtracted.Subscribe(isProtracted => IsProtracted = isProtracted),
            _tileSet3DGrabbingService.TileSetGrabbingRunning.Subscribe(HandleTileSetGrabbingRunning),
            _virtualDevice.LinearStagePosition.Subscribe(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FocusDecrementManualCommand.NotifyCanExecuteChanged();
                    FocusIncrementManualCommand.NotifyCanExecuteChanged();
                });
            }),
            _imagingPipeline.Histogram.Subscribe(histogram => {
                HistogramData = histogram;
                LowerBound = _imagingPipeline.HistogramLowerBound;
                UpperBound = _imagingPipeline.HistogramUpperBound;
            }),
            _virtualDevice.CurrentActiveLightColor.Subscribe(HandleColorChanged)
        ]);
    }

    protected override async Task DeInitializeInternalAsync(ApplicationMode nextMode, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        if (IsProtracted && nextMode != ApplicationMode.Confocal)
        {
            using var msgGuard = _windowService.ShowBusyMessage(Messages.RetractingObjectiveBusyMessage);
            await _virtualDevice.RetractAsync(cancellationToken);
        }
    }

    protected override bool CanManualFocus(double change)
    {
        var limit = _limits.GetFocusRangeForActiveBeam();
        var actualPosition = _virtualDevice.GetCurrentFocusLength();
        var targetPosition = actualPosition + (change * FocusStep);
        return IsProtracted && limit.IsValueInRange(targetPosition);
    }
    protected override bool CanAutoFocus() => IsProtracted && base.CanAutoFocus();
    protected override bool CanManualTilt(double change) => IsProtracted && base.CanManualTilt(change);
    protected override bool CanAutoTilt() => IsProtracted && base.CanAutoFocus();

    async partial void OnFilterTypeChanged(FilterType value)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        using var msgGuard = _windowService.ShowBusyMessage(Messages.ChangingFilterBusyMessage);

        await Task.Run(async () =>
        {
            // in simulation we need to change image, therefore stopping and restarting stream is necessary
            var isLiveImageActive = IsGrabbingImage;
            // simulation only
            if (isLiveImageActive && _isSimulated)
            {
                await StartGrabbingAsync();
            }

            await _virtualDevice.ChangeModeAsync(value);
            Exposure = _virtualDevice.ExposureTime.Microseconds;
            Intensity = _virtualDevice.Intensity.Percent;
            FilterType = await _virtualDevice.GetModeAsync();
            await _imagingPipeline.Clear();

            // simulation only
            if (isLiveImageActive && _isSimulated)
            {
                await StartGrabbingAsync();
            }
        });
    }

    [RelayCommand]
    public void SetGamma()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        _virtualDevice.Gamma = PersistentImagingSettings.ImagingSettings.Gamma;
        PersistentImagingSettings.ImagingSettings.Gamma = _virtualDevice.Gamma;
    }

    [RelayCommand]
    public void SetGain()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        _virtualDevice.Gain = Level.FromDecibels(PersistentImagingSettings.ImagingSettings.Gain);
        PersistentImagingSettings.ImagingSettings.Gain = _virtualDevice.Gain.Decibels;
    }

    [RelayCommand]
    public void SetBinningSize(int selectedBinningSize)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        _virtualDevice.Binning = (BinningSize)selectedBinningSize;
        SelectedBinningSize = _virtualDevice.Binning;
    }

    [RelayCommand]
    public async Task ProtractRetractAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        if (IsProtracted)
        {
            using var msgGuard = _windowService.ShowBusyMessage(Messages.RetractingObjectiveBusyMessage);
            await _virtualDevice.RetractAsync(default);
        }
        else
        {
            using var msgGuard = _windowService.ShowBusyMessage(Messages.ProtractingObjectiveBusyMessage);
            await _virtualDevice.ProtractAsync(default);
        }

        RoiControl.ImagesStateManager.UpdateCanNavigateTo();
    }

    private bool CanIncrementFocus() => IsProtracted && _virtualDevice.CurrentLinearStagePosition < _limits.GetFocusRangeForActiveBeam().Max;

    [RelayCommand(CanExecute = nameof(CanIncrementFocus))]
    public async Task FocusIncrementManualAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _virtualDevice.MoveLinearStageRelativeAsync(FocusStep, default);
    }

    private bool CanDecrementFocus() => IsProtracted && _virtualDevice.CurrentLinearStagePosition > _limits.GetFocusRangeForActiveBeam().Min;

    [RelayCommand(CanExecute = nameof(CanDecrementFocus))]
    public async Task FocusDecrementManualAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _virtualDevice.MoveLinearStageRelativeAsync(-FocusStep, default);
    }


    [RelayCommand]
    public async Task IncrementTiltAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        _ = await _virtualDevice.TiltStageAsync(Angle.FromDegrees(StageTilt));
    }

    [RelayCommand]
    public async Task DecrementTiltAsync()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        _ = await _virtualDevice.TiltStageAsync(Angle.FromDegrees(-StageTilt));
    }

    partial void OnUpperBoundChanged(double value)
    {
        _imagingPipeline.HistogramUpperBound = (int)value;
    }

    partial void OnLowerBoundChanged(double value)
    {
        _imagingPipeline.HistogramLowerBound = (int)value;
    }

    [RelayCommand]
    private void ResetHistogram()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        LowerBound = 0;
        UpperBound = 255;
    }

    partial void OnAutoExposureEnabledChanged(bool value)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        _imagingPipeline.UseAutoEqualize(value).SafeFireAndForget();
        
        if (value)
        {
            LowerBound = _imagingPipeline.HistogramLowerBound;
            UpperBound = _imagingPipeline.HistogramUpperBound;
        }
        else
        {
            LowerBound = 0;
            UpperBound = 255;
        }
    }

    partial void OnFilterTypeChanged(FilterType oldValue, FilterType newValue)
    {
        PersistentImagingSettings.FilterType = newValue;
        RoiControl.ImagesStateManager.UpdateCanNavigateTo();
    }

    public async Task RestoreImageState(ImageMetadata imageMetadata)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.LM ||
            IsProtracted == false ||
            imageMetadata.LuminescenceMetadata == null
        )
        {
            return;
        }

        await _virtualDevice.RestoreImageState(imageMetadata, default);

        SelectedBinningSize = _virtualDevice.Binning;
        PersistentImagingSettings.ImagingSettings.Exposure = _virtualDevice.ExposureTime.Microseconds;
        PersistentImagingSettings.ImagingSettings.Gain = _virtualDevice.Gain.Decibels;
        PersistentImagingSettings.ImagingSettings.Gamma = _virtualDevice.Gamma;
    }

    private void HandleColorChanged(LightColor? nullable)
    {
        Exposure = _virtualDevice.ExposureTime.Microseconds;
        Intensity = _virtualDevice.Intensity.Percent;
    }
}
