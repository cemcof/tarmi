using Tarmi.WPF;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnitsNet;

namespace Tarmi.App.ViewModels;

public partial class WaitingDialogViewModel : ViewModelBase
{
    public string TerminateControlName { get; private set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private Ratio _progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTerminatable))]
    private Action? _terminateAction;

    public bool IsTerminatable => TerminateAction is not null;

    [ObservableProperty]
    private bool _isIndeterminate;

    [RelayCommand]
    private void Terminate() => TerminateAction?.Invoke();

    public WaitingDialogViewModel(string terminateControlName = "Cancel")
    {
        TerminateControlName = terminateControlName;
    }
}
