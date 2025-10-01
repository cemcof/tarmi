using System.Reactive.Linq;
using Betrian.WPF;

using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Betrian.CflmNavi.App.ViewModels.Modes.Viewer
{
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
}
