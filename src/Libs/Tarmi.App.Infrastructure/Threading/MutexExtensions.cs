using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Tarmi.App.Infrastructure.Threading;

public static class MutexExtensions
{
    public static async Task<IDisposable> UseOnceAsync(this Mutex mutex, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutex);

        await mutex.WaitAsync(cancellationToken);
        return Disposable.Create(() =>
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ObjectDisposedException)
            {
                // should not happen but do not fail
                // in case the semaphore is being already disposed
            }
        });
    }

    public static IDisposable UseOnce(this Mutex mutex)
    {
        ArgumentNullException.ThrowIfNull(mutex);

        _ = mutex.WaitOne();
        return Disposable.Create(() =>
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (ObjectDisposedException)
            {
                // should not happen but do not fail
                // in case the semaphore is being already disposed
            }
        });
    }
}
