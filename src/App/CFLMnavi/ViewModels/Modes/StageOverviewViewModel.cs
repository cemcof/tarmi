using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Betrian.App.Infrastructure;
using Betrian.Devices.Thermofisher.Instrument.Types;
using Betrian.Models;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CFLMnavi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes;

public abstract partial class StageOverviewViewModel : ObservableObject
{
    private const double PixelRadius = 140;

    private readonly IVirtualDevice _virtualDevice;
    private readonly IStageNavigation _stageNavigation;
    private readonly IProjectManager _projectManager;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly List<IDisposable> _subscriptions = [];

    public abstract Length WorkingDistance { get; protected set; }
    public virtual bool HasLinearStage => false;

    [ObservableProperty]
    private LengthPoint _samplePosition = LengthPoint.Zero;

    [ObservableProperty]
    private StageState _stageState = StageState.Zero;

    [ObservableProperty]
    private ActiveViewVM? _activeViewVM;

    [ObservableProperty]
    private ObservableProject? _activeProject;

    protected StageOverviewViewModel(IVirtualDevice virtualDevice, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IProjectManager projectManager)
    {
        _virtualDevice = virtualDevice;
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _projectManager = projectManager;
    }

    public Task Initialize(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        StageState = _virtualDevice.StageState;
        _subscriptions.Add(_projectManager.ActiveProject.Subscribe(HandleActiveProjectChange));
        _subscriptions.Add(_virtualDevice.Stage.Subscribe(HandleLinearStagePositionChange));
        ActiveProject = _projectManager.GetActiveProject();
        return Task.CompletedTask;
    }

    public Task DeInitialize()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        return Task.CompletedTask;
    }

    private void HandleActiveProjectChange(ObservableProject? project) => ActiveProject = project;

    private void HandleLinearStagePositionChange(StageState state)
    {
        using var activity = AppTelemetry.UiActivitySource.StartActivity(CreateActivityName());

        StageState = state;
        SamplePosition = _stageNavigation.GetPlanePosition(StageState.CurrentPosition, _safeStageControlling.ActiveCameraView);
    }
    private string CreateActivityName([CallerMemberName] string methodName = "")
        => $"{nameof(StageOverviewViewModel)}::{methodName}";
}
