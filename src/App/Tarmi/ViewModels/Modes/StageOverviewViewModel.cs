using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Tarmi.App.Infrastructure;
using Tarmi.Devices.Thermofisher.Instrument.Types;
using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using Tarmi.VirtualDevices;
using CommunityToolkit.Mvvm.ComponentModel;
using UnitsNet;
using Tarmi.App.ViewModels.Navigation;

namespace Tarmi.App.ViewModels.Modes;

public abstract partial class StageOverviewViewModel : ObservableObject
{
    private readonly IVirtualDevice _virtualDevice;
    private readonly IStageNavigation _stageNavigation;
    private readonly IProjectManager _projectManager;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly List<IDisposable> _subscriptions = [];

    public abstract Length WorkingDistance { get; protected set; }
    public virtual bool HasLinearStage => false;

    [ObservableProperty]
    public partial LengthPoint SamplePosition { get; set; } = LengthPoint.Zero;

    [ObservableProperty]
    public partial StageState StageState { get; set; } = StageState.Zero;

    [ObservableProperty]
    public partial ActiveViewVM? ActiveViewVM { get; set; }

    [ObservableProperty]
    public partial ObservableProject? ActiveProject { get; set; }

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
