using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Tarmi.App.Infrastructure;
using Tarmi.App.Services.Application;
using Tarmi.App.ViewModels.FocusPoints;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.App.WPF;
using Tarmi.Configuration;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.ImagePipeline.Pipelines;
using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes;

public abstract partial class BeamModeViewModelBase : VirtualDeviceViewModel
{
    private readonly IBeamMode _beamMode;

    private readonly ReadOnlyObservableCollection<ElectricCurrent> _availableBeamCurrents;
    private readonly ObservableCollectionExtended<ElectricCurrent> _availableBeamCurrentsSource = [];
    public ReadOnlyObservableCollection<ElectricCurrent> AvailableBeamCurrents => _availableBeamCurrents;
    public override StageOverviewViewModel StageOverview { get; }

    [ObservableProperty]
    public partial BeamState BeamState { get; private set; } = BeamState.Zero;

    [ObservableProperty]
    public partial DetectorState DetectorState { get; private set; } = DetectorState.Zero;

    [ObservableProperty]
    public partial ImageFilterState ImageFilterState { get; private set; } = ImageFilterState.Zero;

    [ObservableProperty]
    public partial ElectricCurrent? SelectedBeamCurrent { get; set; }

    public override TileSetGrabbingViewModel TileSetGrabbing { get; }

    protected BeamModeViewModelBase(
        ILogger logger,
        IBeamMode beamMode,
        IWindowService windowService,
        IProjectManager projectManager,
        ImagingPipeline imagingPipeline,
        IStageNavigation stageNavigation,
        ISafeStageControlling safeStageControlling,
        ApplicationConfig applicationConfig,
        ILimits limits,
        OverviewImageViewModel overviewImageViewModel,
        RoiControlViewModel roiControlViewModel,
        FocusPointControlViewModel focusPointControlViewModel
    )
        : base(logger, beamMode, windowService, projectManager, imagingPipeline, stageNavigation, safeStageControlling, limits, overviewImageViewModel, roiControlViewModel, focusPointControlViewModel, applicationConfig)
    {
        _beamMode = beamMode;

        TileSetGrabbing = new(_logger, windowService, stageNavigation, projectManager, imagingPipeline, safeStageControlling, _tileSetGrabbingService, null, null, applicationConfig, this);

        StageOverview = new StageOverviewBeamModeViewModel(beamMode, stageNavigation, safeStageControlling, projectManager);
        _ = _availableBeamCurrentsSource
            .ToObservableChangeSet()
            .ObserveOnDispatcher()
            .Bind(out _availableBeamCurrents)
            .Subscribe();
    }

    protected override Task InitializeInternalAsync(ApplicationMode prevMode, CancellationToken cancellationToken)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        BeamState = _beamMode.CurrentBeamState;
        DetectorState = _beamMode.CurrentDetectorState;
        ImageFilterState = _beamMode.CurrentImageFilterState;
        _subscriptions.Add(_beamMode.Beam.Subscribe(HandleBeamStateUpdate));
        _subscriptions.Add(_beamMode.Detector.Subscribe(HandleDetectorStateChange));
        _subscriptions.Add(_beamMode.ImageFilter.Subscribe(HandleImageFilterStateChange));
        _availableBeamCurrentsSource.AddRange(_beamMode.AvailableBeamCurrents);
        return Task.CompletedTask;
    }

    partial void OnSelectedBeamCurrentChanged(ElectricCurrent? value)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        var idx = Array.IndexOf(BeamState.BeamCurrents, value);
        if (idx != BeamState.BeamCurrentIndex)
        {
            _beamMode.SetBeamCurrentIndex(idx);
        }
    }

    private void HandleBeamStateUpdate(BeamState beamState)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());
        
        BeamState = beamState;
        // Do not update currents list, keep it fixed
        //_availableBeamCurrentsSource.Clear();
        //_availableBeamCurrentsSource.AddRange(BeamState.BeamCurrents);
        SelectedBeamCurrent = BeamState.BeamCurrents[BeamState.BeamCurrentIndex];
    }

    private void HandleDetectorStateChange(DetectorState state)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        DetectorState = state;
    }

    private void HandleImageFilterStateChange(ImageFilterState imageFilterState)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        ImageFilterState = imageFilterState;
    }
}
