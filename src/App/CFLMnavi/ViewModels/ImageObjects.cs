using Betrian.Models;
using CFLMnavi.Projects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Betrian.CflmNavi.App.ViewModels;

public partial class Fiducial : LabeledPoint
{
    public FiducialPoint? Reference { get; init; }
}

public partial class FocusPoint : LabeledPoint
{
    public CFLMnavi.Projects.FocusPoint? ParentFocusPoint { get; init; }

    [ObservableProperty]
    public partial bool IsAutoFocused { get; set; }
}

public partial class LabeledPoint : XYPoint
{
    [ObservableProperty]
    public partial string? Label { get; set; }
}

public abstract partial class Rectangle : LabeledPoint
{
    [ObservableProperty]
    public partial double Height { get; set; }

    [ObservableProperty]
    public partial double Width { get; set; }

    public Action<Rectangle>? OnResizeFinished { get; set; }

    protected override void MoveFinished()
    {
        OnMoveFinished?.Invoke(this);
        OnResizeFinished?.Invoke(this);
    }

    [RelayCommand]
    private void ResizeFinished() => OnResizeFinished?.Invoke(this);
}

public partial class ROIPoint : LabeledPoint
{
    public RegionOfInterest? RegionOfInterest { get; set; }
}

public partial class GridCenterROIPoint : ROIPoint
{
}

public abstract partial class XYPoint : ObservableObject
{
    public Action<XYPoint>? OnMoveFinished { get; set; }

    [ObservableProperty]
    public partial bool IsInteractive { get; set; } = true;

    [ObservableProperty]
    public partial double X { get; set; }

    [ObservableProperty]
    public partial double Y { get; set; }

    [RelayCommand]
    protected virtual void MoveFinished() => OnMoveFinished?.Invoke(this);
}

public partial class MillingArea : Rectangle
{
    public MillingAreaInfo? MillingAreaInfo { get; internal set; }
}
