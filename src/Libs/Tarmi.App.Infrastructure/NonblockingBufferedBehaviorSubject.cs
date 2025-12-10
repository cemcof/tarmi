using System.Threading.Channels;

namespace Tarmi.App.Infrastructure;

public class NonblockingBufferedBehaviorSubject<T> : NonblockingBufferedSubject<T>
{
    public T Value { get; private set; }

    public NonblockingBufferedBehaviorSubject(int bufferSize, T defaultValue, BoundedChannelFullMode fullMode = BoundedChannelFullMode.DropOldest, Action<T>? itemDropped = null, CancellationToken cancellationToken = default)
        : base(bufferSize, fullMode, itemDropped, cancellationToken)
    {
        Value = defaultValue;
        OnNext(defaultValue);
    }

    public override void OnNext(T value)
    {
        Value = value;
        base.OnNext(value);
    }
}
