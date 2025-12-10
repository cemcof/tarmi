using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Tarmi.App.Infrastructure;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.FocusPoints;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.App.WPF;
using Tarmi.Configuration;
using Tarmi.Configuration.Devices;
using Tarmi.Devices.Arduino.FilterHandler;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes.Confocal;

public partial class ConfocalModeViewModel : VirtualDeviceViewModel
{
    private readonly IConfocalMode _virtualDevice;
    private readonly ConfocalImagingPipeline _imagingPipeline;
    private readonly bool _isSimulated;

    public FieldSelectionViewModel FieldSelectionViewModel;
    public bool IsFieldSeletion { get; private set; } = false;
    protected override string ModeName => "Confocal";
    protected override StageCameraView CameraView => StageCameraView.Confocal;
    public int InputDelayMillis { get; } = 700;
    public RangeDescriptor<Ratio> IntensityRange => _virtualDevice.IntensityRange;
    public double IntensityStep { get; } = 1.0;
    public RangeDescriptor<Level> GainRange => _virtualDevice.GainRange;
    public Level GainStep { get; } = Level.FromDecibels(0.1);

    public Duration Dwell
    {
        get => _virtualDevice.Dwell;
        set => _virtualDevice.Dwell = value;
    }

    public IEnumerable<Duration> AvailableDwellRanges => _virtualDevice.DwellRanges;

    [ObservableProperty]
    private FilterType _filterType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resolution))]
    public partial Length FieldWidth { get; set; }

    partial void OnFieldWidthChanged(Length value) => _virtualDevice.FieldWidth = value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resolution))]
    public partial Length FieldHeight { get; set; }

    partial void OnFieldHeightChanged(Length value) => _virtualDevice.FieldHeight = value;

    public string Resolution => _virtualDevice.Resolution;

    public Length FocusStep
    {
        get => _virtualDevice.FocusStep;
        set => _virtualDevice.FocusStep = value;
    }
    public IEnumerable<Length> AvailableFocusSteps => _virtualDevice.FocusStepSizes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resolution))]
    public partial Length PixelSize { get; set; }

    partial void OnPixelSizeChanged(Length value) => _virtualDevice.PixelSize = value;

    public IEnumerable<Length> AvailablePixelSizes => _virtualDevice.PixelSizes;

    public ElectricPotential ADC
    {
        get => _virtualDevice.ADC;
        set => _virtualDevice.ADC = value;
    }

    public ConfocalLight LaserColor
    {
        get => _virtualDevice.LaserColor;
        set => _virtualDevice.LaserColor = value;
    }

    public Length PinHoleWheelPosition
    {
        get => _virtualDevice.PinHoleWheelPosition;
        set => _virtualDevice.PinHoleWheelPosition = value;
    }

    [ObservableProperty]
    private double _intensity;

    [ObservableProperty]
    private double _gain;

    public IEnumerable<ElectricPotential> AvailableADCRanges => _virtualDevice.ADCRanges;

    public IEnumerable<Length> PinHoleWheelPositions => _virtualDevice.PinHoleSizes;

    public IList<ConfocalLight> LaserColors => _virtualDevice.ConfocalLights;

    [ObservableProperty]
    private double _stageTilt = 1.0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AutoFocusCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusManuallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(AutoTiltCommand))]
    [NotifyCanExecuteChangedFor(nameof(TiltManuallyCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusDecrementManualCommand))]
    [NotifyCanExecuteChangedFor(nameof(FocusIncrementManualCommand))]
    [NotifyCanExecuteChangedFor(nameof(FieldSelectionCommand))]

    public bool _isProtracted;

    public override StageOverviewViewModel StageOverview { get; }

    public override TileSetGrabbingViewModel TileSetGrabbing { get; }

    public ZStackGrabbingViewModel ZStackGrabbing { get; }

    public PersistentImagingSettings PersistentImagingSettings { get; }

    protected override string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(ConfocalModeViewModel)}::{methodName}";

    public ConfocalModeViewModel(
        ILoggerFactory loggerFactory,
        IWindowService windowService,
        IConfocalMode confocalVirtualDevice,
        IProjectManager projectManager,
        ApplicationConfig applicationConfig,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ILimits limits,
        PersistentImagingSettings persistentImagingSettings,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel
    )
        : base(
            loggerFactory.CreateLogger<ConfocalModeViewModel>(),
            confocalVirtualDevice,
            windowService,
            projectManager,
            new ConfocalImagingPipeline(
                loggerFactory.CreateLogger<ConfocalImagingPipeline>(),
                confocalVirtualDevice,
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
        _virtualDevice = confocalVirtualDevice;
        PersistentImagingSettings = persistentImagingSettings;
        _isSimulated = applicationConfig.Simulation.Enabled;
        _imagingPipeline = (ConfocalImagingPipeline)_genericImagingPipeline;
        var zStackGrabbingService = new ZStackGrabbingService(confocalVirtualDevice, confocalVirtualDevice, loggerFactory.CreateLogger<ZStackGrabbingService>());
        var tileSet3DGrabbingService = new TileSet3DGrabbingService(confocalVirtualDevice, _logger, _virtualDevice);
        StageOverview = new StageOverviewConfocalModeViewModel(confocalVirtualDevice, stageNavigation, safeStageControlling, projectManager);
        ZStackGrabbing = new(windowService, stageNavigation, projectManager, _genericImagingPipeline, safeStageControlling, confocalVirtualDevice, zStackGrabbingService, applicationConfig, loggerFactory.CreateLogger<ZStackGrabbingViewModel>(), this);
        TileSetGrabbing = new ConfocalTilesetGrabbingViewModel(_logger, windowService, stageNavigation, confocalVirtualDevice, projectManager, _genericImagingPipeline, safeStageControlling, _tileSetGrabbingService, tileSet3DGrabbingService, ZStackGrabbing, applicationConfig, this);
        FieldWidth = _virtualDevice.FieldWidth;
        FieldHeight = _virtualDevice.FieldHeight;
        PixelSize = _virtualDevice.PixelSize;
        Gain = _virtualDevice.Gain.Decibels;
        FieldSelectionViewModel = new FieldSelectionViewModel(_virtualDevice.ConfocalData);
    }

    protected override void DisposeCore()
    {
        //ConfocalImaging.Dispose();
        base.DisposeCore();
    }

    protected override async Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        FilterType = await _virtualDevice.GetModeAsync();
        PersistentImagingSettings.Initialize(_virtualDevice);

        _subscriptions.AddRange([
            _virtualDevice.IsProtracted.Subscribe(isProtracted => IsProtracted = isProtracted),
            _virtualDevice.LinearStagePosition.Subscribe(_ =>
            {
                FocusDecrementManualCommand.NotifyCanExecuteChanged();
                FocusIncrementManualCommand.NotifyCanExecuteChanged();
            }),
        ]);
        //_subscriptions.Add(_virtualDevice.LaserColor.Subscribe(HandleColorChanged));
    }

    protected override async Task DeInitializeInternalAsync(ApplicationMode nextMode, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        if (IsProtracted && nextMode != ApplicationMode.LM)
        {
            using var msgGuard = _windowService.ShowBusyMessage(Messages.RetractingObjectiveBusyMessage);
            await _virtualDevice.RetractAsync(cancellationToken);
        }
    }

    protected override bool CanManualFocus(double change) => false;
    protected override bool CanAutoFocus() => IsProtracted && base.CanAutoFocus();
    protected override bool CanManualTilt(double change) => false;
    protected override bool CanAutoTilt() => IsProtracted && base.CanAutoFocus();

    [SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
    async partial void OnFilterTypeChanged(FilterType value)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

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
    public void SetIntensity()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        _virtualDevice.Intensity = Ratio.FromPercent(Intensity);
    }

    [RelayCommand]
    public void SetGain()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        _virtualDevice.Gain = Level.FromDecibels(Gain);
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

    public bool FieldSelectionCanExecute =>
        IsProtracted &&
        ImageWithMetadata is not null &&
        ImageWithMetadata.ImageId != Guid.Empty;

    [RelayCommand(CanExecute = nameof(FieldSelectionCanExecute))]
    public async Task FieldSelection()
    {
        // TODO : after testing add into condition ImageWithMetadata.MemoryOrigin
        if (ImageWithMetadata is not null && ImageWithMetadata!.ImageId != Guid.Empty)
        {
            var pixelSize = ImageWithMetadata.GetPixelSize();
            var imSize = ImageWithMetadata.Image.Size;
            Size imageSize = new() { Width = (int)(imSize.Width * pixelSize.X.Nanometers), Height = (int)(imSize.Height * pixelSize.Y.Nanometers) };

            if (!IsFieldSeletion)
            {
                using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

                _virtualDevice.FieldSelection();
                FieldSelectionViewModel.AddFieldSelectionArea(imageSize);
            }
            else if (FieldSelectionViewModel.FieldSelectionArea != null)
            {
                FieldSelectionArea area = FieldSelectionViewModel.FieldSelectionArea;
                FieldWidth = Length.FromNanometers(area.Width * imageSize.Width);
                FieldHeight = Length.FromNanometers(area.Height * imageSize.Height);
                var ratioPoint = new RatioPoint() { X = Ratio.FromDecimalFractions(area.X), Y = Ratio.FromDecimalFractions(area.Y) };
                var stagePosition = _stageNavigation.GetStagePositionFromImageLocation(ratioPoint, ImageWithMetadata, CameraView);

                using (_windowService.ShowBusyMessage(Messages.StageMoveBusyMessage))
                {
                    var success = await _virtualDevice.MoveStageAsync(stagePosition);
                }

                FieldSelectionViewModel.Close();
            }
        }
        else if (IsFieldSeletion)
        {
            FieldSelectionViewModel.Close();
        }

        IsFieldSeletion = !IsFieldSeletion;
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

    partial void OnFilterTypeChanged(FilterType oldValue, FilterType newValue)
    {
        PersistentImagingSettings.FilterType = newValue;
        RoiControl.ImagesStateManager.UpdateCanNavigateTo();
    }

    public async Task RestoreImageState(ImageMetadata imageMetadata)
    {
        using var activity = AppTelemetry.DeviceActivitySource.StartActivity(CreateActivityName());

        if (
            imageMetadata.GetSource() != StageCameraView.Confocal ||
            IsProtracted == false ||
            imageMetadata.ConfocalMetadata == null
        )
        {
            return;
        }

        await _virtualDevice.RestoreImageState(imageMetadata, default);

        PersistentImagingSettings.ImagingSettings.Intensity = _virtualDevice.Intensity.Percent;
        Intensity = _virtualDevice.Intensity.Percent;
    }
}
