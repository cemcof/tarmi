#if IMPLEMENT_LATER
using System.Collections.ObjectModel;
using System.Reactive;
using Betrian.WPF;
using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;

namespace Betrian.CflmNavi.App.ViewModels.Layers;

public partial class LayersViewModel : ViewModelBase
{
    private readonly IProjectManager _projectManager;
    private readonly List<IDisposable> _subscriptions = [];
    private ObservableProject? _activeProject;

    [ObservableProperty]
    public ObservableCollection<LayerDescriptorViewModel> _layers = [];

    public LayersViewModel(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    protected override Task InitializeCoreAsync()
    {
        _subscriptions.Add(_projectManager.ActiveProject.Subscribe(HandleActiveProjectChange));
        return Task.CompletedTask;
    }

    protected override Task DeInitializeCoreAsync()
    {
        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();
        return base.DeInitializeCoreAsync();
    }

    private void HandleActiveProjectChange(ObservableProject? project)
    {
        _activeProject = project;
        if (_activeProject is not null)
        {
            _subscriptions.Add(_activeProject.ImagesChanges.Subscribe(HandleLayerUpdate));
            _subscriptions.Add(_activeProject.TileSetsChanges.Subscribe(HandleLayerUpdate));
            _subscriptions.Add(_activeProject.ZStacksChanges.Subscribe(HandleLayerUpdate));
        }
        UpdateLayers();
    }

    private void HandleLayerUpdate(Unit unit)
    {
        UpdateLayers();
    }

    private void UpdateLayers()
    {
        if (_activeProject is not null)
        {
            Layers.Clear();
            Layers.AddRange(_activeProject.Images.Select(img => new LayeredImageDescriptorViewModel(img, _activeProject)));
            Layers.AddRange(_activeProject.TileSets.Select(img => new TileSetDescriptorViewModel(img, _activeProject)));
            Layers.AddRange(_activeProject.ZStacks.Select(img => new ZStackDescriptorViewModel(img, _activeProject)));
        }
    }
}
#endif // IMPLEMENT_LATER
