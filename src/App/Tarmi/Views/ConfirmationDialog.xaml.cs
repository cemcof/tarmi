using System.Windows;
using Tarmi.App.ViewModels;
using Tarmi.WPF;

namespace Tarmi.App.Views;

public partial class ConfirmationDialog : DarkWindow<ConfirmationDialogViewModel>
{
    public ConfirmationDialog(ConfirmationDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
