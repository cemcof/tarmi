using Tarmi.Models;
using Tarmi.Projects;
using Tarmi.Projects.Implementation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tarmi.App.ViewModels.Layers;

public abstract partial class LayerDescriptorViewModel : ObservableObject
{
    private readonly ObservableProject _activeProject;
    private readonly LayerDescriptor _layerDescriptor;
    protected abstract LayerDescriptor RenameImplementation(string newName);

    public StageCameraView Source => _layerDescriptor.Source;
    public int ImagesCount => _layerDescriptor.ImagesCount;

    [ObservableProperty]
    public bool _renaming;

    [ObservableProperty]
    public bool _isVisible = true;

    [ObservableProperty]
    public string _name;

    protected LayerDescriptorViewModel(LayerDescriptor layerDescriptor, ObservableProject observableProject)
    {
        _layerDescriptor = layerDescriptor;
        _activeProject = observableProject;
        _name = layerDescriptor.Name;
    }

    [RelayCommand]
    public void Remove() => _activeProject.RemoveDescriptor(_layerDescriptor);

    [RelayCommand]
    public void StartRename() => Renaming = true;

    [RelayCommand]
    public void CancelRename()
    {
        Renaming = false;
        Name = _layerDescriptor.Name;
    }

    [RelayCommand]
    public void AcceptRename()
    {
        LayerDescriptor renamedDescriptor = RenameImplementation(Name); 
        _activeProject.AddOrUpdateDescriptor(renamedDescriptor);
    }
}
