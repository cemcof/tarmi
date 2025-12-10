using System.Windows;
using Tarmi.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tarmi.App.ViewModels;

public partial class ConfirmationDialogViewModel : ViewModelBase
{
    private Window? _window;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AcceptText { get; set; } = "OK";

    [ObservableProperty]
    public partial string RejectText { get; set; } = "Cancel";

    [ObservableProperty]
    public partial bool IsAccepted { get; private set; }

    public ConfirmationDialogViewModel()
    {
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    private void Accept()
    {
        IsAccepted = true;
        if (_window is not null)
        {
            _window.DialogResult = true;
            _window.Close();
        }
    }

    [RelayCommand]
    private void Reject()
    {
        IsAccepted = false;
        if (_window is not null)
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }
}

