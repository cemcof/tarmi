using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Windows;
using Tarmi.App.Infrastructure;
using Tarmi.Imaging.Common;
using Tarmi.Models;
using Tarmi.Configuration;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using Tarmi.VirtualDevices.Implementation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UnitsNet;
using Duration = UnitsNet.Duration;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.Modes.LM;

namespace Tarmi.App.ViewModels.Modes;

public enum ZStackStepSetting
{
    VariableNumberOfSteps,
    VariableStepSize,
    SystemOptimized
}

public partial class ZStackGrabbingViewModel : ObservableObject, IZStackGrabbingViewModel, IDisposable
{
    private readonly ZStackGrabbingService _zStackGrabbingService;
    private readonly IImagingPipelineGrabber _pipelineGrabber;
    private readonly ILuminescenceMode _luminescenceMode;
    private readonly IProjectManager _projectManager;
    private readonly Func<StageCameraView> _getCameraView;
    private readonly IWindowService _windowService;
    private readonly IStageNavigation _stageNavigation;
    private readonly LuminescenceImagingViewModel _luminescenceImaging;
    private readonly CompositeDisposable _subscriptions;
    private readonly VirtualDeviceViewModel _parent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcquireZStackCommand))]
    public partial bool CanAcquireZStack { get; private set; }
    public double StepInMicrometers { get; } = 1;

    [NotifyPropertyChangedFor(nameof(RangeInMicrometers))]
    [NotifyPropertyChangedFor(nameof(StepSizeInMicrometers))]
    [ObservableProperty]
    public partial double StartPositionInMicrometers { get; set; }

    // TODO: Fill correct values.
    public RangeDescriptor<Length> ZLimits { get; }

    [NotifyPropertyChangedFor(nameof(RangeInMicrometers))]
    [ObservableProperty]
    public partial double EndPositionInMicrometers { get; set; }

    public double RangeInMicrometers
    {
        get => double.Abs(EndPositionInMicrometers - StartPositionInMicrometers);
        set
        {
            var sign = double.Sign(EndPositionInMicrometers - StartPositionInMicrometers);
            EndPositionInMicrometers = StartPositionInMicrometers + sign * value;
        }
    }

    [ObservableProperty]
    private Length _linearStagePosition;

    [ObservableProperty]
    private int _numberOfSteps = 1;

    [ObservableProperty]
    private double _stepSizeInMicrometers;

    [ObservableProperty]
    private ZStackStepSetting _zStackStepSetting;
    private bool isDisposed;

    private IDisposable? _activeRoiSubscription;
    private BehaviorSubject<bool> _isRoiSelectedSubject;

    public ZStackGrabbingViewModel(
        IWindowService windowService,
        IStageNavigation stageNavigation,
        IProjectManager projectManager,
        IImagingPipelineGrabber pipelineGrabber,
        ISafeStageControlling safeStageControlling,
        ILuminescenceMode luminescenceMode,
        ZStackGrabbingService zStackGrabbingService,
        LuminescenceImagingViewModel luminescenceImaging,
        ApplicationConfig applicationConfig,
        ILogger<ZStackGrabbingViewModel> logger,
        VirtualDeviceViewModel parent
    )
    {
        _windowService = windowService;
        _stageNavigation = stageNavigation;
        _zStackGrabbingService = zStackGrabbingService;
        _pipelineGrabber = pipelineGrabber;
        _luminescenceMode = luminescenceMode;
        _luminescenceImaging = luminescenceImaging;
        _projectManager = projectManager;
        _getCameraView = () => safeStageControlling.ActiveCameraView;
        var linearStageAlignment = applicationConfig.Microscope.Alignment.LinearStage;
        ZLimits = new()
        {
            Max = linearStageAlignment.FocusMaximum,
            Min = linearStageAlignment.FocusMinimum,
        };
        _isRoiSelectedSubject = new(false);

        // TODO: Come up with applicable values
        StartPositionInMicrometers = ZLimits.Max.Micrometers;
        EndPositionInMicrometers = ZLimits.Min.Micrometers;

        _subscriptions = [
            _isRoiSelectedSubject
                .CombineLatest(_luminescenceImaging.CanAcquireData, (isRoiSelected, canAcquireData) => isRoiSelected && canAcquireData)
                .Subscribe(canAcquire => CanAcquireZStack = canAcquire),
            _luminescenceMode.LinearStagePosition.Subscribe(position => LinearStagePosition = position),
            projectManager.ActiveProject.Subscribe(project =>
            {
                _activeRoiSubscription?.Dispose();
                _activeRoiSubscription = project?.ActiveRegionOfInterestIdChanges.Subscribe(
                    _ => _isRoiSelectedSubject.OnNext(
                        projectManager.GetActiveProject()?.GetActiveRegionOfInterest() is not null));
            })
        ];
        _parent = parent;
    }

    private static string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(ZStackGrabbingViewModel)}::{methodName}";

    [RelayCommand]
    private void SelectStepSetting(ZStackStepSetting setting)
    {
        ZStackStepSetting = setting;
    }

    [RelayCommand(CanExecute = nameof(CanAcquireZStack))]
    private async Task AcquireZStack()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        await _parent.RoiControl.ImagesStateManager.ClearState();
        using var cancellationTokenSource = new CancellationTokenSource();
        await _windowService.ShowDeterminateWaitingDialogAsync("Acquiring Z-stack",
            progress => ZStackAcquisitionImplementationAsync(progress, cancellationTokenSource.Token),
            cancellationTokenSource.Cancel);
    }

    [RelayCommand]
    private void CopyCurrentPositionToClipboard()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        Clipboard.SetText($"{LinearStagePosition.Micrometers:F0}");
    }

    [RelayCommand]
    private void UseCurrentPositionAsStartPosition()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        StartPositionInMicrometers = UnitMath.Clamp(LinearStagePosition, ZLimits.Min, ZLimits.Max).Micrometers;
    }

    [RelayCommand]
    private void UseCurrentPositionAsEndPosition()
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        EndPositionInMicrometers = UnitMath.Clamp(LinearStagePosition, ZLimits.Min, ZLimits.Max).Micrometers;
    }

    private async Task ZStackAcquisitionImplementationAsync(IProgress<(string, Ratio)> progress, CancellationToken cancellationToken)
    {
        var activeProject = _projectManager.GetActiveProject() ??
            throw new InvalidOperationException("Active project must exist for Z-stack to be acquired.");

        ZStackOptions zStackOptions = GetZStackOptions();
        
        var lightSettings = _luminescenceImaging.LightSettings
            .Where(settings => settings.IsSelected)
            .ToArray();

        if (lightSettings is [])
        {
            await _zStackGrabbingService.GrabZStackAsync(activeProject, _stageNavigation, _getCameraView.Invoke(), _pipelineGrabber, zStackOptions, progress, null, cancellationToken);
            return;
        }

        var initialColor = _luminescenceMode.ActiveLightColor;
        var initialIntensity = _luminescenceMode.Intensity;
        var initialExposure = _luminescenceMode.ExposureTime;
        var part = Ratio.FromDecimalFractions(1.0 / lightSettings.Length);
        var linkId = UUIDNext.Uuid.NewSequential();
        for (var i = 0; i < lightSettings.Length; i++)
        {
            var innerProgress = new Progress<(string Message, Ratio Percentage)>(inner => progress.Report((inner.Message, i * part + part.DecimalFractions * inner.Percentage)));
            var lightSetting = lightSettings[i];

            await _luminescenceMode.TurnLightOn(lightSetting.Color, cancellationToken);
            var percent = Ratio.FromPercent(lightSetting.ImagingSettings.Intensity);
            await _luminescenceMode.SetIntensityAsync(percent, cancellationToken);
            _luminescenceMode.ExposureTime = Duration.FromMicroseconds(lightSetting.ImagingSettings.Exposure);

            // TODO: Acquire for each light configuration after light settings are merged.
            await _zStackGrabbingService.GrabZStackAsync(activeProject, _stageNavigation, _getCameraView.Invoke(), _pipelineGrabber, zStackOptions, innerProgress, linkId, cancellationToken);
        }

        await _luminescenceMode.TurnLightOff(cancellationToken);
        await _luminescenceMode.SetIntensityAsync(initialIntensity, cancellationToken);
        if (initialColor.HasValue)
        {
            await _luminescenceMode.TurnLightOn(initialColor.Value, cancellationToken);
        }
        _luminescenceMode.ExposureTime = initialExposure;
    }

    public ZStackOptions GetZStackOptions()
    {
        int numberOfSteps;
        Length stepSize;
        switch (ZStackStepSetting)
        {
            case ZStackStepSetting.VariableNumberOfSteps:
                numberOfSteps = NumberOfSteps;
                stepSize = NumberOfSteps > 1 ? Length.FromMicrometers(RangeInMicrometers) / (NumberOfSteps - 1) : Length.Zero;
                break;
            case ZStackStepSetting.VariableStepSize:
                numberOfSteps = StepSizeInMicrometers > 0 ? (int)(RangeInMicrometers / StepSizeInMicrometers) + 1 : 1;
                stepSize = Length.FromMicrometers(StepSizeInMicrometers);
                break;
            // TODO
            case ZStackStepSetting.SystemOptimized:
            default:
                throw new NotImplementedException("System optimized settings are yet unknown.");
        }
        return new()
        {
            NumberOfSteps = numberOfSteps,
            StartPosition = Length.FromMicrometers(StartPositionInMicrometers),
            Step = stepSize
        };
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            _activeRoiSubscription?.Dispose();
            _subscriptions.Dispose();
            GC.SuppressFinalize(this);
            isDisposed = true;
        }
    }
}
