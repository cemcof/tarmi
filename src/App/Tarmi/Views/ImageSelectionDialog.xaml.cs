using Tarmi.App.ViewModels.ROIs;
using Tarmi.WPF;

namespace Tarmi.App.Views;

/// <summary>
/// Interakční logika pro ImageSelectionDialog.xaml
/// </summary>
public partial class ImageSelectionDialog : DarkDialog<ImageSelectionDialogViewModel>
{
    public ImageSelectionDialog(ImageSelectionDialogViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
