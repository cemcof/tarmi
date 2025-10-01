using System.ComponentModel;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;

namespace Betrian.WPF;

public class DarkWindow<TViewModel> : Window where TViewModel : class, IViewModel
{
    private IServiceScope? _serviceScope;
    public TViewModel? ViewModel => DataContext as TViewModel;
    private readonly TViewModel? _presetViewModel;

    public DarkWindow()
    {
        Loaded += DarkWindow_Loaded;
        Unloaded += DarkWindow_Unloaded;
    }

    public DarkWindow(TViewModel viewModel) : this()
    {
        _presetViewModel = viewModel;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        NativeHelper.TrySetDarkMode(this);
    }

    private async void DarkWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            return;
        }
        
        TViewModel? viewModel = _presetViewModel;
        
        if(viewModel is null)
        {
            _serviceScope = WPFDependencyInjection.ServiceProvider.CreateScope();
            viewModel = _serviceScope.ServiceProvider.GetRequiredService<TViewModel>();
        }
        
        await viewModel.Initialize();

        DataContext = viewModel;
    }

    private async void DarkWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.DeInitialize();
        }
        _serviceScope?.Dispose();
    }
}
