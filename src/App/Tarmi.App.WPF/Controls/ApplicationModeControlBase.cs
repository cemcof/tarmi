using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tarmi.App.WPF.ViewModels;
using Tarmi.WPF;

namespace Tarmi.App.WPF.Controls;

public class ApplicationModeControlBase<TViewModel> : UserControl, IApplicationModeControlBase
    where TViewModel : class, IApplicationModeViewModel
{
    private IServiceScope? _serviceScope;
    public TViewModel? ViewModel => DataContext as TViewModel;

    public async Task OnActivated(ApplicationMode prevMode)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }

        _serviceScope = WPFDependencyInjection.ServiceProvider.CreateScope();

        TViewModel viewModel = _serviceScope.ServiceProvider.GetRequiredService<TViewModel>();
        await viewModel.Initialize(prevMode);

        DataContext = viewModel;
    }

    public async Task OnDeactivated(ApplicationMode nextMode)
    {
        if (ViewModel != null)
        {
            await ViewModel.DeInitialize(nextMode);
        }
        _serviceScope?.Dispose();
    }
}
