using Betrian.CflmNavi.App.ViewModels.ROIs;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views
{
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
}
