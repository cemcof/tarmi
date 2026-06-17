using System.Windows;

namespace Tarmi.App.Controls;

public class ImageControlDragEventArgs : EventArgs
{
    public Point Begin { get; init; }
    public Point Previous { get; init; }
    public Point Current { get; init; }
    public Vector Delta => Current - Previous;
}
