using Tarmi.App.ViewModels;
using Tarmi.WPF;

namespace Tarmi.App.Views;

public partial class WaitingDialog : DarkWindow<WaitingDialogViewModel>
{
    public WaitingDialog(WaitingDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
