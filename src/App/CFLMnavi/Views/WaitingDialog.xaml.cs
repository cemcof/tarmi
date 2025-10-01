using Betrian.CflmNavi.App.ViewModels;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views;

public partial class WaitingDialog : DarkWindow<WaitingDialogViewModel>
{
    public WaitingDialog(WaitingDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
