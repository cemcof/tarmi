using System.Collections.ObjectModel;
using Tarmi.WPF;
using Tarmi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tarmi.App.Services.Application;

namespace Tarmi.App.ViewModels.Projects;

public partial class CreateNewProjectDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly IProjectManager _projectManager;
    private readonly IWindowService _windowService;

    public CreateNewProjectDialogViewModel(IProjectManager projectManager, IWindowService windowService)
    {
        _projectManager = projectManager;
        _windowService = windowService;

        _pretiltSettings = new(
            _projectManager
                .GetHolders()
                .Select(holder => new PretiltViewModel(holder))
        );

        _selectedPretiltSetting = PretiltSettings[0];
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _projectDescription = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PretiltViewModel> _pretiltSettings;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private PretiltViewModel _selectedPretiltSetting;

    public event EventHandler<bool>? CloseRequested;

    public Project? CreatedProject { get; internal set; }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    [RelayCommand(CanExecute = nameof(StartNewProjectCanExecute))]
    private void CreateProject()
    {
        if (_projectManager.ProjectExists(ProjectName) && !_windowService.ShowConfirmationDialog(
            "Existing project!",
            "Project directory already exists.\n" +
            "Would you like to remove existing project files?",
            "Yes", "No"))
        {
            return;
        }
        CreatedProject = _projectManager.CreateProject(ProjectName, ProjectDescription, SelectedPretiltSetting.GetHolder());
        CloseRequested?.Invoke(this, true);
    }

    private bool StartNewProjectCanExecute() => ProjectName.IsNotNullOrWhiteSpace();
}
