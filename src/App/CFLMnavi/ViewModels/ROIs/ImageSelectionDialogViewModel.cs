using System.Collections.ObjectModel;
using Betrian.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Betrian.CflmNavi.App.ViewModels.ROIs;

public partial class ImageSelectionDialogViewModel : ObservableObject, IDialogViewModel
{
    public ImageSelectionDialogViewModel(ImageChildVM bindingImage, IEnumerable<ImageChildVM> images)
    {
        Images = new(images);
        BindingImage = bindingImage;
    }

    public ObservableCollection<ImageChildVM> Images { get; }

    public ImageChildVM BindingImage { get; }

    public ImageChildVM? SelectedImage { get; set; }

    public bool IsInitialized => true;

    [RelayCommand]
    private void Select(ImageChildVM image)
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
