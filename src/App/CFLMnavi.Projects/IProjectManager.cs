using System.Collections.ObjectModel;
using Betrian.Models;
using CFLMnavi.Configuration.Holders;
using CFLMnavi.Projects.Implementation;

namespace CFLMnavi.Projects;

public interface IProjectManager : IDisposable
{
    IObservable<ObservableProject?> ActiveProject { get; }
    ObservableProject? GetActiveProject();
    ReadOnlyObservableCollection<ProjectDescriptor> RecentProjects { get; }
    ReadOnlyObservableCollection<ProjectDescriptor> AllProjects { get; }
    public bool ProjectExists(string name);
    Project CreateProject(string name, string description, Holder holder);
    void DeleteProject(ProjectDescriptor project, bool deleteFiles = false);
    Holder[] GetHolders();
    void OpenProject(ProjectDescriptor descriptor);
    void SaveProject(Project project);
}
