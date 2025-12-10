using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;

namespace Tarmi.App.Infrastructure;

public class NonblockingBufferedSubject<T> : ISubject<T>, IDisposable
{
    private bool _isDisposed;
    private readonly IObservable<T> _observable;
    private readonly CancellationTokenSource _readerCancellation = new();
    private readonly Channel<T> _channel;

    public NonblockingBufferedSubject(int bufferSize, BoundedChannelFullMode fullMode = BoundedChannelFullMode.DropOldest, Action<T>? itemDropped = null, CancellationToken cancellationToken = default)
    {
        _channel = Channel.CreateBounded<T>(NonblockingBufferedSubject<T>.GetOptions(bufferSize, fullMode), itemDropped);
        _observable = _channel.Reader.ReadAllAsync(CancellationTokenSource.CreateLinkedTokenSource(_readerCancellation.Token, cancellationToken).Token)
            .ToObservable()
            .Publish()
            .RefCount();
    }

    public IDisposable Subscribe(IObserver<T> observer) => _observable.Subscribe(observer);

    public virtual void OnNext(T value) => _ = _channel.Writer.TryWrite(value);

    public void OnCompleted() => _ = _channel.Writer.TryComplete();

    public void OnError(Exception error) => _ = _channel.Writer.TryComplete(error);

    private static BoundedChannelOptions GetOptions(int bufferSize, BoundedChannelFullMode fullMode)
    {
        return new BoundedChannelOptions(bufferSize)
        {
            AllowSynchronousContinuations = false,
            Capacity = bufferSize,
            SingleReader = false,
            SingleWriter = true,
            FullMode = fullMode
        };
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                OnCompleted();
                _readerCancellation.Cancel();
                _readerCancellation.Dispose();
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
