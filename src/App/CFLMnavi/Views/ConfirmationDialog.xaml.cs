using System.Windows;
using Betrian.CflmNavi.App.ViewModels;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views;

public partial class ConfirmationDialog : DarkWindow<ConfirmationDialogViewModel>
{
    public ConfirmationDialog(ConfirmationDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
