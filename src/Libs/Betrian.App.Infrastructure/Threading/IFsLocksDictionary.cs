namespace Betrian.App.Infrastructure.Threading;

public interface IFsLocksDictionary
{
    ReaderWriterLockSlim GetLockForPath(string path);
}
