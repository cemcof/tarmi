
namespace Tarmi.WPF;

public interface IControlBase
{
    Task OnActivated();
    Task OnDeactivated();
}
