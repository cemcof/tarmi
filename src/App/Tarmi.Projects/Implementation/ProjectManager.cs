using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tarmi.App.Infrastructure.Options;
using Tarmi.Configuration;
using Tarmi.Configuration.Holders;
using Tarmi.Models.Serialization;

namespace Tarmi.Projects.Implementation;

public class ProjectManager : IProjectManager
{
    private const string RecentProjectsFileName = "recent-projects.xml";
    private const string ProjectFileName = "project.xml";

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ApplicationConfig _applicationConfig;
    private readonly string _projectsBaseDirectory;
    private readonly string _stateBaseDirectory;
    private readonly CompositeDisposable _compositeDisposable = [];
    private readonly BehaviorSubject<ObservableProject?> _activeProject = new(null);

    private readonly ObservableCollectionExtended<ProjectDescriptor> _recentProjectsSource = [];
    private readonly ReadOnlyObservableCollection<ProjectDescriptor> _recentProjects;

    private readonly ObservableCollectionExtended<ProjectDescriptor> _allProjectsSource = [];
    private readonly ReadOnlyObservableCollection<ProjectDescriptor> _allProjects;
    private readonly Lazy<ReadOnlyObservableCollection<ProjectDescriptor>> _lazyAllProjects;

    public ReadOnlyObservableCollection<ProjectDescriptor> RecentProjects => _recentProjects;
#pragma warning disable S4275 // Getters and setters should access the expected fields
    public ReadOnlyObservableCollection<ProjectDescriptor> AllProjects => _lazyAllProjects.Value;
#pragma warning restore S4275 // Getters and setters should access the expected fields

    public IObservable<ObservableProject?> ActiveProject => _activeProject.AsObservable();

    public ProjectManager(ILoggerFactory loggerFactory, ILogger<ProjectManager> logger, ApplicationConfig configuration, IOptions<AppConfigurationOptions> options, TimeProvider timeProvider, ApplicationConfig applicationConfig)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _projectsBaseDirectory = configuration.UserPreferences.ProjectsDirectory;
        _stateBaseDirectory = options.Value.StateDirectory;
        _timeProvider = timeProvider;
        _applicationConfig = applicationConfig;

        _compositeDisposable.Add(
            _recentProjectsSource
                .ToObservableChangeSet()
                .Bind(out _recentProjects)
                .Subscribe()
        );
        LoadRecentProjects();

        _compositeDisposable.Add(
            _allProjectsSource
            .ToObservableChangeSet()
            .Bind(out _allProjects)
            .Subscribe()
            );
        _lazyAllProjects = new(() =>
        {
            LoadAllProjects(configuration.UserPreferences.ProjectsDirectory);
            return _allProjects;
        });
    }

    private void LoadAllProjects(string projectsDirectory)
    {
        _ = Directory.CreateDirectory(projectsDirectory);
        _allProjectsSource.Clear();
        foreach (var directory in Directory.EnumerateDirectories(projectsDirectory))
        {
            if (TryLoadProject(directory, out var project))
            {
                _allProjectsSource.Add(project);
            }
        }
    }

    private bool TryLoadProject(string directory, [NotNullWhen(true)] out ProjectDescriptor? project)
    {
        try
        {
            var path = Path.Combine(directory, ProjectFileName);
            project = DeserializeFromFile<ProjectDescriptor>(path);
            project.Directory = directory;
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to load project at {Path}.", directory);
        }
        project = null;
        return false;
    }

    public ObservableProject? GetActiveProject() => _activeProject.Value;

    public Holder[] GetHolders()
    {
        var holdersConfig = ConfigSerialization.LoadHoldersConfig();
        return holdersConfig.Holders;
    }

    public Project CreateProject(string name, string description, Holder holder)
    {
        _ = Directory.CreateDirectory(_projectsBaseDirectory);
        var directory = Path.Join(_projectsBaseDirectory, NameToFilename(name));
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
        var currentTime = _timeProvider.GetLocalNow();
        var project = new Project()
        {
            Directory = directory,
            Name = name,
            Description = description,
            TimeOfCreation = currentTime,
            TimeOfAccess = currentTime,
            Holder = holder
        };

        foreach (var grid in holder.Grids)
        {
            var point = grid.GetDefaultViewPosition();
            var roi = new GridCenterRegionOfInterest
            {
                Position = point,
                Id = UUIDNext.Uuid.NewSequential(),
                Name = $"{grid.Name} Center",
                GridName = grid.Name
            };
            project.RegionsOfInterest.Add(roi);
        }

        SaveProject(project);
        AddOrUpdateRecentProject(project);

        return project;
    }

    private void AddOrUpdateRecentProject(ProjectDescriptor descriptor)
    {
        var comparer = EqualityComparerFactory.CreateKeyed<ProjectDescriptor, string>(descriptor => descriptor.Directory);
        var index = _recentProjectsSource.IndexOf(descriptor, comparer);
        if (index == -1)
        {
            _recentProjectsSource.Add(descriptor);
        }
        else
        {
            _recentProjectsSource[index] = descriptor;
        }
        SaveRecentProjects();
    }

    public void DeleteProject(ProjectDescriptor project, bool deleteFiles = false)
    {
        if (deleteFiles)
        {
            Directory.Delete(project.Directory, true);
        }
        if (DeleteProjectFromCollection(_recentProjectsSource, project))
        {
            SaveRecentProjects();
        }
        _ = DeleteProjectFromCollection(_allProjectsSource, project);
    }

    private static bool DeleteProjectFromCollection(Collection<ProjectDescriptor> collection, ProjectDescriptor project)
    {
        var comparer = EqualityComparerFactory.Create<ProjectDescriptor>(
            (project1, project2) => project1?.Directory == project2?.Directory
        );
        var index = collection.IndexOf(project, comparer);
        if (index >= 0)
        {
            collection.RemoveAt(index);
            return true;
        }
        return false;
    }

    public void OpenProject(ProjectDescriptor descriptor)
    {
        var path = Path.Join(descriptor.Directory, ProjectFileName);
        var project = DeserializeFromFile<Project>(path);
        project.TimeOfAccess = _timeProvider.GetLocalNow();
        project.Directory = descriptor.Directory;
        SaveProject(project);
        AddOrUpdateRecentProject(project);
        var observableProject = new ObservableProject(this, project, _loggerFactory.CreateLogger<ObservableProject>(), _applicationConfig);
        _activeProject.OnNext(observableProject);
    }

    public void SaveProject(Project project)
    {
        if (!Directory.Exists(project.Directory))
        {
            _ = Directory.CreateDirectory(project.Directory);
        }
        var path = Path.Join(project.Directory, ProjectFileName);
        SerializeToFile(path, project);
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
        GC.SuppressFinalize(this);
    }

    private void LoadRecentProjects()
    {
        _recentProjectsSource.Clear();
        var recentProjectDirectories = LoadRecentProjectDirectories();
        foreach (var directory in recentProjectDirectories)
        {
            if (TryLoadProject(directory, out var project))
            {
                _recentProjectsSource.Add(project);
            }
        }
    }

    private IEnumerable<string> LoadRecentProjectDirectories()
    {
        _ = Directory.CreateDirectory(_stateBaseDirectory);
        var recentProjectsPath = Path.Combine(_stateBaseDirectory, RecentProjectsFileName);
        if (!File.Exists(recentProjectsPath))
        {
            _logger.LogInformation("Creating empty recent projects.");
            SerializeToFile(recentProjectsPath, []);
        }
        return DeserializeFromFile<List<string>>(recentProjectsPath)
            .Distinct(StringComparer.InvariantCultureIgnoreCase);
    }

    private static void SerializeToFile(string path, List<string> strings)
    {
        using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        Helpers.Save(strings, file);
    }

    private static void SerializeToFile(string path, Project project)
    {
        using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        Helpers.Save(project, file);
    }

    private void SaveRecentProjects()
    {
        var directories = _recentProjects
            .Select(project => project.Directory)
            .ToList();
        var path = Path.Combine(_stateBaseDirectory, RecentProjectsFileName);
        SerializeToFile(path, directories);
    }

    private static string NameToFilename(string name, char fill = '-')
    {
        return Path
            .GetInvalidFileNameChars()
            .Aggregate(new StringBuilder(name), (builder, invalid) => builder.Replace(invalid, fill))
            .ToString();
    }

    private static T DeserializeFromFile<T>(string path) where T : class
    {
        string xml;
        {
            using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(file);
            xml = reader.ReadToEnd();
        }

        xml = Helpers.NormalizeXmlString(xml);

        using TextReader xmlStream = new StringReader(xml);
        var xmlReader = new XmlTextReader(xmlStream);
        var serializer = new DataContractSerializer(typeof(T));
        return serializer.ReadObject(xmlReader) as T ??
            throw new InvalidDataException($"Failed to deserialize {typeof(T)} from '{path}'.");
    }

    public bool ProjectExists(string name)
    {
        _ = Directory.CreateDirectory(_projectsBaseDirectory);
        var directory = Path.Join(_projectsBaseDirectory, NameToFilename(name));
        return Directory.Exists(directory);
    }
}
