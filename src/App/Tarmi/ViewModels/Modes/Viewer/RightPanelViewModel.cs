using System.Reactive.Linq;
using Tarmi.WPF;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Tarmi.App.ViewModels.Modes.Viewer;

public partial class RightPanelViewModel : ViewModelBase
{
    private IProjectManager _projectManager;

    [ObservableProperty]
    private ObservableProject? _project;

    public RightPanelViewModel(IProjectManager projectManager)
    {
        _projectManager = projectManager;
        _disposables.Add(_projectManager.ActiveProject.ObserveOnDispatcher().Subscribe(HandleProjectChange));
    }

    private void HandleProjectChange(ObservableProject? project)
    {
        Project = project;
    }
   
}
