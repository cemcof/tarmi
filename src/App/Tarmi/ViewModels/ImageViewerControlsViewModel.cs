using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tarmi.Imaging.Common;

namespace Tarmi.App.ViewModels;

public partial class ImageViewerControlsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayScale))]
    [NotifyPropertyChangedFor(nameof(SecondaryScale))]
    public partial double Scale { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayScale))]
    public partial double ImageScale { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecondaryScale))]
    [NotifyPropertyChangedFor(nameof(PrimaryMinScale))]
    [NotifyPropertyChangedFor(nameof(PrimaryMaxScale))]
    [NotifyPropertyChangedFor(nameof(SecondaryMinScale))]
    [NotifyPropertyChangedFor(nameof(SecondaryMaxScale))]
    private partial double ScaleRatio { get; set; } = 1;

    private const double DefaultMinScale = 0.01;
    private const double DefaultMaxScale = 100;

    public double PrimaryMinScale => DefaultMinScale / Math.Min(ScaleRatio, 1);
    public double PrimaryMaxScale => DefaultMaxScale / Math.Max(ScaleRatio, 1);
    public double SecondaryMinScale => DefaultMinScale * Math.Max(ScaleRatio, 1);
    public double SecondaryMaxScale => DefaultMaxScale * Math.Min(ScaleRatio, 1);

    public double SecondaryScale
    {
        get => ScaleRatio * Scale;
        set => Scale = value / ScaleRatio;
    }

    public void UpdatePixelRatio(ImageMetadata? primary, ImageMetadata? secondary)
    {
        if (primary is null || secondary is null)
        {
            ScaleRatio = 1;
            return;
        }
        ScaleRatio = secondary.GetFieldSize().Width / primary.GetFieldSize().Width;
    }

    public int DisplayScale => (int)(Scale * ImageScale * 100);

    private const double ZoomFactor = 1.1;

    [RelayCommand]
    private void ZoomIn() => Scale *= ZoomFactor;

    [RelayCommand]
    private void ZoomOut() => Scale /= ZoomFactor;

    [RelayCommand]
    private void Zoom100()
    {
        ZoomToFit(); // Zooming to fit first forces the view to reset any pan offset
        Scale = 1.0 / ImageScale;
    }

    [RelayCommand]
    private void ZoomToFit() => Scale = 1.0;
}
