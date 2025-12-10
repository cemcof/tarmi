using System.Windows;

namespace Tarmi.WPF;

public class DarkDialog<TViewModel> : DarkWindow<TViewModel> where TViewModel : class, IDialogViewModel
{
    public DarkDialog() : base()
    {
        RegisterEvents();
    }

    public DarkDialog(TViewModel viewModel) : base(viewModel)
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        DataContextChanged += DarkDialog_DataContextChanged;
        Closed += DarkDialog_Closed;
        if (ViewModel is not null)
        {
            ViewModel.CloseRequested += ViewModel_CloseRequested;
        }
    }

    private void UnregisterEvents()
    {
        DataContextChanged -= DarkDialog_DataContextChanged;
        Closed -= DarkDialog_Closed;
        if (ViewModel is not null)
        {
            ViewModel.CloseRequested -= ViewModel_CloseRequested;
        }
    }

    private void DarkDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is IDialogViewModel oldViewModel)
        {
            oldViewModel.CloseRequested -= ViewModel_CloseRequested;
        }
        if (e.NewValue is IDialogViewModel newViewModel)
        {
            newViewModel.CloseRequested += ViewModel_CloseRequested;
        }
    }

    private void ViewModel_CloseRequested(object? sender, bool dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    private void DarkDialog_Closed(object? sender, EventArgs e)
    {
        UnregisterEvents();
    }
}
