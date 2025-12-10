using System.Reactive.Disposables;

namespace System.Threading;

public static class ReaderWriterLockExtensions
{
    public static IDisposable TakeWriterLock(this ReaderWriterLockSlim readerWriterLock)
    {
        ArgumentNullException.ThrowIfNull(readerWriterLock);

        readerWriterLock.EnterWriteLock();
        return Disposable.Create(() =>
        {
            try
            {
                readerWriterLock.ExitWriteLock();
            }
            catch (ObjectDisposedException)
            {
                // should not happen but do not fail
                // in case the semaphore is being already disposed
            }
        });
    }

    public static IDisposable TakeReaderLock(this ReaderWriterLockSlim readerWriterLock)
    {
        ArgumentNullException.ThrowIfNull(readerWriterLock);

        readerWriterLock.EnterReadLock();
        return Disposable.Create(() =>
        {
            try
            {
                readerWriterLock.ExitReadLock();
            }
            catch (ObjectDisposedException)
            {
                // should not happen but do not fail
                // in case the semaphore is being already disposed
            }
        });
    }
}
