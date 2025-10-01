using System.Windows;
using System.Windows.Controls;

using Betrian.CflmNavi.App.ViewModels;
using Betrian.CflmNavi.App.ViewModels.ROIs;

namespace Betrian.CflmNavi.App.Views;

/// <summary>
/// Interaction logic for ROIs.xaml
/// </summary>
public partial class ROIs : UserControl
{
    public RoiControlViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty) as RoiControlViewModel;
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(RoiControlViewModel), typeof(ROIs), new PropertyMetadata());

    public ROIs()
    {
        InitializeComponent();
    }
}
