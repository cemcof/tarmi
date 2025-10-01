using CFLMnavi.Projects;
using CFLMnavi.VirtualDevices;
using UnitsNet;

namespace Betrian.CflmNavi.App.ViewModels.Modes.LM;

public partial class StageOverviewLMModeViewModel : StageOverviewViewModel
{
    public override Length WorkingDistance { get; protected set; } = Length.FromMillimeters(0);

    public override bool HasLinearStage => true;

    public StageOverviewLMModeViewModel(ILuminescenceMode virtualDevice, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IProjectManager projectManager)
        : base(virtualDevice, stageNavigation, safeStageControlling, projectManager)
    {
        _ = virtualDevice.LinearStagePosition.Subscribe(HandleLinearStagePositionChange);
    }

    private void HandleLinearStagePositionChange(Length position)
    {
        WorkingDistance = position;
        OnPropertyChanged(nameof(WorkingDistance));
    }
}
