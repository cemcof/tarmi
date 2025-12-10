using System.Collections.ObjectModel;
using Tarmi.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tarmi.App.ViewModels.ROIs;

public partial class ImageSelectionDialogViewModel : ObservableObject, IDialogViewModel
{
    public ImageSelectionDialogViewModel(RoiChildVM bindingImage, IEnumerable<RoiChildVM> images)
    {
        Images = new(images);
        BindingImage = bindingImage;
    }

    public ObservableCollection<RoiChildVM> Images { get; }

    public RoiChildVM BindingImage { get; }

    public RoiChildVM? SelectedImage { get; set; }

    public bool IsInitialized => true;

    [RelayCommand]
    private void Select(RoiChildVM image)
    {
        SelectedImage = image;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
    public Task DeInitialize() => Task.CompletedTask;
    public Task Initialize() => Task.CompletedTask;

    public event EventHandler<bool>? CloseRequested;
}
