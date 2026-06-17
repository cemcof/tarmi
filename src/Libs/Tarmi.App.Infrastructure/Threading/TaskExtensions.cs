namespace System.Threading.Tasks;

public static class TaskExtensions
{
    public static void SyncResult(this Task task) => task.GetAwaiter().GetResult();
    public static T SyncResult<T>(this Task<T> task) => task.GetAwaiter().GetResult();

#pragma warning disable S5034 // "ValueTask" should be consumed correctly
    public static void SyncResult(this ValueTask task) => task.GetAwaiter().GetResult();
    public static T SyncResult<T>(this ValueTask<T> task) => task.GetAwaiter().GetResult();
#pragma warning restore S5034 // "ValueTask" should be consumed correctly
}
