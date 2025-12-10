using System.ComponentModel;
using System.Windows.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace Tarmi.WPF;

public class ControlBase<TViewModel> : UserControl, IControlBase
    where TViewModel : class, IViewModel
{
    private IServiceScope? _serviceScope;
    public TViewModel? ViewModel => DataContext as TViewModel;

    public async Task OnActivated()
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        _serviceScope = WPFDependencyInjection.ServiceProvider.CreateScope();

        TViewModel viewModel = _serviceScope.ServiceProvider.GetRequiredService<TViewModel>();
        await viewModel.Initialize();

        DataContext = viewModel;
    }

    public async Task OnDeactivated()
    {
        if (ViewModel != null)
        {
            await ViewModel.DeInitialize();
        }
        _serviceScope?.Dispose();
    }
}
