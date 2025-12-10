using System.Windows;
using System.Reactive.Disposables;
using Tarmi.App.ViewModels;
using Tarmi.App.Views;
using Tarmi.App.Views.Projects;
using Tarmi.Projects;
using Microsoft.Extensions.Logging;
using UnitsNet;
using System.Reactive.Subjects;
using CommunityToolkit.Diagnostics;
using System.Reactive.Linq;
using Tarmi.App.Infrastructure;
using Tarmi.App.ViewModels.ROIs;

namespace Tarmi.App.Services.Application;

public interface IWindowService : IService
{
    IObservable<bool> IsBusy { get; }
    IObservable<string> BusyMessage { get; }

    ProjectDescriptor? ShowProjectSelectionDialog();
    ProjectDescriptor? ShowProjectCreationDialog();
    Task ShowDeterminateWaitingDialogAsync(string dialogTitle, Func<IProgress<(string, Ratio)>, Task> progressTask, Action? terminate = null, string terminateControlName = "Cancel");
    Task ShowIndeterminateWaitingDialogAsync(string dialogTitle, Func<IProgress<string>, Task> progressTask, Action? terminate = null, string terminateControlName = "Cancel");
    IDisposable ShowBusyMessage(string message);
    bool ShowConfirmationDialog(string title, string message, string acceptText = "OK", string rejectText = "Cancel");
    RoiChildVM? ShowImageSelectionDialog(RoiChildVM imageToBind, IEnumerable<RoiChildVM> luminescenceImages);
}

internal class WindowService : IWindowService
{
    private readonly object _lock = new();
    private volatile int _operationCount;
    private readonly BehaviorSubject<bool> _isBusySubject = new(false);
    private readonly BehaviorSubject<string> _busyMessageSubject = new(string.Empty);
    private readonly ILogger _logger;

    public IObservable<bool> IsBusy => _isBusySubject.DistinctUntilChanged().AsObservable();
    public IObservable<string> BusyMessage => _busyMessageSubject.AsObservable();

    public WindowService(ILogger<IWindowService> logger)
    {
        _logger = logger;
    }

    public ProjectDescriptor? ShowProjectCreationDialog()
    {
        var window = new CreateNewProjectDialog();
        var result = window.ShowDialog() ?? false;
        return result ? window.ViewModel?.CreatedProject : null;
    }

    public ProjectDescriptor? ShowProjectSelectionDialog()
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        var window = new ProjectManagerDialog()
        {
            Owner = mainWindow
        };
        mainWindow.Hide();
        var result = window.ShowDialog() ?? false;
        mainWindow.Show();
        return result ? window.ViewModel?.SelectedProject : null;
    }

    public bool ShowConfirmationDialog(string title, string message, string acceptText = "OK", string rejectText = "Cancel")
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;

        var viewModel = new ConfirmationDialogViewModel
        {
            Title = title,
            Text = message,
            AcceptText = acceptText,
            RejectText = rejectText
        };

        var confirmationDialog = new ConfirmationDialog(viewModel)
        {
            Owner = mainWindow
        };
        viewModel.SetWindow(confirmationDialog);

        using var isBusy = SignalIsBusy();

        _ = confirmationDialog.ShowDialog();
        return viewModel.IsAccepted;
    }

    public async Task ShowDeterminateWaitingDialogAsync(string dialogTitle, Func<IProgress<(string, Ratio)>, Task> progressTask, Action? terminate, string terminateControlName)
    {
        await ShowWaitingDialogAsync<string>(
            dialogTitle,
            false,
            async viewModel =>
            {
                var progress = new Progress<(string Text, Ratio Progress)>(state =>
                {
                    viewModel.Progress = state.Progress;
                    viewModel.Text = state.Text;
                });
                await progressTask(progress);
            },
            terminate,
            terminateControlName
        );
    }

    public Task ShowIndeterminateWaitingDialogAsync(string dialogTitle, Func<IProgress<string>, Task> progressTask, Action? terminate, string terminateControlName)
    {
        return ShowWaitingDialogAsync<string>(
            dialogTitle,
            true,
            async viewModel =>
            {
                var progress = new Progress<string>(state => viewModel.Text = state);
                await progressTask(progress);
            },
            terminate,
            terminateControlName
        );
    }

    private async Task ShowWaitingDialogAsync<T>(string dialogTitle, bool isIndeterminate, Func<WaitingDialogViewModel, Task> task, Action? terminate, string terminateControlName)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;

        var viewModel = new WaitingDialogViewModel(terminateControlName)
        {
            Title = dialogTitle,
            IsIndeterminate = isIndeterminate,
            TerminateAction = terminate
        };

        var waitingDialog = new WaitingDialog(viewModel)
        {
            Owner = mainWindow
        };

        using var isBusy = SignalIsBusy();

        waitingDialog.Show();

        try
        {
            _logger.LogInformation("{TitleName} operation started.", dialogTitle);
            await task.Invoke(viewModel);
            _logger.LogInformation("{TitleName} operation finished.", dialogTitle);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{TitleName} operation cancelled.", dialogTitle);
        }
        finally
        {
            waitingDialog.Close();
        }
    }

    public IDisposable ShowBusyMessage(string message)
    {
        _busyMessageSubject.OnNext(message);
        return SignalIsBusy();
    }

    private IDisposable SignalIsBusy()
    {
        lock (_lock)
        {
            _operationCount++;
            _isBusySubject.OnNext(true);
        }

        return Disposable.Create(() =>
        {
            lock (_lock)
            {
                _operationCount--;
                Guard.IsGreaterThanOrEqualTo(_operationCount, 0);
                if (_operationCount == 0)
                {
                    _isBusySubject.OnNext(false);
                }
            }
        });
    }

    public RoiChildVM? ShowImageSelectionDialog(RoiChildVM imageToBind, IEnumerable<RoiChildVM> luminescenceImages)
    {
        var viewModel = new ImageSelectionDialogViewModel(imageToBind, luminescenceImages);
        var dialog = new ImageSelectionDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        using var busy = SignalIsBusy();
        var result = dialog.ShowDialog() ?? false;
        return result ? viewModel.SelectedImage : null;
    }
}
