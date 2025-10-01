using System.Collections.Concurrent;

namespace Betrian.App.Infrastructure.Threading;

internal class FsLocksDictionary : IFsLocksDictionary
{
    private readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _locks = new(StringComparer.InvariantCultureIgnoreCase);

    public ReaderWriterLockSlim GetLockForPath(string path)
    {
        return _locks.GetOrAdd(path, p => new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
    }
}
