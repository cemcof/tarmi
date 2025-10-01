using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Betrian.CflmNavi.App.ViewModels.FocusPoints;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views.FocusPoints;
/// <summary>
/// Interakční logika pro FocusPointsDialog.xaml
/// </summary>
public partial class FocusPointsWindow : DarkWindow<FocusPointsWindowViewModel>
{
    public FocusPointsWindow(FocusPointsWindowViewModel viewModel) : base(viewModel)
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
