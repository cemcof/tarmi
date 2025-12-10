using Tarmi.Projects;
using Tarmi.VirtualDevices;
using UnitsNet;

namespace Tarmi.App.ViewModels.Modes.Confocal;

public partial class StageOverviewConfocalModeViewModel : StageOverviewViewModel
{
    public override Length WorkingDistance { get; protected set; } = Length.FromMillimeters(0);

    public override bool HasLinearStage => true;

    public StageOverviewConfocalModeViewModel(IConfocalMode virtualDevice, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IProjectManager projectManager)
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
