using CFLMnavi.Projects;
using CFLMnavi.Projects.Implementation;

namespace Betrian.CflmNavi.App.ViewModels.Layers;

public class TileSetDescriptorViewModel : LayerDescriptorViewModel
{
    private readonly TileSetDescriptor _tileSetDescriptor;

    public TileSetDescriptorViewModel(TileSetDescriptor layerDescriptor, ObservableProject observableProject) : base(layerDescriptor, observableProject) 
    {
        _tileSetDescriptor = layerDescriptor;
    }

    protected override TileSetDescriptor RenameImplementation(string newName)
    {
        return _tileSetDescriptor with { Name = newName };
    }
}
