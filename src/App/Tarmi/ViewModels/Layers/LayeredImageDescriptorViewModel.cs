using Tarmi.Projects;
using Tarmi.Projects.Implementation;

namespace Tarmi.App.ViewModels.Layers;

public class LayeredImageDescriptorViewModel : LayerDescriptorViewModel
{
    private readonly LayeredImageDescriptor _layeredImageDescriptor;

    public LayeredImageDescriptorViewModel(LayeredImageDescriptor layerDescriptor, ObservableProject observableProject) : base(layerDescriptor, observableProject) 
    {        
        _layeredImageDescriptor = layerDescriptor;
    }

    protected override LayeredImageDescriptor RenameImplementation(string newName)
    {
        return _layeredImageDescriptor with { Name = newName };
    }
}
