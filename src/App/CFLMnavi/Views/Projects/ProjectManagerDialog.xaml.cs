using System.Windows.Controls.Primitives;
using Betrian.CflmNavi.App.ViewModels.Projects;
using Betrian.WPF;

namespace Betrian.CflmNavi.App.Views.Projects;

public partial class ProjectManagerDialog : DarkDialog<ProjectManagerDialogViewModel>
{
    public ProjectManagerDialog()
    {
        InitializeComponent();
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e)
    {
        Left = Left + e.HorizontalChange;
        Top = Top + e.VerticalChange;
    }
}
