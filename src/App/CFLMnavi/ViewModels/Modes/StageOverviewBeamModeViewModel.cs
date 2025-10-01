using Betrian.Devices.Thermofisher.Instrument.Types;

using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes.SEM;

public partial class StageOverviewBeamModeViewModel : StageOverviewViewModel
{
    private readonly IBeamMode _beamMode;

    public override Length WorkingDistance { get; protected set; }

    public StageOverviewBeamModeViewModel(IBeamMode beamMode, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IProjectManager projectManager)
        : base(beamMode, stageNavigation, safeStageControlling, projectManager)
    {
        _beamMode = beamMode;
        _ = _beamMode.Beam.Subscribe(HandleBeamStateChange);
    }

    private void HandleBeamStateChange(BeamState beamState)
    {
        WorkingDistance = beamState.FreeWorkingDistance;
        OnPropertyChanged(nameof(WorkingDistance));
    }
}
