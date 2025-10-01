using System.Reactive.Disposables;

namespace System.Threading;

public static class SemaphoreExtensions
{
    public static IDisposable UseOnce(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        return UseOnceAsync(semaphore, cancellationToken).SyncResult();
    }

    public static async Task<IDisposable> UseOnceAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(semaphore);

        await semaphore.WaitAsync(cancellationToken);
        return Disposable.Create(() =>
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // should not happen but do not fail
                // in case the semaphore is being already disposed
            }
        });
    }
}
