using Tarmi.App.ViewModels;
using Tarmi.WPF;
using Fluxera.Extensions.Hosting;

namespace Tarmi.App.Views;

public partial class MainWindow : DarkWindow<MainWindowViewModel>, IMainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
