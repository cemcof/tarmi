using System.Windows.Controls.Primitives;
using Tarmi.App.ViewModels.Projects;
using Tarmi.WPF;

namespace Tarmi.App.Views.Projects;

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
