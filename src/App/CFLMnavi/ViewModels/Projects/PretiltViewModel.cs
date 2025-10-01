using CFLMnavi.Configuration.Holders;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Betrian.CflmNavi.App.ViewModels.Projects;

public partial class PretiltViewModel : ObservableObject
{
    private readonly Holder _holder;

    public PretiltViewModel(Holder holder)
    {
        _holder = holder;
    }

    public string Name => _holder.Name;

    public double Tilt => _holder.PreTilt.Degrees;

    public Holder GetHolder() => _holder;
}
