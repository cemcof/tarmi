using System.Collections.ObjectModel;
using Tarmi.Models;
using Tarmi.Configuration.Holders;
using Tarmi.Projects.Implementation;

namespace Tarmi.Projects;

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
