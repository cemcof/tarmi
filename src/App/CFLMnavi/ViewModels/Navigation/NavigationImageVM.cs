using System.Windows.Media.Imaging;

namespace Betrian.CflmNavi.App.ViewModels;

public class NavigationImageVM
{
    public required BitmapSource Image { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }

}
