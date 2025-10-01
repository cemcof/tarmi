using Betrian.CflmNavi.App.ViewModels;
using Betrian.WPF;
using Fluxera.Extensions.Hosting;

namespace Betrian.CflmNavi.App.Views;

public partial class MainWindow : DarkWindow<MainWindowViewModel>, IMainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
