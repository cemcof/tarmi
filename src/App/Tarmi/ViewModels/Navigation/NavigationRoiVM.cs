using Tarmi.Projects.Implementation;

using Tarmi.VirtualDevices;

using CommunityToolkit.Mvvm.Input;
using Tarmi.App.Services.Application;
using Tarmi.App.Infrastructure;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.ViewModels.Navigation;

public partial class NavigationRoiVM : ObservableProjectVMBase
{
    public double X { get; init; }
    public double Y { get; init; }

    private readonly Tarmi.Projects.RegionOfInterest _roi;
    private readonly IStageNavigation _stageNavigation;
    private readonly ISafeStageControlling _safeStageControlling;
    private readonly IWindowService _windowService;

    public List<object> Markers { get; set; } = [];

    public List<RoiChildVM> RoiChildVMs { get; set; } = [];

    public NavigationRoiVM(ObservableProject observableProject, Tarmi.Projects.RegionOfInterest regionOfInterest, IStageNavigation stageNavigation, ISafeStageControlling safeStageControlling, IWindowService windowService)
        : base(observableProject)
    {
        _roi = regionOfInterest;
        _stageNavigation = stageNavigation;
        _safeStageControlling = safeStageControlling;
        _windowService = windowService;
        Name = _roi.Name;
    }

    [RelayCommand]
    private async Task NavigateToRoi()
    {
        var position = _stageNavigation.GetStagePosition(_roi.Position, _safeStageControlling.ActiveCameraView);
        using (_windowService.ShowBusyMessage(Messages.StageMoveBusyMessage))
        {
            _ = await _safeStageControlling.MoveStageAsync(position);
        }
    }
}
