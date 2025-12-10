using System.Windows;
using Tarmi.App.WPF;

namespace Tarmi.App.ViewModels.Modes.Viewer;

public class ModeItem
{
    public ApplicationMode Mode { get; set; }
    public string Display { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public Visibility Visibility { get; set; } = Visibility.Visible;
}
