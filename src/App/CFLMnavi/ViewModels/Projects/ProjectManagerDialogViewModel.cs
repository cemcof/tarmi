using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Betrian.CflmNavi.App.Services.Application;
using Betrian.WPF;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Betrian.CflmNavi.App.ViewModels.Projects;

public enum ShownProjects
{
    Recent,
    All
}

public partial class ProjectManagerDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly IProjectManager _projectManager;
    private readonly IWindowService _windowService;
    private readonly Lazy<ICollectionView> _recentProjectsView;
    private readonly Lazy<ICollectionView> _allProjectsView;

    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Projects))]
    public partial ShownProjects ShownProjects { get; set; }

    public ProjectDescriptor? SelectedProject { get; private set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string oldValue, string newValue) => Projects.Refresh();

    public ICollectionView Projects => ShownProjects switch
    {
        ShownProjects.Recent => _recentProjectsView.Value,
        ShownProjects.All => _allProjectsView.Value,
        _ => throw new InvalidOperationException()
    };

    private ICollectionView CreateProjectsCollectionView(ReadOnlyObservableCollection<ProjectDescriptor> collection)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(collection);
        view.Filter = IsSearchedProject;
        var sortDescription = new SortDescription(nameof(ProjectDescriptor.TimeOfAccess), ListSortDirection.Descending);
        view.SortDescriptions.Add(sortDescription);
        return view;
    }

    private bool IsSearchedProject(object obj)
    {
        return obj is ProjectDescriptor project && project.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase);
    }

    public ProjectManagerDialogViewModel(IProjectManager projectManager, IWindowService windowService)
    {
        _projectManager = projectManager;
        _windowService = windowService;

        _recentProjectsView = new(() => CreateProjectsCollectionView(_projectManager.RecentProjects));
        _allProjectsView = new(() => CreateProjectsCollectionView(_projectManager.AllProjects));
    }

    [RelayCommand]
    private void DeleteProject(ProjectDescriptor descriptor)
    {
        _projectManager.DeleteProject(descriptor);
    }

    [RelayCommand]
    private void SelectProject(ProjectDescriptor descriptor)
    {
        SelectedProject = descriptor;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void CreateNewProject()
    {
        SelectedProject = _windowService.ShowProjectCreationDialog();
        if (SelectedProject is null)
        {
            return;
        }
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    [RelayCommand]
    private void SwitchProjectsShown()
    {
        ShownProjects = ShownProjects == ShownProjects.Recent ? ShownProjects.All : ShownProjects.Recent;
    }
}
