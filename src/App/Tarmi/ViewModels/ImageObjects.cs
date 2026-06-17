using System.Windows;
using System.Windows.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Tarmi.App.Controls;
using Tarmi.Models;
using Tarmi.Projects;

namespace Tarmi.App.ViewModels;

public partial class Fiducial : LabeledPoint
{
    public FiducialPoint? Reference { get; init; }
}

public partial class FocusPoint : LabeledPoint
{
    public Tarmi.Projects.FocusPoint? ParentFocusPoint { get; init; }

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

    [ObservableProperty]
    public partial double RealHeight { get; set; }

    [ObservableProperty]
    public partial double RealWidth { get; set; }

    public Action<Rectangle>? OnResizeFinished { get; set; }

    protected override void MoveFinished()
    {
        OnMoveFinished?.Invoke(this);
        OnResizeFinished?.Invoke(this);
    }

    [RelayCommand]
    private void ResizeFinished() => OnResizeFinished?.Invoke(this);

    protected override void ResizeOverride(double dX, double dY)
    {
        base.ResizeOverride(dX, dY);
        double newWidth = RealWidth + dX / _currentScale;
        double newHeight = RealHeight + dY / _currentScale;

        if (newWidth > 1)
        {
            RealWidth = newWidth;
        }

        if (newHeight > 1)
        {
            RealHeight = newHeight;
        }
        UpdateDimensions();
    }

    protected override void ScaleOverride(double scale)
    {
        base.ScaleOverride(scale);
        UpdateDimensions();
    }

    private void UpdateDimensions()
    {
        Width = RealWidth * _currentScale;
        Height = RealHeight * _currentScale;
    }
}

public partial class ROIPoint : LabeledPoint
{
    public RegionOfInterest? RegionOfInterest { get; set; }
}

public partial class GridCenterROIPoint : ROIPoint
{
}

public abstract partial class XYPoint : ObservableObject, IScaleAwareItem
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

    protected double _currentScale;

    public void Move(double dX, double dY)
    {
        MoveOverride(dX, dY);
    }

    public void Resize(double dX, double dY)
    {
        ResizeOverride(dX, dY);
    }

    public void Scale(double scale)
    {
        _currentScale = scale;
        ScaleOverride(scale);
    }

    protected virtual void ResizeOverride(double dX, double dY)
    { }

    protected virtual void MoveOverride(double dX, double dY)
    {
        X += dX / _currentScale;
        Y += dY / _currentScale;
    }

    protected virtual void ScaleOverride(double scale)
    {
    }
}

public partial class MillingArea : Rectangle
{
    public MillingAreaInfo? MillingAreaInfo { get; internal set; }
}

public partial class FieldSelectionArea : Rectangle
{
    public RatioRectangle Definition { get; init; } = RatioRectangle.Zero;
}
