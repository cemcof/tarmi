namespace CFLMnavi.WPF.Controls;

public interface IApplicationModeControlBase
{
    Task OnActivated(ApplicationMode prevMode);
    Task OnDeactivated(ApplicationMode nextMode);
}
