using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tarmi.App.ViewModels;

public partial class ImageViewerControlsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayScale))]
    private double _scale = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayScale))]
    private double _imageScale = 1;

    public int DisplayScale => (int)(Scale * ImageScale * 100);

    [RelayCommand]
    private void ZoomIn() => Scale *= 1.1;

    [RelayCommand]
    private void ZoomOut()
    {
        double newScale = double.Round(Scale * (1 / 1.1), 3);
        if (newScale >= 1)
        {
            Scale = newScale;
        }
    }

    [RelayCommand]
    private void Zoom100()
    {
        ZoomToFit(); // Zooming to fit first forces the view to reset any pan offset
        Scale = 1.0 / ImageScale;
    }

    [RelayCommand]
    private void ZoomToFit() => Scale = 1.0;
}
