using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tarmi.Imaging.Common;
using Tarmi.WPF;

namespace Tarmi.App.ViewModels.Modes.Viewer;

public partial class MainAreaViewModel : ViewModelBase
{
    [ObservableProperty]
    private ImageWithMetadata? _sampleImage;

    public List<XYPoint> DisplayItems { get; } =
    [
        new ROIPoint() { X = 100, Y = 100, Label = "First ROI" },
        new ROIPoint() { X = 1220, Y = 650, Label = "Second ROI", IsInteractive = false },
        new FocusPoint() { X = 250, Y = 535 },
        new Fiducial() { X = 900,  Y = 350, Label = "Fiducial 1" },
    ];

    public List<MillingArea> MillingAreas { get; } = [
        new MillingArea() { X = 300, Y = 300, Width = 100, Height = 30, Label = "Mill 1" },
        new MillingArea() { X = 300, Y = 350, Width = 100, Height = 30, Label = "Mill 2" },
    ];

    [RelayCommand]
    private void AreaClick(Point position)
    { 
        // is executed when user click somewhere in the area
    }

    [RelayCommand]
    private void Focus(double change)
    {
        // is executed when user slides the focus slider
    }

    [RelayCommand]
    private void Tilt(double change)
    {
        // is executed when user slides the tilt slider
    }

    protected override Task InitializeCoreAsync()
    {
        return Task.CompletedTask;
    }
}
