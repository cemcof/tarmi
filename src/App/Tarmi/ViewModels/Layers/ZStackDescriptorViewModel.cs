using Tarmi.Projects;
using Tarmi.Projects.Implementation;

namespace Tarmi.App.ViewModels.Layers;

public class ZStackDescriptorViewModel : LayerDescriptorViewModel
{
    private readonly ZStackDescriptor _zStackDescriptor;

    public ZStackDescriptorViewModel(ZStackDescriptor layerDescriptor, ObservableProject observableProject) : base(layerDescriptor, observableProject)
    {
        _zStackDescriptor = layerDescriptor;
    }

    protected override ZStackDescriptor RenameImplementation(string newName)
    {
        return _zStackDescriptor with { Name = newName };
    }
}
