using Tarmi.Imaging.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using Tarmi.App.ViewModels.ROIs;
using Tarmi.App.Controls;

namespace Tarmi.App.ViewModels;

public partial class ImageStackViewerViewModel : ObservableObject
{
    private readonly RoiControlViewModel _roiControlViewModel;

    [ObservableProperty]
    private ImageWithMetadata? _imageWithMetadata;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderActive))]
    [NotifyPropertyChangedFor(nameof(MaxImageIndex))]
    private StackedImageChildVM? _activeZStack;

    [ObservableProperty]
    private int _selectedImageIndex;

    public int MaxImageIndex => ActiveZStack?.Count - 1 ?? 0;
    public bool SliderActive => ActiveZStack is not null;

    public ImageStackViewerViewModel(RoiControlViewModel roiControlViewModel)
    {
        _roiControlViewModel = roiControlViewModel;        
    }

    partial void OnImageWithMetadataChanged(ImageWithMetadata? value)
    {
        UpdateActiveZStack();
    }

    private void UpdateActiveZStack()
    {
        IEnumerable<StackedImageChildVM> stackVMs = _roiControlViewModel.SelectedRoi?.RoiChildVMs.OfType<StackedImageChildVM>() ?? [];
        ActiveZStack = stackVMs.FirstOrDefault(stackVM => stackVM.SelectionType.IsOneOf(ImageSelection.Primary, ImageSelection.Overlay));
    }

    partial void OnSelectedImageIndexChanged(int value)
    {
        if (ActiveZStack is not null && value != ActiveZStack.Index) 
        {
            ActiveZStack.Index = value;
            SelectedImageIndex = ActiveZStack.Index;
        }
    }
}
