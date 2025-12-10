using System.Reactive.Disposables;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tarmi.App.WPF.ViewModels;

public abstract partial class ApplicationModeViewModelBase : ObservableObject, IDisposable, IApplicationModeViewModel
{
    protected readonly CompositeDisposable _disposables = [];
    private bool _disposedValue;
    private bool _isInitialized;

    public bool IsInitialized
    {
        get => _isInitialized;
        private set
        {
            _isInitialized = value;
            OnPropertyChanged();
        }
    }

    public async Task DeInitialize(ApplicationMode nextMode)
    {
        if (!IsInitialized)
        {
            return;
        }

        await DeInitializeCoreAsync(nextMode);
        IsInitialized = false;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            _disposables.Dispose();

            try
            {
                DisposeCore();
            }
            finally
            {
                _disposedValue = true;
            }
        }
        GC.SuppressFinalize(this);
    }

    public async Task Initialize(ApplicationMode prevMode)
    {
        if (IsInitialized)
        {
            return;
        }

        await InitializeCoreAsync(prevMode);
        IsInitialized = true;
    }

    protected virtual Task DeInitializeCoreAsync(ApplicationMode nextMode) => Task.CompletedTask;

    protected virtual void DisposeCore()
    {
    }

    protected virtual Task InitializeCoreAsync(ApplicationMode prevMode) => Task.CompletedTask;
}
