using System.Reactive.Disposables;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Tarmi.WPF;

public abstract partial class ViewModelBase : ObservableObject, IDisposable, IViewModel
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

    public async Task DeInitialize()
    {
        if (!IsInitialized)
        {
            return;
        }

        await DeInitializeCoreAsync();
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

    public async Task Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        await InitializeCoreAsync();
        IsInitialized = true;
    }

    protected virtual Task DeInitializeCoreAsync() => Task.CompletedTask;

    protected virtual void DisposeCore()
    { }

    protected virtual Task InitializeCoreAsync() => Task.CompletedTask;
}
