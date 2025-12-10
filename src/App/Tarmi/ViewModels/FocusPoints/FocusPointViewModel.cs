using CommunityToolkit.Mvvm.ComponentModel;
using UnitsNet;

namespace Tarmi.App.ViewModels.FocusPoints;
public partial class FocusPointViewModel : ObservableObject
{
    public Tarmi.Projects.FocusPoint FocusPoint { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private Length _x;
    [ObservableProperty]
    private Length _y;

    [ObservableProperty]
    private Length? _z;


    public FocusPointViewModel(Tarmi.Projects.FocusPoint focusPoint)
    {
        FocusPoint = focusPoint;

        _x = focusPoint.PlaneLocation.X;
        _y = focusPoint.PlaneLocation.Y;
        _z = focusPoint.Z;
    }
}
