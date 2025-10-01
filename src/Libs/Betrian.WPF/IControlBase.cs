
namespace Betrian.WPF;

public interface IControlBase
{
    Task OnActivated();
    Task OnDeactivated();
}