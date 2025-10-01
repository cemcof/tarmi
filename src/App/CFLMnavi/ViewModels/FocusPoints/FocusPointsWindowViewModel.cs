using System.Collections.ObjectModel;
using Betrian.CflmNavi.App.ViewModels.Modes;
using Betrian.CflmNavi.App.ViewModels.ROIs;
using Betrian.WPF;
using CFLMnavi.Projects.Implementation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Betrian.CflmNavi.App.ViewModels.FocusPoints;

public partial class FocusPointsWindowViewModel : ViewModelBase
{
    private readonly ObservableProject? _project;
    private readonly TileSetVirtualChildVM _tileSetViewModel;

    [ObservableProperty]
    private ObservableCollection<FocusPointViewModel> _focusPoints;

    [ObservableProperty]
    private bool _areAllFocusPointsSelected;

    // Required by the type construction validation
    public FocusPointsWindowViewModel()
    {
        throw new NotImplementedException();
    }

    public FocusPointsWindowViewModel(ObservableProject project, TileSetVirtualChildVM tileSetViewModel)
    {
        _project = project;
        _tileSetViewModel = tileSetViewModel;
        _focusPoints = [.. _tileSetViewModel.Descriptor.GrabbingOptions.FocusPoints.Select(fp => new FocusPointViewModel(fp))];
    }

    [RelayCommand]
    private async Task RemoveFocusPoint(FocusPointViewModel focusPoint)
    {
        _ = FocusPoints?.Remove(focusPoint);
        _project?.RemoveFocusPoint(_tileSetViewModel.Descriptor, focusPoint.FocusPoint);
        await InvalidateImage();
    }

    [RelayCommand]
    private void SwitchAllFocusPointsSelection()
    {
        AreAllFocusPointsSelected = !AreAllFocusPointsSelected;
        foreach (var focusPoint in FocusPoints)
        {
            focusPoint.IsSelected = AreAllFocusPointsSelected;
        }
    }

    [RelayCommand]
    private void SwitchFocusPointSelection(FocusPointViewModel focusPoint)
    {
        focusPoint.IsSelected = !focusPoint.IsSelected;
        AreAllFocusPointsSelected = false;
    }

    [RelayCommand]
    private async Task RemoveSelectedFocusPoints()
    {
        var selectedFocusPoints = FocusPoints.Where(fp => fp.IsSelected).ToList();
        foreach (var focusPoint in selectedFocusPoints)
        {
            _ = FocusPoints?.Remove(focusPoint);
            _project?.RemoveFocusPoint(_tileSetViewModel.Descriptor, focusPoint.FocusPoint);
        }
        AreAllFocusPointsSelected = false;
        await InvalidateImage();
    }

    [RelayCommand]
    private async Task RemoveAllFocusPoints()
    {
        foreach (var focusPoint in FocusPoints)
        {
            _project?.RemoveFocusPoint(_tileSetViewModel.Descriptor, focusPoint.FocusPoint);
        }
        FocusPoints.Clear();
        AreAllFocusPointsSelected = false;
        await InvalidateImage();
    }

    private async Task InvalidateImage()
    {
        if (_tileSetViewModel.RoiVM.Parent.ActiveDevice is VirtualDeviceViewModel deviceVm)
        {
            await deviceVm.ImagingPipeline.Invalidate();
        }
    }
}
